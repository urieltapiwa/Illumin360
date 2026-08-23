using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace Illumin360.StudentWeb.Services;

/// <summary>The result of a self-registration attempt.</summary>
/// <param name="StatusCode">HTTP status to return to the caller.</param>
/// <param name="Code">Machine-readable outcome code.</param>
/// <param name="Message">Human-readable message.</param>
public sealed record RegistrationResult(int StatusCode, string Code, string Message);

/// <summary>Self-registration request payload from the sign-up form.</summary>
/// <param name="FirstName">Given name.</param>
/// <param name="LastName">Family name.</param>
/// <param name="Email">Email (also used as the username).</param>
/// <param name="Password">Chosen password (min 12 chars — realm policy).</param>
/// <param name="City">Home city.</param>
/// <param name="Field">Field of study (student).</param>
/// <param name="School">Institution (student).</param>
/// <param name="Role">Headline role (professional).</param>
/// <param name="Company">Company name (employer).</param>
public sealed record RegisterRequest(
    string? FirstName,
    string? LastName,
    string? Email,
    string? Password,
    string? City,
    string? Field,
    string? School,
    string? Role,
    string? Company);

/// <summary>
/// Provisions a self-registered user: creates the Keycloak identity (via a confidential service-account
/// client using the Keycloak Admin API), assigns the type-specific realm role, and creates the matching
/// domain profile by calling the domain service through the gateway with the service token (which carries
/// the <c>admin.write</c> role). Employers get an identity only (no employers service yet).
/// </summary>
/// <param name="httpFactory">HTTP client factory.</param>
/// <param name="config">Configuration (Registration section).</param>
public sealed class KeycloakRegistrar(IHttpClientFactory httpFactory, IConfiguration config)
{
    private const int MinPasswordLength = 12;

    private readonly IHttpClientFactory _httpFactory = httpFactory;
    private readonly IConfiguration _config = config;

    /// <summary>Registers a user of the given type (student/professional/employer).</summary>
    /// <param name="type">User type from the route.</param>
    /// <param name="req">The sign-up payload.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The outcome to surface to the caller.</returns>
    public async Task<RegistrationResult> RegisterAsync(string type, RegisterRequest req, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(req);

        var role = type switch
        {
            "student" => "client.student",
            "professional" => "client.user",
            "employer" => "client.employer",
            "business" => "client.user",
            _ => null,
        };
        if (role is null)
        {
            return new RegistrationResult(StatusCodes.Status404NotFound, "unknown_type", "Unknown registration type.");
        }

        // --- validation ---
        if (string.IsNullOrWhiteSpace(req.FirstName) || string.IsNullOrWhiteSpace(req.LastName))
        {
            return new RegistrationResult(StatusCodes.Status400BadRequest, "name_required", "First and last name are required.");
        }

        if (string.IsNullOrWhiteSpace(req.Email) || !req.Email.Contains('@', StringComparison.Ordinal))
        {
            return new RegistrationResult(StatusCodes.Status400BadRequest, "email_invalid", "A valid email is required.");
        }

        if (string.IsNullOrWhiteSpace(req.Password) || req.Password.Length < MinPasswordLength)
        {
            return new RegistrationResult(StatusCodes.Status400BadRequest, "password_weak", $"Password must be at least {MinPasswordLength} characters.");
        }

        if (string.IsNullOrWhiteSpace(req.City))
        {
            return new RegistrationResult(StatusCodes.Status400BadRequest, "city_required", "City is required.");
        }

        var kcBase = _config["Registration:KeycloakBaseUrl"] ?? "http://keycloak:8080";
        var realm = _config["Registration:Realm"] ?? "illumin360";
        var clientId = _config["Registration:ClientId"] ?? "illumin360-registration";
        var clientSecret = _config["Registration:ClientSecret"] ?? string.Empty;

        var http = _httpFactory.CreateClient();

        // --- 1. service token (client_credentials) ---
        var token = await GetServiceTokenAsync(http, kcBase, realm, clientId, clientSecret, ct).ConfigureAwait(false);
        if (token is null)
        {
            return new RegistrationResult(StatusCodes.Status502BadGateway, "idp_unavailable", "Identity provider unavailable.");
        }

        http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var email = req.Email.Trim();

        // --- 2. duplicate check ---
        using (var existing = await http.GetAsync($"{kcBase}/admin/realms/{realm}/users?email={Uri.EscapeDataString(email)}&exact=true", ct).ConfigureAwait(false))
        {
            if (existing.IsSuccessStatusCode)
            {
                var arr = await existing.Content.ReadFromJsonAsync<JsonElement[]>(ct).ConfigureAwait(false);
                if (arr is { Length: > 0 })
                {
                    return new RegistrationResult(StatusCodes.Status409Conflict, "email_taken", "An account with this email already exists.");
                }
            }
        }

        // --- 3. create the Keycloak user ---
        var createBody = new
        {
            username = email,
            email,
            firstName = req.FirstName!.Trim(),
            lastName = req.LastName!.Trim(),
            enabled = true,
            emailVerified = false, // must verify via email before the browser login flow completes
            requiredActions = new[] { "VERIFY_EMAIL" },
            credentials = new[] { new { type = "password", value = req.Password, temporary = false } },
        };
        using (var create = await http.PostAsJsonAsync($"{kcBase}/admin/realms/{realm}/users", createBody, ct).ConfigureAwait(false))
        {
            if (create.StatusCode == System.Net.HttpStatusCode.Conflict)
            {
                return new RegistrationResult(StatusCodes.Status409Conflict, "email_taken", "An account with this email already exists.");
            }

            if (!create.IsSuccessStatusCode)
            {
                return new RegistrationResult(StatusCodes.Status400BadRequest, "create_failed", "Could not create the account.");
            }
        }

        // --- 4. fetch the new user id + assign the type role ---
        var users = await http.GetFromJsonAsync<JsonElement[]>($"{kcBase}/admin/realms/{realm}/users?email={Uri.EscapeDataString(email)}&exact=true", ct).ConfigureAwait(false);
        if (users is not { Length: > 0 })
        {
            return new RegistrationResult(StatusCodes.Status500InternalServerError, "provision_failed", "Account created but could not be finalised.");
        }

        var userId = users[0].GetProperty("id").GetString()!;

        var roleRep = await http.GetFromJsonAsync<JsonElement>($"{kcBase}/admin/realms/{realm}/roles/{role}", ct).ConfigureAwait(false);
        var mapping = new[] { new { id = roleRep.GetProperty("id").GetString(), name = roleRep.GetProperty("name").GetString() } };
        using (var assign = await http.PostAsJsonAsync($"{kcBase}/admin/realms/{realm}/users/{userId}/role-mappings/realm", mapping, ct).ConfigureAwait(false))
        {
            if (!assign.IsSuccessStatusCode)
            {
                return new RegistrationResult(StatusCodes.Status500InternalServerError, "role_failed", "Account created but role assignment failed.");
            }
        }

        // --- 5. create the domain profile; compensate (delete the identity) if it fails → atomic outcome ---
        var profileOk = await CreateDomainProfileAsync(http, token, type, req, ct).ConfigureAwait(false);
        if (!profileOk)
        {
            (await http.DeleteAsync($"{kcBase}/admin/realms/{realm}/users/{userId}", ct).ConfigureAwait(false)).Dispose();
            return new RegistrationResult(StatusCodes.Status502BadGateway, "profile_failed", "Could not create your profile — please try again.");
        }

        // --- 6. send the verification email (Keycloak → SMTP) ---
        (await http.PutAsync($"{kcBase}/admin/realms/{realm}/users/{userId}/send-verify-email", content: null, ct).ConfigureAwait(false)).Dispose();

        return new RegistrationResult(StatusCodes.Status201Created, "registered", "Account created. Check your email to verify your address, then sign in.");
    }

    private static async Task<string?> GetServiceTokenAsync(HttpClient http, string kcBase, string realm, string clientId, string clientSecret, CancellationToken ct)
    {
        using var resp = await http.PostAsync(
            $"{kcBase}/realms/{realm}/protocol/openid-connect/token",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"] = "client_credentials",
                ["client_id"] = clientId,
                ["client_secret"] = clientSecret,
            }),
            ct).ConfigureAwait(false);

        if (!resp.IsSuccessStatusCode)
        {
            return null;
        }

        var payload = await resp.Content.ReadFromJsonAsync<JsonElement>(ct).ConfigureAwait(false);
        return payload.TryGetProperty("access_token", out var t) ? t.GetString() : null;
    }

    private async Task<bool> CreateDomainProfileAsync(HttpClient http, string token, string type, RegisterRequest req, CancellationToken ct)
    {
        // Employer: identity + client.employer role only — no employers service to profile into yet.
        if (type == "employer" || type == "business")
        {
            return true;
        }

        var gateway = _config["Registration:GatewayBaseUrl"] ?? "http://gateway:8080";
        http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        try
        {
            var (path, body) = type == "student"
                ? ($"{gateway}/api/students", (object)new
                {
                    firstName = req.FirstName!.Trim(),
                    lastName = req.LastName!.Trim(),
                    field = string.IsNullOrWhiteSpace(req.Field) ? "General" : req.Field.Trim(),
                    school = req.School?.Trim() ?? string.Empty,
                    year = string.Empty,
                    graduating = string.Empty,
                    program = "Self-registered",
                    city = req.City!.Trim(),
                })
                : ($"{gateway}/api/professionals", new
                {
                    firstName = req.FirstName!.Trim(),
                    lastName = req.LastName!.Trim(),
                    role = string.IsNullOrWhiteSpace(req.Role) ? "Professional" : req.Role.Trim(),
                    city = req.City!.Trim(),
                    nationality = string.Empty,
                    availability = "Open to opportunities",
                    headline = string.Empty,
                });

            using var resp = await http.PostAsJsonAsync(path, body, ct).ConfigureAwait(false);
            return resp.IsSuccessStatusCode;
        }
        catch (HttpRequestException)
        {
            return false;
        }
    }
}

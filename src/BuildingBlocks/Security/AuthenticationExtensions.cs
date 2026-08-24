using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace Illumin360.Security;

/// <summary>
/// Shared service-layer authentication/authorization wiring. Validates Keycloak-issued JWT bearer
/// access tokens (relayed by the portals) and projects the realm roles Keycloak emits under
/// <c>realm_access.roles</c> into ASP.NET <see cref="ClaimTypes.Role"/> claims so role policies work
/// (charter Part 7: authZ enforced at the service layer).
/// <para>
/// Each MVC portal authenticates against its own Keycloak realm (admin/student/professional/business/
/// employer/support), so a relayed access token can carry any of several issuers. The gateway also fans
/// a single portal's calls out to shared services, so every service must trust every portal realm. This
/// registers one <see cref="JwtBearerDefaults.AuthenticationScheme"/> handler per realm and a policy
/// scheme that forwards each request to the handler matching the token's <c>iss</c>.
/// </para>
/// </summary>
public static class AuthenticationExtensions
{
    /// <summary>The authorization policy name for admin-tier access (any <c>admin.*</c> realm role).</summary>
    public const string AdminPolicy = "admin";

    /// <summary>The authorization policy name for mutating admin access (<c>admin.write</c>/<c>admin.superuser</c>).</summary>
    public const string AdminWritePolicy = "admin.write";

    /// <summary>Policy for a signed-in professional acting on their own data (role <c>client.user</c>; admins allowed).</summary>
    public const string ProfessionalPolicy = "professional";

    /// <summary>Policy for a signed-in student acting on their own data (role <c>client.user</c>; admins allowed).</summary>
    public const string StudentPolicy = "student";

    /// <summary>Policy for an employer managing their own company profile / team (role <c>client.employer</c>; admins allowed).</summary>
    public const string EmployerPolicy = "employer";

    /// <summary>Policy for support agents working the ticket queue (roles <c>support.*</c>; admins allowed).</summary>
    public const string SupportPolicy = "support";

    /// <summary>The composite scheme that selects a per-realm handler by the token issuer.</summary>
    public const string MultiRealmScheme = "kc-multirealm";

    private const string DefaultBackChannelBase = "http://keycloak:8080";
    private const string DefaultFrontChannelBase = "http://localhost:8080";

    // Every portal realm plus the legacy shared realm (old SPA/BFF), so existing tokens keep validating.
    private static readonly string[] DefaultRealms =
        ["admin", "student", "professional", "business", "employer", "support", "illumin360"];

    private static readonly string[] AdminRoles = ["admin.read", "admin.write", "admin.superuser"];
    private static readonly string[] AdminWriteRoles = ["admin.write", "admin.superuser"];

    // Signed-in end users acting on their own data (professional / student portals). Admins are allowed through.
    private static readonly string[] ClientUserRoles = ["client.user", "admin.write", "admin.superuser"];

    // An employer managing their own company profile / team. Admins are allowed through.
    private static readonly string[] EmployerRoles = ["client.employer", "admin.write", "admin.superuser"];

    // Support agents working the ticket queue. Admins are allowed through.
    private static readonly string[] SupportRoles = ["support.l1", "support.l2", "support.lead", "admin.read", "admin.write", "admin.superuser"];

    /// <summary>
    /// Adds multi-realm JWT bearer authentication (against Keycloak) and the admin authorization policies.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">
    /// App configuration. Reads <c>Oidc:BackChannelBase</c> (in-cluster host for JWKS/discovery, default
    /// <c>http://keycloak:8080</c>), <c>Oidc:FrontChannelBase</c> (browser-facing host, default
    /// <c>http://localhost:8080</c>) and <c>Oidc:Realms</c> (the realm names to trust; defaults to the six
    /// portal realms plus the legacy <c>illumin360</c> realm). Both hosts are accepted as valid issuers per
    /// realm, because Keycloak stamps <c>iss</c> with whichever host the user authenticated against.
    /// </param>
    /// <returns>The same collection for chaining.</returns>
    public static IServiceCollection AddIllumin360Auth(
        this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var backBase = (configuration["Oidc:BackChannelBase"] ?? DefaultBackChannelBase).TrimEnd('/');
        var frontBase = (configuration["Oidc:FrontChannelBase"] ?? DefaultFrontChannelBase).TrimEnd('/');
        var realms = configuration.GetSection("Oidc:Realms").Get<string[]>() ?? DefaultRealms;
        if (realms.Length == 0)
        {
            realms = DefaultRealms;
        }

        // Map each realm's possible issuers (back- and front-channel) to that realm's handler scheme, so the
        // policy scheme can forward by `iss` without re-parsing hosts on every request.
        var issuerToScheme = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var realm in realms)
        {
            var scheme = SchemeFor(realm);
            issuerToScheme[$"{backBase}/realms/{realm}"] = scheme;
            issuerToScheme[$"{frontBase}/realms/{realm}"] = scheme;
        }

        var authBuilder = services.AddAuthentication(MultiRealmScheme)
            .AddPolicyScheme(MultiRealmScheme, MultiRealmScheme, options =>
            {
                options.ForwardDefaultSelector = context =>
                {
                    var issuer = ReadIssuer(context.Request.Headers.Authorization);
                    if (issuer is not null && issuerToScheme.TryGetValue(issuer, out var scheme))
                    {
                        return scheme;
                    }

                    // Unknown/no issuer: fall through to the first realm's handler, which will reject it.
                    return SchemeFor(realms[0]);
                };
            });

        foreach (var realm in realms)
        {
            var authority = $"{backBase}/realms/{realm}";
            var frontChannel = $"{frontBase}/realms/{realm}";
            authBuilder.AddJwtBearer(SchemeFor(realm), options =>
            {
                // JWKS + discovery come from the back-channel authority (reachable inside the cluster).
                options.Authority = authority;
                options.MapInboundClaims = false;

                // Keycloak runs over plain HTTP in the local/dev cluster; metadata is fetched in-network.
                options.RequireHttpsMetadata = false;

                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,

                    // The access token's `iss` is the front-channel host the user authenticated against,
                    // while discovery/JWKS is fetched from the back-channel host.
                    ValidIssuers = [authority, frontChannel],

                    // No audience mapper is configured in the realms yet, so the access token's `aud`
                    // does not carry the service name. Revisit once a client-scope audience mapper exists.
                    ValidateAudience = false,

                    ValidateLifetime = true,
                    RoleClaimType = ClaimTypes.Role,
                    NameClaimType = "preferred_username",
                };

                options.Events = new JwtBearerEvents
                {
                    OnTokenValidated = static context =>
                    {
                        ProjectRealmRoles(context.Principal);
                        return Task.CompletedTask;
                    },
                };
            });
        }

        services.AddAuthorization(options =>
        {
            options.AddPolicy(AdminPolicy, policy => policy.RequireRole(AdminRoles));
            options.AddPolicy(AdminWritePolicy, policy => policy.RequireRole(AdminWriteRoles));
            options.AddPolicy(ProfessionalPolicy, policy => policy.RequireRole(ClientUserRoles));
            options.AddPolicy(StudentPolicy, policy => policy.RequireRole(ClientUserRoles));
            options.AddPolicy(EmployerPolicy, policy => policy.RequireRole(EmployerRoles));
            options.AddPolicy(SupportPolicy, policy => policy.RequireRole(SupportRoles));
        });

        return services;
    }

    /// <summary>The per-realm JWT handler scheme name (e.g. <c>kc-admin</c>).</summary>
    /// <param name="realm">The Keycloak realm name.</param>
    /// <returns>The handler scheme name for that realm.</returns>
    private static string SchemeFor(string realm) => $"kc-{realm}";

    /// <summary>
    /// Reads the (unvalidated) <c>iss</c> claim from a bearer token so the request can be routed to the
    /// realm handler that will actually validate it. Signature verification happens in that handler.
    /// </summary>
    /// <param name="authorizationHeader">The raw <c>Authorization</c> header value.</param>
    /// <returns>The issuer string, or <see langword="null"/> if absent/unparseable.</returns>
    private static string? ReadIssuer(string? authorizationHeader)
    {
        if (string.IsNullOrEmpty(authorizationHeader)
            || !authorizationHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var token = authorizationHeader["Bearer ".Length..].Trim();
        var parts = token.Split('.');
        if (parts.Length < 2)
        {
            return null;
        }

        try
        {
            var payload = Base64UrlDecode(parts[1]);
            using var document = JsonDocument.Parse(payload);
            return document.RootElement.TryGetProperty("iss", out var iss) ? iss.GetString() : null;
        }
        catch (Exception ex) when (ex is FormatException or JsonException)
        {
            return null;
        }
    }

    /// <summary>Decodes a base64url segment (JWT payload) into UTF-8 bytes.</summary>
    /// <param name="segment">The base64url-encoded segment.</param>
    /// <returns>The decoded bytes.</returns>
    private static byte[] Base64UrlDecode(string segment)
    {
        var s = segment.Replace('-', '+').Replace('_', '/');
        s = (s.Length % 4) switch
        {
            2 => s + "==",
            3 => s + "=",
            _ => s,
        };
        return Convert.FromBase64String(s);
    }

    /// <summary>
    /// Projects Keycloak realm roles (emitted as a JSON object under the <c>realm_access</c> claim) into
    /// individual <see cref="ClaimTypes.Role"/> claims. ASP.NET's JWT handler does not flatten these
    /// automatically, so without this projection <c>RequireRole</c>/<c>[Authorize(Roles=…)]</c> never match.
    /// </summary>
    /// <param name="principal">The validated principal to augment.</param>
    private static void ProjectRealmRoles(ClaimsPrincipal? principal)
    {
        if (principal?.Identity is not ClaimsIdentity identity)
        {
            return;
        }

        var realmAccess = principal.FindFirst("realm_access")?.Value;
        if (string.IsNullOrEmpty(realmAccess))
        {
            return;
        }

        using var document = JsonDocument.Parse(realmAccess);
        if (!document.RootElement.TryGetProperty("roles", out var roles)
            || roles.ValueKind != JsonValueKind.Array)
        {
            return;
        }

        foreach (var role in roles.EnumerateArray())
        {
            var name = role.GetString();
            if (!string.IsNullOrEmpty(name) && !identity.HasClaim(ClaimTypes.Role, name))
            {
                identity.AddClaim(new Claim(ClaimTypes.Role, name));
            }
        }
    }
}

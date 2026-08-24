using System.Net.Http.Json;
using Microsoft.AspNetCore.Authentication;

namespace Illumin360.EmployerWeb.Services;

// Live company profile returned by GET /api/employers/me (Employers service).
public sealed record EmployerProfile(string? Id, string? CompanyName, string? Industry, string? City, string? Website, string? About);

// Attaches the signed-in user's access token to gateway calls (relay). GET /me is currently
// anonymous, but sending the token keeps parity with the protected endpoints.
public sealed class TokenRelayHandler(IHttpContextAccessor accessor) : DelegatingHandler
{
    private readonly IHttpContextAccessor _accessor = accessor;

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var http = _accessor.HttpContext;
        if (http?.User.Identity?.IsAuthenticated == true)
        {
            var token = await http.GetTokenAsync("access_token").ConfigureAwait(false);
            if (!string.IsNullOrEmpty(token))
            {
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            }
        }

        return await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
    }
}

// Typed client for the Employers service via the gateway.
public sealed partial class EmployersApiClient(HttpClient http)
{
    private readonly HttpClient _http = http;

    public async Task<EmployerProfile?> GetProfileAsync(CancellationToken ct = default)
    {
        try
        {
            return await _http.GetFromJsonAsync<EmployerProfile>("/api/employers/me", ct).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or System.Text.Json.JsonException)
        {
            return null; // API unavailable — the view falls back to placeholder values.
        }
    }
}

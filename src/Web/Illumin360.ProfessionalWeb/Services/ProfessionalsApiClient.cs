using System.Net.Http.Json;
using Microsoft.AspNetCore.Authentication;

namespace Illumin360.ProfessionalWeb.Services;

// Live dashboard shape returned by GET /api/professionals/me (Professionals service).
public sealed record Persona(string? Name, string? Role, string? City, string? Nationality, string? Availability, string? Headline, int ProfileStrength, int Percentile, string? MemberSince);

public sealed record ProfessionalKpis(int ProfileViews, int ViewsDelta, int MatchOpportunities, int MatchDelta, int ActiveApplications, int ResponseRate, int AvgMatch, int Interviews);

public sealed record Match(string? Role, string? Company, string? City, string? Industry, int MatchScore, string? Type, string? Posted, string? Status);

public sealed record ProfessionalDashboard(Persona? Persona, ProfessionalKpis? Kpis, int[]? ViewsTrend, Match[]? Matches);

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

// Typed client for the Professionals service via the gateway.
public sealed partial class ProfessionalsApiClient(HttpClient http)
{
    private readonly HttpClient _http = http;

    public async Task<ProfessionalDashboard?> GetDashboardAsync(CancellationToken ct = default)
    {
        try
        {
            return await _http.GetFromJsonAsync<ProfessionalDashboard>("/api/professionals/me", ct).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or System.Text.Json.JsonException)
        {
            return null; // API unavailable — the view falls back to placeholder values.
        }
    }
}

using System.Net.Http.Json;
using Microsoft.AspNetCore.Authentication;

namespace Illumin360.BusinessWeb.Services;

// Live talent-marketplace insights from GET /api/admin/talent-insights (Admin service).
public sealed record TalentInsights(int TotalTalent, int TotalCompanies, int ActiveCompanies, int VerifiedEntities, int PendingReview, int[]? Mix);

// Relays the signed-in user's access token to the gateway.
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

// Typed client for the talent-insights summary via the gateway.
public sealed class InsightsApiClient(HttpClient http)
{
    private readonly HttpClient _http = http;

    public async Task<TalentInsights?> GetInsightsAsync(CancellationToken ct = default)
    {
        try
        {
            return await _http.GetFromJsonAsync<TalentInsights>("/api/admin/talent-insights", ct).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or System.Text.Json.JsonException)
        {
            return null; // API unavailable — the view falls back to placeholder values.
        }
    }
}

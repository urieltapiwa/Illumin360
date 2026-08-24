using System.Net.Http.Json;
using Microsoft.AspNetCore.Authentication;

namespace Illumin360.SupportWeb.Services;

// Live support-queue metrics from GET /api/admin/support-summary (Admin service).
public sealed record VolumePoint(string? Label, int Created, int Resolved);

public sealed record SupportSummary(int Open, int Assigned, int Resolved, int P1, int P2, int P3, int[]? PriorityMix, VolumePoint[]? Volume);

// Relays the signed-in agent's access token to the gateway.
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

// Typed client for the support-queue summary via the gateway.
public sealed partial class SupportApiClient(HttpClient http)
{
    private readonly HttpClient _http = http;

    public async Task<SupportSummary?> GetSummaryAsync(CancellationToken ct = default)
    {
        try
        {
            return await _http.GetFromJsonAsync<SupportSummary>("/api/admin/support-summary", ct).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or System.Text.Json.JsonException)
        {
            return null; // API unavailable — the view falls back to placeholder values.
        }
    }
}

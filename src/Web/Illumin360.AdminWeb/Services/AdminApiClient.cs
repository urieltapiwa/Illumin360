using System.Net.Http.Json;
using Microsoft.AspNetCore.Authentication;

namespace Illumin360.AdminWeb.Services;

// Live platform-operations summary from GET /api/admin/summary (Admin service).
public sealed record GrowthPoint(string? Label, int Talent, int Companies);

public sealed record RegionPoint(string? Region, int Count);

public sealed record AdminSummary(int TotalAccounts, int ActiveAccounts, int SuspendedAccounts, int Companies, int Talent, int PendingVerifications, int OpenTickets, int[]? AccountMix, GrowthPoint[]? Growth, RegionPoint[]? Regions);

// Live platform MRR trend from GET /api/billing/mrr-trend (Billing service).
public sealed record MrrPoint(string? Label, long MrrMinor);

public sealed record MrrTrend(string? Currency, MrrPoint[]? Points);

// Relays the signed-in admin's access token to the gateway.
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

// Typed client for the Admin summary via the gateway.
public sealed partial class AdminApiClient(HttpClient http)
{
    private readonly HttpClient _http = http;

    public async Task<AdminSummary?> GetSummaryAsync(CancellationToken ct = default)
    {
        try
        {
            return await _http.GetFromJsonAsync<AdminSummary>("/api/admin/summary", ct).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or System.Text.Json.JsonException)
        {
            return null; // API unavailable — the view falls back to placeholder values.
        }
    }

    public async Task<MrrTrend?> GetMrrTrendAsync(CancellationToken ct = default)
    {
        try
        {
            return await _http.GetFromJsonAsync<MrrTrend>("/api/billing/mrr-trend", ct).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or System.Text.Json.JsonException)
        {
            return null; // Billing unavailable — the view falls back to placeholder values.
        }
    }
}

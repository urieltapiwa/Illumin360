using System.Net.Http.Json;
using Microsoft.AspNetCore.Authentication;

namespace Illumin360.AdminWeb.Services;

// Live platform-operations summary from GET /api/admin/summary (Admin service).
public sealed record GrowthPoint(string? Label, int Talent, int Companies);

public sealed record AdminSummary(int TotalAccounts, int ActiveAccounts, int SuspendedAccounts, int Companies, int Talent, int PendingVerifications, int OpenTickets, int[]? AccountMix, GrowthPoint[]? Growth);

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
public sealed class AdminApiClient(HttpClient http)
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
}

using System.Net.Http.Json;

namespace Illumin360.SupportWeb.Services;

// A ticket in the support queue (from admin-api's ticket board, via the gateway).
public sealed record TicketRow(Guid Id, string? Subject, string? Priority, string? Requester, string? Status, string? Assignee);

// Queue operations for the Support workspace. Reads and mutations go through the gateway with the
// signed-in agent's relayed token; the ticket endpoints accept support roles (SupportPolicy).
public sealed partial class SupportApiClient
{
    public async Task<IReadOnlyList<TicketRow>> GetTicketsAsync(string status, CancellationToken ct = default)
    {
        try
        {
            return await _http.GetFromJsonAsync<List<TicketRow>>($"/api/admin/tickets?status={Uri.EscapeDataString(status)}", ct).ConfigureAwait(false) ?? [];
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or System.Text.Json.JsonException)
        {
            return [];
        }
    }

    public Task<bool> AssignTicketAsync(Guid id, CancellationToken ct = default) => PostAsync($"/api/admin/tickets/{id}/assign", ct);

    public Task<bool> ResolveTicketAsync(Guid id, CancellationToken ct = default) => PostAsync($"/api/admin/tickets/{id}/resolve", ct);

    private async Task<bool> PostAsync(string url, CancellationToken ct)
    {
        try
        {
            using var resp = await _http.PostAsync(url, content: null, ct).ConfigureAwait(false);
            return resp.IsSuccessStatusCode;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            return false;
        }
    }
}

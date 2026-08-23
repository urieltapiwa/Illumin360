using System.Net.Http.Json;

namespace Illumin360.AdminWeb.Services;

// Rows returned by the admin-api management endpoints (via the gateway).
public sealed record AccountRow(Guid Id, string? Name, string? Kind, string? Email, string? Status, string? Region);

public sealed record VerificationRow(Guid Id, string? Entity, string? Kind, string? Risk, string? Submitted, string? Status, string? DecidedBy);

public sealed record TicketRow(Guid Id, string? Subject, string? Priority, string? Requester, string? Status, string? Assignee);

public sealed record AuditRow(Guid Id, string? Actor, string? Action, string? EntityType, string? EntityId, string? Summary, DateTimeOffset OccurredAt);

// Console operations for the Admin portal. Reads and mutations both go through the gateway with the
// signed-in admin's relayed access token (TokenRelayHandler); mutations require the admin-write role,
// which the admin session already carries.
public sealed partial class AdminApiClient
{
    public async Task<IReadOnlyList<AccountRow>> GetAccountsAsync(string? status, CancellationToken ct = default)
    {
        var url = string.IsNullOrWhiteSpace(status) ? "/api/admin/accounts" : $"/api/admin/accounts?status={Uri.EscapeDataString(status)}";
        return await GetListAsync<AccountRow>(url, ct).ConfigureAwait(false);
    }

    public Task<bool> SuspendAccountAsync(Guid id, CancellationToken ct = default) => PostAsync($"/api/admin/accounts/{id}/suspend", ct);

    public Task<bool> ActivateAccountAsync(Guid id, CancellationToken ct = default) => PostAsync($"/api/admin/accounts/{id}/activate", ct);

    public Task<IReadOnlyList<VerificationRow>> GetVerificationsAsync(string status, CancellationToken ct = default)
        => GetListAsync<VerificationRow>($"/api/admin/verifications?status={Uri.EscapeDataString(status)}", ct);

    public Task<bool> ApproveVerificationAsync(Guid id, CancellationToken ct = default) => PostAsync($"/api/admin/verifications/{id}/approve", ct);

    public Task<bool> RejectVerificationAsync(Guid id, CancellationToken ct = default) => PostAsync($"/api/admin/verifications/{id}/reject", ct);

    public Task<IReadOnlyList<TicketRow>> GetTicketsAsync(string status, CancellationToken ct = default)
        => GetListAsync<TicketRow>($"/api/admin/tickets?status={Uri.EscapeDataString(status)}", ct);

    public Task<bool> AssignTicketAsync(Guid id, CancellationToken ct = default) => PostAsync($"/api/admin/tickets/{id}/assign", ct);

    public Task<bool> ResolveTicketAsync(Guid id, CancellationToken ct = default) => PostAsync($"/api/admin/tickets/{id}/resolve", ct);

    public Task<IReadOnlyList<AuditRow>> GetAuditAsync(CancellationToken ct = default)
        => GetListAsync<AuditRow>("/api/admin/audit", ct);

    private async Task<IReadOnlyList<T>> GetListAsync<T>(string url, CancellationToken ct)
    {
        try
        {
            return await _http.GetFromJsonAsync<List<T>>(url, ct).ConfigureAwait(false) ?? [];
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or System.Text.Json.JsonException)
        {
            return [];
        }
    }

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

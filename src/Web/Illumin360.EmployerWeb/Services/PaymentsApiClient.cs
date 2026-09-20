using System.Net.Http.Json;

namespace Illumin360.EmployerWeb.Services;

// Mirrors the Payments service DTOs (GET/POST /api/payments/*), reached through the gateway.
public sealed record ContractDto(Guid Id, Guid ClientId, Guid TalentId, Guid? RequestId, string Title, string Currency, string Status, DateTimeOffset CreatedAt);

public sealed record MilestoneDto(Guid Id, int Order, string Title, long AmountMinor, string Status, DateTimeOffset? FundedAt, DateTimeOffset? SubmittedAt, DateTimeOffset? DecidedAt);

public sealed record MovementDto(Guid Id, Guid MilestoneId, string Kind, long AmountMinor, string Currency, DateTimeOffset CreatedAt);

public sealed record ContractDetailDto(ContractDto Contract, IReadOnlyList<MilestoneDto> Milestones, IReadOnlyList<MovementDto> Movements);

// Typed client for the Payments service (contracts, milestones, escrow transitions) via the gateway.
// Uses the signed-in employer's relayed token (all payments endpoints require authorization).
public sealed class PaymentsApiClient(HttpClient http)
{
    private readonly HttpClient _http = http;

    public async Task<IReadOnlyList<ContractDto>> ListContractsAsync(Guid clientId, CancellationToken ct = default)
    {
        try
        {
            return await _http.GetFromJsonAsync<List<ContractDto>>($"/api/payments/contracts?clientId={clientId}", ct).ConfigureAwait(false) ?? [];
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or System.Text.Json.JsonException)
        {
            return [];
        }
    }

    public async Task<ContractDetailDto?> GetContractAsync(Guid id, CancellationToken ct = default)
    {
        try
        {
            return await _http.GetFromJsonAsync<ContractDetailDto>($"/api/payments/contracts/{id}", ct).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or System.Text.Json.JsonException)
        {
            return null;
        }
    }

    public async Task<ContractDto?> CreateContractAsync(Guid clientId, Guid talentId, string title, string currency, CancellationToken ct = default)
    {
        try
        {
            using var resp = await _http.PostAsJsonAsync("/api/payments/contracts", new { clientId, talentId, requestId = (Guid?)null, title, currency }, ct).ConfigureAwait(false);
            return resp.IsSuccessStatusCode ? await resp.Content.ReadFromJsonAsync<ContractDto>(ct).ConfigureAwait(false) : null;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or System.Text.Json.JsonException)
        {
            return null;
        }
    }

    public Task<bool> AddMilestoneAsync(Guid contractId, string title, long amountMinor, CancellationToken ct = default)
        => PostAsync($"/api/payments/contracts/{contractId}/milestones", new { title, amountMinor }, ct);

    public Task<bool> ActivateAsync(Guid contractId, CancellationToken ct = default)
        => PostAsync($"/api/payments/contracts/{contractId}/activate", null, ct);

    public Task<bool> CancelAsync(Guid contractId, CancellationToken ct = default)
        => PostAsync($"/api/payments/contracts/{contractId}/cancel", null, ct);

    public Task<bool> MilestoneActionAsync(Guid milestoneId, string verb, CancellationToken ct = default)
        => PostAsync($"/api/payments/milestones/{milestoneId}/{verb}", null, ct);

    private async Task<bool> PostAsync(string url, object? body, CancellationToken ct)
    {
        try
        {
            using var resp = body is null
                ? await _http.PostAsync(url, null, ct).ConfigureAwait(false)
                : await _http.PostAsJsonAsync(url, body, ct).ConfigureAwait(false);
            return resp.IsSuccessStatusCode;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            return false;
        }
    }
}

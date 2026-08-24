using System.Net.Http.Json;

namespace Illumin360.EmployerWeb.Services;

// A team member on the current employer account.
public sealed record TeamMember(Guid Id, string? Email, string? DisplayName, string? Role, DateTimeOffset InvitedAt);

// Self-service console operations for the Employer portal. Reads and mutations go through the gateway
// with the signed-in employer's relayed token; mutations require the employer role (EmployerPolicy).
public sealed partial class EmployersApiClient
{
    public async Task<bool> UpdateProfileAsync(string industry, string city, string? website, string? about, CancellationToken ct = default)
    {
        try
        {
            using var resp = await _http.PutAsJsonAsync("/api/employers/me", new { industry, city, website, about }, ct).ConfigureAwait(false);
            return resp.IsSuccessStatusCode;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            return false;
        }
    }

    public async Task<IReadOnlyList<TeamMember>> GetTeamAsync(CancellationToken ct = default)
    {
        try
        {
            return await _http.GetFromJsonAsync<List<TeamMember>>("/api/employers/me/team", ct).ConfigureAwait(false) ?? [];
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or System.Text.Json.JsonException)
        {
            return [];
        }
    }

    public async Task<bool> InviteMemberAsync(string email, string displayName, string role, CancellationToken ct = default)
    {
        try
        {
            using var resp = await _http.PostAsJsonAsync("/api/employers/me/team", new { email, displayName, role }, ct).ConfigureAwait(false);
            return resp.IsSuccessStatusCode;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            return false;
        }
    }

    public async Task<bool> ChangeRoleAsync(Guid memberId, string role, CancellationToken ct = default)
    {
        try
        {
            using var resp = await _http.PutAsJsonAsync($"/api/employers/me/team/{memberId}/role", new { role }, ct).ConfigureAwait(false);
            return resp.IsSuccessStatusCode;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            return false;
        }
    }

    public async Task<bool> RemoveMemberAsync(Guid memberId, CancellationToken ct = default)
    {
        try
        {
            using var resp = await _http.DeleteAsync($"/api/employers/me/team/{memberId}", ct).ConfigureAwait(false);
            return resp.IsSuccessStatusCode;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            return false;
        }
    }
}

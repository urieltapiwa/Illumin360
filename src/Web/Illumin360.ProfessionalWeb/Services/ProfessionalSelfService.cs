using System.Net.Http.Json;

namespace Illumin360.ProfessionalWeb.Services;

// A role match on the current professional's dashboard.
public sealed record ProMatch(Guid Id, string? Role, string? Company, string? City, string? Industry, int Match, int SalaryLo, int SalaryHi, string? Type, string? Posted, string? Status);

// An editable skill on the current professional's profile.
public sealed record EditableSkill(Guid Id, string? Name, int Level, string? Trend, int Endorsements);

// Slices of GET /me used by the self-service pages (other fields ignored).
public sealed record ProMatches(ProMatch[]? Matches);

public sealed record ProSkills(EditableSkill[]? Skills);

// Professional self-service actions (match apply/save/dismiss, add/remove skills). All go through the
// gateway with the signed-in professional's relayed token (ProfessionalPolicy).
public sealed partial class ProfessionalsApiClient
{
    public async Task<IReadOnlyList<ProMatch>> GetMatchesAsync(CancellationToken ct = default)
    {
        try
        {
            var me = await _http.GetFromJsonAsync<ProMatches>("/api/professionals/me", ct).ConfigureAwait(false);
            return me?.Matches ?? [];
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or System.Text.Json.JsonException)
        {
            return [];
        }
    }

    public async Task<IReadOnlyList<EditableSkill>> GetSkillsAsync(CancellationToken ct = default)
    {
        try
        {
            var me = await _http.GetFromJsonAsync<ProSkills>("/api/professionals/me", ct).ConfigureAwait(false);
            return me?.Skills ?? [];
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or System.Text.Json.JsonException)
        {
            return [];
        }
    }

    // action: "apply" | "save" | "dismiss"
    public Task<bool> MatchActionAsync(Guid id, string action, CancellationToken ct = default)
        => PostAsync($"/api/professionals/me/matches/{id}/{action}", ct);

    public async Task<bool> AddSkillAsync(string name, int level, CancellationToken ct = default)
    {
        try
        {
            using var resp = await _http.PostAsJsonAsync("/api/professionals/me/skills", new { name, level }, ct).ConfigureAwait(false);
            return resp.IsSuccessStatusCode;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            return false;
        }
    }

    public async Task<bool> RemoveSkillAsync(Guid id, CancellationToken ct = default)
    {
        try
        {
            using var resp = await _http.DeleteAsync($"/api/professionals/me/skills/{id}", ct).ConfigureAwait(false);
            return resp.IsSuccessStatusCode;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            return false;
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

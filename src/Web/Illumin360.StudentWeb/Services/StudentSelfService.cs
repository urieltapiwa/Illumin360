using System.Net.Http.Json;

namespace Illumin360.StudentWeb.Services;

// An internship match on the current student's dashboard.
public sealed record StudentMatch(Guid Id, string? Role, string? Company, string? City, int Match, int StipendLo, int StipendHi, string? Type, string? Posted, string? Status);

// Just the slice of GET /me needed for the Internships page (other fields ignored).
public sealed record StudentMatches(StudentMatch[]? Matches);

// Student self-service actions (apply/save/dismiss a match, set availability). All go through the
// gateway with the signed-in student's relayed token (StudentPolicy).
public sealed partial class StudentsApiClient
{
    public async Task<IReadOnlyList<StudentMatch>> GetMatchesAsync(CancellationToken ct = default)
    {
        try
        {
            var me = await _http.GetFromJsonAsync<StudentMatches>("/api/students/me", ct).ConfigureAwait(false);
            return me?.Matches ?? [];
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or System.Text.Json.JsonException)
        {
            return [];
        }
    }

    // action: "apply" | "save" | "dismiss"
    public Task<bool> MatchActionAsync(Guid id, string action, CancellationToken ct = default)
        => PostAsync($"/api/students/me/matches/{id}/{action}", ct);

    public async Task<bool> SetAvailabilityAsync(string availability, CancellationToken ct = default)
    {
        try
        {
            using var resp = await _http.PostAsJsonAsync("/api/students/me/availability", new { availability }, ct).ConfigureAwait(false);
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

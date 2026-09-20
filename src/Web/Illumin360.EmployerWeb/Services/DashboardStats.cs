using System.Net.Http.Json;

namespace Illumin360.EmployerWeb.Services;

// A labelled count (funnel stage, city, availability bucket, …) as returned by the stats endpoints.
public sealed record CountByLabel(string Label, int Count);

// One month of recruitment activity from the Recruitment service trend.
public sealed record MonthlyPoint(string Month, int Applications, int Hires);

// Subset of GET /api/candidates/stats we surface on the dashboard.
public sealed record CandidateStats(int Total, IReadOnlyList<CountByLabel>? ByCity, IReadOnlyList<CountByLabel>? ByAvailability);

// Subset of GET /api/recruitment/stats we surface on the dashboard.
public sealed record RecruitmentStats(
    int TotalRequests,
    int OpenRequests,
    int FilledRequests,
    int TotalApplications,
    int TotalHires,
    IReadOnlyList<CountByLabel>? Funnel,
    IReadOnlyList<MonthlyPoint>? Trend);

// Composite view model for the Employer dashboard: live company profile + live cross-service stats.
public sealed record EmployerDashboard(
    EmployerProfile? Profile,
    int TeamCount,
    CandidateStats? Candidates,
    RecruitmentStats? Recruitment);

public sealed partial class EmployersApiClient
{
    // Aggregate candidate pool stats (anonymous endpoint). Null when the Candidates service is unavailable.
    public async Task<CandidateStats?> GetCandidateStatsAsync(CancellationToken ct = default)
    {
        try
        {
            return await _http.GetFromJsonAsync<CandidateStats>("/api/candidates/stats", ct).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or System.Text.Json.JsonException)
        {
            return null;
        }
    }

    // Aggregate recruitment stats — open roles, funnel and monthly trend (anonymous endpoint).
    public async Task<RecruitmentStats?> GetRecruitmentStatsAsync(CancellationToken ct = default)
    {
        try
        {
            return await _http.GetFromJsonAsync<RecruitmentStats>("/api/recruitment/stats", ct).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or System.Text.Json.JsonException)
        {
            return null;
        }
    }
}

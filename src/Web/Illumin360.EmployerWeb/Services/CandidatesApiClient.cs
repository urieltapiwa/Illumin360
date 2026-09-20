using System.Net.Http.Json;

namespace Illumin360.EmployerWeb.Services;

// Mirrors the Candidates service search DTOs (GET /api/candidates/search), reached through the gateway.
public sealed record CandidateDto(Guid Id, string FirstName, string LastName, string City, string Nationality, string Availability, string? PublicHeadline);

public sealed record CandidateFacets(IReadOnlyList<CountByLabel> Cities, IReadOnlyList<CountByLabel> Availability);

public sealed record CandidateSearchResult(IReadOnlyList<CandidateDto> Items, int Total, int Page, int PageSize, CandidateFacets Facets);

// CV metadata returned by GET /api/candidates/{id}/cv (404 → no CV on file).
public sealed record CandidateCv(string FileName, string ContentType, long Size, DateTimeOffset UploadedAt);

// "More like this" result from GET /api/candidates/{id}/similar.
public sealed record SimilarCandidate(Guid Id, string Name, string City, string? Headline, string Availability, int Score);

// Typed client for faceted candidate search (anonymous endpoint; token relayed for parity).
public sealed class CandidatesApiClient(HttpClient http)
{
    private readonly HttpClient _http = http;

    public async Task<CandidateSearchResult?> SearchAsync(
        string? city, string? availability, string? q, bool? hasCv, int page, int pageSize, bool blind, CancellationToken ct = default)
    {
        var query = new List<string>();
        if (!string.IsNullOrWhiteSpace(city))
        {
            query.Add($"city={Uri.EscapeDataString(city)}");
        }

        if (!string.IsNullOrWhiteSpace(availability))
        {
            query.Add($"availability={Uri.EscapeDataString(availability)}");
        }

        if (!string.IsNullOrWhiteSpace(q))
        {
            query.Add($"q={Uri.EscapeDataString(q)}");
        }

        if (hasCv is not null)
        {
            query.Add($"hasCv={(hasCv.Value ? "true" : "false")}");
        }

        if (blind)
        {
            query.Add("blind=true");
        }

        query.Add($"page={page}");
        query.Add($"pageSize={pageSize}");
        var url = "/api/candidates/search?" + string.Join("&", query);

        try
        {
            return await _http.GetFromJsonAsync<CandidateSearchResult>(url, ct).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or System.Text.Json.JsonException)
        {
            return null;
        }
    }

    // A single candidate by id (anonymous endpoint). Null when not found / unavailable.
    public async Task<CandidateDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        try
        {
            return await _http.GetFromJsonAsync<CandidateDto>($"/api/candidates/{id}", ct).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or System.Text.Json.JsonException)
        {
            return null;
        }
    }

    // CV metadata for a candidate; null when there's no CV on file (404) or the service is unavailable.
    public async Task<CandidateCv?> GetCvAsync(Guid id, CancellationToken ct = default)
    {
        try
        {
            return await _http.GetFromJsonAsync<CandidateCv>($"/api/candidates/{id}/cv", ct).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or System.Text.Json.JsonException)
        {
            return null;
        }
    }

    // "More like this" — candidates similar to the seed (requires a signed-in user; token is relayed).
    public async Task<IReadOnlyList<SimilarCandidate>> GetSimilarAsync(Guid id, int take = 5, CancellationToken ct = default)
    {
        try
        {
            return await _http.GetFromJsonAsync<List<SimilarCandidate>>($"/api/candidates/{id}/similar?take={take}", ct).ConfigureAwait(false) ?? [];
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or System.Text.Json.JsonException)
        {
            return [];
        }
    }
}

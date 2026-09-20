using Microsoft.AspNetCore.Mvc;
using Illumin360.EmployerWeb.Services;

namespace Illumin360.EmployerWeb.Controllers;

// The current search criteria, echoed back to the view so the form stays populated.
public sealed record CandidateSearchVm(
    string? Q,
    string? City,
    string? Availability,
    bool? HasCv,
    bool Blind,
    int Page,
    CandidateSearchResult? Result);

// A single candidate plus their CV metadata (if any) and "more like this" for the detail page.
public sealed record CandidateDetailVm(CandidateDto Candidate, CandidateCv? Cv, IReadOnlyList<SimilarCandidate> Similar);

// Faceted candidate search over the Candidates service (via the gateway).
public class CandidatesController(CandidatesApiClient candidates) : Controller
{
    private const int PageSize = 12;
    private readonly CandidatesApiClient _candidates = candidates;

    public async Task<IActionResult> Index(
        string? q, string? city, string? availability, bool? hasCv, bool blind = false, int page = 1, CancellationToken ct = default)
    {
        if (page < 1)
        {
            page = 1;
        }

        var result = await _candidates.SearchAsync(city, availability, q, hasCv, page, PageSize, blind, ct);
        return View(new CandidateSearchVm(q, city, availability, hasCv, blind, page, result));
    }

    public async Task<IActionResult> Details(Guid id, CancellationToken ct)
    {
        var candidate = await _candidates.GetByIdAsync(id, ct);
        if (candidate is null)
        {
            TempData["Msg"] = "Candidate not found.";
            return RedirectToAction(nameof(Index));
        }

        var cvTask = _candidates.GetCvAsync(id, ct);
        var similarTask = _candidates.GetSimilarAsync(id, 5, ct);
        await Task.WhenAll(cvTask, similarTask);
        return View(new CandidateDetailVm(candidate, cvTask.Result, similarTask.Result));
    }
}

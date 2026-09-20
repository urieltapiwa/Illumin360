using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Illumin360.EmployerWeb.Models;
using Illumin360.EmployerWeb.Services;

namespace Illumin360.EmployerWeb.Controllers;

public class HomeController(EmployersApiClient employers) : Controller
{
    private readonly EmployersApiClient _employers = employers;

    public async Task<IActionResult> Index(CancellationToken ct)
    {
        // Live company profile + team (Employers service) and cross-service pool/pipeline stats
        // (Candidates + Recruitment services) — fetched in parallel through the gateway.
        var profileTask = _employers.GetProfileAsync(ct);
        var teamTask = _employers.GetTeamAsync(ct);
        var candidatesTask = _employers.GetCandidateStatsAsync(ct);
        var recruitmentTask = _employers.GetRecruitmentStatsAsync(ct);
        await Task.WhenAll(profileTask, teamTask, candidatesTask, recruitmentTask);

        return View(new EmployerDashboard(
            profileTask.Result,
            teamTask.Result.Count,
            candidatesTask.Result,
            recruitmentTask.Result));
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}

using Microsoft.AspNetCore.Mvc;
using Illumin360.ProfessionalWeb.Services;

namespace Illumin360.ProfessionalWeb.Controllers;

public class MatchesController(ProfessionalsApiClient professionals) : Controller
{
    private readonly ProfessionalsApiClient _professionals = professionals;

    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var matches = await _professionals.GetMatchesAsync(ct);
        return View(matches);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Act(Guid id, string action, CancellationToken ct)
    {
        var a = action ?? string.Empty;
        var verb = a is "apply" or "save" or "dismiss" ? a : string.Empty;
        if (verb.Length > 0)
        {
            var ok = await _professionals.MatchActionAsync(id, verb, ct);
            TempData["Msg"] = ok ? $"Match {verb} recorded." : "Could not update — please try again.";
        }

        return RedirectToAction(nameof(Index));
    }
}

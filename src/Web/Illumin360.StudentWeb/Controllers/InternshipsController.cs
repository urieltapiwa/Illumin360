using Microsoft.AspNetCore.Mvc;
using Illumin360.StudentWeb.Services;

namespace Illumin360.StudentWeb.Controllers;

public class InternshipsController(StudentsApiClient students) : Controller
{
    private readonly StudentsApiClient _students = students;

    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var matches = await _students.GetMatchesAsync(ct);
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
            var ok = await _students.MatchActionAsync(id, verb, ct);
            TempData["Msg"] = ok ? $"Match {verb} recorded." : "Could not update — please try again.";
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SetAvailability(string availability, CancellationToken ct)
    {
        await _students.SetAvailabilityAsync(availability ?? "Open to opportunities", ct);
        TempData["Msg"] = "Availability updated.";
        return RedirectToAction(nameof(Index));
    }
}

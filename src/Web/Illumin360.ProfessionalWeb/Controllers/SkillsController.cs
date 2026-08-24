using Microsoft.AspNetCore.Mvc;
using Illumin360.ProfessionalWeb.Services;

namespace Illumin360.ProfessionalWeb.Controllers;

public class SkillsController(ProfessionalsApiClient professionals) : Controller
{
    private readonly ProfessionalsApiClient _professionals = professionals;

    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var skills = await _professionals.GetSkillsAsync(ct);
        return View(skills);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Add(string name, int level, CancellationToken ct)
    {
        if (!string.IsNullOrWhiteSpace(name))
        {
            var ok = await _professionals.AddSkillAsync(name.Trim(), Math.Clamp(level, 1, 100), ct);
            TempData["Msg"] = ok ? "Skill added." : "Could not add the skill — please try again.";
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Remove(Guid id, CancellationToken ct)
    {
        await _professionals.RemoveSkillAsync(id, ct);
        return RedirectToAction(nameof(Index));
    }
}

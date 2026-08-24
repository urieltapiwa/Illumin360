using Microsoft.AspNetCore.Mvc;
using Illumin360.EmployerWeb.Services;

namespace Illumin360.EmployerWeb.Controllers;

public class ProfileController(EmployersApiClient employers) : Controller
{
    private readonly EmployersApiClient _employers = employers;

    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var profile = await _employers.GetProfileAsync(ct);
        return View(profile);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(string industry, string city, string? website, string? about, CancellationToken ct)
    {
        var ok = await _employers.UpdateProfileAsync(industry ?? string.Empty, city ?? string.Empty, website, about, ct);
        TempData["Saved"] = ok ? "Company profile updated." : "Could not save — please try again.";
        return RedirectToAction(nameof(Index));
    }
}

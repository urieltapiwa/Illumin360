using Microsoft.AspNetCore.Mvc;
using Illumin360.StudentWeb.Services;

namespace Illumin360.StudentWeb.Controllers;

public class ProfileController(StudentsApiClient students) : Controller
{
    private readonly StudentsApiClient _students = students;

    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var dashboard = await _students.GetDashboardAsync(ct);
        return View(dashboard?.Persona);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(string field, string school, string city, CancellationToken ct)
    {
        var ok = await _students.UpdateProfileAsync(field ?? string.Empty, school ?? string.Empty, city ?? string.Empty, ct);
        TempData["Saved"] = ok ? "Profile updated." : "Could not save — please try again.";
        return RedirectToAction(nameof(Index));
    }
}

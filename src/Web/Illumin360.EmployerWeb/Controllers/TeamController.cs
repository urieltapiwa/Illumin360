using Microsoft.AspNetCore.Mvc;
using Illumin360.EmployerWeb.Services;

namespace Illumin360.EmployerWeb.Controllers;

public class TeamController(EmployersApiClient employers) : Controller
{
    private readonly EmployersApiClient _employers = employers;

    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var team = await _employers.GetTeamAsync(ct);
        return View(team);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Invite(string email, string displayName, string role, CancellationToken ct)
    {
        var ok = await _employers.InviteMemberAsync(email ?? string.Empty, displayName ?? string.Empty, role ?? "viewer", ct);
        TempData["Msg"] = ok ? "Team member invited." : "Could not invite — check the details and try again.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangeRole(Guid id, string role, CancellationToken ct)
    {
        await _employers.ChangeRoleAsync(id, role ?? "viewer", ct);
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Remove(Guid id, CancellationToken ct)
    {
        await _employers.RemoveMemberAsync(id, ct);
        return RedirectToAction(nameof(Index));
    }
}

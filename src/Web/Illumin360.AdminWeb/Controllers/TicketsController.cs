using Microsoft.AspNetCore.Mvc;
using Illumin360.AdminWeb.Services;

namespace Illumin360.AdminWeb.Controllers;

public class TicketsController(AdminApiClient admin) : Controller
{
    private readonly AdminApiClient _admin = admin;

    public async Task<IActionResult> Index(string? status, CancellationToken ct)
    {
        var s = string.IsNullOrWhiteSpace(status) ? "open" : status;
        var rows = await _admin.GetTicketsAsync(s, ct);
        ViewData["Status"] = s;
        return View(rows);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Assign(Guid id, CancellationToken ct)
    {
        await _admin.AssignTicketAsync(id, ct);
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Resolve(Guid id, CancellationToken ct)
    {
        await _admin.ResolveTicketAsync(id, ct);
        return RedirectToAction(nameof(Index));
    }
}

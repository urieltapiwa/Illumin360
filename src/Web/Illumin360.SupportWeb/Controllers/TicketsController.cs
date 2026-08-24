using Microsoft.AspNetCore.Mvc;
using Illumin360.SupportWeb.Services;

namespace Illumin360.SupportWeb.Controllers;

public class TicketsController(SupportApiClient support) : Controller
{
    private readonly SupportApiClient _support = support;

    public async Task<IActionResult> Index(string? status, CancellationToken ct)
    {
        var s = string.IsNullOrWhiteSpace(status) ? "open" : status;
        var rows = await _support.GetTicketsAsync(s, ct);
        ViewData["Status"] = s;
        return View(rows);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Assign(Guid id, string? status, CancellationToken ct)
    {
        await _support.AssignTicketAsync(id, ct);
        return RedirectToAction(nameof(Index), new { status });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Resolve(Guid id, string? status, CancellationToken ct)
    {
        await _support.ResolveTicketAsync(id, ct);
        return RedirectToAction(nameof(Index), new { status });
    }
}

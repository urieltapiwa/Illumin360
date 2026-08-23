using Microsoft.AspNetCore.Mvc;
using Illumin360.AdminWeb.Services;

namespace Illumin360.AdminWeb.Controllers;

public class VerificationsController(AdminApiClient admin) : Controller
{
    private readonly AdminApiClient _admin = admin;

    public async Task<IActionResult> Index(string? status, CancellationToken ct)
    {
        var s = string.IsNullOrWhiteSpace(status) ? "pending" : status;
        var rows = await _admin.GetVerificationsAsync(s, ct);
        ViewData["Status"] = s;
        return View(rows);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Approve(Guid id, CancellationToken ct)
    {
        await _admin.ApproveVerificationAsync(id, ct);
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reject(Guid id, CancellationToken ct)
    {
        await _admin.RejectVerificationAsync(id, ct);
        return RedirectToAction(nameof(Index));
    }
}

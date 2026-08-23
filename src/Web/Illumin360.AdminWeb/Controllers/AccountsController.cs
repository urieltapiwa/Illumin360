using Microsoft.AspNetCore.Mvc;
using Illumin360.AdminWeb.Services;

namespace Illumin360.AdminWeb.Controllers;

public class AccountsController(AdminApiClient admin) : Controller
{
    private readonly AdminApiClient _admin = admin;

    public async Task<IActionResult> Index(string? kind, string? status, CancellationToken ct)
    {
        var rows = await _admin.GetAccountsAsync(status, ct);
        if (!string.IsNullOrWhiteSpace(kind))
        {
            rows = [.. rows.Where(r => string.Equals(r.Kind, kind, StringComparison.OrdinalIgnoreCase))];
        }

        ViewData["Kind"] = kind;
        ViewData["Status"] = status;
        return View(rows);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Suspend(Guid id, CancellationToken ct)
    {
        await _admin.SuspendAccountAsync(id, ct);
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Activate(Guid id, CancellationToken ct)
    {
        await _admin.ActivateAccountAsync(id, ct);
        return RedirectToAction(nameof(Index));
    }
}

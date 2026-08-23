using Microsoft.AspNetCore.Mvc;
using Illumin360.AdminWeb.Services;

namespace Illumin360.AdminWeb.Controllers;

public class AuditController(AdminApiClient admin) : Controller
{
    private readonly AdminApiClient _admin = admin;

    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var rows = await _admin.GetAuditAsync(ct);
        return View(rows);
    }
}

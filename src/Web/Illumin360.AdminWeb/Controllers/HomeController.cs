using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Illumin360.AdminWeb.Models;
using Illumin360.AdminWeb.Services;

namespace Illumin360.AdminWeb.Controllers;

public class HomeController(AdminApiClient admin) : Controller
{
    private readonly AdminApiClient _admin = admin;

    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var summary = await _admin.GetSummaryAsync(ct);
        return View(summary);
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}

using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Illumin360.SupportWeb.Models;
using Illumin360.SupportWeb.Services;

namespace Illumin360.SupportWeb.Controllers;

public class HomeController(SupportApiClient support) : Controller
{
    private readonly SupportApiClient _support = support;

    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var summary = await _support.GetSummaryAsync(ct);
        return View(summary);
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}

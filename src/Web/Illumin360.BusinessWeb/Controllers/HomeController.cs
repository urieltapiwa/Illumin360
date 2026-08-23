using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Illumin360.BusinessWeb.Models;
using Illumin360.BusinessWeb.Services;

namespace Illumin360.BusinessWeb.Controllers;

public class HomeController(InsightsApiClient insights) : Controller
{
    private readonly InsightsApiClient _insights = insights;

    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var data = await _insights.GetInsightsAsync(ct);
        return View(data);
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}

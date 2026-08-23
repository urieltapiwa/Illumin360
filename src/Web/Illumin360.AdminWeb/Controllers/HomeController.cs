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

        // MRR trend comes from a different service (Billing), so it rides in ViewData rather than the model.
        var mrr = await _admin.GetMrrTrendAsync(ct);
        if (mrr?.Points is { Length: > 0 } points)
        {
            ViewData["MrrLabels"] = "[" + string.Join(",", points.Select(p => "\"" + p.Label + "\"")) + "]";
            ViewData["MrrValues"] = "[" + string.Join(",", points.Select(p => p.MrrMinor / 100)) + "]"; // minor units -> currency
            ViewData["MrrCurrency"] = mrr.Currency ?? "NAD";
        }

        return View(summary);
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}

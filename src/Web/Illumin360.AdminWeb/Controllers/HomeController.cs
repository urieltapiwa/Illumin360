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

        // Live verification queue — same source the Verifications console page reads (top 6 pending).
        var pending = await _admin.GetVerificationsAsync("pending", ct);
        ViewData["Verifications"] = pending?.Take(6).ToList();

        // Live system health — Admin API probes each service's /health/ready.
        ViewData["SystemHealth"] = await _admin.GetSystemHealthAsync(ct);

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

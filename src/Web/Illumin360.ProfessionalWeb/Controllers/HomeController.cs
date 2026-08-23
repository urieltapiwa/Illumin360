using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Illumin360.ProfessionalWeb.Models;
using Illumin360.ProfessionalWeb.Services;

namespace Illumin360.ProfessionalWeb.Controllers;

public class HomeController(ProfessionalsApiClient professionals) : Controller
{
    private readonly ProfessionalsApiClient _professionals = professionals;

    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var dashboard = await _professionals.GetDashboardAsync(ct);
        return View(dashboard);
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}

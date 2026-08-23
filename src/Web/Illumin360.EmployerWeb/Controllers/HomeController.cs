using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Illumin360.EmployerWeb.Models;
using Illumin360.EmployerWeb.Services;

namespace Illumin360.EmployerWeb.Controllers;

public class HomeController(EmployersApiClient employers) : Controller
{
    private readonly EmployersApiClient _employers = employers;

    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var profile = await _employers.GetProfileAsync(ct);
        return View(profile);
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}

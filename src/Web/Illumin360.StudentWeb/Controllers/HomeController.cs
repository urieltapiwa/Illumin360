using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Illumin360.StudentWeb.Models;
using Illumin360.StudentWeb.Services;

namespace Illumin360.StudentWeb.Controllers;

public class HomeController(StudentsApiClient students) : Controller
{
    private readonly StudentsApiClient _students = students;

    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var dashboard = await _students.GetDashboardAsync(ct);
        return View(dashboard);
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}

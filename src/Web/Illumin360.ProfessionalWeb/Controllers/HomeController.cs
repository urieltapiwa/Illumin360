using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Illumin360.ProfessionalWeb.Models;

namespace Illumin360.ProfessionalWeb.Controllers;

public class HomeController : Controller
{
    public IActionResult Index()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}

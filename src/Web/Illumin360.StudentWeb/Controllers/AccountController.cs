using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Illumin360.StudentWeb.Services;

namespace Illumin360.StudentWeb.Controllers;

[AllowAnonymous]
public class AccountController(KeycloakRegistrar registrar) : Controller
{
    private readonly KeycloakRegistrar _registrar = registrar;

    [HttpGet]
    public IActionResult Register() => View(new RegisterViewModel());

    [HttpGet]
    public IActionResult Registered() => View();

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel vm, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(vm);

        if (!ModelState.IsValid)
        {
            return View(vm);
        }

        var result = await _registrar.RegisterAsync(
            "student",
            new RegisterRequest(vm.FirstName, vm.LastName, vm.Email, vm.Password, vm.City, vm.Field, vm.School, vm.Role, vm.Company),
            ct);

        if (result.StatusCode == StatusCodes.Status201Created)
        {
            return RedirectToAction(nameof(Registered));
        }

        ModelState.AddModelError(string.Empty, result.Message);
        return View(vm);
    }
}

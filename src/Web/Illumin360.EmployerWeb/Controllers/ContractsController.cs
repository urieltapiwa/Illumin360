using Microsoft.AspNetCore.Mvc;
using Illumin360.EmployerWeb.Services;

namespace Illumin360.EmployerWeb.Controllers;

// View model for the contracts list — the employer's client id + their contracts.
public sealed record ContractsListVm(Guid ClientId, IReadOnlyList<ContractDto> Contracts);

// Contracts & milestone payments for the current employer (Payments service, via the gateway).
public class ContractsController(EmployersApiClient employers, PaymentsApiClient payments) : Controller
{
    private readonly EmployersApiClient _employers = employers;
    private readonly PaymentsApiClient _payments = payments;

    // Resolves the signed-in employer's client id from their profile (the Payments ClientId).
    private async Task<Guid> ClientIdAsync(CancellationToken ct)
    {
        var profile = await _employers.GetProfileAsync(ct);
        return Guid.TryParse(profile?.Id, out var id) ? id : Guid.Empty;
    }

    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var clientId = await ClientIdAsync(ct);
        var contracts = clientId == Guid.Empty
            ? Array.Empty<ContractDto>()
            : await _payments.ListContractsAsync(clientId, ct);
        return View(new ContractsListVm(clientId, contracts));
    }

    public async Task<IActionResult> Details(Guid id, CancellationToken ct)
    {
        var detail = await _payments.GetContractAsync(id, ct);
        if (detail is null)
        {
            TempData["Msg"] = "Contract not found.";
            return RedirectToAction(nameof(Index));
        }

        return View(detail);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(string title, string? currency, Guid? talentId, CancellationToken ct)
    {
        var clientId = await ClientIdAsync(ct);
        if (clientId == Guid.Empty || string.IsNullOrWhiteSpace(title))
        {
            TempData["Msg"] = "A title is required to create a contract.";
            return RedirectToAction(nameof(Index));
        }

        // Prefer the candidate the contract was started from; otherwise a placeholder until talent is assigned.
        var talent = talentId is { } t && t != Guid.Empty ? t : Guid.NewGuid();
        var created = await _payments.CreateContractAsync(clientId, talent, title.Trim(), string.IsNullOrWhiteSpace(currency) ? "NAD" : currency.Trim(), ct);
        if (created is null)
        {
            TempData["Msg"] = "Could not create the contract — please try again.";
            return RedirectToAction(nameof(Index));
        }

        return RedirectToAction(nameof(Details), new { id = created.Id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddMilestone(Guid id, string title, decimal amount, CancellationToken ct)
    {
        var ok = await _payments.AddMilestoneAsync(id, title ?? string.Empty, (long)Math.Round(amount * 100m), ct);
        TempData["Msg"] = ok ? "Milestone added." : "Could not add milestone (contract must be a draft).";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Activate(Guid id, CancellationToken ct)
    {
        var ok = await _payments.ActivateAsync(id, ct);
        TempData["Msg"] = ok ? "Contract activated." : "Could not activate (add at least one milestone first).";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(Guid id, CancellationToken ct)
    {
        var ok = await _payments.CancelAsync(id, ct);
        TempData["Msg"] = ok ? "Contract cancelled." : "Could not cancel this contract.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Milestone(Guid id, Guid milestoneId, string verb, CancellationToken ct)
    {
        var allowed = new[] { "fund", "submit", "approve", "refund" };
        if (Array.IndexOf(allowed, verb) < 0)
        {
            return RedirectToAction(nameof(Details), new { id });
        }

        var ok = await _payments.MilestoneActionAsync(milestoneId, verb, ct);
        TempData["Msg"] = ok ? $"Milestone {verb} succeeded." : $"Could not {verb} this milestone in its current state.";
        return RedirectToAction(nameof(Details), new { id });
    }
}

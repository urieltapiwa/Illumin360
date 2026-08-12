using Illumin360.Billing.Application.Abstractions;
using Illumin360.Billing.Domain;
using Illumin360.SharedKernel;

namespace Illumin360.Billing.Application.Billing;

/// <summary>A pricing plan.</summary>
public sealed record PlanDto(Guid Id, string Code, string Name, long PriceMinor, string Currency, string Interval, IReadOnlyList<string> Features);

/// <summary>A subscription.</summary>
public sealed record SubscriptionDto(Guid Id, Guid CustomerId, Guid PlanId, string PlanCode, string Status, DateTimeOffset CurrentPeriodEnd, string? CheckoutUrl);

/// <summary>An invoice.</summary>
public sealed record InvoiceDto(Guid Id, long AmountMinor, string Currency, string Status, DateTimeOffset PeriodStart, DateTimeOffset PeriodEnd, DateTimeOffset? PaidAt);

/// <summary>A customer's entitlements (the feature keys granted by their active plan).</summary>
public sealed record EntitlementsDto(Guid CustomerId, string? PlanCode, string Status, IReadOnlyList<string> Features);

/// <summary>Creates a pricing plan.</summary>
public sealed record CreatePlanCommand(string Code, string Name, long PriceMinor, string Currency, string Interval, IReadOnlyList<string>? Features) : ICommand<PlanDto>;

/// <summary>Lists active plans.</summary>
public sealed record ListPlansQuery : IQuery<IReadOnlyList<PlanDto>>;

/// <summary>Subscribes a customer to a plan (by code).</summary>
public sealed record SubscribeCommand(Guid CustomerId, string PlanCode) : ICommand<SubscriptionDto>;

/// <summary>Cancels a customer's subscription.</summary>
public sealed record CancelSubscriptionCommand(Guid CustomerId) : ICommand<SubscriptionDto>;

/// <summary>Gets a customer's current subscription.</summary>
public sealed record GetSubscriptionQuery(Guid CustomerId) : IQuery<SubscriptionDto>;

/// <summary>Lists a customer's invoices.</summary>
public sealed record ListInvoicesQuery(Guid CustomerId) : IQuery<IReadOnlyList<InvoiceDto>>;

/// <summary>Gets a customer's entitlements.</summary>
public sealed record GetEntitlementsQuery(Guid CustomerId) : IQuery<EntitlementsDto>;

/// <summary>Shared mapping.</summary>
internal static class BillingMap
{
    /// <summary>Projects a plan.</summary>
    /// <param name="p">The plan.</param>
    /// <returns>The DTO.</returns>
    public static PlanDto ToDto(Plan p) => new(p.Id, p.Code, p.Name, p.PriceMinor, p.Currency, p.Interval.ToString(), p.Features);

    /// <summary>Projects an invoice.</summary>
    /// <param name="i">The invoice.</param>
    /// <returns>The DTO.</returns>
    public static InvoiceDto ToDto(Invoice i) => new(i.Id, i.AmountMinor, i.Currency, i.Status.ToString(), i.PeriodStart, i.PeriodEnd, i.PaidAt);
}

/// <summary>Handles <see cref="CreatePlanCommand"/>.</summary>
/// <param name="repository">The billing repository.</param>
public sealed class CreatePlanCommandHandler(IBillingRepository repository) : ICommandHandler<CreatePlanCommand, PlanDto>
{
    private readonly IBillingRepository _repository = repository;

    /// <inheritdoc />
    public async Task<Result<PlanDto>> HandleAsync(CreatePlanCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        if (!Enum.TryParse<BillingInterval>(command.Interval, ignoreCase: true, out var interval))
        {
            return Error.Validation("plan.interval_invalid", "Interval must be 'Monthly' or 'Annual'.");
        }

        if (await _repository.GetPlanByCodeAsync(command.Code?.Trim().ToLowerInvariant() ?? string.Empty, cancellationToken).ConfigureAwait(false) is not null)
        {
            return Error.Conflict("plan.code_taken", "A plan with that code already exists.");
        }

        var plan = Plan.Create(command.Code!, command.Name, command.PriceMinor, command.Currency, interval, command.Features, DateTimeOffset.UtcNow);
        if (plan.IsFailure)
        {
            return plan.Error!;
        }

        _repository.AddPlan(plan.Value!);
        await _repository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return BillingMap.ToDto(plan.Value!);
    }
}

/// <summary>Handles <see cref="ListPlansQuery"/>.</summary>
/// <param name="repository">The billing repository.</param>
public sealed class ListPlansQueryHandler(IBillingRepository repository) : IQueryHandler<ListPlansQuery, IReadOnlyList<PlanDto>>
{
    private readonly IBillingRepository _repository = repository;

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<PlanDto>>> HandleAsync(ListPlansQuery query, CancellationToken cancellationToken)
    {
        var plans = await _repository.ListPlansAsync(cancellationToken).ConfigureAwait(false);
        return plans.Select(BillingMap.ToDto).ToList();
    }
}

/// <summary>
/// Handles <see cref="SubscribeCommand"/> — starts the recurring mandate at the provider, creates an active
/// subscription for the first period, and records a Paid invoice for it.
/// </summary>
/// <param name="repository">The billing repository.</param>
/// <param name="provider">The billing provider.</param>
public sealed class SubscribeCommandHandler(IBillingRepository repository, IBillingProvider provider) : ICommandHandler<SubscribeCommand, SubscriptionDto>
{
    private readonly IBillingRepository _repository = repository;
    private readonly IBillingProvider _provider = provider;

    /// <inheritdoc />
    public async Task<Result<SubscriptionDto>> HandleAsync(SubscribeCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var plan = await _repository.GetPlanByCodeAsync(command.PlanCode?.Trim().ToLowerInvariant() ?? string.Empty, cancellationToken).ConfigureAwait(false);
        if (plan is null)
        {
            return Error.NotFound("plan.not_found", "Plan not found.");
        }

        var existing = await _repository.GetActiveSubscriptionForCustomerAsync(command.CustomerId, cancellationToken).ConfigureAwait(false);
        if (existing is not null)
        {
            return Error.Conflict("subscription.exists", "The customer already has a subscription.");
        }

        var now = DateTimeOffset.UtcNow;
        var start = await _provider.StartSubscriptionAsync($"sub-{command.CustomerId}", plan.PriceMinor, plan.Currency, cancellationToken).ConfigureAwait(false);
        if (!start.Success)
        {
            return new Error("billing.start_failed", start.Error ?? "The billing provider could not start the subscription.");
        }

        var subscription = Subscription.Start(command.CustomerId, plan.Id, now, plan.NextPeriodEnd(now), now);
        if (subscription.IsFailure)
        {
            return subscription.Error!;
        }

        subscription.Value!.SetProviderRef(start.Reference, now);
        _repository.AddSubscription(subscription.Value!);

        var invoice = Invoice.Issue(subscription.Value!.Id, plan.PriceMinor, plan.Currency, now, subscription.Value!.CurrentPeriodEnd, now);
        invoice.MarkPaid(start.Reference, now);
        _repository.AddInvoice(invoice);

        await _repository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        var s = subscription.Value!;
        return new SubscriptionDto(s.Id, s.CustomerId, s.PlanId, plan.Code, s.Status.ToString(), s.CurrentPeriodEnd, start.CheckoutUrl);
    }
}

/// <summary>Handles <see cref="CancelSubscriptionCommand"/>.</summary>
/// <param name="repository">The billing repository.</param>
/// <param name="provider">The billing provider.</param>
public sealed class CancelSubscriptionCommandHandler(IBillingRepository repository, IBillingProvider provider) : ICommandHandler<CancelSubscriptionCommand, SubscriptionDto>
{
    private readonly IBillingRepository _repository = repository;
    private readonly IBillingProvider _provider = provider;

    /// <inheritdoc />
    public async Task<Result<SubscriptionDto>> HandleAsync(CancelSubscriptionCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var subscription = await _repository.GetActiveSubscriptionForCustomerAsync(command.CustomerId, cancellationToken).ConfigureAwait(false);
        if (subscription is null)
        {
            return Error.NotFound("subscription.not_found", "No active subscription for that customer.");
        }

        var cancelled = subscription.Cancel(DateTimeOffset.UtcNow);
        if (cancelled.IsFailure)
        {
            return cancelled.Error!;
        }

        if (!string.IsNullOrWhiteSpace(subscription.ProviderRef))
        {
            await _provider.CancelSubscriptionAsync(subscription.ProviderRef, cancellationToken).ConfigureAwait(false);
        }

        await _repository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        var plan = await _repository.GetPlanAsync(subscription.PlanId, cancellationToken).ConfigureAwait(false);
        return new SubscriptionDto(subscription.Id, subscription.CustomerId, subscription.PlanId, plan?.Code ?? string.Empty, subscription.Status.ToString(), subscription.CurrentPeriodEnd, null);
    }
}

/// <summary>Handles <see cref="GetSubscriptionQuery"/>.</summary>
/// <param name="repository">The billing repository.</param>
public sealed class GetSubscriptionQueryHandler(IBillingRepository repository) : IQueryHandler<GetSubscriptionQuery, SubscriptionDto>
{
    private readonly IBillingRepository _repository = repository;

    /// <inheritdoc />
    public async Task<Result<SubscriptionDto>> HandleAsync(GetSubscriptionQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        var s = await _repository.GetActiveSubscriptionForCustomerAsync(query.CustomerId, cancellationToken).ConfigureAwait(false);
        if (s is null)
        {
            return Error.NotFound("subscription.not_found", "No active subscription for that customer.");
        }

        var plan = await _repository.GetPlanAsync(s.PlanId, cancellationToken).ConfigureAwait(false);
        return new SubscriptionDto(s.Id, s.CustomerId, s.PlanId, plan?.Code ?? string.Empty, s.Status.ToString(), s.CurrentPeriodEnd, null);
    }
}

/// <summary>Handles <see cref="ListInvoicesQuery"/>.</summary>
/// <param name="repository">The billing repository.</param>
public sealed class ListInvoicesQueryHandler(IBillingRepository repository) : IQueryHandler<ListInvoicesQuery, IReadOnlyList<InvoiceDto>>
{
    private readonly IBillingRepository _repository = repository;

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<InvoiceDto>>> HandleAsync(ListInvoicesQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        var s = await _repository.GetActiveSubscriptionForCustomerAsync(query.CustomerId, cancellationToken).ConfigureAwait(false);
        if (s is null)
        {
            return new List<InvoiceDto>();
        }

        var invoices = await _repository.ListInvoicesForSubscriptionAsync(s.Id, cancellationToken).ConfigureAwait(false);
        return invoices.Select(BillingMap.ToDto).ToList();
    }
}

/// <summary>Handles <see cref="GetEntitlementsQuery"/> — the plan features gating functionality by plan.</summary>
/// <param name="repository">The billing repository.</param>
public sealed class GetEntitlementsQueryHandler(IBillingRepository repository) : IQueryHandler<GetEntitlementsQuery, EntitlementsDto>
{
    private readonly IBillingRepository _repository = repository;

    /// <inheritdoc />
    public async Task<Result<EntitlementsDto>> HandleAsync(GetEntitlementsQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        var s = await _repository.GetActiveSubscriptionForCustomerAsync(query.CustomerId, cancellationToken).ConfigureAwait(false);
        if (s is null || s.Status == SubscriptionStatus.Canceled)
        {
            return new EntitlementsDto(query.CustomerId, null, "none", []);
        }

        var plan = await _repository.GetPlanAsync(s.PlanId, cancellationToken).ConfigureAwait(false);

        // Entitlements apply while Active or PastDue (grace); a cancelled/absent subscription grants nothing.
        var features = s.Status is SubscriptionStatus.Active or SubscriptionStatus.PastDue or SubscriptionStatus.Trialing ? plan?.Features ?? [] : [];
        return new EntitlementsDto(query.CustomerId, plan?.Code, s.Status.ToString(), features);
    }
}

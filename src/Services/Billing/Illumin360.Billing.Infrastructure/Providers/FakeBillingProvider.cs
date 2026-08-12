using Illumin360.Billing.Application.Abstractions;

namespace Illumin360.Billing.Infrastructure.Providers;

/// <summary>
/// Deterministic no-op recurring-billing provider (default; local/tests). Moves no real money — every call
/// succeeds with a stable reference so the subscribe → invoice → renew flow is exercised end-to-end without a
/// live PSP. Real adapters (DPO/Flutterwave/N-Genius) replace this behind <see cref="IBillingProvider"/>.
/// </summary>
public sealed class FakeBillingProvider : IBillingProvider
{
    /// <inheritdoc />
    public Task<BillingResult> StartSubscriptionAsync(string idempotencyKey, long amountMinor, string currency, CancellationToken cancellationToken)
        => Task.FromResult(new BillingResult(true, $"fake-sub-{idempotencyKey}"));

    /// <inheritdoc />
    public Task<BillingResult> ChargeAsync(string idempotencyKey, string providerRef, long amountMinor, string currency, CancellationToken cancellationToken)
        => Task.FromResult(new BillingResult(true, $"fake-charge-{idempotencyKey}"));

    /// <inheritdoc />
    public Task<BillingResult> CancelSubscriptionAsync(string providerRef, CancellationToken cancellationToken)
        => Task.FromResult(new BillingResult(true, $"fake-cancel-{providerRef}"));
}

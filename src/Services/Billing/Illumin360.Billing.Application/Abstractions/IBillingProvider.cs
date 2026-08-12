namespace Illumin360.Billing.Application.Abstractions;

/// <summary>The outcome of a billing-provider operation.</summary>
/// <param name="Success">Whether it succeeded.</param>
/// <param name="Reference">The provider's reference (recurring token / charge id) for reconciliation.</param>
/// <param name="CheckoutUrl">A hosted-checkout URL when the first payment must be collected interactively.</param>
/// <param name="Error">A short failure reason when <paramref name="Success"/> is false.</param>
public sealed record BillingResult(bool Success, string Reference, string? CheckoutUrl = null, string? Error = null);

/// <summary>
/// Port for the recurring-billing provider (subscription collection). <b>Default is a fake (no real money).</b>
/// Real adapters (DPO — the NAD-capable one; Flutterwave; N-Genius) collect from the customer via a hosted
/// first payment + a stored token, then re-charge each cycle — they never handle raw card data (PCI SAQ-A).
/// Going live needs the provider's recurring feature enabled + credentials + the D2 sign-off.
/// </summary>
public interface IBillingProvider
{
    /// <summary>Starts a subscription: sets up the recurring mandate / first charge for a plan.</summary>
    /// <param name="idempotencyKey">Caller key (e.g. subscription id) — safe to retry.</param>
    /// <param name="amountMinor">The plan's per-cycle amount in minor units.</param>
    /// <param name="currency">ISO-4217 currency.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<BillingResult> StartSubscriptionAsync(string idempotencyKey, long amountMinor, string currency, CancellationToken cancellationToken);

    /// <summary>Charges one recurring cycle against the stored mandate/token.</summary>
    /// <param name="idempotencyKey">Caller key (e.g. invoice id) — safe to retry.</param>
    /// <param name="providerRef">The recurring reference from <see cref="StartSubscriptionAsync"/>.</param>
    /// <param name="amountMinor">Amount in minor units.</param>
    /// <param name="currency">ISO-4217 currency.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<BillingResult> ChargeAsync(string idempotencyKey, string providerRef, long amountMinor, string currency, CancellationToken cancellationToken);

    /// <summary>Cancels the recurring mandate at the provider.</summary>
    /// <param name="providerRef">The recurring reference.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<BillingResult> CancelSubscriptionAsync(string providerRef, CancellationToken cancellationToken);
}

namespace Illumin360.Payments.Application.Abstractions;

/// <summary>The outcome of a payment-provider operation.</summary>
/// <param name="Success">Whether the operation succeeded.</param>
/// <param name="Reference">The provider's reference for the movement (idempotency/reconciliation handle).</param>
/// <param name="Error">A short failure reason when <paramref name="Success"/> is false.</param>
public sealed record PaymentResult(bool Success, string Reference, string? Error = null);

/// <summary>
/// Port for the licensed payment provider (PSP) that actually moves money — held behind an interface so the
/// domain/ledger stay provider-agnostic (see the transaction-layer design doc). Phase 1 ships a deterministic
/// fake; a real adapter (Flutterwave / Stripe Connect, per decision D1) is Phase 2, swapped here with no
/// change to the flows. All methods are idempotent by <c>idempotencyKey</c>.
/// </summary>
public interface IPaymentProvider
{
    /// <summary>Holds (funds into escrow) an amount from the client for a milestone.</summary>
    /// <param name="idempotencyKey">Caller-generated key (e.g. milestone id) — safe to retry.</param>
    /// <param name="amountMinor">Amount in minor units.</param>
    /// <param name="currency">ISO-4217 currency.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<PaymentResult> CreateHoldAsync(string idempotencyKey, long amountMinor, string currency, CancellationToken cancellationToken);

    /// <summary>Releases a held amount to the talent (minus any platform fee handled by the caller).</summary>
    /// <param name="idempotencyKey">Caller-generated key — safe to retry.</param>
    /// <param name="holdReference">The reference returned by <see cref="CreateHoldAsync"/>.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<PaymentResult> ReleaseAsync(string idempotencyKey, string holdReference, CancellationToken cancellationToken);

    /// <summary>Refunds a held amount back to the client.</summary>
    /// <param name="idempotencyKey">Caller-generated key — safe to retry.</param>
    /// <param name="holdReference">The reference returned by <see cref="CreateHoldAsync"/>.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<PaymentResult> RefundAsync(string idempotencyKey, string holdReference, CancellationToken cancellationToken);
}

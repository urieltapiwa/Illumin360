namespace Illumin360.Payments.Application.Abstractions;

/// <summary>The outcome of a payment-provider operation.</summary>
/// <param name="Success">Whether the operation succeeded.</param>
/// <param name="Reference">The provider's reference for the movement (idempotency/reconciliation handle).</param>
/// <param name="Error">A short failure reason when <paramref name="Success"/> is false.</param>
public sealed record PaymentResult(bool Success, string Reference, string? Error = null);

/// <summary>Instruction to release held funds to the talent (transfer-to-destination).</summary>
/// <param name="IdempotencyKey">Caller-generated key (safe to retry).</param>
/// <param name="HoldReference">The reference returned by <c>CreateHoldAsync</c>.</param>
/// <param name="AmountMinor">Amount to release, in minor units.</param>
/// <param name="Currency">ISO-4217 currency.</param>
/// <param name="DestinationAccount">The talent's provider payout account (subaccount / connected id / bank ref).</param>
public sealed record ReleaseInstruction(string IdempotencyKey, string HoldReference, long AmountMinor, string Currency, string DestinationAccount);

/// <summary>Instruction to refund held funds to the client.</summary>
/// <param name="IdempotencyKey">Caller-generated key (safe to retry).</param>
/// <param name="HoldReference">The reference returned by <c>CreateHoldAsync</c>.</param>
/// <param name="AmountMinor">Amount to refund, in minor units.</param>
/// <param name="Currency">ISO-4217 currency.</param>
public sealed record RefundInstruction(string IdempotencyKey, string HoldReference, long AmountMinor, string Currency);

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

    /// <summary>Releases held funds to the talent's payout account (transfer-to-destination).</summary>
    /// <param name="instruction">The release instruction (hold ref + amount + currency + destination).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<PaymentResult> ReleaseAsync(ReleaseInstruction instruction, CancellationToken cancellationToken);

    /// <summary>Refunds held funds back to the client.</summary>
    /// <param name="instruction">The refund instruction (hold ref + amount + currency).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<PaymentResult> RefundAsync(RefundInstruction instruction, CancellationToken cancellationToken);
}

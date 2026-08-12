using Illumin360.Payments.Application.Abstractions;

namespace Illumin360.Payments.Infrastructure;

/// <summary>
/// Deterministic, no-op payment provider for Phase 1 / local / tests — it moves no real money. Every call
/// succeeds and returns a stable reference derived from the idempotency key, so the whole contract → fund →
/// submit → approve flow is exercised end-to-end without a live PSP. The real adapter (Flutterwave / Stripe
/// Connect, per decision D1) replaces this in Phase 2 behind the same <see cref="IPaymentProvider"/> port.
/// </summary>
public sealed class FakePaymentProvider : IPaymentProvider
{
    /// <inheritdoc />
    public Task<PaymentResult> CreateHoldAsync(string idempotencyKey, long amountMinor, string currency, CancellationToken cancellationToken)
        => Task.FromResult(new PaymentResult(true, $"fake-hold-{idempotencyKey}"));

    /// <inheritdoc />
    public Task<PaymentResult> ReleaseAsync(ReleaseInstruction instruction, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(instruction);
        return Task.FromResult(new PaymentResult(true, $"fake-release-{instruction.IdempotencyKey}"));
    }

    /// <inheritdoc />
    public Task<PaymentResult> RefundAsync(RefundInstruction instruction, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(instruction);
        return Task.FromResult(new PaymentResult(true, $"fake-refund-{instruction.IdempotencyKey}"));
    }
}

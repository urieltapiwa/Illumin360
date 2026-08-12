namespace Illumin360.Payments.Application.Abstractions;

/// <summary>
/// Marketplace commercial settings. The platform take-rate (commission) is charged on each milestone release:
/// the talent receives the milestone amount minus the fee, and the fee is recorded as a <c>Fee</c> ledger
/// movement. Default 0% preserves the no-fee behaviour; set via <c>Payments:PlatformFeePercent</c>.
/// </summary>
public sealed record MarketplaceOptions
{
    /// <summary>Platform commission as a percentage of the milestone amount (0–100). Default 0 (no fee).</summary>
    public decimal PlatformFeePercent { get; init; }

    /// <summary>Computes the fee (in minor units) for a milestone amount, floored, clamped to [0, amount].</summary>
    /// <param name="amountMinor">The milestone amount in minor units.</param>
    /// <returns>The platform fee in minor units.</returns>
    public long FeeFor(long amountMinor)
    {
        var pct = Math.Clamp(PlatformFeePercent, 0m, 100m);
        var fee = (long)Math.Floor(amountMinor * pct / 100m);
        return Math.Clamp(fee, 0, amountMinor);
    }
}

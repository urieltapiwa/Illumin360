using Illumin360.SharedKernel;

namespace Illumin360.Payments.Domain;

/// <summary>KYC/verification state of a talent's payout destination.</summary>
public enum PayoutAccountStatus
{
    /// <summary>Registered but not yet verified — cannot receive payouts.</summary>
    Pending,

    /// <summary>Verified — releases may pay out to it.</summary>
    Verified,
}

/// <summary>
/// A talent's payout destination at the payment provider (subaccount / connected-account id / bank reference).
/// A milestone release only pays out to a <see cref="PayoutAccountStatus.Verified"/> account. We store the
/// provider's reference, never raw bank details (those live at the provider). One per talent.
/// </summary>
public sealed class PayoutAccount : Entity<Guid>
{
    private PayoutAccount(Guid id)
        : base(id)
    {
    }

    /// <summary>The talent this payout account belongs to.</summary>
    public Guid TalentId { get; private set; }

    /// <summary>The provider's payout reference (subaccount / connected-account id).</summary>
    public string ProviderAccount { get; private set; } = string.Empty;

    /// <summary>Verification status.</summary>
    public PayoutAccountStatus Status { get; private set; }

    /// <summary>When created (UTC).</summary>
    public DateTimeOffset CreatedAt { get; private set; }

    /// <summary>When last updated (UTC).</summary>
    public DateTimeOffset UpdatedAt { get; private set; }

    /// <summary>Registers a talent's payout account (starts Pending).</summary>
    /// <param name="talentId">The talent id.</param>
    /// <param name="providerAccount">The provider payout reference (required).</param>
    /// <param name="now">Creation timestamp (UTC).</param>
    /// <returns>The payout account, or a validation error.</returns>
    public static Result<PayoutAccount> Register(Guid talentId, string providerAccount, DateTimeOffset now)
    {
        if (talentId == Guid.Empty)
        {
            return Error.Validation("payout.talent_required", "A talent id is required.");
        }

        if (string.IsNullOrWhiteSpace(providerAccount) || providerAccount.Length > 200)
        {
            return Error.Validation("payout.account_invalid", "A provider payout reference (≤ 200 chars) is required.");
        }

        return new PayoutAccount(Guid.NewGuid())
        {
            TalentId = talentId,
            ProviderAccount = providerAccount.Trim(),
            Status = PayoutAccountStatus.Pending,
            CreatedAt = now,
            UpdatedAt = now,
        };
    }

    /// <summary>Marks the account verified (KYC passed).</summary>
    /// <param name="now">Reference time (UTC).</param>
    public void Verify(DateTimeOffset now)
    {
        Status = PayoutAccountStatus.Verified;
        UpdatedAt = now;
    }

    /// <summary>Updates the provider reference (resets to Pending re-verification).</summary>
    /// <param name="providerAccount">The new reference.</param>
    /// <param name="now">Reference time (UTC).</param>
    public void UpdateReference(string providerAccount, DateTimeOffset now)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(providerAccount);
        ProviderAccount = providerAccount.Trim();
        Status = PayoutAccountStatus.Pending;
        UpdatedAt = now;
    }
}

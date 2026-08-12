using Illumin360.SharedKernel;

namespace Illumin360.Billing.Domain;

/// <summary>Invoice lifecycle.</summary>
public enum InvoiceStatus
{
    /// <summary>Issued, awaiting a charge.</summary>
    Open,

    /// <summary>Charged successfully.</summary>
    Paid,

    /// <summary>The charge failed.</summary>
    Failed,

    /// <summary>Voided.</summary>
    Void,
}

/// <summary>An invoice for one billing period of a <see cref="Subscription"/>. Append-only history of charges.</summary>
public sealed class Invoice : Entity<Guid>
{
    private Invoice(Guid id)
        : base(id)
    {
    }

    /// <summary>The subscription billed.</summary>
    public Guid SubscriptionId { get; private set; }

    /// <summary>Amount in minor units.</summary>
    public long AmountMinor { get; private set; }

    /// <summary>ISO-4217 currency.</summary>
    public string Currency { get; private set; } = string.Empty;

    /// <summary>Invoice status.</summary>
    public InvoiceStatus Status { get; private set; }

    /// <summary>Billed period start (UTC).</summary>
    public DateTimeOffset PeriodStart { get; private set; }

    /// <summary>Billed period end (UTC).</summary>
    public DateTimeOffset PeriodEnd { get; private set; }

    /// <summary>When issued (UTC).</summary>
    public DateTimeOffset IssuedAt { get; private set; }

    /// <summary>When paid (UTC), if applicable.</summary>
    public DateTimeOffset? PaidAt { get; private set; }

    /// <summary>Provider charge reference.</summary>
    public string? ProviderRef { get; private set; }

    /// <summary>Issues an open invoice for a period.</summary>
    /// <param name="subscriptionId">The subscription.</param>
    /// <param name="amountMinor">Amount in minor units.</param>
    /// <param name="currency">ISO-4217 currency.</param>
    /// <param name="periodStart">Period start (UTC).</param>
    /// <param name="periodEnd">Period end (UTC).</param>
    /// <param name="now">Issue timestamp (UTC).</param>
    /// <returns>The invoice.</returns>
    public static Invoice Issue(Guid subscriptionId, long amountMinor, string currency, DateTimeOffset periodStart, DateTimeOffset periodEnd, DateTimeOffset now)
        => new(Guid.NewGuid())
        {
            SubscriptionId = subscriptionId,
            AmountMinor = amountMinor,
            Currency = currency,
            Status = InvoiceStatus.Open,
            PeriodStart = periodStart,
            PeriodEnd = periodEnd,
            IssuedAt = now,
        };

    /// <summary>Marks the invoice paid.</summary>
    /// <param name="providerRef">The provider charge reference.</param>
    /// <param name="now">Reference time (UTC).</param>
    public void MarkPaid(string providerRef, DateTimeOffset now)
    {
        Status = InvoiceStatus.Paid;
        ProviderRef = providerRef;
        PaidAt = now;
    }

    /// <summary>Marks the invoice failed.</summary>
    public void MarkFailed() => Status = InvoiceStatus.Failed;
}

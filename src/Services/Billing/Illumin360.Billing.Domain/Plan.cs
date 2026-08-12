using Illumin360.SharedKernel;

namespace Illumin360.Billing.Domain;

/// <summary>Billing cycle length.</summary>
public enum BillingInterval
{
    /// <summary>Billed monthly.</summary>
    Monthly,

    /// <summary>Billed annually.</summary>
    Annual,
}

/// <summary>
/// A subscription pricing plan the platform sells to its customers (employers). Price is integer minor units +
/// ISO-4217 currency. <see cref="Features"/> are entitlement keys used to gate functionality by plan.
/// </summary>
public sealed class Plan : Entity<Guid>
{
    private Plan(Guid id)
        : base(id)
    {
    }

    /// <summary>Stable code/slug (unique), e.g. <c>pro</c>.</summary>
    public string Code { get; private set; } = string.Empty;

    /// <summary>Display name.</summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>Price per cycle in minor units.</summary>
    public long PriceMinor { get; private set; }

    /// <summary>ISO-4217 currency.</summary>
    public string Currency { get; private set; } = string.Empty;

    /// <summary>Billing interval.</summary>
    public BillingInterval Interval { get; private set; }

    /// <summary>Entitlement keys granted by this plan (stored pipe-joined).</summary>
    public IReadOnlyList<string> Features =>
        string.IsNullOrEmpty(FeaturesCsv) ? [] : FeaturesCsv.Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    /// <summary>Whether the plan can be subscribed to.</summary>
    public bool Active { get; private set; }

    /// <summary>When created (UTC).</summary>
    public DateTimeOffset CreatedAt { get; private set; }

    /// <summary>Pipe-joined feature keys (persisted).</summary>
    public string FeaturesCsv { get; private set; } = string.Empty;

    /// <summary>Creates an active plan.</summary>
    /// <param name="code">Stable code (required, ≤ 40 chars).</param>
    /// <param name="name">Display name (required, ≤ 120 chars).</param>
    /// <param name="priceMinor">Price per cycle in minor units (≥ 0).</param>
    /// <param name="currency">3-letter ISO currency.</param>
    /// <param name="interval">Billing interval.</param>
    /// <param name="features">Entitlement keys.</param>
    /// <param name="now">Creation timestamp (UTC).</param>
    /// <returns>The plan, or a validation error.</returns>
    public static Result<Plan> Create(string code, string name, long priceMinor, string currency, BillingInterval interval, IEnumerable<string>? features, DateTimeOffset now)
    {
        if (string.IsNullOrWhiteSpace(code) || code.Length > 40)
        {
            return Error.Validation("plan.code_invalid", "A plan code (≤ 40 chars) is required.");
        }

        if (string.IsNullOrWhiteSpace(name) || name.Length > 120)
        {
            return Error.Validation("plan.name_invalid", "A plan name (≤ 120 chars) is required.");
        }

        if (priceMinor < 0)
        {
            return Error.Validation("plan.price_invalid", "Price cannot be negative.");
        }

        if (string.IsNullOrWhiteSpace(currency) || currency.Trim().Length != 3)
        {
            return Error.Validation("plan.currency_invalid", "A 3-letter ISO currency code is required.");
        }

        var featureList = (features ?? []).Where(f => !string.IsNullOrWhiteSpace(f)).Select(f => f.Trim()).Distinct(StringComparer.OrdinalIgnoreCase);
        return new Plan(Guid.NewGuid())
        {
            Code = code.Trim().ToLowerInvariant(),
            Name = name.Trim(),
            PriceMinor = priceMinor,
            Currency = currency.Trim().ToUpperInvariant(),
            Interval = interval,
            FeaturesCsv = string.Join('|', featureList),
            Active = true,
            CreatedAt = now,
        };
    }

    /// <summary>Advances a period start by one interval.</summary>
    /// <param name="from">The period start.</param>
    /// <returns>The next period end.</returns>
    public DateTimeOffset NextPeriodEnd(DateTimeOffset from) => Interval == BillingInterval.Annual ? from.AddYears(1) : from.AddMonths(1);
}

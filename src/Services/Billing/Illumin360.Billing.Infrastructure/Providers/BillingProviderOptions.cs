namespace Illumin360.Billing.Infrastructure.Providers;

/// <summary>Which recurring-billing provider collects subscriptions. Fake is the default (no real money).</summary>
public enum BillingProviderKind
{
    /// <summary>Deterministic no-op (default).</summary>
    Fake,

    /// <summary>DPO Group — the NAD-capable option for Namibian customers.</summary>
    Dpo,

    /// <summary>Flutterwave — Payment Plans / tokenized charges (ZAR/USD for the Namibian context; no NAD).</summary>
    Flutterwave,

    /// <summary>Network International N-Genius — recurring service (AED-centric; no documented NAD/ZAR).</summary>
    NGenius,
}

/// <summary>
/// Recurring-billing provider selection + credentials. <b>Default Fake / disabled</b> — no external call until a
/// provider is chosen, its recurring feature is enabled by the provider, credentials are supplied, and the D2
/// sign-off is in hand.
/// </summary>
public sealed record BillingProviderOptions
{
    /// <summary>The provider.</summary>
    public BillingProviderKind Provider { get; init; } = BillingProviderKind.Fake;

    /// <summary>Master switch — a real adapter is used only when true AND a <see cref="BaseUrl"/> is set.</summary>
    public bool Enabled { get; init; }

    /// <summary>Provider API base URL.</summary>
    public string? BaseUrl { get; init; }

    /// <summary>Provider secret/API key.</summary>
    public string? SecretKey { get; init; }

    /// <summary>Provider-specific extra (N-Genius outlet ref; DPO company token).</summary>
    public string? Extra { get; init; }

    /// <summary>Whether a real provider adapter should be used.</summary>
    public bool UseReal => Provider != BillingProviderKind.Fake && Enabled && !string.IsNullOrWhiteSpace(BaseUrl);
}

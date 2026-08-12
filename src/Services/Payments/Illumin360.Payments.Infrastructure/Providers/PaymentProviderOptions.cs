namespace Illumin360.Payments.Infrastructure.Providers;

/// <summary>Which real payment provider backs the marketplace (decision D1). Fake is the Phase-1 default.</summary>
public enum PaymentProviderKind
{
    /// <summary>Deterministic no-op (Phase 1 default; moves no real money).</summary>
    Fake,

    /// <summary>Flutterwave — pan-African; the recommended provider for a Namibia/SADC launch.</summary>
    Flutterwave,

    /// <summary>Stripe Connect — for a US/EU pilot (not available for Namibian accounts).</summary>
    Stripe,

    /// <summary>Network International N-Genius — MEA gateway.</summary>
    NGenius,

    /// <summary>DPO Group — Southern/East-African acquiring.</summary>
    Dpo,
}

/// <summary>
/// Payment-provider selection + credentials. <b>Default is <see cref="PaymentProviderKind.Fake"/> and
/// <see cref="Enabled"/> = false</b>, so no real money moves and no external call is made until a real
/// provider is chosen, credentials supplied, and the legal sign-off (D2) is in hand.
/// </summary>
public sealed record PaymentProviderOptions
{
    /// <summary>The provider to use.</summary>
    public PaymentProviderKind Provider { get; init; } = PaymentProviderKind.Fake;

    /// <summary>Master switch — a real adapter is used only when this is true AND a <see cref="BaseUrl"/> is set.</summary>
    public bool Enabled { get; init; }

    /// <summary>The provider API base URL (sandbox or live).</summary>
    public string? BaseUrl { get; init; }

    /// <summary>The provider secret/API key.</summary>
    public string? SecretKey { get; init; }

    /// <summary>Provider-specific extra (e.g. N-Genius outlet reference, DPO company token).</summary>
    public string? Extra { get; init; }

    /// <summary>Whether a real provider adapter should be used (a real provider, enabled, and configured).</summary>
    public bool UseReal => Provider != PaymentProviderKind.Fake && Enabled && !string.IsNullOrWhiteSpace(BaseUrl);
}

using Illumin360.SharedKernel;

namespace Illumin360.Recruitment.Domain;

/// <summary>
/// An employment offer extended to a candidate for a specific application. Owned and migration-managed by
/// the Recruitment service. The lifecycle is draft → sent → accepted/declined, with withdraw available
/// before a decision; terminal states reject further transitions.
/// </summary>
public sealed class Offer : Entity<OfferId>
{
    // EF Core materialisation constructor.
    private Offer(OfferId id)
        : base(id)
    {
    }

    /// <summary>The application this offer is for.</summary>
    public Guid ApplicationId { get; private init; }

    /// <summary>Role title captured on the offer.</summary>
    public string Title { get; private set; } = string.Empty;

    /// <summary>Annual salary amount.</summary>
    public decimal SalaryAmount { get; private set; }

    /// <summary>ISO-ish currency code (e.g. <c>NAD</c>).</summary>
    public string Currency { get; private set; } = "NAD";

    /// <summary>Proposed start date.</summary>
    public DateOnly StartDate { get; private set; }

    /// <summary>Offer status.</summary>
    public OfferStatus Status { get; private set; }

    /// <summary>Optional notes / terms.</summary>
    public string? Notes { get; private set; }

    /// <summary>When the offer was created (UTC).</summary>
    public DateTimeOffset CreatedAt { get; private set; }

    /// <summary>When the offer reached a terminal decision (UTC), if applicable.</summary>
    public DateTimeOffset? DecidedAt { get; private set; }

    /// <summary>The name the candidate typed as their e-signature, if signed.</summary>
    public string? SignedByName { get; private set; }

    /// <summary>When the offer was e-signed (UTC), if signed.</summary>
    public DateTimeOffset? SignedAt { get; private set; }

    /// <summary>Drafts a new offer for an application.</summary>
    /// <param name="applicationId">The application (required).</param>
    /// <param name="title">Role title (required).</param>
    /// <param name="salaryAmount">Salary amount (must be &gt; 0).</param>
    /// <param name="currency">Currency code (defaults to NAD when blank).</param>
    /// <param name="startDate">Proposed start date (required).</param>
    /// <param name="notes">Optional notes (≤ 2000 chars).</param>
    /// <param name="createdAt">Creation timestamp (UTC).</param>
    /// <returns>The drafted offer, or a validation error.</returns>
    public static Result<Offer> Draft(Guid applicationId, string title, decimal salaryAmount, string? currency, DateOnly startDate, string? notes, DateTimeOffset createdAt)
    {
        if (applicationId == Guid.Empty)
        {
            return Error.Validation("offer.application_required", "An application id is required.");
        }

        if (string.IsNullOrWhiteSpace(title))
        {
            return Error.Validation("offer.title_required", "A role title is required.");
        }

        if (salaryAmount <= 0)
        {
            return Error.Validation("offer.salary_invalid", "The salary must be greater than zero.");
        }

        if (startDate == default)
        {
            return Error.Validation("offer.start_date_required", "A start date is required.");
        }

        if (notes is { Length: > 2000 })
        {
            return Error.Validation("offer.notes_too_long", "Notes must be 2000 characters or fewer.");
        }

        return new Offer(OfferId.New())
        {
            ApplicationId = applicationId,
            Title = title.Trim(),
            SalaryAmount = salaryAmount,
            Currency = string.IsNullOrWhiteSpace(currency) ? "NAD" : currency.Trim().ToUpperInvariant(),
            StartDate = startDate,
            Status = OfferStatus.Draft,
            Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim(),
            CreatedAt = createdAt,
        };
    }

    /// <summary>Extends the offer to the candidate (draft → sent).</summary>
    /// <returns>Success, or a conflict if not in draft.</returns>
    public Result<Offer> Send()
    {
        if (Status != OfferStatus.Draft)
        {
            return Error.Conflict("offer.not_draft", "Only a draft offer can be sent.");
        }

        Status = OfferStatus.Sent;
        return this;
    }

    /// <summary>Records the candidate accepting the offer (sent → accepted).</summary>
    /// <param name="decidedAt">Decision timestamp (UTC).</param>
    /// <returns>Success, or a conflict if not awaiting a decision.</returns>
    public Result<Offer> Accept(DateTimeOffset decidedAt) => Decide(OfferStatus.Accepted, decidedAt);

    /// <summary>Records the candidate declining the offer (sent → declined).</summary>
    /// <param name="decidedAt">Decision timestamp (UTC).</param>
    /// <returns>Success, or a conflict if not awaiting a decision.</returns>
    public Result<Offer> Decline(DateTimeOffset decidedAt) => Decide(OfferStatus.Declined, decidedAt);

    /// <summary>
    /// Records the candidate e-signing the offer, which accepts it (sent → accepted). Captures the typed
    /// signature name and timestamp.
    /// </summary>
    /// <param name="signerName">The name the candidate typed as their signature (required).</param>
    /// <param name="at">Signature timestamp (UTC).</param>
    /// <returns>Success, or a validation/conflict error.</returns>
    public Result<Offer> Sign(string signerName, DateTimeOffset at)
    {
        if (string.IsNullOrWhiteSpace(signerName))
        {
            return Error.Validation("offer.signer_required", "A signature name is required.");
        }

        if (Status != OfferStatus.Sent)
        {
            return Error.Conflict("offer.not_sent", "Only a sent offer can be signed.");
        }

        Status = OfferStatus.Accepted;
        DecidedAt = at;
        SignedByName = signerName.Trim();
        SignedAt = at;
        return this;
    }

    /// <summary>Withdraws the offer before a decision (draft/sent → withdrawn).</summary>
    /// <param name="decidedAt">Withdrawal timestamp (UTC).</param>
    /// <returns>Success, or a conflict if already decided or withdrawn.</returns>
    public Result<Offer> Withdraw(DateTimeOffset decidedAt)
    {
        if (Status is OfferStatus.Accepted or OfferStatus.Declined or OfferStatus.Withdrawn)
        {
            return Error.Conflict("offer.already_final", "A decided or withdrawn offer cannot be withdrawn.");
        }

        Status = OfferStatus.Withdrawn;
        DecidedAt = decidedAt;
        return this;
    }

    /// <summary>Rehydrates a fully-specified offer for demo seeding / import.</summary>
    /// <param name="id">Identity.</param>
    /// <param name="applicationId">Application id.</param>
    /// <param name="title">Title.</param>
    /// <param name="salaryAmount">Salary amount.</param>
    /// <param name="currency">Currency.</param>
    /// <param name="startDate">Start date.</param>
    /// <param name="status">Status.</param>
    /// <param name="notes">Notes.</param>
    /// <param name="createdAt">Creation timestamp (UTC).</param>
    /// <param name="decidedAt">Decision timestamp (UTC), if any.</param>
    /// <param name="signedByName">E-signature name, if signed.</param>
    /// <param name="signedAt">E-signature timestamp (UTC), if signed.</param>
    /// <returns>The hydrated offer.</returns>
    public static Offer Seed(Guid id, Guid applicationId, string title, decimal salaryAmount, string currency, DateOnly startDate, OfferStatus status, string? notes, DateTimeOffset createdAt, DateTimeOffset? decidedAt, string? signedByName = null, DateTimeOffset? signedAt = null)
        => new(new OfferId(id))
        {
            ApplicationId = applicationId,
            Title = title,
            SalaryAmount = salaryAmount,
            Currency = currency,
            StartDate = startDate,
            Status = status,
            Notes = notes,
            CreatedAt = createdAt,
            DecidedAt = decidedAt,
            SignedByName = signedByName,
            SignedAt = signedAt,
        };

    private Result<Offer> Decide(OfferStatus decision, DateTimeOffset decidedAt)
    {
        if (Status != OfferStatus.Sent)
        {
            return Error.Conflict("offer.not_sent", "Only a sent offer can be accepted or declined.");
        }

        Status = decision;
        DecidedAt = decidedAt;
        return this;
    }
}

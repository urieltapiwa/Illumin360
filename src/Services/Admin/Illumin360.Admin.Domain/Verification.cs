using Illumin360.SharedKernel;

namespace Illumin360.Admin.Domain;

/// <summary>Lifecycle state of a verification request.</summary>
public enum VerificationStatus
{
    /// <summary>Awaiting an admin decision.</summary>
    Pending,

    /// <summary>Approved by an admin.</summary>
    Approved,

    /// <summary>Rejected by an admin.</summary>
    Rejected,
}

/// <summary>
/// A pending compliance/identity verification (company or talent) that an administrator reviews and
/// approves or rejects. Aggregate root of the admin verification-queue bounded context.
/// </summary>
public sealed class Verification : Entity<VerificationId>
{
    private Verification(VerificationId id)
        : base(id)
    {
    }

    private Verification(VerificationId id, string entity, string kind, string riskLevel, string submittedLabel)
        : base(id)
    {
        Entity = entity;
        Kind = kind;
        RiskLevel = riskLevel;
        SubmittedLabel = submittedLabel;
        Status = VerificationStatus.Pending;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>The entity under review (company or person name).</summary>
    public string Entity { get; private set; } = string.Empty;

    /// <summary>Kind of verification (e.g. "Company verification", "Talent verification").</summary>
    public string Kind { get; private set; } = string.Empty;

    /// <summary>Risk band (Low / Medium / High).</summary>
    public string RiskLevel { get; private set; } = string.Empty;

    /// <summary>Relative submitted-time label (e.g. "34m ago").</summary>
    public string SubmittedLabel { get; private set; } = string.Empty;

    /// <summary>Current decision state.</summary>
    public VerificationStatus Status { get; private set; }

    /// <summary>Username of the admin who decided (null while pending).</summary>
    public string? DecidedBy { get; private set; }

    /// <summary>When the decision was made (null while pending).</summary>
    public DateTimeOffset? DecidedAt { get; private set; }

    /// <summary>When the verification was created (UTC).</summary>
    public DateTimeOffset CreatedAt { get; private set; }

    /// <summary>Submits a new verification into the pending queue.</summary>
    /// <param name="entity">Entity under review.</param>
    /// <param name="kind">Kind of verification.</param>
    /// <param name="riskLevel">Risk band.</param>
    /// <param name="submittedLabel">Relative submitted-time label.</param>
    /// <returns>A successful result with the verification, or a validation error.</returns>
    public static Result<Verification> Submit(string entity, string kind, string riskLevel, string submittedLabel)
    {
        if (string.IsNullOrWhiteSpace(entity))
        {
            return Error.Validation("verification.entity_required", "Entity is required.");
        }

        if (string.IsNullOrWhiteSpace(kind))
        {
            return Error.Validation("verification.kind_required", "Kind is required.");
        }

        return new Verification(
            VerificationId.New(), entity.Trim(), kind.Trim(), string.IsNullOrWhiteSpace(riskLevel) ? "Low" : riskLevel.Trim(), submittedLabel?.Trim() ?? string.Empty);
    }

    /// <summary>Rehydrates a verification from storage/seed with a fixed identity (raises no event).</summary>
    /// <param name="id">Identity.</param>
    /// <param name="entity">Entity under review.</param>
    /// <param name="kind">Kind of verification.</param>
    /// <param name="riskLevel">Risk band.</param>
    /// <param name="submittedLabel">Relative submitted-time label.</param>
    /// <param name="createdAt">Creation timestamp (UTC).</param>
    /// <returns>The hydrated, pending verification.</returns>
    public static Verification Seed(
        Guid id, string entity, string kind, string riskLevel, string submittedLabel, DateTimeOffset createdAt)
        => new(new VerificationId(id))
        {
            Entity = entity,
            Kind = kind,
            RiskLevel = riskLevel,
            SubmittedLabel = submittedLabel,
            Status = VerificationStatus.Pending,
            CreatedAt = createdAt,
        };

    /// <summary>Approves the verification.</summary>
    /// <param name="decidedBy">Username of the deciding admin.</param>
    /// <returns>Success, or a conflict error if it is not pending.</returns>
    public Result<Verification> Approve(string decidedBy) => Decide(VerificationStatus.Approved, decidedBy);

    /// <summary>Rejects the verification.</summary>
    /// <param name="decidedBy">Username of the deciding admin.</param>
    /// <returns>Success, or a conflict error if it is not pending.</returns>
    public Result<Verification> Reject(string decidedBy) => Decide(VerificationStatus.Rejected, decidedBy);

    private Result<Verification> Decide(VerificationStatus outcome, string decidedBy)
    {
        if (Status != VerificationStatus.Pending)
        {
            return Error.Conflict("verification.already_decided", $"Verification is already {Status}.");
        }

        Status = outcome;
        DecidedBy = string.IsNullOrWhiteSpace(decidedBy) ? "admin" : decidedBy.Trim();
        DecidedAt = DateTimeOffset.UtcNow;
        Raise(new VerificationDecided(Id, Entity, outcome.ToString(), DecidedBy, DecidedAt.Value));
        return this;
    }
}

/// <summary>Raised when a verification is approved or rejected.</summary>
/// <param name="VerificationId">The verification identity.</param>
/// <param name="Entity">Entity under review.</param>
/// <param name="Outcome">"Approved" or "Rejected".</param>
/// <param name="DecidedBy">Deciding admin username.</param>
/// <param name="OccurredOn">When the decision occurred (UTC).</param>
public sealed record VerificationDecided(
    VerificationId VerificationId, string Entity, string Outcome, string DecidedBy, DateTimeOffset OccurredOn) : IDomainEvent;

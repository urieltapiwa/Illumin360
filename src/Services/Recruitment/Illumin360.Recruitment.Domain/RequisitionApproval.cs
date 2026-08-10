using Illumin360.SharedKernel;

namespace Illumin360.Recruitment.Domain;

/// <summary>Approval lifecycle state of a requisition.</summary>
public enum ApprovalStatus
{
    /// <summary>Not yet submitted for approval.</summary>
    Draft,

    /// <summary>Submitted and awaiting an approver's decision.</summary>
    Submitted,

    /// <summary>Approved — the requisition may be published.</summary>
    Approved,

    /// <summary>Rejected — may be revised and resubmitted.</summary>
    Rejected,
}

/// <summary>
/// Service-owned approval workflow for a (externally-seeded) requisition, keyed 1:1 by request id.
/// Lifecycle: draft → submitted → approved/rejected; a rejected requisition may be resubmitted.
/// </summary>
public sealed class RequisitionApproval : Entity<Guid>
{
    private RequisitionApproval(Guid id)
        : base(id)
    {
    }

    /// <summary>The requisition under approval.</summary>
    public Guid RequestId { get; private init; }

    /// <summary>Current approval status.</summary>
    public ApprovalStatus Status { get; private set; }

    /// <summary>Who decided (approved/rejected), if applicable.</summary>
    public string? Approver { get; private set; }

    /// <summary>Reason captured on rejection, if applicable.</summary>
    public string? Reason { get; private set; }

    /// <summary>When last submitted (UTC), if applicable.</summary>
    public DateTimeOffset? SubmittedAt { get; private set; }

    /// <summary>When last decided (UTC), if applicable.</summary>
    public DateTimeOffset? DecidedAt { get; private set; }

    /// <summary>Creates a draft approval record for a requisition.</summary>
    /// <param name="requestId">The requisition (required).</param>
    /// <returns>The draft approval, or a validation error.</returns>
    public static Result<RequisitionApproval> Create(Guid requestId)
    {
        if (requestId == Guid.Empty)
        {
            return Error.Validation("requisition.request_required", "A request id is required.");
        }

        return new RequisitionApproval(Guid.NewGuid()) { RequestId = requestId, Status = ApprovalStatus.Draft };
    }

    /// <summary>Rehydrates an approval for import/seeding.</summary>
    /// <param name="id">Identity.</param>
    /// <param name="requestId">Request id.</param>
    /// <param name="status">Status.</param>
    /// <param name="approver">Approver.</param>
    /// <param name="reason">Rejection reason.</param>
    /// <param name="submittedAt">Submitted timestamp (UTC).</param>
    /// <param name="decidedAt">Decision timestamp (UTC).</param>
    /// <returns>The hydrated approval.</returns>
    public static RequisitionApproval Seed(Guid id, Guid requestId, ApprovalStatus status, string? approver, string? reason, DateTimeOffset? submittedAt, DateTimeOffset? decidedAt)
        => new(id)
        {
            RequestId = requestId,
            Status = status,
            Approver = approver,
            Reason = reason,
            SubmittedAt = submittedAt,
            DecidedAt = decidedAt,
        };

    /// <summary>Submits the requisition for approval (draft/rejected → submitted).</summary>
    /// <param name="at">Submission timestamp (UTC).</param>
    /// <returns>Success, or a conflict if already submitted/approved.</returns>
    public Result<RequisitionApproval> Submit(DateTimeOffset at)
    {
        if (Status is ApprovalStatus.Submitted or ApprovalStatus.Approved)
        {
            return Error.Conflict("approval.not_submittable", "Only a draft or rejected requisition can be submitted.");
        }

        Status = ApprovalStatus.Submitted;
        SubmittedAt = at;
        Approver = null;
        Reason = null;
        DecidedAt = null;
        return this;
    }

    /// <summary>Approves a submitted requisition (submitted → approved).</summary>
    /// <param name="approver">The approver's name.</param>
    /// <param name="at">Decision timestamp (UTC).</param>
    /// <returns>Success, or a validation/conflict error.</returns>
    public Result<RequisitionApproval> Approve(string approver, DateTimeOffset at)
    {
        if (string.IsNullOrWhiteSpace(approver))
        {
            return Error.Validation("approval.approver_required", "An approver name is required.");
        }

        if (Status != ApprovalStatus.Submitted)
        {
            return Error.Conflict("approval.not_pending", "Only a submitted requisition can be approved.");
        }

        Status = ApprovalStatus.Approved;
        Approver = approver.Trim();
        Reason = null;
        DecidedAt = at;
        return this;
    }

    /// <summary>Rejects a submitted requisition (submitted → rejected).</summary>
    /// <param name="approver">The approver's name.</param>
    /// <param name="reason">Rejection reason (required, ≤ 500 chars).</param>
    /// <param name="at">Decision timestamp (UTC).</param>
    /// <returns>Success, or a validation/conflict error.</returns>
    public Result<RequisitionApproval> Reject(string approver, string reason, DateTimeOffset at)
    {
        if (string.IsNullOrWhiteSpace(approver))
        {
            return Error.Validation("approval.approver_required", "An approver name is required.");
        }

        if (string.IsNullOrWhiteSpace(reason))
        {
            return Error.Validation("approval.reason_required", "A rejection reason is required.");
        }

        if (reason.Length > 500)
        {
            return Error.Validation("approval.reason_too_long", "A rejection reason must be 500 characters or fewer.");
        }

        if (Status != ApprovalStatus.Submitted)
        {
            return Error.Conflict("approval.not_pending", "Only a submitted requisition can be rejected.");
        }

        Status = ApprovalStatus.Rejected;
        Approver = approver.Trim();
        Reason = reason.Trim();
        DecidedAt = at;
        return this;
    }
}

using Illumin360.Recruitment.Application.Abstractions;
using Illumin360.Recruitment.Domain;
using Illumin360.SharedKernel;

namespace Illumin360.Recruitment.Application.Recruitment;

/// <summary>A requisition's approval state.</summary>
/// <param name="Status">Approval status (draft/submitted/approved/rejected).</param>
/// <param name="Approver">Who decided, if applicable.</param>
/// <param name="Reason">Rejection reason, if applicable.</param>
/// <param name="SubmittedAt">When submitted (UTC), if applicable.</param>
/// <param name="DecidedAt">When decided (UTC), if applicable.</param>
public sealed record ApprovalDto(string Status, string? Approver, string? Reason, DateTimeOffset? SubmittedAt, DateTimeOffset? DecidedAt)
{
    /// <summary>Projects a domain approval into the transport DTO (defaulting to draft when none).</summary>
    /// <param name="a">The approval, or null if none.</param>
    /// <returns>The transport DTO.</returns>
    public static ApprovalDto FromDomain(RequisitionApproval? a)
        => a is null
            ? new ApprovalDto(ApprovalStatus.Draft.ToString().ToLowerInvariant(), null, null, null, null)
            : new ApprovalDto(a.Status.ToString().ToLowerInvariant(), a.Approver, a.Reason, a.SubmittedAt, a.DecidedAt);
}

/// <summary>Gets a requisition's approval state.</summary>
/// <param name="RequestId">The requisition id.</param>
public sealed record GetApprovalQuery(Guid RequestId) : IQuery<ApprovalDto>;

/// <summary>The approval transitions a requisition can undergo.</summary>
public enum ApprovalAction
{
    /// <summary>Submit a draft/rejected requisition for approval.</summary>
    Submit,

    /// <summary>Approve a submitted requisition.</summary>
    Approve,

    /// <summary>Reject a submitted requisition.</summary>
    Reject,
}

/// <summary>Transitions a requisition's approval state.</summary>
/// <param name="RequestId">The requisition id.</param>
/// <param name="Action">The transition to apply.</param>
/// <param name="Approver">Approver name (required for approve/reject).</param>
/// <param name="Reason">Rejection reason (required for reject).</param>
public sealed record TransitionApprovalCommand(Guid RequestId, ApprovalAction Action, string? Approver, string? Reason) : ICommand<ApprovalDto>;

/// <summary>Handles <see cref="GetApprovalQuery"/>.</summary>
/// <param name="repository">The recruitment repository.</param>
public sealed class GetApprovalQueryHandler(IRecruitmentRepository repository)
    : IQueryHandler<GetApprovalQuery, ApprovalDto>
{
    private readonly IRecruitmentRepository _repository = repository;

    /// <inheritdoc />
    public async Task<Result<ApprovalDto>> HandleAsync(GetApprovalQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        var approval = await _repository.GetApprovalAsync(query.RequestId, cancellationToken).ConfigureAwait(false);
        return ApprovalDto.FromDomain(approval);
    }
}

/// <summary>Handles <see cref="TransitionApprovalCommand"/>, creating the approval row on first submit.</summary>
/// <param name="repository">The recruitment repository.</param>
public sealed class TransitionApprovalCommandHandler(IRecruitmentRepository repository)
    : ICommandHandler<TransitionApprovalCommand, ApprovalDto>
{
    private readonly IRecruitmentRepository _repository = repository;

    /// <inheritdoc />
    public async Task<Result<ApprovalDto>> HandleAsync(TransitionApprovalCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var request = await _repository.GetByIdAsync(new RequestId(command.RequestId), cancellationToken).ConfigureAwait(false);
        if (request is null)
        {
            return Error.NotFound("request.not_found", "No matching requisition was found.");
        }

        var approval = await _repository.GetApprovalAsync(command.RequestId, cancellationToken).ConfigureAwait(false);
        if (approval is null)
        {
            var creation = RequisitionApproval.Create(command.RequestId);
            if (creation.IsFailure)
            {
                return creation.Error!;
            }

            approval = creation.Value!;
            _repository.AddApproval(approval);
        }

        var now = DateTimeOffset.UtcNow;
        var transition = command.Action switch
        {
            ApprovalAction.Submit => approval.Submit(now),
            ApprovalAction.Approve => approval.Approve(command.Approver ?? string.Empty, now),
            ApprovalAction.Reject => approval.Reject(command.Approver ?? string.Empty, command.Reason ?? string.Empty, now),
            _ => Error.Validation("approval.action_invalid", "Unknown approval action."),
        };

        if (transition.IsFailure)
        {
            return transition.Error!;
        }

        await _repository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return ApprovalDto.FromDomain(approval);
    }
}

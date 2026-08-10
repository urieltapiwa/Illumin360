using Illumin360.Recruitment.Application.Abstractions;
using Illumin360.SharedKernel;
using ApplicationId = Illumin360.Recruitment.Domain.ApplicationId;
using IntegrationEvents = Illumin360.Recruitment.IntegrationEvents;

namespace Illumin360.Recruitment.Application.Recruitment;

/// <summary>A bulk pipeline transition.</summary>
public enum ApplicationBulkAction
{
    /// <summary>Advance each application to the next stage.</summary>
    Advance,

    /// <summary>Reject each application (terminal).</summary>
    Reject,
}

/// <summary>The outcome for a single application in a bulk transition.</summary>
/// <param name="ApplicationId">The application id.</param>
/// <param name="Ok">Whether the transition succeeded.</param>
/// <param name="Status">The resulting status, when successful.</param>
/// <param name="Error">The failure code, when unsuccessful.</param>
public sealed record BulkItemResult(Guid ApplicationId, bool Ok, string? Status, string? Error);

/// <summary>The summary of a bulk transition.</summary>
/// <param name="Requested">Number of applications requested.</param>
/// <param name="Succeeded">Number that transitioned successfully.</param>
/// <param name="Failed">Number that failed (missing / illegal transition).</param>
/// <param name="Items">Per-application results.</param>
public sealed record BulkTransitionResultDto(int Requested, int Succeeded, int Failed, IReadOnlyList<BulkItemResult> Items);

/// <summary>Applies the same pipeline transition to many applications at once.</summary>
/// <param name="ApplicationIds">The applications to transition.</param>
/// <param name="Action">The transition to apply.</param>
public sealed record BulkTransitionApplicationsCommand(IReadOnlyList<Guid> ApplicationIds, ApplicationBulkAction Action) : ICommand<BulkTransitionResultDto>;

/// <summary>Handles <see cref="BulkTransitionApplicationsCommand"/>, applying each transition independently.</summary>
/// <param name="repository">The recruitment repository.</param>
/// <param name="eventPublisher">Integration-event publisher (transactional outbox).</param>
public sealed class BulkTransitionApplicationsCommandHandler(IRecruitmentRepository repository, IIntegrationEventPublisher eventPublisher)
    : ICommandHandler<BulkTransitionApplicationsCommand, BulkTransitionResultDto>
{
    // Cap the batch so one request can't fan out unbounded work.
    private const int MaxBatch = 200;

    private readonly IRecruitmentRepository _repository = repository;
    private readonly IIntegrationEventPublisher _eventPublisher = eventPublisher;

    /// <inheritdoc />
    public async Task<Result<BulkTransitionResultDto>> HandleAsync(BulkTransitionApplicationsCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (command.ApplicationIds is null || command.ApplicationIds.Count == 0)
        {
            return Error.Validation("bulk.empty", "At least one application id is required.");
        }

        if (command.ApplicationIds.Count > MaxBatch)
        {
            return Error.Validation("bulk.too_large", $"A bulk action is limited to {MaxBatch} applications.");
        }

        var now = DateTimeOffset.UtcNow;
        var items = new List<BulkItemResult>(command.ApplicationIds.Count);
        var changed = false;

        // De-duplicate ids while preserving order.
        foreach (var id in command.ApplicationIds.Distinct())
        {
            var application = await _repository.GetApplicationAsync(new ApplicationId(id), cancellationToken).ConfigureAwait(false);
            if (application is null)
            {
                items.Add(new BulkItemResult(id, false, null, "application.not_found"));
                continue;
            }

            var transition = command.Action == ApplicationBulkAction.Advance
                ? application.Advance(now)
                : application.Reject(now);

            if (transition.IsFailure)
            {
                items.Add(new BulkItemResult(id, false, application.Status, transition.Error!.Code));
                continue;
            }

            await _eventPublisher.PublishAsync(
                new IntegrationEvents.ApplicationStatusChanged(
                    application.Id.Value, application.TalentId, application.TalentType, application.Status, now),
                cancellationToken).ConfigureAwait(false);
            items.Add(new BulkItemResult(id, true, application.Status, null));
            changed = true;
        }

        if (changed)
        {
            await _repository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }

        var succeeded = items.Count(i => i.Ok);
        return new BulkTransitionResultDto(items.Count, succeeded, items.Count - succeeded, items);
    }
}

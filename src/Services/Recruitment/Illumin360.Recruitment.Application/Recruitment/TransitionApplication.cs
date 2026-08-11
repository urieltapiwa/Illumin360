using Illumin360.Recruitment.Application.Abstractions;
using Illumin360.Recruitment.Domain;
using Illumin360.SharedKernel;
using ApplicationId = Illumin360.Recruitment.Domain.ApplicationId;
using IntegrationEvents = Illumin360.Recruitment.IntegrationEvents;

namespace Illumin360.Recruitment.Application.Recruitment;

/// <summary>Advances an application to the next pipeline stage.</summary>
/// <param name="ApplicationId">The application id.</param>
public sealed record AdvanceApplicationCommand(Guid ApplicationId) : ICommand<ApplicationDto>;

/// <summary>Rejects an application (terminal), optionally with a free-text reason.</summary>
/// <param name="ApplicationId">The application id.</param>
/// <param name="Reason">Optional free-text rejection reason.</param>
/// <param name="RejectedBy">Who rejected, if known.</param>
public sealed record RejectApplicationCommand(Guid ApplicationId, string? Reason = null, string? RejectedBy = null) : ICommand<ApplicationDto>;

/// <summary>Handles <see cref="AdvanceApplicationCommand"/>.</summary>
/// <param name="repository">The recruitment repository.</param>
/// <param name="eventPublisher">Integration-event publisher (transactional outbox).</param>
public sealed class AdvanceApplicationCommandHandler(IRecruitmentRepository repository, IIntegrationEventPublisher eventPublisher)
    : ICommandHandler<AdvanceApplicationCommand, ApplicationDto>
{
    private readonly IRecruitmentRepository _repository = repository;
    private readonly IIntegrationEventPublisher _eventPublisher = eventPublisher;

    /// <inheritdoc />
    public async Task<Result<ApplicationDto>> HandleAsync(AdvanceApplicationCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var application = await _repository.GetApplicationAsync(new ApplicationId(command.ApplicationId), cancellationToken).ConfigureAwait(false);
        if (application is null)
        {
            return Error.NotFound("application.not_found", "No matching application was found.");
        }

        var advanced = application.Advance(DateTimeOffset.UtcNow);
        if (advanced.IsFailure)
        {
            return advanced.Error!;
        }

        // Capture a labelled outcome when the advance results in a hire (LTR training data).
        if (application.IsHire && await _repository.GetMatchOutcomeAsync(application.Id.Value, cancellationToken).ConfigureAwait(false) is null)
        {
            var now = DateTimeOffset.UtcNow;
            var f = await _repository.GetOutcomeFeaturesAsync(application.Id.Value, application.RequestId.Value, cancellationToken).ConfigureAwait(false)
                ?? new OutcomeFeatureSnapshot("direct", false, 0, null, false, 0, 0, 0);
            var days = (int)(now - application.AppliedAt).TotalDays;
            var outcome = MatchOutcome.Capture(application.Id.Value, application.RequestId.Value, application.TalentId, application.TalentType, application.MatchScore, true, now, f.Source, f.Remote, f.InterviewCount, f.AvgInterviewRating, f.HadOffer, days, f.CitySignal, f.RoleSignal, f.SkillSignal);
            if (outcome.IsSuccess)
            {
                _repository.AddMatchOutcome(outcome.Value!);
            }
        }

        await _eventPublisher.PublishAsync(
            new IntegrationEvents.ApplicationStatusChanged(
                application.Id.Value, application.TalentId, application.TalentType, application.Status, DateTimeOffset.UtcNow),
            cancellationToken).ConfigureAwait(false);
        await _repository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return ApplicationDto.FromDomain(application);
    }
}

/// <summary>Handles <see cref="RejectApplicationCommand"/>.</summary>
/// <param name="repository">The recruitment repository.</param>
/// <param name="eventPublisher">Integration-event publisher (transactional outbox).</param>
public sealed class RejectApplicationCommandHandler(IRecruitmentRepository repository, IIntegrationEventPublisher eventPublisher)
    : ICommandHandler<RejectApplicationCommand, ApplicationDto>
{
    private readonly IRecruitmentRepository _repository = repository;
    private readonly IIntegrationEventPublisher _eventPublisher = eventPublisher;

    /// <inheritdoc />
    public async Task<Result<ApplicationDto>> HandleAsync(RejectApplicationCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var application = await _repository.GetApplicationAsync(new ApplicationId(command.ApplicationId), cancellationToken).ConfigureAwait(false);
        if (application is null)
        {
            return Error.NotFound("application.not_found", "No matching application was found.");
        }

        var rejected = application.Reject(DateTimeOffset.UtcNow);
        if (rejected.IsFailure)
        {
            return rejected.Error!;
        }

        // Record a free-text reason if given (only when the application isn't already rejected).
        string? storedReason = null;
        if (!string.IsNullOrWhiteSpace(command.Reason))
        {
            var creation = ApplicationRejection.Create(command.ApplicationId, command.Reason, command.RejectedBy, DateTimeOffset.UtcNow);
            if (creation.IsFailure)
            {
                return creation.Error!;
            }

            _repository.AddApplicationRejection(creation.Value!);
            storedReason = creation.Value!.Reason;
        }

        // Capture a labelled outcome for the rejection (LTR training data).
        if (await _repository.GetMatchOutcomeAsync(application.Id.Value, cancellationToken).ConfigureAwait(false) is null)
        {
            var now = DateTimeOffset.UtcNow;
            var f = await _repository.GetOutcomeFeaturesAsync(application.Id.Value, application.RequestId.Value, cancellationToken).ConfigureAwait(false)
                ?? new OutcomeFeatureSnapshot("direct", false, 0, null, false, 0, 0, 0);
            var days = (int)(now - application.AppliedAt).TotalDays;
            var outcome = MatchOutcome.Capture(application.Id.Value, application.RequestId.Value, application.TalentId, application.TalentType, application.MatchScore, false, now, f.Source, f.Remote, f.InterviewCount, f.AvgInterviewRating, f.HadOffer, days, f.CitySignal, f.RoleSignal, f.SkillSignal);
            if (outcome.IsSuccess)
            {
                _repository.AddMatchOutcome(outcome.Value!);
            }
        }

        await _eventPublisher.PublishAsync(
            new IntegrationEvents.ApplicationStatusChanged(
                application.Id.Value, application.TalentId, application.TalentType, application.Status, DateTimeOffset.UtcNow),
            cancellationToken).ConfigureAwait(false);
        await _repository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return ApplicationDto.FromDomain(application, storedReason);
    }
}

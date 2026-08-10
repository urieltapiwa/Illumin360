using Illumin360.Recruitment.Application.Abstractions;
using Illumin360.Recruitment.Domain;
using Illumin360.SharedKernel;
using ApplicationId = Illumin360.Recruitment.Domain.ApplicationId;
using IntegrationEvents = Illumin360.Recruitment.IntegrationEvents;

namespace Illumin360.Recruitment.Application.Recruitment;

/// <summary>Advances an application to the next pipeline stage.</summary>
/// <param name="ApplicationId">The application id.</param>
public sealed record AdvanceApplicationCommand(Guid ApplicationId) : ICommand<ApplicationDto>;

/// <summary>Rejects an application (terminal).</summary>
/// <param name="ApplicationId">The application id.</param>
public sealed record RejectApplicationCommand(Guid ApplicationId) : ICommand<ApplicationDto>;

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

        await _eventPublisher.PublishAsync(
            new IntegrationEvents.ApplicationStatusChanged(
                application.Id.Value, application.TalentId, application.TalentType, application.Status, DateTimeOffset.UtcNow),
            cancellationToken).ConfigureAwait(false);
        await _repository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return ApplicationDto.FromDomain(application);
    }
}

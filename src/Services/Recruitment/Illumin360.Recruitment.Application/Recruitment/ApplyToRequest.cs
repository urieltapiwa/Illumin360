using Illumin360.Recruitment.Application.Abstractions;
using Illumin360.Recruitment.Domain;
using Illumin360.SharedKernel;
using IntegrationEvents = Illumin360.Recruitment.IntegrationEvents;

namespace Illumin360.Recruitment.Application.Recruitment;

/// <summary>Command: a talent applies to an open recruitment request from a portal.</summary>
/// <param name="RequestId">The request being applied to.</param>
/// <param name="TalentId">The applying talent's id.</param>
/// <param name="TalentType">Talent type (<c>student</c> or <c>professional</c>).</param>
public sealed record ApplyToRequestCommand(
    Guid RequestId,
    Guid TalentId,
    string TalentType) : ICommand<ApplicationDto>;

/// <summary>Handles <see cref="ApplyToRequestCommand"/> by recording a fresh application against the request.</summary>
/// <param name="repository">The recruitment repository.</param>
/// <param name="eventPublisher">Integration-event publisher (transactional outbox).</param>
public sealed class ApplyToRequestCommandHandler(IRecruitmentRepository repository, IIntegrationEventPublisher eventPublisher)
    : ICommandHandler<ApplyToRequestCommand, ApplicationDto>
{
    private readonly IRecruitmentRepository _repository = repository;
    private readonly IIntegrationEventPublisher _eventPublisher = eventPublisher;

    /// <inheritdoc />
    public async Task<Result<ApplicationDto>> HandleAsync(ApplyToRequestCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (command.TalentId == Guid.Empty)
        {
            return Error.Validation("application.talent_required", "A talent id is required to apply.");
        }

        var requestId = new RequestId(command.RequestId);
        var request = await _repository.GetByIdAsync(requestId, cancellationToken).ConfigureAwait(false);
        if (request is null)
        {
            return Error.NotFound("request.not_found", "No matching recruitment request was found.");
        }

        if (request.Status != RequestStatus.Open)
        {
            return Error.Conflict("request.not_open", "This role is no longer open for applications.");
        }

        if (await _repository.HasApplicationAsync(requestId, command.TalentId, cancellationToken).ConfigureAwait(false))
        {
            return Error.Conflict("application.already_applied", "You have already applied to this role.");
        }

        var talentType = string.IsNullOrWhiteSpace(command.TalentType) ? "professional" : command.TalentType.Trim();
        var application = RecruitmentApplication.Apply(requestId, command.TalentId, talentType, DateTimeOffset.UtcNow);
        _repository.AddApplication(application);

        // Staged into the outbox in the same transaction as the application (transactional outbox).
        await _eventPublisher.PublishAsync(
            new IntegrationEvents.ApplicationSubmitted(
                application.Id.Value, requestId.Value, command.TalentId, talentType, application.AppliedAt),
            cancellationToken).ConfigureAwait(false);

        await _repository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return ApplicationDto.FromDomain(application);
    }
}

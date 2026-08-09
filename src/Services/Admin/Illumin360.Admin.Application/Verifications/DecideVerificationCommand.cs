using Illumin360.Admin.Application.Abstractions;
using Illumin360.Admin.Domain;
using Illumin360.SharedKernel;
using IntegrationEvents = Illumin360.Admin.IntegrationEvents;

namespace Illumin360.Admin.Application.Verifications;

/// <summary>The decision to apply to a verification.</summary>
public enum VerificationDecision
{
    /// <summary>Approve the verification.</summary>
    Approve,

    /// <summary>Reject the verification.</summary>
    Reject,
}

/// <summary>Approves or rejects a pending verification.</summary>
/// <param name="Id">The verification id.</param>
/// <param name="Decision">Approve or reject.</param>
/// <param name="DecidedBy">Username of the acting admin (from the access token).</param>
public sealed record DecideVerificationCommand(Guid Id, VerificationDecision Decision, string DecidedBy)
    : ICommand<VerificationDto>;

/// <summary>Handles <see cref="DecideVerificationCommand"/>.</summary>
/// <param name="repository">The verification repository.</param>
/// <param name="publisher">The integration-event publisher (outbox-backed).</param>
public sealed class DecideVerificationCommandHandler(
    IVerificationRepository repository,
    IIntegrationEventPublisher publisher)
    : ICommandHandler<DecideVerificationCommand, VerificationDto>
{
    private readonly IVerificationRepository _repository = repository;
    private readonly IIntegrationEventPublisher _publisher = publisher;

    /// <inheritdoc />
    public async Task<Result<VerificationDto>> HandleAsync(
        DecideVerificationCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var verification = await _repository.GetByIdAsync(new VerificationId(command.Id), cancellationToken)
            .ConfigureAwait(false);
        if (verification is null)
        {
            return Error.NotFound("verification.not_found", "No matching verification was found.");
        }

        var outcome = command.Decision == VerificationDecision.Approve
            ? verification.Approve(command.DecidedBy)
            : verification.Reject(command.DecidedBy);

        if (outcome.IsFailure)
        {
            return outcome.Error!;
        }

        foreach (var domainEvent in verification.DomainEvents)
        {
            if (domainEvent is VerificationDecided decided)
            {
                await _publisher.PublishAsync(
                    new IntegrationEvents.VerificationDecided(
                        decided.VerificationId.Value, decided.Entity, decided.Outcome, decided.DecidedBy, decided.OccurredOn),
                    cancellationToken).ConfigureAwait(false);
            }
        }

        verification.ClearDomainEvents();
        await _repository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return VerificationDto.FromDomain(verification);
    }
}

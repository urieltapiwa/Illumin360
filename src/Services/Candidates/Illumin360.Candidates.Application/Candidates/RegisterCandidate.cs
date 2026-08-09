using Illumin360.Candidates.Application.Abstractions;
using Illumin360.Candidates.Domain;
using Illumin360.SharedKernel;
using IntegrationEvents = Illumin360.Candidates.IntegrationEvents;

namespace Illumin360.Candidates.Application.Candidates;

/// <summary>Command: register a new candidate into the talent pool.</summary>
/// <param name="FirstName">Given name (required).</param>
/// <param name="LastName">Family name (required).</param>
/// <param name="City">City (required).</param>
/// <param name="Nationality">Nationality (required).</param>
/// <param name="Availability">Availability status name; defaults to <c>ActivelyLooking</c> when omitted.</param>
/// <param name="PublicHeadline">Optional public headline (≤ 150 chars).</param>
public sealed record RegisterCandidateCommand(
    string FirstName,
    string LastName,
    string City,
    string Nationality,
    string? Availability = null,
    string? PublicHeadline = null) : ICommand<CandidateDto>;

/// <summary>Handles <see cref="RegisterCandidateCommand"/> by registering and persisting a candidate.</summary>
public sealed class RegisterCandidateCommandHandler(
    ICandidateRepository repository,
    IIntegrationEventPublisher eventPublisher)
    : ICommandHandler<RegisterCandidateCommand, CandidateDto>
{
    private readonly ICandidateRepository _repository = repository;
    private readonly IIntegrationEventPublisher _eventPublisher = eventPublisher;

    /// <inheritdoc />
    public async Task<Result<CandidateDto>> HandleAsync(
        RegisterCandidateCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var availability = AvailabilityStatus.ActivelyLooking;
        if (!string.IsNullOrWhiteSpace(command.Availability)
            && !Enum.TryParse(command.Availability, ignoreCase: true, out availability))
        {
            return Error.Validation(
                "candidate.invalid_availability",
                $"'{command.Availability}' is not a valid availability status.");
        }

        var registration = Candidate.Register(
            command.FirstName,
            command.LastName,
            command.City,
            command.Nationality,
            availability,
            command.PublicHeadline);

        if (registration.IsFailure)
        {
            return registration.Error!;
        }

        var candidate = registration.Value!;
        _repository.Add(candidate);

        // Publish integration events through the MassTransit bus outbox: they are written to the
        // outbox table inside the same SaveChanges transaction as the aggregate and delivered to
        // the broker only after commit (transactional outbox — charter Part 5/13).
        foreach (var domainEvent in candidate.DomainEvents)
        {
            if (domainEvent is CandidateRegistered registered)
            {
                await _eventPublisher.PublishAsync(
                    new IntegrationEvents.CandidateRegistered(registered.CandidateId.Value, registered.OccurredOn),
                    cancellationToken).ConfigureAwait(false);
            }
        }

        candidate.ClearDomainEvents();
        await _repository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return CandidateDto.FromDomain(candidate);
    }
}

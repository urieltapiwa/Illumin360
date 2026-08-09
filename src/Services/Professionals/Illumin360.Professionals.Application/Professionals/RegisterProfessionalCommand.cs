using Illumin360.Professionals.Application.Abstractions;
using Illumin360.Professionals.Domain;
using Illumin360.SharedKernel;
using IntegrationEvents = Illumin360.Professionals.IntegrationEvents;

namespace Illumin360.Professionals.Application.Professionals;

/// <summary>Summary of a registered professional.</summary>
/// <param name="Id">The professional's id.</param>
/// <param name="Name">The professional's full name.</param>
/// <param name="Role">Headline role.</param>
/// <param name="City">Home city.</param>
public sealed record ProfessionalSummaryDto(Guid Id, string Name, string Role, string City);

/// <summary>Registers a new professional on the marketplace.</summary>
/// <param name="FirstName">Given name.</param>
/// <param name="LastName">Family name.</param>
/// <param name="Role">Headline role.</param>
/// <param name="City">Home city.</param>
/// <param name="Nationality">Nationality.</param>
/// <param name="Availability">Availability label.</param>
/// <param name="Headline">Public headline.</param>
public sealed record RegisterProfessionalCommand(
    string FirstName,
    string LastName,
    string Role,
    string City,
    string Nationality,
    string Availability,
    string Headline) : ICommand<ProfessionalSummaryDto>;

/// <summary>Handles <see cref="RegisterProfessionalCommand"/>.</summary>
/// <param name="repository">The professional repository.</param>
/// <param name="publisher">The integration-event publisher (outbox-backed).</param>
public sealed class RegisterProfessionalCommandHandler(
    IProfessionalRepository repository,
    IIntegrationEventPublisher publisher)
    : ICommandHandler<RegisterProfessionalCommand, ProfessionalSummaryDto>
{
    private readonly IProfessionalRepository _repository = repository;
    private readonly IIntegrationEventPublisher _publisher = publisher;

    /// <inheritdoc />
    public async Task<Result<ProfessionalSummaryDto>> HandleAsync(
        RegisterProfessionalCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var creation = Professional.Register(
            command.FirstName,
            command.LastName,
            command.Role,
            command.City,
            command.Nationality,
            command.Availability,
            command.Headline);

        if (creation.IsFailure)
        {
            return creation.Error!;
        }

        var professional = creation.Value!;
        _repository.Add(professional);

        // Translate in-process domain events into cross-service integration events (written to the
        // outbox in the same transaction; delivered to the broker only after commit — ADR-0007).
        foreach (var domainEvent in professional.DomainEvents)
        {
            if (domainEvent is ProfessionalRegistered registered)
            {
                await _publisher.PublishAsync(
                    new IntegrationEvents.ProfessionalRegistered(
                        registered.ProfessionalId.Value, registered.FullName, registered.OccurredOn),
                    cancellationToken).ConfigureAwait(false);
            }
        }

        professional.ClearDomainEvents();
        await _repository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return new ProfessionalSummaryDto(professional.Id.Value, professional.FullName, professional.Role, professional.City);
    }
}

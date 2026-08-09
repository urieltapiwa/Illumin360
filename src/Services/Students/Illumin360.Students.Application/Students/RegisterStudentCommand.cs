using Illumin360.SharedKernel;
using Illumin360.Students.Application.Abstractions;
using Illumin360.Students.Domain;
using IntegrationEvents = Illumin360.Students.IntegrationEvents;

namespace Illumin360.Students.Application.Students;

/// <summary>Summary of a registered student.</summary>
/// <param name="Id">The student's id.</param>
/// <param name="Name">The student's full name.</param>
/// <param name="Field">Field of study.</param>
/// <param name="City">Home city.</param>
public sealed record StudentSummaryDto(Guid Id, string Name, string Field, string City);

/// <summary>Registers a new student on the programme.</summary>
/// <param name="FirstName">Given name.</param>
/// <param name="LastName">Family name.</param>
/// <param name="Field">Field of study.</param>
/// <param name="School">Institution.</param>
/// <param name="Year">Year-of-study label.</param>
/// <param name="Graduating">Expected graduation year label.</param>
/// <param name="Program">Sponsoring programme.</param>
/// <param name="City">Home city.</param>
public sealed record RegisterStudentCommand(
    string FirstName,
    string LastName,
    string Field,
    string School,
    string Year,
    string Graduating,
    string Program,
    string City) : ICommand<StudentSummaryDto>;

/// <summary>Handles <see cref="RegisterStudentCommand"/>.</summary>
/// <param name="repository">The student repository.</param>
/// <param name="publisher">The integration-event publisher (outbox-backed).</param>
public sealed class RegisterStudentCommandHandler(
    IStudentRepository repository,
    IIntegrationEventPublisher publisher)
    : ICommandHandler<RegisterStudentCommand, StudentSummaryDto>
{
    private readonly IStudentRepository _repository = repository;
    private readonly IIntegrationEventPublisher _publisher = publisher;

    /// <inheritdoc />
    public async Task<Result<StudentSummaryDto>> HandleAsync(
        RegisterStudentCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var creation = Student.Register(
            command.FirstName,
            command.LastName,
            command.Field,
            command.School,
            command.Year,
            command.Graduating,
            command.Program,
            command.City);

        if (creation.IsFailure)
        {
            return creation.Error!;
        }

        var student = creation.Value!;
        _repository.Add(student);

        // Translate in-process domain events into cross-service integration events (written to the
        // outbox in the same transaction; delivered to the broker only after commit — ADR-0007).
        foreach (var domainEvent in student.DomainEvents)
        {
            if (domainEvent is StudentRegistered registered)
            {
                await _publisher.PublishAsync(
                    new IntegrationEvents.StudentRegistered(
                        registered.StudentId.Value, registered.FullName, registered.OccurredOn),
                    cancellationToken).ConfigureAwait(false);
            }
        }

        student.ClearDomainEvents();
        await _repository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return new StudentSummaryDto(student.Id.Value, student.FullName, student.Field, student.City);
    }
}

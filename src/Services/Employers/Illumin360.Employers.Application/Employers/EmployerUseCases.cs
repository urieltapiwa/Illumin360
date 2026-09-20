using Illumin360.Employers.Application.Abstractions;
using Illumin360.Employers.Domain;
using Illumin360.SharedKernel;

namespace Illumin360.Employers.Application.Employers;

/// <summary>Public company profile for an employer.</summary>
/// <param name="Id">Employer id.</param>
/// <param name="CompanyName">Company name.</param>
/// <param name="Industry">Industry.</param>
/// <param name="City">City.</param>
/// <param name="Website">Website, if any.</param>
/// <param name="About">About blurb, if any.</param>
public sealed record EmployerDto(Guid Id, string CompanyName, string Industry, string City, string? Website, string? About)
{
    /// <summary>Projects a domain <see cref="Employer"/> into the transport DTO.</summary>
    /// <param name="e">The employer.</param>
    /// <returns>The transport DTO.</returns>
    public static EmployerDto FromDomain(Employer e)
    {
        ArgumentNullException.ThrowIfNull(e);
        return new EmployerDto(e.Id.Value, e.CompanyName, e.Industry, e.City, e.Website, e.About);
    }
}

/// <summary>Query for an employer profile; null id returns the subject-owned or default ("me") employer.</summary>
/// <param name="Id">Employer id, or null for the default.</param>
/// <param name="Subject">Identity-provider subject ("sub") of the signed-in employer, or null.</param>
public sealed record GetEmployerQuery(Guid? Id = null, string? Subject = null) : IQuery<EmployerDto>;

/// <summary>Registers a new employer company profile.</summary>
/// <param name="CompanyName">Company name.</param>
/// <param name="Industry">Industry.</param>
/// <param name="City">City.</param>
/// <param name="Website">Website, if any.</param>
/// <param name="About">About blurb, if any.</param>
/// <param name="Subject">Identity-provider subject ("sub") that owns this profile, if any.</param>
public sealed record RegisterEmployerCommand(string CompanyName, string Industry, string City, string? Website, string? About, string? Subject = null) : ICommand<EmployerDto>;

/// <summary>Updates the current ("me") employer's editable profile fields.</summary>
public sealed record UpdateEmployerProfileCommand(string Industry, string City, string? Website, string? About) : ICommand<EmployerDto>;

/// <summary>Handles <see cref="GetEmployerQuery"/>.</summary>
/// <param name="repository">The employer repository.</param>
public sealed class GetEmployerQueryHandler(IEmployerRepository repository)
    : IQueryHandler<GetEmployerQuery, EmployerDto>
{
    private readonly IEmployerRepository _repository = repository;

    /// <inheritdoc />
    public async Task<Result<EmployerDto>> HandleAsync(GetEmployerQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        Employer? employer;
        if (query.Id is { } id)
        {
            employer = await _repository.GetByIdAsync(new EmployerId(id), cancellationToken).ConfigureAwait(false);
        }
        else if (!string.IsNullOrWhiteSpace(query.Subject))
        {
            employer = await _repository.GetBySubjectAsync(query.Subject, cancellationToken).ConfigureAwait(false)
                ?? await _repository.GetDefaultAsync(cancellationToken).ConfigureAwait(false);
        }
        else
        {
            employer = await _repository.GetDefaultAsync(cancellationToken).ConfigureAwait(false);
        }

        if (employer is null)
        {
            return Error.NotFound("employer.not_found", "No matching employer was found.");
        }

        return EmployerDto.FromDomain(employer);
    }
}

/// <summary>Handles <see cref="RegisterEmployerCommand"/>.</summary>
/// <param name="repository">The employer repository.</param>
/// <param name="publisher">The integration-event publisher (outbox-backed).</param>
public sealed class RegisterEmployerCommandHandler(
    IEmployerRepository repository,
    Abstractions.IIntegrationEventPublisher publisher)
    : ICommandHandler<RegisterEmployerCommand, EmployerDto>
{
    private readonly IEmployerRepository _repository = repository;
    private readonly Abstractions.IIntegrationEventPublisher _publisher = publisher;

    /// <inheritdoc />
    public async Task<Result<EmployerDto>> HandleAsync(RegisterEmployerCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var creation = Employer.Register(command.CompanyName, command.Industry, command.City, command.Website, command.About, command.Subject);
        if (creation.IsFailure)
        {
            return creation.Error!;
        }

        var employer = creation.Value!;
        _repository.Add(employer);

        // Announce the new company so other services (e.g. the Admin directory) can react. Written to the
        // outbox in the same transaction as the SaveChanges below; delivered to the broker only after commit.
        await _publisher.PublishAsync(
            new Illumin360.Employers.IntegrationEvents.EmployerRegistered(
                employer.Id.Value, employer.CompanyName, employer.City, employer.CreatedAt),
            cancellationToken).ConfigureAwait(false);

        await _repository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return EmployerDto.FromDomain(employer);
    }
}

/// <summary>Handles <see cref="UpdateEmployerProfileCommand"/>.</summary>
/// <param name="repository">The employer repository.</param>
public sealed class UpdateEmployerProfileCommandHandler(IEmployerRepository repository)
    : ICommandHandler<UpdateEmployerProfileCommand, EmployerDto>
{
    private readonly IEmployerRepository _repository = repository;

    /// <inheritdoc />
    public async Task<Result<EmployerDto>> HandleAsync(UpdateEmployerProfileCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var me = await _repository.GetDefaultTrackedAsync(cancellationToken).ConfigureAwait(false);
        if (me is null)
        {
            return Error.NotFound("employer.not_found", "No employer profile found.");
        }

        var update = me.UpdateProfile(command.Industry, command.City, command.Website, command.About);
        if (update.IsFailure)
        {
            return update.Error!;
        }

        await _repository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return EmployerDto.FromDomain(me);
    }
}

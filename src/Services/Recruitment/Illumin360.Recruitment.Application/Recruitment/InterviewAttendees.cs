using Illumin360.Recruitment.Application.Abstractions;
using Illumin360.Recruitment.Domain;
using Illumin360.SharedKernel;

namespace Illumin360.Recruitment.Application.Recruitment;

/// <summary>A member of an interview panel.</summary>
/// <param name="Id">Attendee id.</param>
/// <param name="Name">Attendee name.</param>
/// <param name="Email">Attendee email, if any.</param>
/// <param name="Role">Panel role.</param>
public sealed record InterviewAttendeeDto(Guid Id, string Name, string? Email, string Role)
{
    /// <summary>Projects a domain <see cref="InterviewAttendee"/> into the transport DTO.</summary>
    /// <param name="a">The attendee.</param>
    /// <returns>The transport DTO.</returns>
    public static InterviewAttendeeDto FromDomain(InterviewAttendee a)
    {
        ArgumentNullException.ThrowIfNull(a);
        return new InterviewAttendeeDto(a.Id, a.Name, a.Email, a.Role);
    }
}

/// <summary>Lists an interview's panel attendees.</summary>
/// <param name="InterviewId">The interview id.</param>
public sealed record GetInterviewAttendeesQuery(Guid InterviewId) : IQuery<IReadOnlyList<InterviewAttendeeDto>>;

/// <summary>Adds a panel attendee to an interview.</summary>
/// <param name="InterviewId">The interview id.</param>
/// <param name="Name">Attendee name.</param>
/// <param name="Email">Attendee email.</param>
/// <param name="Role">Panel role.</param>
public sealed record AddInterviewAttendeeCommand(Guid InterviewId, string Name, string? Email, string? Role) : ICommand<InterviewAttendeeDto>;

/// <summary>Removes a panel attendee.</summary>
/// <param name="AttendeeId">The attendee id.</param>
public sealed record RemoveInterviewAttendeeCommand(Guid AttendeeId) : ICommand<bool>;

/// <summary>Handles <see cref="GetInterviewAttendeesQuery"/>.</summary>
/// <param name="repository">The recruitment repository.</param>
public sealed class GetInterviewAttendeesQueryHandler(IRecruitmentRepository repository)
    : IQueryHandler<GetInterviewAttendeesQuery, IReadOnlyList<InterviewAttendeeDto>>
{
    private readonly IRecruitmentRepository _repository = repository;

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<InterviewAttendeeDto>>> HandleAsync(GetInterviewAttendeesQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        var attendees = await _repository.ListInterviewAttendeesAsync(query.InterviewId, cancellationToken).ConfigureAwait(false);
        return attendees.Select(InterviewAttendeeDto.FromDomain).ToList();
    }
}

/// <summary>Handles <see cref="AddInterviewAttendeeCommand"/>.</summary>
/// <param name="repository">The recruitment repository.</param>
public sealed class AddInterviewAttendeeCommandHandler(IRecruitmentRepository repository)
    : ICommandHandler<AddInterviewAttendeeCommand, InterviewAttendeeDto>
{
    private readonly IRecruitmentRepository _repository = repository;

    /// <inheritdoc />
    public async Task<Result<InterviewAttendeeDto>> HandleAsync(AddInterviewAttendeeCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var interview = await _repository.GetInterviewAsync(new InterviewId(command.InterviewId), cancellationToken).ConfigureAwait(false);
        if (interview is null)
        {
            return Error.NotFound("interview.not_found", "No matching interview was found.");
        }

        var creation = InterviewAttendee.Create(command.InterviewId, command.Name, command.Email, command.Role, DateTimeOffset.UtcNow);
        if (creation.IsFailure)
        {
            return creation.Error!;
        }

        _repository.AddInterviewAttendee(creation.Value!);
        await _repository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return InterviewAttendeeDto.FromDomain(creation.Value!);
    }
}

/// <summary>Handles <see cref="RemoveInterviewAttendeeCommand"/>.</summary>
/// <param name="repository">The recruitment repository.</param>
public sealed class RemoveInterviewAttendeeCommandHandler(IRecruitmentRepository repository)
    : ICommandHandler<RemoveInterviewAttendeeCommand, bool>
{
    private readonly IRecruitmentRepository _repository = repository;

    /// <inheritdoc />
    public async Task<Result<bool>> HandleAsync(RemoveInterviewAttendeeCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var attendee = await _repository.GetInterviewAttendeeAsync(command.AttendeeId, cancellationToken).ConfigureAwait(false);
        if (attendee is null)
        {
            return Error.NotFound("attendee.not_found", "No matching attendee was found.");
        }

        _repository.RemoveInterviewAttendee(attendee);
        await _repository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return true;
    }
}

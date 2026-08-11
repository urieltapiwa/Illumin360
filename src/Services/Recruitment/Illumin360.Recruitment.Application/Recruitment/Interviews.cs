using System.Globalization;
using System.Text;
using Illumin360.Recruitment.Application.Abstractions;
using Illumin360.Recruitment.Domain;
using Illumin360.SharedKernel;

namespace Illumin360.Recruitment.Application.Recruitment;

/// <summary>Transport DTO for an interview.</summary>
/// <param name="Id">Interview id.</param>
/// <param name="ApplicationId">The application.</param>
/// <param name="ScheduledAt">Start (UTC).</param>
/// <param name="DurationMinutes">Duration.</param>
/// <param name="Location">Location/mode.</param>
/// <param name="Status">scheduled/completed/cancelled.</param>
/// <param name="FeedbackRating">Scorecard rating, if completed.</param>
/// <param name="FeedbackComment">Scorecard comment, if completed.</param>
public sealed record InterviewDto(Guid Id, Guid ApplicationId, DateTimeOffset ScheduledAt, int DurationMinutes, string Location, string Status, int? FeedbackRating, string? FeedbackComment)
{
    /// <summary>Projects a domain <see cref="Interview"/> into the transport DTO.</summary>
    /// <param name="i">The interview.</param>
    /// <returns>The transport DTO.</returns>
    public static InterviewDto FromDomain(Interview i)
    {
        ArgumentNullException.ThrowIfNull(i);
        return new InterviewDto(i.Id.Value, i.ApplicationId, i.ScheduledAt, i.DurationMinutes, i.Location, i.Status, i.FeedbackRating, i.FeedbackComment);
    }
}

/// <summary>Builds a minimal iCalendar (.ics) invite for an interview.</summary>
public static class Ics
{
    /// <summary>Renders a single-event VCALENDAR for the interview, optionally listing the panel.</summary>
    /// <param name="interview">The interview.</param>
    /// <param name="attendees">The interview panel, if any (emitted as ATTENDEE lines).</param>
    /// <returns>The .ics text.</returns>
    public static string Build(Interview interview, IReadOnlyList<InterviewAttendee>? attendees = null)
    {
        ArgumentNullException.ThrowIfNull(interview);
        var start = interview.ScheduledAt.UtcDateTime;
        var end = start.AddMinutes(interview.DurationMinutes);
        static string Fmt(DateTime dt) => dt.ToString("yyyyMMdd'T'HHmmss'Z'", CultureInfo.InvariantCulture);

        var sb = new StringBuilder();
        sb.Append("BEGIN:VCALENDAR\r\n");
        sb.Append("VERSION:2.0\r\n");
        sb.Append("PRODID:-//Illumin360//Interviews//EN\r\n");
        sb.Append("BEGIN:VEVENT\r\n");
        sb.Append(CultureInfo.InvariantCulture, $"UID:{interview.Id.Value}@illumin360\r\n");
        sb.Append(CultureInfo.InvariantCulture, $"DTSTAMP:{Fmt(interview.CreatedAt.UtcDateTime)}\r\n");
        sb.Append(CultureInfo.InvariantCulture, $"DTSTART:{Fmt(start)}\r\n");
        sb.Append(CultureInfo.InvariantCulture, $"DTEND:{Fmt(end)}\r\n");
        sb.Append("SUMMARY:Illumin360 interview\r\n");
        sb.Append(CultureInfo.InvariantCulture, $"LOCATION:{Escape(interview.Location)}\r\n");
        foreach (var attendee in attendees ?? [])
        {
            var contact = string.IsNullOrWhiteSpace(attendee.Email) ? "invalid:nomail" : $"mailto:{attendee.Email}";
            sb.Append(CultureInfo.InvariantCulture, $"ATTENDEE;CN={Escape(attendee.Name)}:{contact}\r\n");
        }

        sb.Append("END:VEVENT\r\n");
        sb.Append("END:VCALENDAR\r\n");
        return sb.ToString();
    }

    /// <summary>
    /// Renders a multi-event VCALENDAR feed (subscribable by Google/Outlook) covering every interview
    /// passed in. Cancelled interviews are marked CANCELLED so subscribers drop them.
    /// </summary>
    /// <param name="calendarName">The calendar display name (X-WR-CALNAME).</param>
    /// <param name="interviews">The interviews to include.</param>
    /// <returns>The .ics feed text.</returns>
    public static string BuildFeed(string calendarName, IReadOnlyList<Interview> interviews)
    {
        ArgumentNullException.ThrowIfNull(interviews);
        ArgumentNullException.ThrowIfNull(calendarName);
        static string Fmt(DateTime dt) => dt.ToString("yyyyMMdd'T'HHmmss'Z'", CultureInfo.InvariantCulture);

        var sb = new StringBuilder();
        sb.Append("BEGIN:VCALENDAR\r\n");
        sb.Append("VERSION:2.0\r\n");
        sb.Append("PRODID:-//Illumin360//Interviews//EN\r\n");
        sb.Append("CALSCALE:GREGORIAN\r\n");
        sb.Append("METHOD:PUBLISH\r\n");
        sb.Append(CultureInfo.InvariantCulture, $"X-WR-CALNAME:{Escape(calendarName)}\r\n");
        foreach (var interview in interviews)
        {
            var start = interview.ScheduledAt.UtcDateTime;
            var end = start.AddMinutes(interview.DurationMinutes);
            sb.Append("BEGIN:VEVENT\r\n");
            sb.Append(CultureInfo.InvariantCulture, $"UID:{interview.Id.Value}@illumin360\r\n");
            sb.Append(CultureInfo.InvariantCulture, $"DTSTAMP:{Fmt(interview.CreatedAt.UtcDateTime)}\r\n");
            sb.Append(CultureInfo.InvariantCulture, $"DTSTART:{Fmt(start)}\r\n");
            sb.Append(CultureInfo.InvariantCulture, $"DTEND:{Fmt(end)}\r\n");
            sb.Append("SUMMARY:Illumin360 interview\r\n");
            sb.Append(CultureInfo.InvariantCulture, $"LOCATION:{Escape(interview.Location)}\r\n");
            if (string.Equals(interview.Status, "cancelled", StringComparison.OrdinalIgnoreCase))
            {
                sb.Append("STATUS:CANCELLED\r\n");
            }

            sb.Append("END:VEVENT\r\n");
        }

        sb.Append("END:VCALENDAR\r\n");
        return sb.ToString();
    }

    private static string Escape(string value) =>
        value.Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace(",", "\\,", StringComparison.Ordinal)
            .Replace(";", "\\;", StringComparison.Ordinal);
}

/// <summary>Schedules an interview for an application.</summary>
public sealed record ScheduleInterviewCommand(Guid ApplicationId, DateTimeOffset ScheduledAt, int DurationMinutes, string Location) : ICommand<InterviewDto>;

/// <summary>Records a scorecard and completes an interview.</summary>
public sealed record RecordInterviewFeedbackCommand(Guid InterviewId, int Rating, string? Comment) : ICommand<InterviewDto>;

/// <summary>Cancels a scheduled interview.</summary>
public sealed record CancelInterviewCommand(Guid InterviewId) : ICommand<InterviewDto>;

/// <summary>Lists an application's interviews.</summary>
public sealed record GetInterviewsQuery(Guid ApplicationId) : IQuery<IReadOnlyList<InterviewDto>>;

/// <summary>Gets an interview's .ics invite text.</summary>
public sealed record GetInterviewIcsQuery(Guid InterviewId) : IQuery<string>;

/// <summary>Gets a subscribable .ics calendar feed of all a talent's interviews.</summary>
public sealed record GetTalentCalendarFeedQuery(Guid TalentId) : IQuery<string>;

/// <summary>Handles <see cref="GetTalentCalendarFeedQuery"/>.</summary>
/// <param name="repository">The recruitment repository.</param>
public sealed class GetTalentCalendarFeedQueryHandler(IRecruitmentRepository repository)
    : IQueryHandler<GetTalentCalendarFeedQuery, string>
{
    private readonly IRecruitmentRepository _repository = repository;

    /// <inheritdoc />
    public async Task<Result<string>> HandleAsync(GetTalentCalendarFeedQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        var interviews = await _repository.ListInterviewsForTalentAsync(query.TalentId, cancellationToken).ConfigureAwait(false);
        return Result<string>.Success(Ics.BuildFeed("Illumin360 interviews", interviews));
    }
}

/// <summary>Handles <see cref="ScheduleInterviewCommand"/>.</summary>
/// <param name="repository">The recruitment repository.</param>
public sealed class ScheduleInterviewCommandHandler(IRecruitmentRepository repository)
    : ICommandHandler<ScheduleInterviewCommand, InterviewDto>
{
    private readonly IRecruitmentRepository _repository = repository;

    /// <inheritdoc />
    public async Task<Result<InterviewDto>> HandleAsync(ScheduleInterviewCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var creation = Interview.Schedule(command.ApplicationId, command.ScheduledAt, command.DurationMinutes, command.Location, DateTimeOffset.UtcNow);
        if (creation.IsFailure)
        {
            return creation.Error!;
        }

        _repository.AddInterview(creation.Value!);
        await _repository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return InterviewDto.FromDomain(creation.Value!);
    }
}

/// <summary>Handles <see cref="RecordInterviewFeedbackCommand"/>.</summary>
/// <param name="repository">The recruitment repository.</param>
public sealed class RecordInterviewFeedbackCommandHandler(IRecruitmentRepository repository)
    : ICommandHandler<RecordInterviewFeedbackCommand, InterviewDto>
{
    private readonly IRecruitmentRepository _repository = repository;

    /// <inheritdoc />
    public async Task<Result<InterviewDto>> HandleAsync(RecordInterviewFeedbackCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var interview = await _repository.GetInterviewAsync(new InterviewId(command.InterviewId), cancellationToken).ConfigureAwait(false);
        if (interview is null)
        {
            return Error.NotFound("interview.not_found", "No matching interview was found.");
        }

        var result = interview.RecordFeedback(command.Rating, command.Comment);
        if (result.IsFailure)
        {
            return result.Error!;
        }

        await _repository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return InterviewDto.FromDomain(interview);
    }
}

/// <summary>Handles <see cref="CancelInterviewCommand"/>.</summary>
/// <param name="repository">The recruitment repository.</param>
public sealed class CancelInterviewCommandHandler(IRecruitmentRepository repository)
    : ICommandHandler<CancelInterviewCommand, InterviewDto>
{
    private readonly IRecruitmentRepository _repository = repository;

    /// <inheritdoc />
    public async Task<Result<InterviewDto>> HandleAsync(CancelInterviewCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var interview = await _repository.GetInterviewAsync(new InterviewId(command.InterviewId), cancellationToken).ConfigureAwait(false);
        if (interview is null)
        {
            return Error.NotFound("interview.not_found", "No matching interview was found.");
        }

        var result = interview.Cancel();
        if (result.IsFailure)
        {
            return result.Error!;
        }

        await _repository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return InterviewDto.FromDomain(interview);
    }
}

/// <summary>Handles <see cref="GetInterviewsQuery"/>.</summary>
/// <param name="repository">The recruitment repository.</param>
public sealed class GetInterviewsQueryHandler(IRecruitmentRepository repository)
    : IQueryHandler<GetInterviewsQuery, IReadOnlyList<InterviewDto>>
{
    private readonly IRecruitmentRepository _repository = repository;

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<InterviewDto>>> HandleAsync(GetInterviewsQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        var interviews = await _repository.ListInterviewsForApplicationAsync(query.ApplicationId, cancellationToken).ConfigureAwait(false);
        return interviews.Select(InterviewDto.FromDomain).ToList();
    }
}

/// <summary>Handles <see cref="GetInterviewIcsQuery"/>.</summary>
/// <param name="repository">The recruitment repository.</param>
public sealed class GetInterviewIcsQueryHandler(IRecruitmentRepository repository)
    : IQueryHandler<GetInterviewIcsQuery, string>
{
    private readonly IRecruitmentRepository _repository = repository;

    /// <inheritdoc />
    public async Task<Result<string>> HandleAsync(GetInterviewIcsQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        var interview = await _repository.GetInterviewAsync(new InterviewId(query.InterviewId), cancellationToken).ConfigureAwait(false);
        if (interview is null)
        {
            return Error.NotFound("interview.not_found", "No matching interview was found.");
        }

        var attendees = await _repository.ListInterviewAttendeesAsync(query.InterviewId, cancellationToken).ConfigureAwait(false);
        return Ics.Build(interview, attendees);
    }
}

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
    /// <summary>Renders a single-event VCALENDAR for the interview.</summary>
    /// <param name="interview">The interview.</param>
    /// <returns>The .ics text.</returns>
    public static string Build(Interview interview)
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
        sb.Append("END:VEVENT\r\n");
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

        return Ics.Build(interview);
    }
}

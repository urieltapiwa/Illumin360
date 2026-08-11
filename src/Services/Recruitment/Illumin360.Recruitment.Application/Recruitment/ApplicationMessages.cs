using Illumin360.Recruitment.Application.Abstractions;
using Illumin360.Recruitment.Domain;
using Illumin360.SharedKernel;
using ApplicationId = Illumin360.Recruitment.Domain.ApplicationId;

namespace Illumin360.Recruitment.Application.Recruitment;

/// <summary>A message in an application conversation.</summary>
/// <param name="Id">Message id.</param>
/// <param name="Sender">Sender side (recruiter/talent).</param>
/// <param name="SenderName">Sender display name.</param>
/// <param name="Body">Message body.</param>
/// <param name="SentAt">When sent (UTC).</param>
/// <param name="Read">Whether read by the other side.</param>
public sealed record MessageDto(Guid Id, string Sender, string SenderName, string Body, DateTimeOffset SentAt, bool Read)
{
    /// <summary>Projects a domain <see cref="ApplicationMessage"/> into the transport DTO.</summary>
    /// <param name="m">The message.</param>
    /// <returns>The transport DTO.</returns>
    public static MessageDto FromDomain(ApplicationMessage m)
    {
        ArgumentNullException.ThrowIfNull(m);
        return new MessageDto(m.Id, m.Sender.ToWire(), m.SenderName, m.Body, m.SentAt, m.IsRead);
    }
}

/// <summary>Lists an application's conversation, oldest first.</summary>
/// <param name="ApplicationId">The application id.</param>
public sealed record GetApplicationThreadQuery(Guid ApplicationId) : IQuery<IReadOnlyList<MessageDto>>;

/// <summary>Posts a message to an application conversation.</summary>
/// <param name="ApplicationId">The application id.</param>
/// <param name="Sender">Sender side (recruiter/talent).</param>
/// <param name="SenderName">Sender display name.</param>
/// <param name="Body">Message body.</param>
public sealed record SendApplicationMessageCommand(Guid ApplicationId, string? Sender, string SenderName, string Body) : ICommand<MessageDto>;

/// <summary>Marks the other side's messages in a conversation as read.</summary>
/// <param name="ApplicationId">The application id.</param>
/// <param name="Reader">The reading side (recruiter/talent) — its counterpart's messages are marked read.</param>
public sealed record MarkThreadReadCommand(Guid ApplicationId, string? Reader) : ICommand<int>;

/// <summary>Handles <see cref="GetApplicationThreadQuery"/>.</summary>
/// <param name="repository">The recruitment repository.</param>
public sealed class GetApplicationThreadQueryHandler(IRecruitmentRepository repository)
    : IQueryHandler<GetApplicationThreadQuery, IReadOnlyList<MessageDto>>
{
    private readonly IRecruitmentRepository _repository = repository;

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<MessageDto>>> HandleAsync(GetApplicationThreadQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        var messages = await _repository.ListApplicationMessagesAsync(query.ApplicationId, cancellationToken).ConfigureAwait(false);
        return messages.Select(MessageDto.FromDomain).ToList();
    }
}

/// <summary>Handles <see cref="SendApplicationMessageCommand"/>.</summary>
/// <param name="repository">The recruitment repository.</param>
public sealed class SendApplicationMessageCommandHandler(IRecruitmentRepository repository)
    : ICommandHandler<SendApplicationMessageCommand, MessageDto>
{
    private readonly IRecruitmentRepository _repository = repository;

    /// <inheritdoc />
    public async Task<Result<MessageDto>> HandleAsync(SendApplicationMessageCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var application = await _repository.GetApplicationAsync(new ApplicationId(command.ApplicationId), cancellationToken).ConfigureAwait(false);
        if (application is null)
        {
            return Error.NotFound("application.not_found", "No matching application was found.");
        }

        var creation = ApplicationMessage.Post(command.ApplicationId, command.Sender, command.SenderName, command.Body, DateTimeOffset.UtcNow);
        if (creation.IsFailure)
        {
            return creation.Error!;
        }

        _repository.AddApplicationMessage(creation.Value!);
        await _repository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return MessageDto.FromDomain(creation.Value!);
    }
}

/// <summary>Handles <see cref="MarkThreadReadCommand"/> — marks the counterpart's messages read.</summary>
/// <param name="repository">The recruitment repository.</param>
public sealed class MarkThreadReadCommandHandler(IRecruitmentRepository repository)
    : ICommandHandler<MarkThreadReadCommand, int>
{
    private readonly IRecruitmentRepository _repository = repository;

    /// <inheritdoc />
    public async Task<Result<int>> HandleAsync(MarkThreadReadCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (!MessageSenders.TryParse(command.Reader, out var reader))
        {
            return Error.Validation("message.sender_invalid", "Reader must be recruiter or talent.");
        }

        var messages = await _repository.ListApplicationMessagesTrackedAsync(command.ApplicationId, cancellationToken).ConfigureAwait(false);
        var now = DateTimeOffset.UtcNow;
        var count = 0;
        foreach (var message in messages.Where(m => m.Sender != reader && !m.IsRead))
        {
            message.MarkRead(now);
            count++;
        }

        if (count > 0)
        {
            await _repository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }

        return count;
    }
}

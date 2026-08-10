using Illumin360.Professionals.Application.Abstractions;
using Illumin360.SharedKernel;

namespace Illumin360.Professionals.Application.Professionals;

/// <summary>An in-app notification for the current professional.</summary>
/// <param name="Id">Notification id.</param>
/// <param name="Kind">Category.</param>
/// <param name="Text">Text.</param>
/// <param name="IsRead">Whether read.</param>
/// <param name="CreatedAt">Creation timestamp (UTC).</param>
public sealed record NotificationDto(Guid Id, string Kind, string Text, bool IsRead, DateTimeOffset CreatedAt);

/// <summary>Lists the current ("me") professional's notifications.</summary>
/// <param name="UnreadOnly">Whether to return only unread.</param>
public sealed record GetNotificationsQuery(bool UnreadOnly = false) : IQuery<IReadOnlyList<NotificationDto>>;

/// <summary>Marks one notification read.</summary>
/// <param name="Id">Notification id.</param>
public sealed record MarkNotificationReadCommand(Guid Id) : ICommand<bool>;

/// <summary>Marks all of the current ("me") professional's notifications read.</summary>
public sealed record MarkAllNotificationsReadCommand : ICommand<int>;

/// <summary>Handles <see cref="GetNotificationsQuery"/>.</summary>
/// <param name="repository">The professional repository.</param>
public sealed class GetNotificationsQueryHandler(IProfessionalRepository repository)
    : IQueryHandler<GetNotificationsQuery, IReadOnlyList<NotificationDto>>
{
    private readonly IProfessionalRepository _repository = repository;

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<NotificationDto>>> HandleAsync(GetNotificationsQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var meId = await _repository.GetDefaultProfessionalIdAsync(cancellationToken).ConfigureAwait(false);
        if (meId is not { } id)
        {
            return Array.Empty<NotificationDto>();
        }

        var items = await _repository.ListNotificationsAsync(id, query.UnreadOnly, cancellationToken).ConfigureAwait(false);
        return items.Select(n => new NotificationDto(n.Id, n.Kind, n.Text, n.IsRead, n.CreatedAt)).ToList();
    }
}

/// <summary>Handles <see cref="MarkNotificationReadCommand"/>.</summary>
/// <param name="repository">The professional repository.</param>
public sealed class MarkNotificationReadCommandHandler(IProfessionalRepository repository)
    : ICommandHandler<MarkNotificationReadCommand, bool>
{
    private readonly IProfessionalRepository _repository = repository;

    /// <inheritdoc />
    public async Task<Result<bool>> HandleAsync(MarkNotificationReadCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var notification = await _repository.GetNotificationAsync(command.Id, cancellationToken).ConfigureAwait(false);
        if (notification is null)
        {
            return Error.NotFound("notification.not_found", "No matching notification was found.");
        }

        notification.MarkRead();
        await _repository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return true;
    }
}

/// <summary>Handles <see cref="MarkAllNotificationsReadCommand"/>.</summary>
/// <param name="repository">The professional repository.</param>
public sealed class MarkAllNotificationsReadCommandHandler(IProfessionalRepository repository)
    : ICommandHandler<MarkAllNotificationsReadCommand, int>
{
    private readonly IProfessionalRepository _repository = repository;

    /// <inheritdoc />
    public async Task<Result<int>> HandleAsync(MarkAllNotificationsReadCommand command, CancellationToken cancellationToken)
    {
        var meId = await _repository.GetDefaultProfessionalIdAsync(cancellationToken).ConfigureAwait(false);
        if (meId is not { } id)
        {
            return 0;
        }

        return await _repository.MarkAllNotificationsReadAsync(id, cancellationToken).ConfigureAwait(false);
    }
}

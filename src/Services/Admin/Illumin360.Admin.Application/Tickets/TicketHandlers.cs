using Illumin360.Admin.Application.Abstractions;
using Illumin360.Admin.Domain;
using Illumin360.SharedKernel;
using IntegrationEvents = Illumin360.Admin.IntegrationEvents;

namespace Illumin360.Admin.Application.Tickets;

/// <summary>Lists tickets, optionally filtered by status (default: open).</summary>
/// <param name="Status">Status filter (open/assigned/resolved), or null for all.</param>
public sealed record GetTicketsQuery(string? Status = "open") : IQuery<IReadOnlyList<TicketDto>>;

/// <summary>The triage action to apply to a ticket.</summary>
public enum TicketAction
{
    /// <summary>Assign the ticket to the acting admin.</summary>
    Assign,

    /// <summary>Resolve the ticket.</summary>
    Resolve,
}

/// <summary>Assigns or resolves a support ticket.</summary>
/// <param name="Id">The ticket id.</param>
/// <param name="Action">Assign or resolve.</param>
/// <param name="ActingAdmin">Username of the acting admin (from the access token).</param>
public sealed record TriageTicketCommand(Guid Id, TicketAction Action, string ActingAdmin) : ICommand<TicketDto>;

/// <summary>Handles <see cref="GetTicketsQuery"/>.</summary>
/// <param name="repository">The ticket repository.</param>
public sealed class GetTicketsQueryHandler(ITicketRepository repository)
    : IQueryHandler<GetTicketsQuery, IReadOnlyList<TicketDto>>
{
    private readonly ITicketRepository _repository = repository;

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<TicketDto>>> HandleAsync(
        GetTicketsQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        var items = await _repository.ListAsync(query.Status, cancellationToken).ConfigureAwait(false);
        return Result<IReadOnlyList<TicketDto>>.Success([.. items.Select(TicketDto.FromDomain)]);
    }
}

/// <summary>Handles <see cref="TriageTicketCommand"/>.</summary>
/// <param name="repository">The ticket repository.</param>
/// <param name="publisher">The integration-event publisher (outbox-backed).</param>
public sealed class TriageTicketCommandHandler(ITicketRepository repository, IIntegrationEventPublisher publisher)
    : ICommandHandler<TriageTicketCommand, TicketDto>
{
    private readonly ITicketRepository _repository = repository;
    private readonly IIntegrationEventPublisher _publisher = publisher;

    /// <inheritdoc />
    public async Task<Result<TicketDto>> HandleAsync(TriageTicketCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var ticket = await _repository.GetByIdAsync(new TicketId(command.Id), cancellationToken).ConfigureAwait(false);
        if (ticket is null)
        {
            return Error.NotFound("ticket.not_found", "No matching ticket was found.");
        }

        var outcome = command.Action == TicketAction.Assign
            ? ticket.Assign(command.ActingAdmin)
            : ticket.Resolve(command.ActingAdmin);

        if (outcome.IsFailure)
        {
            return outcome.Error!;
        }

        foreach (var domainEvent in ticket.DomainEvents)
        {
            if (domainEvent is TicketTriaged triaged)
            {
                await _publisher.PublishAsync(
                    new IntegrationEvents.TicketTriaged(triaged.TicketId.Value, triaged.Status, triaged.Assignee, triaged.OccurredOn),
                    cancellationToken).ConfigureAwait(false);
            }
        }

        ticket.ClearDomainEvents();
        await _repository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return TicketDto.FromDomain(ticket);
    }
}

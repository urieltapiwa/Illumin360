using Illumin360.Admin.Domain;

namespace Illumin360.Admin.Application.Tickets;

/// <summary>Transport DTO for a support ticket.</summary>
/// <param name="Id">Ticket id.</param>
/// <param name="Subject">Subject.</param>
/// <param name="Priority">Priority band (P1/P2/P3).</param>
/// <param name="Requester">Who raised it.</param>
/// <param name="Status">Lifecycle state (open/assigned/resolved).</param>
/// <param name="Assignee">Assigned agent, if any.</param>
public sealed record TicketDto(Guid Id, string Subject, string Priority, string Requester, string Status, string? Assignee)
{
    /// <summary>Projects a domain <see cref="Ticket"/> into the transport DTO.</summary>
    /// <param name="t">The ticket.</param>
    /// <returns>The transport DTO.</returns>
    public static TicketDto FromDomain(Ticket t)
    {
        ArgumentNullException.ThrowIfNull(t);
        var status = t.Status switch
        {
            TicketStatus.Assigned => "assigned",
            TicketStatus.Resolved => "resolved",
            _ => "open",
        };
        return new TicketDto(t.Id.Value, t.Subject, t.Priority, t.Requester, status, t.Assignee);
    }
}

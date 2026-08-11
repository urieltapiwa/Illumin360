using Illumin360.Admin.Application.Abstractions;
using Illumin360.Admin.Domain;
using Illumin360.SharedKernel;

namespace Illumin360.Admin.Application.Audit;

/// <summary>A viewable audit-trail entry.</summary>
/// <param name="Id">Entry id.</param>
/// <param name="Actor">Acting admin.</param>
/// <param name="Action">Action code.</param>
/// <param name="EntityType">Entity kind.</param>
/// <param name="EntityId">Entity id, if any.</param>
/// <param name="Summary">Human-readable summary.</param>
/// <param name="OccurredAt">When it occurred (UTC).</param>
public sealed record AuditEntryDto(Guid Id, string Actor, string Action, string EntityType, string? EntityId, string Summary, DateTimeOffset OccurredAt)
{
    /// <summary>Projects a domain <see cref="AuditEntry"/> into the transport DTO.</summary>
    /// <param name="e">The entry.</param>
    /// <returns>The transport DTO.</returns>
    public static AuditEntryDto FromDomain(AuditEntry e)
    {
        ArgumentNullException.ThrowIfNull(e);
        return new AuditEntryDto(e.Id, e.Actor, e.Action, e.EntityType, e.EntityId, e.Summary, e.OccurredAt);
    }
}

/// <summary>Lists the audit trail, newest first, with optional action filter and paging.</summary>
/// <param name="Action">Optional action-code prefix filter.</param>
/// <param name="Page">1-based page number.</param>
/// <param name="PageSize">Page size (1–100).</param>
public sealed record GetAuditLogQuery(string? Action = null, int Page = 1, int PageSize = 50) : IQuery<IReadOnlyList<AuditEntryDto>>;

/// <summary>Handles <see cref="GetAuditLogQuery"/>.</summary>
/// <param name="repository">The audit repository.</param>
public sealed class GetAuditLogQueryHandler(IAuditRepository repository)
    : IQueryHandler<GetAuditLogQuery, IReadOnlyList<AuditEntryDto>>
{
    private readonly IAuditRepository _repository = repository;

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<AuditEntryDto>>> HandleAsync(GetAuditLogQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 1, 100);
        var entries = await _repository.ListAsync(query.Action, (page - 1) * pageSize, pageSize, cancellationToken).ConfigureAwait(false);
        return Result<IReadOnlyList<AuditEntryDto>>.Success([.. entries.Select(AuditEntryDto.FromDomain)]);
    }
}

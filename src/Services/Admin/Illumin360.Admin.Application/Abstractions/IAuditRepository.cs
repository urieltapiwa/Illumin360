using Illumin360.Admin.Domain;

namespace Illumin360.Admin.Application.Abstractions;

/// <summary>Append + read port for the administrative audit trail.</summary>
public interface IAuditRepository
{
    /// <summary>Stages a new audit entry for insertion (persisted with the acting command's SaveChanges).</summary>
    /// <param name="entry">The audit entry to add.</param>
    void Add(AuditEntry entry);

    /// <summary>Lists audit entries, newest first, optionally filtered by action prefix.</summary>
    /// <param name="action">Optional action-code prefix filter (e.g. <c>verification</c>).</param>
    /// <param name="skip">Records to skip.</param>
    /// <param name="take">Page size.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<IReadOnlyList<AuditEntry>> ListAsync(string? action, int skip, int take, CancellationToken cancellationToken);
}

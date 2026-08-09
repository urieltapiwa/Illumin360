using Illumin360.Admin.Application.Abstractions;
using Illumin360.SharedKernel;

namespace Illumin360.Admin.Application.Verifications;

/// <summary>Lists verifications, optionally filtered by status (default: pending).</summary>
/// <param name="Status">Status filter (pending/approved/rejected), or null for all.</param>
public sealed record GetVerificationsQuery(string? Status = "pending") : IQuery<IReadOnlyList<VerificationDto>>;

/// <summary>Handles <see cref="GetVerificationsQuery"/>.</summary>
/// <param name="repository">The verification repository.</param>
public sealed class GetVerificationsQueryHandler(IVerificationRepository repository)
    : IQueryHandler<GetVerificationsQuery, IReadOnlyList<VerificationDto>>
{
    private readonly IVerificationRepository _repository = repository;

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<VerificationDto>>> HandleAsync(
        GetVerificationsQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        var items = await _repository.ListAsync(query.Status, cancellationToken).ConfigureAwait(false);
        return Result<IReadOnlyList<VerificationDto>>.Success([.. items.Select(VerificationDto.FromDomain)]);
    }
}

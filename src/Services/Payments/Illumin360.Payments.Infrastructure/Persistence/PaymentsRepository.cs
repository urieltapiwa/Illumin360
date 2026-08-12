using Illumin360.Payments.Application.Abstractions;
using Illumin360.Payments.Domain;
using Microsoft.EntityFrameworkCore;

namespace Illumin360.Payments.Infrastructure.Persistence;

/// <summary>EF Core implementation of <see cref="IPaymentsRepository"/>.</summary>
/// <param name="db">The payments database context.</param>
public sealed class PaymentsRepository(PaymentsDbContext db) : IPaymentsRepository
{
    private readonly PaymentsDbContext _db = db;

    /// <inheritdoc />
    public void AddContract(Contract contract) => _db.Contracts.Add(contract);

    /// <inheritdoc />
    public async Task<Contract?> GetContractAsync(ContractId id, CancellationToken cancellationToken)
        => await _db.Contracts.FirstOrDefaultAsync(c => c.Id == id, cancellationToken).ConfigureAwait(false);

    /// <inheritdoc />
    public async Task<IReadOnlyList<Contract>> ListContractsAsync(Guid? clientId, Guid? talentId, CancellationToken cancellationToken)
    {
        var query = _db.Contracts.AsNoTracking().AsQueryable();
        if (clientId is { } c)
        {
            query = query.Where(x => x.ClientId == c);
        }

        if (talentId is { } t)
        {
            query = query.Where(x => x.TalentId == t);
        }

        return await query.OrderByDescending(x => x.CreatedAt).ToListAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public void AddMilestone(Milestone milestone) => _db.Milestones.Add(milestone);

    /// <inheritdoc />
    public async Task<Milestone?> GetMilestoneAsync(MilestoneId id, CancellationToken cancellationToken)
        => await _db.Milestones.FirstOrDefaultAsync(m => m.Id == id, cancellationToken).ConfigureAwait(false);

    /// <inheritdoc />
    public async Task<IReadOnlyList<Milestone>> ListMilestonesAsync(ContractId contractId, CancellationToken cancellationToken)
        => await _db.Milestones.AsNoTracking()
            .Where(m => m.ContractId == contractId)
            .OrderBy(m => m.Order)
            .ToListAsync(cancellationToken).ConfigureAwait(false);

    /// <inheritdoc />
    public void AddMovement(LedgerMovement movement) => _db.LedgerMovements.Add(movement);

    /// <inheritdoc />
    public async Task<IReadOnlyList<LedgerMovement>> ListMovementsAsync(ContractId contractId, CancellationToken cancellationToken)
        => await _db.LedgerMovements.AsNoTracking()
            .Where(l => l.ContractId == contractId)
            .OrderBy(l => l.CreatedAt)
            .ToListAsync(cancellationToken).ConfigureAwait(false);

    /// <inheritdoc />
    public void AddPayoutAccount(PayoutAccount account) => _db.PayoutAccounts.Add(account);

    /// <inheritdoc />
    public async Task<PayoutAccount?> GetPayoutAccountAsync(Guid talentId, CancellationToken cancellationToken)
        => await _db.PayoutAccounts.FirstOrDefaultAsync(p => p.TalentId == talentId, cancellationToken).ConfigureAwait(false);

    /// <inheritdoc />
    public async Task SaveChangesAsync(CancellationToken cancellationToken)
        => await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
}

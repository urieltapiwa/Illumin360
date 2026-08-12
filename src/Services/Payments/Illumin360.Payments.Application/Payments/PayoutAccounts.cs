using Illumin360.Payments.Application.Abstractions;
using Illumin360.Payments.Domain;
using Illumin360.SharedKernel;

namespace Illumin360.Payments.Application.Payments;

/// <summary>A talent's payout account.</summary>
/// <param name="TalentId">The talent.</param>
/// <param name="ProviderAccount">The provider payout reference.</param>
/// <param name="Status">Verification status (Pending/Verified).</param>
public sealed record PayoutAccountDto(Guid TalentId, string ProviderAccount, string Status);

/// <summary>Registers (or updates) a talent's payout account.</summary>
/// <param name="TalentId">The talent.</param>
/// <param name="ProviderAccount">The provider payout reference.</param>
public sealed record RegisterPayoutAccountCommand(Guid TalentId, string ProviderAccount) : ICommand<PayoutAccountDto>;

/// <summary>Marks a talent's payout account verified (KYC passed).</summary>
/// <param name="TalentId">The talent.</param>
public sealed record VerifyPayoutAccountCommand(Guid TalentId) : ICommand<PayoutAccountDto>;

/// <summary>Gets a talent's payout account.</summary>
/// <param name="TalentId">The talent.</param>
public sealed record GetPayoutAccountQuery(Guid TalentId) : IQuery<PayoutAccountDto>;

/// <summary>Handles <see cref="RegisterPayoutAccountCommand"/> (upsert — updating resets to Pending).</summary>
/// <param name="repository">The payments repository.</param>
public sealed class RegisterPayoutAccountCommandHandler(IPaymentsRepository repository) : ICommandHandler<RegisterPayoutAccountCommand, PayoutAccountDto>
{
    private readonly IPaymentsRepository _repository = repository;

    /// <inheritdoc />
    public async Task<Result<PayoutAccountDto>> HandleAsync(RegisterPayoutAccountCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        var now = DateTimeOffset.UtcNow;
        var existing = await _repository.GetPayoutAccountAsync(command.TalentId, cancellationToken).ConfigureAwait(false);
        if (existing is not null)
        {
            if (string.IsNullOrWhiteSpace(command.ProviderAccount))
            {
                return Error.Validation("payout.account_invalid", "A provider payout reference is required.");
            }

            existing.UpdateReference(command.ProviderAccount, now);
            await _repository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return new PayoutAccountDto(existing.TalentId, existing.ProviderAccount, existing.Status.ToString());
        }

        var created = PayoutAccount.Register(command.TalentId, command.ProviderAccount, now);
        if (created.IsFailure)
        {
            return created.Error!;
        }

        _repository.AddPayoutAccount(created.Value!);
        await _repository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return new PayoutAccountDto(created.Value!.TalentId, created.Value!.ProviderAccount, created.Value!.Status.ToString());
    }
}

/// <summary>Handles <see cref="VerifyPayoutAccountCommand"/>.</summary>
/// <param name="repository">The payments repository.</param>
public sealed class VerifyPayoutAccountCommandHandler(IPaymentsRepository repository) : ICommandHandler<VerifyPayoutAccountCommand, PayoutAccountDto>
{
    private readonly IPaymentsRepository _repository = repository;

    /// <inheritdoc />
    public async Task<Result<PayoutAccountDto>> HandleAsync(VerifyPayoutAccountCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        var account = await _repository.GetPayoutAccountAsync(command.TalentId, cancellationToken).ConfigureAwait(false);
        if (account is null)
        {
            return Error.NotFound("payout.not_found", "No payout account for that talent.");
        }

        account.Verify(DateTimeOffset.UtcNow);
        await _repository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return new PayoutAccountDto(account.TalentId, account.ProviderAccount, account.Status.ToString());
    }
}

/// <summary>Handles <see cref="GetPayoutAccountQuery"/>.</summary>
/// <param name="repository">The payments repository.</param>
public sealed class GetPayoutAccountQueryHandler(IPaymentsRepository repository) : IQueryHandler<GetPayoutAccountQuery, PayoutAccountDto>
{
    private readonly IPaymentsRepository _repository = repository;

    /// <inheritdoc />
    public async Task<Result<PayoutAccountDto>> HandleAsync(GetPayoutAccountQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        var account = await _repository.GetPayoutAccountAsync(query.TalentId, cancellationToken).ConfigureAwait(false);
        return account is null
            ? Error.NotFound("payout.not_found", "No payout account for that talent.")
            : new PayoutAccountDto(account.TalentId, account.ProviderAccount, account.Status.ToString());
    }
}

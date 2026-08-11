using Illumin360.Admin.Application.Abstractions;
using Illumin360.Admin.Domain;
using Illumin360.SharedKernel;
using IntegrationEvents = Illumin360.Admin.IntegrationEvents;

namespace Illumin360.Admin.Application.Accounts;

/// <summary>Lists accounts, optionally filtered by status (default: all).</summary>
/// <param name="Status">Status filter (active/suspended), or null for all.</param>
public sealed record GetAccountsQuery(string? Status = null) : IQuery<IReadOnlyList<AccountDto>>;

/// <summary>The account access change to apply.</summary>
public enum AccountAction
{
    /// <summary>Suspend the account.</summary>
    Suspend,

    /// <summary>Reactivate the account.</summary>
    Activate,
}

/// <summary>Suspends or reactivates a platform account.</summary>
/// <param name="Id">The account id.</param>
/// <param name="Action">Suspend or activate.</param>
/// <param name="ActingAdmin">Username of the acting admin (from the access token).</param>
public sealed record SetAccountStatusCommand(Guid Id, AccountAction Action, string ActingAdmin) : ICommand<AccountDto>;

/// <summary>Handles <see cref="GetAccountsQuery"/>.</summary>
/// <param name="repository">The account repository.</param>
public sealed class GetAccountsQueryHandler(IAccountRepository repository)
    : IQueryHandler<GetAccountsQuery, IReadOnlyList<AccountDto>>
{
    private readonly IAccountRepository _repository = repository;

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<AccountDto>>> HandleAsync(
        GetAccountsQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        var items = await _repository.ListAsync(query.Status, cancellationToken).ConfigureAwait(false);
        return Result<IReadOnlyList<AccountDto>>.Success([.. items.Select(AccountDto.FromDomain)]);
    }
}

/// <summary>Handles <see cref="SetAccountStatusCommand"/>.</summary>
/// <param name="repository">The account repository.</param>
/// <param name="publisher">The integration-event publisher (outbox-backed).</param>
/// <param name="audit">The audit-trail repository.</param>
public sealed class SetAccountStatusCommandHandler(IAccountRepository repository, IIntegrationEventPublisher publisher, IAuditRepository audit)
    : ICommandHandler<SetAccountStatusCommand, AccountDto>
{
    private readonly IAccountRepository _repository = repository;
    private readonly IIntegrationEventPublisher _publisher = publisher;
    private readonly IAuditRepository _audit = audit;

    /// <inheritdoc />
    public async Task<Result<AccountDto>> HandleAsync(SetAccountStatusCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var account = await _repository.GetByIdAsync(new AccountId(command.Id), cancellationToken).ConfigureAwait(false);
        if (account is null)
        {
            return Error.NotFound("account.not_found", "No matching account was found.");
        }

        var outcome = command.Action == AccountAction.Suspend
            ? account.Suspend(command.ActingAdmin)
            : account.Activate(command.ActingAdmin);

        if (outcome.IsFailure)
        {
            return outcome.Error!;
        }

        foreach (var domainEvent in account.DomainEvents)
        {
            if (domainEvent is AccountStatusChanged changed)
            {
                await _publisher.PublishAsync(
                    new IntegrationEvents.AccountStatusChanged(changed.AccountId.Value, changed.Status, changed.ChangedBy, changed.OccurredOn),
                    cancellationToken).ConfigureAwait(false);
            }
        }

        account.ClearDomainEvents();

        var verb = command.Action == AccountAction.Suspend ? "suspended" : "activated";
        _audit.Add(AuditEntry.Record(
            command.ActingAdmin,
            $"account.{verb}",
            "account",
            command.Id.ToString(),
            $"Account {verb} by {command.ActingAdmin}.",
            DateTimeOffset.UtcNow));

        await _repository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return AccountDto.FromDomain(account);
    }
}

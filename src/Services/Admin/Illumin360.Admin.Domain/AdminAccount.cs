using Illumin360.SharedKernel;

namespace Illumin360.Admin.Domain;

/// <summary>Strongly-typed identity for an <see cref="AdminAccount"/>.</summary>
/// <param name="Value">The underlying GUID.</param>
public readonly record struct AccountId(Guid Value)
{
    /// <summary>Creates a new random identity.</summary>
    /// <returns>A fresh <see cref="AccountId"/>.</returns>
    public static AccountId New() => new(Guid.NewGuid());

    /// <inheritdoc />
    public override string ToString() => Value.ToString();
}

/// <summary>Account access state.</summary>
public enum AccountStatus
{
    /// <summary>Active, can sign in and use the platform.</summary>
    Active,

    /// <summary>Suspended by an admin.</summary>
    Suspended,
}

/// <summary>
/// A platform account (talent or company) shown in the admin user-management view. Aggregate root of
/// the admin account-directory; suspend/activate is the admin action.
/// </summary>
public sealed class AdminAccount : Entity<AccountId>
{
    private AdminAccount(AccountId id)
        : base(id)
    {
    }

    /// <summary>Display name.</summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>Account kind ("Talent" or "Company").</summary>
    public string Kind { get; private set; } = string.Empty;

    /// <summary>Contact email.</summary>
    public string Email { get; private set; } = string.Empty;

    /// <summary>Current access state.</summary>
    public AccountStatus Status { get; private set; }

    /// <summary>Home city / region (used for the talent-by-region breakdown).</summary>
    public string Region { get; private set; } = string.Empty;

    /// <summary>When the account was created (UTC).</summary>
    public DateTimeOffset CreatedAt { get; private set; }

    /// <summary>Rehydrates an account from seed/storage with a fixed identity (raises no event).</summary>
    /// <param name="id">Identity.</param>
    /// <param name="name">Display name.</param>
    /// <param name="kind">"Talent" or "Company".</param>
    /// <param name="email">Contact email.</param>
    /// <param name="region">Home city / region.</param>
    /// <param name="createdAt">Creation timestamp (UTC).</param>
    /// <returns>The hydrated, active account.</returns>
    public static AdminAccount Seed(Guid id, string name, string kind, string email, string region, DateTimeOffset createdAt)
        => new(new AccountId(id))
        {
            Name = name,
            Kind = kind,
            Email = email,
            Region = region,
            Status = AccountStatus.Active,
            CreatedAt = createdAt,
        };

    /// <summary>Suspends the account.</summary>
    /// <param name="by">Acting admin username.</param>
    /// <returns>Success, or a conflict error if already suspended.</returns>
    public Result<AdminAccount> Suspend(string by) => Set(AccountStatus.Suspended, by);

    /// <summary>Reactivates the account.</summary>
    /// <param name="by">Acting admin username.</param>
    /// <returns>Success, or a conflict error if already active.</returns>
    public Result<AdminAccount> Activate(string by) => Set(AccountStatus.Active, by);

    private Result<AdminAccount> Set(AccountStatus target, string by)
    {
        if (Status == target)
        {
            return Error.Conflict("account.no_change", $"Account is already {target}.");
        }

        Status = target;
        Raise(new AccountStatusChanged(Id, target.ToString(), string.IsNullOrWhiteSpace(by) ? "admin" : by.Trim(), DateTimeOffset.UtcNow));
        return this;
    }
}

/// <summary>Raised when an account is suspended or reactivated.</summary>
/// <param name="AccountId">The account identity.</param>
/// <param name="Status">New status.</param>
/// <param name="ChangedBy">Acting admin username.</param>
/// <param name="OccurredOn">When it occurred (UTC).</param>
public sealed record AccountStatusChanged(AccountId AccountId, string Status, string ChangedBy, DateTimeOffset OccurredOn) : IDomainEvent;

using Illumin360.Admin.Domain;

namespace Illumin360.Admin.Application.Accounts;

/// <summary>Transport DTO for a platform account in the admin user-management view.</summary>
/// <param name="Id">Account id.</param>
/// <param name="Name">Display name.</param>
/// <param name="Kind">"Talent" or "Company".</param>
/// <param name="Email">Contact email.</param>
/// <param name="Status">Access state (active/suspended).</param>
/// <param name="Region">Home city / region.</param>
public sealed record AccountDto(Guid Id, string Name, string Kind, string Email, string Status, string Region)
{
    /// <summary>Projects a domain <see cref="AdminAccount"/> into the transport DTO.</summary>
    /// <param name="a">The account.</param>
    /// <returns>The transport DTO.</returns>
    public static AccountDto FromDomain(AdminAccount a)
    {
        ArgumentNullException.ThrowIfNull(a);
        var status = a.Status == AccountStatus.Suspended ? "suspended" : "active";
        return new AccountDto(a.Id.Value, a.Name, a.Kind, a.Email, status, a.Region);
    }
}

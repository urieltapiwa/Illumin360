using Illumin360.Admin.Application.Abstractions;
using Illumin360.Admin.Domain;
using Illumin360.SharedKernel;

namespace Illumin360.Admin.Application.Dashboard;

/// <summary>Platform-operations summary for the Admin portal dashboard.</summary>
/// <param name="TotalAccounts">All platform accounts.</param>
/// <param name="ActiveAccounts">Accounts in the active state.</param>
/// <param name="SuspendedAccounts">Accounts an admin has suspended.</param>
/// <param name="Companies">Accounts of kind "Company".</param>
/// <param name="Talent">Accounts of kind "Talent".</param>
/// <param name="PendingVerifications">Verifications awaiting a decision.</param>
/// <param name="OpenTickets">Support tickets not yet resolved.</param>
/// <param name="AccountMix">Company vs talent counts, for the donut.</param>
public sealed record AdminSummaryDto(
    int TotalAccounts,
    int ActiveAccounts,
    int SuspendedAccounts,
    int Companies,
    int Talent,
    int PendingVerifications,
    int OpenTickets,
    IReadOnlyList<int> AccountMix);

/// <summary>Talent-marketplace insights for the Business portal dashboard.</summary>
/// <param name="TotalTalent">Talent accounts on the platform.</param>
/// <param name="TotalCompanies">Company accounts on the platform.</param>
/// <param name="ActiveCompanies">Company accounts in the active state.</param>
/// <param name="VerifiedEntities">Verifications approved to date.</param>
/// <param name="PendingReview">Verifications still pending.</param>
/// <param name="Mix">Talent vs companies, for the donut.</param>
public sealed record TalentInsightsDto(
    int TotalTalent,
    int TotalCompanies,
    int ActiveCompanies,
    int VerifiedEntities,
    int PendingReview,
    IReadOnlyList<int> Mix);

/// <summary>Support-queue metrics for the Support portal dashboard.</summary>
/// <param name="Open">Open (untriaged) tickets.</param>
/// <param name="Assigned">Tickets assigned to an agent.</param>
/// <param name="Resolved">Resolved tickets.</param>
/// <param name="P1">Priority-1 tickets outstanding (open or assigned).</param>
/// <param name="P2">Priority-2 tickets outstanding.</param>
/// <param name="P3">Priority-3 tickets outstanding.</param>
/// <param name="PriorityMix">P1/P2/P3 outstanding counts, for the donut.</param>
public sealed record SupportSummaryDto(
    int Open,
    int Assigned,
    int Resolved,
    int P1,
    int P2,
    int P3,
    IReadOnlyList<int> PriorityMix);

/// <summary>Platform-operations summary (accounts, verifications, tickets).</summary>
public sealed record GetAdminSummaryQuery : IQuery<AdminSummaryDto>;

/// <summary>Talent-marketplace insights derived from the account directory and verification queue.</summary>
public sealed record GetTalentInsightsQuery : IQuery<TalentInsightsDto>;

/// <summary>Support-queue metrics derived from the ticket board.</summary>
public sealed record GetSupportSummaryQuery : IQuery<SupportSummaryDto>;

/// <summary>Handles <see cref="GetAdminSummaryQuery"/> by aggregating the admin repositories.</summary>
/// <param name="accounts">The account repository.</param>
/// <param name="verifications">The verification repository.</param>
/// <param name="tickets">The ticket repository.</param>
public sealed class GetAdminSummaryQueryHandler(
    IAccountRepository accounts,
    IVerificationRepository verifications,
    ITicketRepository tickets)
    : IQueryHandler<GetAdminSummaryQuery, AdminSummaryDto>
{
    private readonly IAccountRepository _accounts = accounts;
    private readonly IVerificationRepository _verifications = verifications;
    private readonly ITicketRepository _tickets = tickets;

    /// <inheritdoc />
    public async Task<Result<AdminSummaryDto>> HandleAsync(GetAdminSummaryQuery query, CancellationToken cancellationToken)
    {
        var allAccounts = await _accounts.ListAsync(null, cancellationToken).ConfigureAwait(false);
        var allVerifications = await _verifications.ListAsync(null, cancellationToken).ConfigureAwait(false);
        var allTickets = await _tickets.ListAsync(null, cancellationToken).ConfigureAwait(false);

        var active = allAccounts.Count(a => a.Status == AccountStatus.Active);
        var suspended = allAccounts.Count(a => a.Status == AccountStatus.Suspended);
        var companies = allAccounts.Count(a => string.Equals(a.Kind, "Company", StringComparison.OrdinalIgnoreCase));
        var talent = allAccounts.Count - companies;
        var pending = allVerifications.Count(v => v.Status == VerificationStatus.Pending);
        var openTickets = allTickets.Count(t => t.Status != TicketStatus.Resolved);

        return Result<AdminSummaryDto>.Success(new AdminSummaryDto(
            allAccounts.Count,
            active,
            suspended,
            companies,
            talent,
            pending,
            openTickets,
            [companies, talent]));
    }
}

/// <summary>Handles <see cref="GetTalentInsightsQuery"/>.</summary>
/// <param name="accounts">The account repository.</param>
/// <param name="verifications">The verification repository.</param>
public sealed class GetTalentInsightsQueryHandler(
    IAccountRepository accounts,
    IVerificationRepository verifications)
    : IQueryHandler<GetTalentInsightsQuery, TalentInsightsDto>
{
    private readonly IAccountRepository _accounts = accounts;
    private readonly IVerificationRepository _verifications = verifications;

    /// <inheritdoc />
    public async Task<Result<TalentInsightsDto>> HandleAsync(GetTalentInsightsQuery query, CancellationToken cancellationToken)
    {
        var allAccounts = await _accounts.ListAsync(null, cancellationToken).ConfigureAwait(false);
        var allVerifications = await _verifications.ListAsync(null, cancellationToken).ConfigureAwait(false);

        var companies = allAccounts.Count(a => string.Equals(a.Kind, "Company", StringComparison.OrdinalIgnoreCase));
        var talent = allAccounts.Count - companies;
        var activeCompanies = allAccounts.Count(a =>
            string.Equals(a.Kind, "Company", StringComparison.OrdinalIgnoreCase) && a.Status == AccountStatus.Active);
        var verified = allVerifications.Count(v => v.Status == VerificationStatus.Approved);
        var pending = allVerifications.Count(v => v.Status == VerificationStatus.Pending);

        return Result<TalentInsightsDto>.Success(new TalentInsightsDto(
            talent,
            companies,
            activeCompanies,
            verified,
            pending,
            [talent, companies]));
    }
}

/// <summary>Handles <see cref="GetSupportSummaryQuery"/>.</summary>
/// <param name="tickets">The ticket repository.</param>
public sealed class GetSupportSummaryQueryHandler(ITicketRepository tickets)
    : IQueryHandler<GetSupportSummaryQuery, SupportSummaryDto>
{
    private readonly ITicketRepository _tickets = tickets;

    /// <inheritdoc />
    public async Task<Result<SupportSummaryDto>> HandleAsync(GetSupportSummaryQuery query, CancellationToken cancellationToken)
    {
        var all = await _tickets.ListAsync(null, cancellationToken).ConfigureAwait(false);

        var open = all.Count(t => t.Status == TicketStatus.Open);
        var assigned = all.Count(t => t.Status == TicketStatus.Assigned);
        var resolved = all.Count(t => t.Status == TicketStatus.Resolved);

        static bool Outstanding(Ticket t) => t.Status != TicketStatus.Resolved;
        var p1 = all.Count(t => Outstanding(t) && string.Equals(t.Priority, "P1", StringComparison.OrdinalIgnoreCase));
        var p2 = all.Count(t => Outstanding(t) && string.Equals(t.Priority, "P2", StringComparison.OrdinalIgnoreCase));
        var p3 = all.Count(t => Outstanding(t) && string.Equals(t.Priority, "P3", StringComparison.OrdinalIgnoreCase));

        return Result<SupportSummaryDto>.Success(new SupportSummaryDto(
            open, assigned, resolved, p1, p2, p3, [p1, p2, p3]));
    }
}

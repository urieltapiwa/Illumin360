using Illumin360.Recruitment.Application.Abstractions;
using Illumin360.Recruitment.Domain;
using Illumin360.SharedKernel;

namespace Illumin360.Recruitment.Application.Recruitment;

/// <summary>An employee/network referral of a candidate for a requisition.</summary>
/// <param name="Id">Referral id.</param>
/// <param name="RequestId">The requisition referred for.</param>
/// <param name="ReferrerName">The referrer's name.</param>
/// <param name="ReferrerEmail">The referrer's email, if any.</param>
/// <param name="CandidateName">The referred candidate's name.</param>
/// <param name="CandidateEmail">The referred candidate's email.</param>
/// <param name="Note">Optional note.</param>
/// <param name="CreatedAt">When submitted (UTC).</param>
public sealed record ReferralDto(Guid Id, Guid RequestId, string ReferrerName, string? ReferrerEmail, string CandidateName, string CandidateEmail, string? Note, DateTimeOffset CreatedAt)
{
    /// <summary>Projects a domain <see cref="Referral"/> into the transport DTO.</summary>
    /// <param name="r">The referral.</param>
    /// <returns>The transport DTO.</returns>
    public static ReferralDto FromDomain(Referral r)
    {
        ArgumentNullException.ThrowIfNull(r);
        return new ReferralDto(r.Id, r.RequestId, r.ReferrerName, r.ReferrerEmail, r.CandidateName, r.CandidateEmail, r.Note, r.CreatedAt);
    }
}

/// <summary>Lists a requisition's referrals, newest first.</summary>
/// <param name="RequestId">The requisition id.</param>
public sealed record GetReferralsQuery(Guid RequestId) : IQuery<IReadOnlyList<ReferralDto>>;

/// <summary>Submits a referral for a requisition.</summary>
/// <param name="RequestId">The requisition id.</param>
/// <param name="ReferrerName">The referrer's name.</param>
/// <param name="ReferrerEmail">The referrer's email.</param>
/// <param name="CandidateName">The candidate's name.</param>
/// <param name="CandidateEmail">The candidate's email.</param>
/// <param name="Note">Optional note.</param>
public sealed record SubmitReferralCommand(Guid RequestId, string ReferrerName, string? ReferrerEmail, string CandidateName, string CandidateEmail, string? Note) : ICommand<ReferralDto>;

/// <summary>Handles <see cref="GetReferralsQuery"/>.</summary>
/// <param name="repository">The recruitment repository.</param>
public sealed class GetReferralsQueryHandler(IRecruitmentRepository repository)
    : IQueryHandler<GetReferralsQuery, IReadOnlyList<ReferralDto>>
{
    private readonly IRecruitmentRepository _repository = repository;

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<ReferralDto>>> HandleAsync(GetReferralsQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        var referrals = await _repository.ListReferralsAsync(query.RequestId, cancellationToken).ConfigureAwait(false);
        return referrals.Select(ReferralDto.FromDomain).ToList();
    }
}

/// <summary>Handles <see cref="SubmitReferralCommand"/>.</summary>
/// <param name="repository">The recruitment repository.</param>
public sealed class SubmitReferralCommandHandler(IRecruitmentRepository repository)
    : ICommandHandler<SubmitReferralCommand, ReferralDto>
{
    private readonly IRecruitmentRepository _repository = repository;

    /// <inheritdoc />
    public async Task<Result<ReferralDto>> HandleAsync(SubmitReferralCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var request = await _repository.GetByIdAsync(new RequestId(command.RequestId), cancellationToken).ConfigureAwait(false);
        if (request is null)
        {
            return Error.NotFound("request.not_found", "No matching requisition was found.");
        }

        var creation = Referral.Create(
            command.RequestId,
            command.ReferrerName,
            command.ReferrerEmail,
            command.CandidateName,
            command.CandidateEmail,
            command.Note,
            DateTimeOffset.UtcNow);
        if (creation.IsFailure)
        {
            return creation.Error!;
        }

        _repository.AddReferral(creation.Value!);
        await _repository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return ReferralDto.FromDomain(creation.Value!);
    }
}

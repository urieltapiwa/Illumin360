using Illumin360.Professionals.Application.Abstractions;
using Illumin360.Professionals.Domain;
using Illumin360.SharedKernel;

namespace Illumin360.Professionals.Application.Professionals;

/// <summary>An endorsement / reference for a skill.</summary>
/// <param name="Id">Endorsement id.</param>
/// <param name="Endorser">Who endorsed.</param>
/// <param name="Note">Optional reference note.</param>
/// <param name="CreatedAt">When endorsed (UTC).</param>
public sealed record SkillEndorsementDto(Guid Id, string Endorser, string? Note, DateTimeOffset CreatedAt)
{
    /// <summary>Projects a domain <see cref="SkillEndorsement"/> into the transport DTO.</summary>
    /// <param name="e">The endorsement.</param>
    /// <returns>The transport DTO.</returns>
    public static SkillEndorsementDto FromDomain(SkillEndorsement e)
    {
        ArgumentNullException.ThrowIfNull(e);
        return new SkillEndorsementDto(e.Id, e.Endorser, e.Note, e.CreatedAt);
    }
}

/// <summary>Endorses a professional's skill (a peer/recruiter reference).</summary>
/// <param name="SkillId">The skill id.</param>
/// <param name="Endorser">Endorser name.</param>
/// <param name="Note">Optional reference note.</param>
public sealed record EndorseSkillCommand(Guid SkillId, string Endorser, string? Note) : ICommand<SkillEndorsementDto>;

/// <summary>Lists a skill's endorsements, newest first.</summary>
/// <param name="SkillId">The skill id.</param>
public sealed record GetSkillEndorsementsQuery(Guid SkillId) : IQuery<IReadOnlyList<SkillEndorsementDto>>;

/// <summary>Handles <see cref="EndorseSkillCommand"/>.</summary>
/// <param name="repository">The professional repository.</param>
public sealed class EndorseSkillCommandHandler(IProfessionalRepository repository)
    : ICommandHandler<EndorseSkillCommand, SkillEndorsementDto>
{
    private readonly IProfessionalRepository _repository = repository;

    /// <inheritdoc />
    public async Task<Result<SkillEndorsementDto>> HandleAsync(EndorseSkillCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var skill = await _repository.GetSkillByIdAsync(command.SkillId, cancellationToken).ConfigureAwait(false);
        if (skill is null)
        {
            return Error.NotFound("skill.not_found", "No matching skill was found.");
        }

        var creation = SkillEndorsement.Create(command.SkillId, command.Endorser, command.Note, DateTimeOffset.UtcNow);
        if (creation.IsFailure)
        {
            return creation.Error!;
        }

        if (await _repository.EndorsementExistsAsync(command.SkillId, creation.Value!.Endorser, cancellationToken).ConfigureAwait(false))
        {
            return Error.Conflict("endorsement.exists", "This person has already endorsed this skill.");
        }

        _repository.AddEndorsement(creation.Value!);
        skill.Endorse();
        await _repository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return SkillEndorsementDto.FromDomain(creation.Value!);
    }
}

/// <summary>Handles <see cref="GetSkillEndorsementsQuery"/>.</summary>
/// <param name="repository">The professional repository.</param>
public sealed class GetSkillEndorsementsQueryHandler(IProfessionalRepository repository)
    : IQueryHandler<GetSkillEndorsementsQuery, IReadOnlyList<SkillEndorsementDto>>
{
    private readonly IProfessionalRepository _repository = repository;

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<SkillEndorsementDto>>> HandleAsync(GetSkillEndorsementsQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        var endorsements = await _repository.ListEndorsementsAsync(query.SkillId, cancellationToken).ConfigureAwait(false);
        return endorsements.Select(SkillEndorsementDto.FromDomain).ToList();
    }
}

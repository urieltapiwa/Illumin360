using Illumin360.Candidates.Application.Abstractions;
using Illumin360.Candidates.Domain;
using Illumin360.SharedKernel;

namespace Illumin360.Candidates.Application.Candidates;

/// <summary>A recruiter talent pool (shortlist).</summary>
/// <param name="Id">Pool id.</param>
/// <param name="Name">Pool name.</param>
/// <param name="MemberCount">Number of candidates in the pool.</param>
public sealed record TalentPoolDto(Guid Id, string Name, int MemberCount);

/// <summary>A candidate in a pool.</summary>
/// <param name="CandidateId">Candidate id.</param>
/// <param name="Name">Candidate full name.</param>
/// <param name="City">Candidate city.</param>
public sealed record PoolMemberDto(Guid CandidateId, string Name, string City);

/// <summary>Creates a talent pool.</summary>
public sealed record CreateTalentPoolCommand(string Name) : ICommand<TalentPoolDto>;

/// <summary>Adds a candidate to a pool.</summary>
public sealed record AddToPoolCommand(Guid PoolId, Guid CandidateId) : ICommand<bool>;

/// <summary>Removes a candidate from a pool.</summary>
public sealed record RemoveFromPoolCommand(Guid PoolId, Guid CandidateId) : ICommand<bool>;

/// <summary>Lists all talent pools.</summary>
public sealed record GetPoolsQuery : IQuery<IReadOnlyList<TalentPoolDto>>;

/// <summary>Lists a pool's members (enriched with candidate details).</summary>
public sealed record GetPoolMembersQuery(Guid PoolId) : IQuery<IReadOnlyList<PoolMemberDto>>;

/// <summary>Handles <see cref="CreateTalentPoolCommand"/>.</summary>
/// <param name="repository">The candidate repository.</param>
public sealed class CreateTalentPoolCommandHandler(ICandidateRepository repository)
    : ICommandHandler<CreateTalentPoolCommand, TalentPoolDto>
{
    private readonly ICandidateRepository _repository = repository;

    /// <inheritdoc />
    public async Task<Result<TalentPoolDto>> HandleAsync(CreateTalentPoolCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var creation = TalentPool.Create(command.Name, DateTimeOffset.UtcNow);
        if (creation.IsFailure)
        {
            return creation.Error!;
        }

        _repository.AddPool(creation.Value!);
        await _repository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return new TalentPoolDto(creation.Value!.Id.Value, creation.Value!.Name, 0);
    }
}

/// <summary>Handles <see cref="AddToPoolCommand"/>.</summary>
/// <param name="repository">The candidate repository.</param>
public sealed class AddToPoolCommandHandler(ICandidateRepository repository)
    : ICommandHandler<AddToPoolCommand, bool>
{
    private readonly ICandidateRepository _repository = repository;

    /// <inheritdoc />
    public async Task<Result<bool>> HandleAsync(AddToPoolCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var poolId = new TalentPoolId(command.PoolId);
        if (await _repository.GetPoolAsync(poolId, cancellationToken).ConfigureAwait(false) is null)
        {
            return Error.NotFound("pool.not_found", "No matching talent pool was found.");
        }

        var candidateId = new CandidateId(command.CandidateId);
        if (await _repository.GetByIdAsync(candidateId, cancellationToken).ConfigureAwait(false) is null)
        {
            return Error.NotFound("candidate.not_found", "No matching candidate was found.");
        }

        if (await _repository.GetPoolMemberAsync(poolId, candidateId, cancellationToken).ConfigureAwait(false) is not null)
        {
            return Error.Conflict("pool.already_member", "The candidate is already in this pool.");
        }

        _repository.AddPoolMember(new TalentPoolMember(Guid.NewGuid(), poolId, candidateId, DateTimeOffset.UtcNow));
        await _repository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return true;
    }
}

/// <summary>Handles <see cref="RemoveFromPoolCommand"/>.</summary>
/// <param name="repository">The candidate repository.</param>
public sealed class RemoveFromPoolCommandHandler(ICandidateRepository repository)
    : ICommandHandler<RemoveFromPoolCommand, bool>
{
    private readonly ICandidateRepository _repository = repository;

    /// <inheritdoc />
    public async Task<Result<bool>> HandleAsync(RemoveFromPoolCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var member = await _repository.GetPoolMemberAsync(new TalentPoolId(command.PoolId), new CandidateId(command.CandidateId), cancellationToken).ConfigureAwait(false);
        if (member is null)
        {
            return Error.NotFound("pool.member_not_found", "The candidate is not in this pool.");
        }

        _repository.RemovePoolMember(member);
        await _repository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return true;
    }
}

/// <summary>Handles <see cref="GetPoolsQuery"/>.</summary>
/// <param name="repository">The candidate repository.</param>
public sealed class GetPoolsQueryHandler(ICandidateRepository repository)
    : IQueryHandler<GetPoolsQuery, IReadOnlyList<TalentPoolDto>>
{
    private readonly ICandidateRepository _repository = repository;

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<TalentPoolDto>>> HandleAsync(GetPoolsQuery query, CancellationToken cancellationToken)
    {
        var pools = await _repository.ListPoolsAsync(cancellationToken).ConfigureAwait(false);
        var result = new List<TalentPoolDto>(pools.Count);
        foreach (var pool in pools)
        {
            var members = await _repository.ListPoolMembersAsync(pool.Id, cancellationToken).ConfigureAwait(false);
            result.Add(new TalentPoolDto(pool.Id.Value, pool.Name, members.Count));
        }

        return result;
    }
}

/// <summary>Handles <see cref="GetPoolMembersQuery"/>.</summary>
/// <param name="repository">The candidate repository.</param>
public sealed class GetPoolMembersQueryHandler(ICandidateRepository repository)
    : IQueryHandler<GetPoolMembersQuery, IReadOnlyList<PoolMemberDto>>
{
    private readonly ICandidateRepository _repository = repository;

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<PoolMemberDto>>> HandleAsync(GetPoolMembersQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var poolId = new TalentPoolId(query.PoolId);
        if (await _repository.GetPoolAsync(poolId, cancellationToken).ConfigureAwait(false) is null)
        {
            return Error.NotFound("pool.not_found", "No matching talent pool was found.");
        }

        var members = await _repository.ListPoolMembersAsync(poolId, cancellationToken).ConfigureAwait(false);
        var rows = new List<PoolMemberDto>(members.Count);
        foreach (var member in members)
        {
            var candidate = await _repository.GetByIdAsync(member.CandidateId, cancellationToken).ConfigureAwait(false);
            if (candidate is not null)
            {
                rows.Add(new PoolMemberDto(candidate.Id.Value, $"{candidate.FirstName} {candidate.LastName}".Trim(), candidate.City));
            }
        }

        return rows;
    }
}

using Illumin360.Candidates.Application.Abstractions;
using Illumin360.Candidates.Domain;
using Illumin360.SharedKernel;

namespace Illumin360.Candidates.Application.Candidates;

/// <summary>Right-to-be-forgotten: permanently erases a candidate and all their owned data.</summary>
/// <param name="CandidateId">The candidate id.</param>
public sealed record EraseCandidateCommand(Guid CandidateId) : ICommand<bool>;

/// <summary>Handles <see cref="EraseCandidateCommand"/>.</summary>
/// <param name="repository">The candidate repository.</param>
public sealed class EraseCandidateCommandHandler(ICandidateRepository repository)
    : ICommandHandler<EraseCandidateCommand, bool>
{
    private readonly ICandidateRepository _repository = repository;

    /// <inheritdoc />
    public async Task<Result<bool>> HandleAsync(EraseCandidateCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var candidateId = new CandidateId(command.CandidateId);
        var candidate = await _repository.GetByIdAsync(candidateId, cancellationToken).ConfigureAwait(false);
        if (candidate is null)
        {
            return Error.NotFound("candidate.not_found", "No matching candidate was found.");
        }

        await _repository.EraseCandidateAsync(candidateId, cancellationToken).ConfigureAwait(false);
        return true;
    }
}

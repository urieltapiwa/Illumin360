using Illumin360.Professionals.Application.Abstractions;
using Illumin360.Professionals.Domain;
using Illumin360.SharedKernel;

namespace Illumin360.Professionals.Application.Professionals;

/// <summary>Action a professional takes on a surfaced match.</summary>
public enum MatchAction
{
    /// <summary>Save for later.</summary>
    Save,

    /// <summary>Dismiss / not interested.</summary>
    Dismiss,

    /// <summary>Apply to the match.</summary>
    Apply,
}

/// <summary>Saves, dismisses, or applies to a match on the current ("me") professional.</summary>
/// <param name="MatchId">The match id.</param>
/// <param name="Action">Save / dismiss / apply.</param>
public sealed record UpdateMatchStatusCommand(Guid MatchId, MatchAction Action) : ICommand<MatchDto>;

/// <summary>Updates the current ("me") professional's availability label.</summary>
/// <param name="Availability">New availability label.</param>
public sealed record SetAvailabilityCommand(string Availability) : ICommand<string>;

/// <summary>Handles <see cref="UpdateMatchStatusCommand"/>.</summary>
/// <param name="repository">The professional repository.</param>
public sealed class UpdateMatchStatusCommandHandler(IProfessionalRepository repository)
    : ICommandHandler<UpdateMatchStatusCommand, MatchDto>
{
    private readonly IProfessionalRepository _repository = repository;

    /// <inheritdoc />
    public async Task<Result<MatchDto>> HandleAsync(UpdateMatchStatusCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var meId = await _repository.GetDefaultProfessionalIdAsync(cancellationToken).ConfigureAwait(false);
        if (meId is not { } id)
        {
            return Error.NotFound("professional.not_found", "No professional profile found.");
        }

        var match = await _repository.GetMatchAsync(id, command.MatchId, cancellationToken).ConfigureAwait(false);
        if (match is null)
        {
            return Error.NotFound("match.not_found", "No matching opportunity was found.");
        }

        switch (command.Action)
        {
            case MatchAction.Save:
                match.Save();
                break;
            case MatchAction.Dismiss:
                match.Dismiss();
                break;
            case MatchAction.Apply:
            default:
                var applied = match.Apply();
                if (applied.IsFailure)
                {
                    return applied.Error!;
                }

                var me = await _repository.GetTrackedAsync(id, cancellationToken).ConfigureAwait(false);
                me?.RecordApplication();
                break;
        }

        await _repository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        var status = match.Status switch
        {
            MatchStatus.Saved => "saved",
            MatchStatus.Dismissed => "dismissed",
            MatchStatus.Applied => "applied",
            _ => "new",
        };
        return new MatchDto(match.Role, match.Company, match.City, match.Industry, match.MatchScore, match.SalaryLo, match.SalaryHi, match.Type, match.PostedLabel, match.Id, status);
    }
}

/// <summary>Handles <see cref="SetAvailabilityCommand"/>.</summary>
/// <param name="repository">The professional repository.</param>
public sealed class SetAvailabilityCommandHandler(IProfessionalRepository repository)
    : ICommandHandler<SetAvailabilityCommand, string>
{
    private readonly IProfessionalRepository _repository = repository;

    /// <inheritdoc />
    public async Task<Result<string>> HandleAsync(SetAvailabilityCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var meId = await _repository.GetDefaultProfessionalIdAsync(cancellationToken).ConfigureAwait(false);
        if (meId is not { } id)
        {
            return Error.NotFound("professional.not_found", "No professional profile found.");
        }

        var me = await _repository.GetTrackedAsync(id, cancellationToken).ConfigureAwait(false);
        if (me is null)
        {
            return Error.NotFound("professional.not_found", "No professional profile found.");
        }

        me.SetAvailability(command.Availability);
        await _repository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return me.Availability;
    }
}

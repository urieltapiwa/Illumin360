using Illumin360.Students.Application.Abstractions;
using Illumin360.Students.Domain;
using Illumin360.SharedKernel;

namespace Illumin360.Students.Application.Students;

/// <summary>Action a student takes on a surfaced internship/graduate match.</summary>
public enum MatchAction
{
    /// <summary>Save for later.</summary>
    Save,

    /// <summary>Dismiss / not interested.</summary>
    Dismiss,

    /// <summary>Apply to the match.</summary>
    Apply,
}

/// <summary>Saves, dismisses, or applies to a match on the current ("me") student.</summary>
/// <param name="MatchId">The match id.</param>
/// <param name="Action">Save / dismiss / apply.</param>
public sealed record UpdateMatchStatusCommand(Guid MatchId, MatchAction Action) : ICommand<MatchDto>;

/// <summary>Updates the current ("me") student's availability label.</summary>
/// <param name="Availability">New availability label.</param>
public sealed record SetAvailabilityCommand(string Availability) : ICommand<string>;

/// <summary>Handles <see cref="UpdateMatchStatusCommand"/>.</summary>
/// <param name="repository">The student repository.</param>
public sealed class UpdateMatchStatusCommandHandler(IStudentRepository repository)
    : ICommandHandler<UpdateMatchStatusCommand, MatchDto>
{
    private readonly IStudentRepository _repository = repository;

    /// <inheritdoc />
    public async Task<Result<MatchDto>> HandleAsync(UpdateMatchStatusCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var meId = await _repository.GetDefaultStudentIdAsync(cancellationToken).ConfigureAwait(false);
        if (meId is not { } id)
        {
            return Error.NotFound("student.not_found", "No student profile found.");
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
        return new MatchDto(match.Role, match.Company, match.City, match.MatchScore, match.StipendLo, match.StipendHi, match.Type, match.PostedLabel, match.Id, status);
    }
}

/// <summary>Handles <see cref="SetAvailabilityCommand"/>.</summary>
/// <param name="repository">The student repository.</param>
public sealed class SetAvailabilityCommandHandler(IStudentRepository repository)
    : ICommandHandler<SetAvailabilityCommand, string>
{
    private readonly IStudentRepository _repository = repository;

    /// <inheritdoc />
    public async Task<Result<string>> HandleAsync(SetAvailabilityCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var meId = await _repository.GetDefaultStudentIdAsync(cancellationToken).ConfigureAwait(false);
        if (meId is not { } id)
        {
            return Error.NotFound("student.not_found", "No student profile found.");
        }

        var me = await _repository.GetTrackedAsync(id, cancellationToken).ConfigureAwait(false);
        if (me is null)
        {
            return Error.NotFound("student.not_found", "No student profile found.");
        }

        me.SetAvailability(command.Availability);
        await _repository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return me.Availability;
    }
}

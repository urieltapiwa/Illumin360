using Illumin360.Professionals.Application.Abstractions;
using Illumin360.Professionals.Domain;
using Illumin360.SharedKernel;

namespace Illumin360.Professionals.Application.Professionals;

/// <summary>An editable skill with its proficiency.</summary>
/// <param name="Id">Skill id.</param>
/// <param name="Name">Skill name.</param>
/// <param name="Level">Proficiency (0–100).</param>
/// <param name="Trend">Market trend tag.</param>
/// <param name="Endorsements">Number of endorsements received.</param>
public sealed record EditableSkillDto(Guid Id, string Name, int Level, string Trend, int Endorsements)
{
    /// <summary>Projects a domain <see cref="ProfessionalSkill"/> into the transport DTO.</summary>
    /// <param name="s">The skill.</param>
    /// <returns>The transport DTO.</returns>
    public static EditableSkillDto FromDomain(ProfessionalSkill s)
    {
        ArgumentNullException.ThrowIfNull(s);
        return new EditableSkillDto(s.Id, s.Name, s.Level, s.Trend, s.Endorsements);
    }
}

/// <summary>Adds a skill (with proficiency) to the current ("me") professional's profile.</summary>
/// <param name="Name">Skill name.</param>
/// <param name="Level">Proficiency (0–100).</param>
public sealed record AddSkillCommand(string Name, int Level) : ICommand<EditableSkillDto>;

/// <summary>Updates the proficiency of one of the current professional's skills.</summary>
/// <param name="SkillId">Skill id.</param>
/// <param name="Level">New proficiency (0–100).</param>
public sealed record UpdateSkillLevelCommand(Guid SkillId, int Level) : ICommand<EditableSkillDto>;

/// <summary>Removes one of the current professional's skills.</summary>
/// <param name="SkillId">Skill id.</param>
public sealed record RemoveSkillCommand(Guid SkillId) : ICommand<bool>;

/// <summary>Handles <see cref="AddSkillCommand"/>.</summary>
/// <param name="repository">The professional repository.</param>
public sealed class AddSkillCommandHandler(IProfessionalRepository repository)
    : ICommandHandler<AddSkillCommand, EditableSkillDto>
{
    private const string DefaultTrend = "steady";

    private readonly IProfessionalRepository _repository = repository;

    /// <inheritdoc />
    public async Task<Result<EditableSkillDto>> HandleAsync(AddSkillCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (string.IsNullOrWhiteSpace(command.Name))
        {
            return Error.Validation("skill.name_required", "A skill name is required.");
        }

        if (command.Level is < 0 or > 100)
        {
            return Error.Validation("skill.level_invalid", "Proficiency must be between 0 and 100.");
        }

        var meId = await _repository.GetDefaultProfessionalIdAsync(cancellationToken).ConfigureAwait(false);
        if (meId is not { } id)
        {
            return Error.NotFound("professional.not_found", "No professional profile found.");
        }

        var name = command.Name.Trim();
        var existing = await _repository.ListSkillsAsync(id, cancellationToken).ConfigureAwait(false);
        if (existing.Any(s => string.Equals(s.Name, name, StringComparison.OrdinalIgnoreCase)))
        {
            return Error.Conflict("skill.exists", "That skill is already on your profile.");
        }

        var sort = existing.Count == 0 ? 0 : existing.Max(s => s.Sort) + 1;
        var skill = new ProfessionalSkill(Guid.NewGuid(), id, name, command.Level, DefaultTrend, sort);
        _repository.AddSkill(skill);
        await _repository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return EditableSkillDto.FromDomain(skill);
    }
}

/// <summary>Handles <see cref="UpdateSkillLevelCommand"/>.</summary>
/// <param name="repository">The professional repository.</param>
public sealed class UpdateSkillLevelCommandHandler(IProfessionalRepository repository)
    : ICommandHandler<UpdateSkillLevelCommand, EditableSkillDto>
{
    private readonly IProfessionalRepository _repository = repository;

    /// <inheritdoc />
    public async Task<Result<EditableSkillDto>> HandleAsync(UpdateSkillLevelCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var meId = await _repository.GetDefaultProfessionalIdAsync(cancellationToken).ConfigureAwait(false);
        if (meId is not { } id)
        {
            return Error.NotFound("professional.not_found", "No professional profile found.");
        }

        var skill = await _repository.GetSkillAsync(id, command.SkillId, cancellationToken).ConfigureAwait(false);
        if (skill is null)
        {
            return Error.NotFound("skill.not_found", "No matching skill was found.");
        }

        var update = skill.UpdateLevel(command.Level);
        if (update.IsFailure)
        {
            return update.Error!;
        }

        await _repository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return EditableSkillDto.FromDomain(skill);
    }
}

/// <summary>Handles <see cref="RemoveSkillCommand"/>.</summary>
/// <param name="repository">The professional repository.</param>
public sealed class RemoveSkillCommandHandler(IProfessionalRepository repository)
    : ICommandHandler<RemoveSkillCommand, bool>
{
    private readonly IProfessionalRepository _repository = repository;

    /// <inheritdoc />
    public async Task<Result<bool>> HandleAsync(RemoveSkillCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var meId = await _repository.GetDefaultProfessionalIdAsync(cancellationToken).ConfigureAwait(false);
        if (meId is not { } id)
        {
            return Error.NotFound("professional.not_found", "No professional profile found.");
        }

        var skill = await _repository.GetSkillAsync(id, command.SkillId, cancellationToken).ConfigureAwait(false);
        if (skill is null)
        {
            return Error.NotFound("skill.not_found", "No matching skill was found.");
        }

        _repository.RemoveSkill(skill);
        await _repository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return true;
    }
}

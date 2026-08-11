using Illumin360.Candidates.Application.Abstractions;
using Illumin360.Candidates.Domain;
using Illumin360.SharedKernel;

namespace Illumin360.Candidates.Application.Candidates;

/// <summary>An admin-defined candidate custom field.</summary>
/// <param name="Id">Definition id.</param>
/// <param name="Key">Stable machine key.</param>
/// <param name="Label">Human label.</param>
/// <param name="Kind">Input type (text/number/boolean/select).</param>
/// <param name="Options">Options (select only).</param>
/// <param name="SortOrder">Ordering.</param>
public sealed record CustomFieldDto(Guid Id, string Key, string Label, string Kind, IReadOnlyList<string> Options, int SortOrder)
{
    /// <summary>Projects a domain definition into the transport DTO.</summary>
    /// <param name="d">The definition.</param>
    /// <returns>The transport DTO.</returns>
    public static CustomFieldDto FromDomain(CustomFieldDefinition d)
    {
        ArgumentNullException.ThrowIfNull(d);
        return new CustomFieldDto(d.Id, d.Key, d.Label, d.Kind.ToWire(), d.Options, d.SortOrder);
    }
}

/// <summary>A candidate's value for a custom field, with the field's label/kind for display.</summary>
/// <param name="DefinitionId">The field id.</param>
/// <param name="Key">Field key.</param>
/// <param name="Label">Field label.</param>
/// <param name="Kind">Field kind.</param>
/// <param name="Value">The candidate's value.</param>
public sealed record CustomValueDto(Guid DefinitionId, string Key, string Label, string Kind, string Value);

/// <summary>A single custom-field value being submitted.</summary>
/// <param name="DefinitionId">The field id.</param>
/// <param name="Value">The value.</param>
public sealed record CustomValueInput(Guid DefinitionId, string? Value);

/// <summary>Lists the custom-field definitions (ascending sort order).</summary>
public sealed record GetCustomFieldsQuery : IQuery<IReadOnlyList<CustomFieldDto>>;

/// <summary>Adds a custom-field definition (appended). Duplicate keys are rejected.</summary>
/// <param name="Label">Label.</param>
/// <param name="Kind">Kind name.</param>
/// <param name="Options">Options (select only).</param>
public sealed record AddCustomFieldCommand(string Label, string? Kind, IReadOnlyList<string>? Options) : ICommand<CustomFieldDto>;

/// <summary>Removes a custom-field definition.</summary>
/// <param name="Id">The definition id.</param>
public sealed record RemoveCustomFieldCommand(Guid Id) : ICommand<bool>;

/// <summary>Lists a candidate's custom-field values (joined to definitions).</summary>
/// <param name="CandidateId">The candidate id.</param>
public sealed record GetCandidateCustomValuesQuery(Guid CandidateId) : IQuery<IReadOnlyList<CustomValueDto>>;

/// <summary>Sets (replaces) a candidate's custom-field values.</summary>
/// <param name="CandidateId">The candidate id.</param>
/// <param name="Values">The values.</param>
public sealed record SetCandidateCustomValuesCommand(Guid CandidateId, IReadOnlyList<CustomValueInput> Values) : ICommand<int>;

/// <summary>Handles <see cref="GetCustomFieldsQuery"/>.</summary>
/// <param name="repository">The candidate repository.</param>
public sealed class GetCustomFieldsQueryHandler(ICandidateRepository repository)
    : IQueryHandler<GetCustomFieldsQuery, IReadOnlyList<CustomFieldDto>>
{
    private readonly ICandidateRepository _repository = repository;

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<CustomFieldDto>>> HandleAsync(GetCustomFieldsQuery query, CancellationToken cancellationToken)
    {
        var fields = await _repository.ListCustomFieldsAsync(cancellationToken).ConfigureAwait(false);
        return fields.Select(CustomFieldDto.FromDomain).ToList();
    }
}

/// <summary>Handles <see cref="AddCustomFieldCommand"/>.</summary>
/// <param name="repository">The candidate repository.</param>
public sealed class AddCustomFieldCommandHandler(ICandidateRepository repository)
    : ICommandHandler<AddCustomFieldCommand, CustomFieldDto>
{
    private readonly ICandidateRepository _repository = repository;

    /// <inheritdoc />
    public async Task<Result<CustomFieldDto>> HandleAsync(AddCustomFieldCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var existing = await _repository.ListCustomFieldsAsync(cancellationToken).ConfigureAwait(false);
        if (!string.IsNullOrWhiteSpace(command.Label)
            && existing.Any(f => f.Key == CustomFieldDefinition.KeyFrom(command.Label)))
        {
            return Error.Conflict("customfield.duplicate", "A custom field with that name already exists.");
        }

        var nextOrder = existing.Count == 0 ? 0 : existing.Max(f => f.SortOrder) + 1;
        var creation = CustomFieldDefinition.Create(command.Label, command.Kind, command.Options, nextOrder, DateTimeOffset.UtcNow);
        if (creation.IsFailure)
        {
            return creation.Error!;
        }

        _repository.AddCustomField(creation.Value!);
        await _repository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return CustomFieldDto.FromDomain(creation.Value!);
    }
}

/// <summary>Handles <see cref="RemoveCustomFieldCommand"/>.</summary>
/// <param name="repository">The candidate repository.</param>
public sealed class RemoveCustomFieldCommandHandler(ICandidateRepository repository)
    : ICommandHandler<RemoveCustomFieldCommand, bool>
{
    private readonly ICandidateRepository _repository = repository;

    /// <inheritdoc />
    public async Task<Result<bool>> HandleAsync(RemoveCustomFieldCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var field = await _repository.GetCustomFieldAsync(command.Id, cancellationToken).ConfigureAwait(false);
        if (field is null)
        {
            return Error.NotFound("customfield.not_found", "No matching custom field was found.");
        }

        _repository.RemoveCustomField(field);
        await _repository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return true;
    }
}

/// <summary>Handles <see cref="GetCandidateCustomValuesQuery"/>.</summary>
/// <param name="repository">The candidate repository.</param>
public sealed class GetCandidateCustomValuesQueryHandler(ICandidateRepository repository)
    : IQueryHandler<GetCandidateCustomValuesQuery, IReadOnlyList<CustomValueDto>>
{
    private readonly ICandidateRepository _repository = repository;

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<CustomValueDto>>> HandleAsync(GetCandidateCustomValuesQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var fields = await _repository.ListCustomFieldsAsync(cancellationToken).ConfigureAwait(false);
        var values = await _repository.ListCandidateValuesAsync(query.CandidateId, cancellationToken).ConfigureAwait(false);
        var byDef = values.ToDictionary(v => v.DefinitionId, v => v.Value);

        // Return every defined field, with the candidate's value (empty when unset), in field order.
        return fields
            .Select(f => new CustomValueDto(f.Id, f.Key, f.Label, f.Kind.ToWire(), byDef.TryGetValue(f.Id, out var v) ? v : string.Empty))
            .ToList();
    }
}

/// <summary>Handles <see cref="SetCandidateCustomValuesCommand"/> — replaces the candidate's values.</summary>
/// <param name="repository">The candidate repository.</param>
public sealed class SetCandidateCustomValuesCommandHandler(ICandidateRepository repository)
    : ICommandHandler<SetCandidateCustomValuesCommand, int>
{
    private readonly ICandidateRepository _repository = repository;

    /// <inheritdoc />
    public async Task<Result<int>> HandleAsync(SetCandidateCustomValuesCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        if (command.CandidateId == Guid.Empty)
        {
            return Error.Validation("customvalue.candidate_required", "A candidate id is required.");
        }

        var definitions = (await _repository.ListCustomFieldsAsync(cancellationToken).ConfigureAwait(false))
            .Select(f => f.Id)
            .ToHashSet();

        var existing = await _repository.ListCandidateValuesTrackedAsync(command.CandidateId, cancellationToken).ConfigureAwait(false);
        foreach (var prior in existing)
        {
            _repository.RemoveCandidateValue(prior);
        }

        var now = DateTimeOffset.UtcNow;
        var saved = 0;
        foreach (var input in command.Values ?? [])
        {
            if (string.IsNullOrWhiteSpace(input.Value) || !definitions.Contains(input.DefinitionId))
            {
                continue; // skip blanks + values for unknown/removed fields
            }

            var creation = CandidateCustomValue.Create(command.CandidateId, input.DefinitionId, input.Value, now);
            if (creation.IsFailure)
            {
                return creation.Error!;
            }

            _repository.AddCandidateValue(creation.Value!);
            saved++;
        }

        await _repository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return saved;
    }
}

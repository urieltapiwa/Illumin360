using Illumin360.SharedKernel;

namespace Illumin360.Candidates.Domain;

/// <summary>The input type of an admin-defined custom field.</summary>
public enum CustomFieldKind
{
    /// <summary>Single-line free text.</summary>
    Text,

    /// <summary>Numeric.</summary>
    Number,

    /// <summary>Yes/no.</summary>
    Boolean,

    /// <summary>One choice from a fixed option list.</summary>
    Select,
}

/// <summary>Parsing helpers for <see cref="CustomFieldKind"/>.</summary>
public static class CustomFieldKinds
{
    /// <summary>Parses a kind name case-insensitively (defaults to text).</summary>
    /// <param name="value">The name.</param>
    /// <param name="kind">The parsed kind.</param>
    /// <returns>True if recognised.</returns>
    public static bool TryParse(string? value, out CustomFieldKind kind)
        => Enum.TryParse(value, ignoreCase: true, out kind) && Enum.IsDefined(kind);

    /// <summary>The canonical lower-case wire name.</summary>
    /// <param name="kind">The kind.</param>
    /// <returns>The lower-case name.</returns>
    public static string ToWire(this CustomFieldKind kind) => kind.ToString().ToLowerInvariant();
}

/// <summary>
/// An admin-defined custom field that applies to every candidate record (e.g. "Right to work",
/// "Notice period"). Service-owned + migration-managed. Values live in <see cref="CandidateCustomValue"/>.
/// </summary>
public sealed class CustomFieldDefinition : Entity<Guid>
{
    private CustomFieldDefinition(Guid id)
        : base(id)
    {
    }

    /// <summary>Stable machine key (unique, normalised lower-kebab).</summary>
    public string Key { get; private set; } = string.Empty;

    /// <summary>Human label shown to recruiters.</summary>
    public string Label { get; private set; } = string.Empty;

    /// <summary>The input type.</summary>
    public CustomFieldKind Kind { get; private set; }

    /// <summary>Options for a <see cref="CustomFieldKind.Select"/>, pipe-joined. Empty otherwise.</summary>
    public string OptionsCsv { get; private set; } = string.Empty;

    /// <summary>Ordering (ascending).</summary>
    public int SortOrder { get; private set; }

    /// <summary>When created (UTC).</summary>
    public DateTimeOffset CreatedAt { get; private set; }

    /// <summary>The select options (empty for non-select kinds).</summary>
    public IReadOnlyList<string> Options =>
        string.IsNullOrEmpty(OptionsCsv) ? [] : OptionsCsv.Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    /// <summary>Normalises a label into a stable machine key (lower-kebab).</summary>
    /// <param name="label">The label.</param>
    /// <returns>The key.</returns>
    public static string KeyFrom(string label)
    {
        ArgumentNullException.ThrowIfNull(label);
        var chars = label.Trim().ToLowerInvariant()
            .Select(c => char.IsLetterOrDigit(c) ? c : '-')
            .ToArray();
        return string.Join('-', new string(chars).Split('-', StringSplitOptions.RemoveEmptyEntries));
    }

    /// <summary>Defines a custom field.</summary>
    /// <param name="label">Label (required, ≤ 80 chars).</param>
    /// <param name="kind">Kind name (defaults to text).</param>
    /// <param name="options">Options for a select (≥ 2 required when kind is select).</param>
    /// <param name="sortOrder">Ordering.</param>
    /// <param name="createdAt">Creation timestamp (UTC).</param>
    /// <returns>The definition, or a validation error.</returns>
    public static Result<CustomFieldDefinition> Create(string label, string? kind, IReadOnlyList<string>? options, int sortOrder, DateTimeOffset createdAt)
    {
        if (string.IsNullOrWhiteSpace(label))
        {
            return Error.Validation("customfield.label_required", "A field label is required.");
        }

        if (label.Trim().Length > 80)
        {
            return Error.Validation("customfield.label_too_long", "A field label must be 80 characters or fewer.");
        }

        if (!CustomFieldKinds.TryParse(string.IsNullOrWhiteSpace(kind) ? "text" : kind, out var parsed))
        {
            return Error.Validation("customfield.kind_invalid", "The field type is not recognised.");
        }

        var key = KeyFrom(label);
        if (key.Length == 0)
        {
            return Error.Validation("customfield.label_invalid", "The label must contain letters or digits.");
        }

        var cleaned = (options ?? []).Select(o => o.Trim()).Where(o => o.Length > 0).ToList();
        if (parsed == CustomFieldKind.Select && cleaned.Count < 2)
        {
            return Error.Validation("customfield.options_required", "A select field needs at least two options.");
        }

        return new CustomFieldDefinition(Guid.NewGuid())
        {
            Key = key,
            Label = label.Trim(),
            Kind = parsed,
            OptionsCsv = parsed == CustomFieldKind.Select ? string.Join('|', cleaned) : string.Empty,
            SortOrder = sortOrder,
            CreatedAt = createdAt,
        };
    }
}

/// <summary>A candidate's value for a <see cref="CustomFieldDefinition"/>. Keyed by candidate + definition.</summary>
public sealed class CandidateCustomValue : Entity<Guid>
{
    private CandidateCustomValue(Guid id)
        : base(id)
    {
    }

    /// <summary>The candidate.</summary>
    public Guid CandidateId { get; private init; }

    /// <summary>The field definition.</summary>
    public Guid DefinitionId { get; private init; }

    /// <summary>The value (free text / number-as-text / "true"/"false" / chosen option).</summary>
    public string Value { get; private set; } = string.Empty;

    /// <summary>When captured (UTC).</summary>
    public DateTimeOffset CreatedAt { get; private set; }

    /// <summary>Records a candidate's value for a field.</summary>
    /// <param name="candidateId">The candidate (required).</param>
    /// <param name="definitionId">The field (required).</param>
    /// <param name="value">The value (≤ 2000 chars).</param>
    /// <param name="createdAt">Creation timestamp (UTC).</param>
    /// <returns>The value, or a validation error.</returns>
    public static Result<CandidateCustomValue> Create(Guid candidateId, Guid definitionId, string? value, DateTimeOffset createdAt)
    {
        if (candidateId == Guid.Empty)
        {
            return Error.Validation("customvalue.candidate_required", "A candidate id is required.");
        }

        if (definitionId == Guid.Empty)
        {
            return Error.Validation("customvalue.definition_required", "A field id is required.");
        }

        var trimmed = (value ?? string.Empty).Trim();
        if (trimmed.Length > 2000)
        {
            return Error.Validation("customvalue.too_long", "A value must be 2000 characters or fewer.");
        }

        return new CandidateCustomValue(Guid.NewGuid())
        {
            CandidateId = candidateId,
            DefinitionId = definitionId,
            Value = trimmed,
            CreatedAt = createdAt,
        };
    }
}

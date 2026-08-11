using Illumin360.SharedKernel;

namespace Illumin360.Recruitment.Domain;

/// <summary>The input type of an application-form question.</summary>
public enum QuestionKind
{
    /// <summary>Single-line free text.</summary>
    Text,

    /// <summary>Multi-line free text.</summary>
    Textarea,

    /// <summary>Yes/no.</summary>
    Boolean,

    /// <summary>Numeric.</summary>
    Number,

    /// <summary>One choice from a fixed option list.</summary>
    Select,
}

/// <summary>Parsing helpers for <see cref="QuestionKind"/>.</summary>
public static class QuestionKinds
{
    /// <summary>Parses a question-kind name case-insensitively (defaults to <see cref="QuestionKind.Text"/>).</summary>
    /// <param name="value">The name (e.g. <c>select</c>).</param>
    /// <param name="kind">The parsed kind.</param>
    /// <returns>True if recognised.</returns>
    public static bool TryParse(string? value, out QuestionKind kind)
        => Enum.TryParse(value, ignoreCase: true, out kind) && Enum.IsDefined(kind);

    /// <summary>The canonical lower-case wire name (e.g. <c>select</c>).</summary>
    /// <param name="kind">The kind.</param>
    /// <returns>The lower-case name.</returns>
    public static string ToWire(this QuestionKind kind) => kind.ToString().ToLowerInvariant();
}

/// <summary>
/// A configurable application-form / screening question attached to an (externally-seeded) recruitment
/// request. Service-owned + migration-managed; keyed by request id. Candidates answer these when they
/// apply (see <see cref="ApplicationAnswer"/>).
/// </summary>
public sealed class ApplicationFormQuestion : Entity<Guid>
{
    private ApplicationFormQuestion(Guid id)
        : base(id)
    {
    }

    /// <summary>The requisition this question belongs to.</summary>
    public Guid RequestId { get; private init; }

    /// <summary>The question text shown to the candidate.</summary>
    public string Label { get; private set; } = string.Empty;

    /// <summary>The input type.</summary>
    public QuestionKind Kind { get; private set; }

    /// <summary>Options for a <see cref="QuestionKind.Select"/>, stored pipe-joined ("a|b|c"). Empty otherwise.</summary>
    public string OptionsCsv { get; private set; } = string.Empty;

    /// <summary>Whether an answer is required.</summary>
    public bool Required { get; private set; }

    /// <summary>Ordering within the form (ascending).</summary>
    public int SortOrder { get; private set; }

    /// <summary>When the question was created (UTC).</summary>
    public DateTimeOffset CreatedAt { get; private set; }

    /// <summary>The select options (empty for non-select kinds).</summary>
    public IReadOnlyList<string> Options =>
        string.IsNullOrEmpty(OptionsCsv) ? [] : OptionsCsv.Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    /// <summary>Adds a question to a requisition's application form.</summary>
    /// <param name="requestId">The requisition (required).</param>
    /// <param name="label">Question text (required, ≤ 300 chars).</param>
    /// <param name="kind">Input-type name (defaults to text).</param>
    /// <param name="options">Options for a select question (required, ≥ 2, when kind is select).</param>
    /// <param name="required">Whether an answer is required.</param>
    /// <param name="sortOrder">Ordering within the form.</param>
    /// <param name="createdAt">Creation timestamp (UTC).</param>
    /// <returns>The question, or a validation error.</returns>
    public static Result<ApplicationFormQuestion> Create(
        Guid requestId,
        string label,
        string? kind,
        IReadOnlyList<string>? options,
        bool required,
        int sortOrder,
        DateTimeOffset createdAt)
    {
        if (requestId == Guid.Empty)
        {
            return Error.Validation("form.request_required", "A requisition id is required.");
        }

        if (string.IsNullOrWhiteSpace(label))
        {
            return Error.Validation("form.label_required", "A question label is required.");
        }

        if (label.Trim().Length > 300)
        {
            return Error.Validation("form.label_too_long", "A question label must be 300 characters or fewer.");
        }

        if (!QuestionKinds.TryParse(string.IsNullOrWhiteSpace(kind) ? "text" : kind, out var parsedKind))
        {
            return Error.Validation("form.kind_invalid", "The question type is not recognised.");
        }

        var cleaned = (options ?? [])
            .Select(o => o.Trim())
            .Where(o => o.Length > 0)
            .ToList();

        if (parsedKind == QuestionKind.Select && cleaned.Count < 2)
        {
            return Error.Validation("form.options_required", "A select question needs at least two options.");
        }

        return new ApplicationFormQuestion(Guid.NewGuid())
        {
            RequestId = requestId,
            Label = label.Trim(),
            Kind = parsedKind,
            OptionsCsv = parsedKind == QuestionKind.Select ? string.Join('|', cleaned) : string.Empty,
            Required = required,
            SortOrder = sortOrder,
            CreatedAt = createdAt,
        };
    }
}

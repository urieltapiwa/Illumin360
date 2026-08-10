using Illumin360.SharedKernel;

namespace Illumin360.Recruitment.Domain;

/// <summary>
/// A reusable requisition template a recruiter can save and later apply to create a pre-filled
/// requisition. Owned + migration-managed by the service. Tags are stored as a normalised, semicolon-
/// joined string (there is no need to query templates by individual tag).
/// </summary>
public sealed class JobTemplate : Entity<Guid>
{
    private JobTemplate(Guid id)
        : base(id)
    {
    }

    /// <summary>Unique template name.</summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>Default role title.</summary>
    public string Title { get; private set; } = string.Empty;

    /// <summary>Default city, if any.</summary>
    public string? City { get; private set; }

    /// <summary>Default number of positions.</summary>
    public int Positions { get; private set; }

    /// <summary>Default lower salary bound, if any.</summary>
    public int? SalaryMin { get; private set; }

    /// <summary>Default upper salary bound, if any.</summary>
    public int? SalaryMax { get; private set; }

    /// <summary>Default currency code.</summary>
    public string Currency { get; private set; } = "NAD";

    /// <summary>Default employment type.</summary>
    public EmploymentType EmploymentType { get; private set; }

    /// <summary>Default remote flag.</summary>
    public bool Remote { get; private set; }

    /// <summary>Normalised tags, semicolon-joined (internal storage).</summary>
    public string TagsCsv { get; private set; } = string.Empty;

    /// <summary>When the template was created (UTC).</summary>
    public DateTimeOffset CreatedAt { get; private set; }

    /// <summary>The template's tags as a list.</summary>
    public IReadOnlyList<string> Tags =>
        string.IsNullOrEmpty(TagsCsv) ? [] : TagsCsv.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    /// <summary>Creates a job template.</summary>
    /// <param name="name">Unique template name (required).</param>
    /// <param name="title">Default role title (required).</param>
    /// <param name="city">Default city, if any.</param>
    /// <param name="positions">Default positions (≥ 1).</param>
    /// <param name="salaryMin">Default lower salary bound (≥ 0), if any.</param>
    /// <param name="salaryMax">Default upper salary bound (≥ min), if any.</param>
    /// <param name="currency">Default currency code (defaults to NAD).</param>
    /// <param name="employmentType">Employment-type name.</param>
    /// <param name="remote">Default remote flag.</param>
    /// <param name="tags">Default tags.</param>
    /// <param name="createdAt">Creation timestamp (UTC).</param>
    /// <returns>The template, or a validation error.</returns>
    public static Result<JobTemplate> Create(string name, string title, string? city, int positions, int? salaryMin, int? salaryMax, string? currency, string? employmentType, bool remote, IEnumerable<string>? tags, DateTimeOffset createdAt)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return Error.Validation("template.name_required", "A template name is required.");
        }

        if (string.IsNullOrWhiteSpace(title))
        {
            return Error.Validation("template.title_required", "A role title is required.");
        }

        if (positions < 1)
        {
            return Error.Validation("template.positions_invalid", "Positions must be at least 1.");
        }

        if (salaryMin is < 0 || salaryMax is < 0)
        {
            return Error.Validation("template.salary_negative", "Salary values cannot be negative.");
        }

        if (salaryMin is { } lo && salaryMax is { } hi && lo > hi)
        {
            return Error.Validation("template.salary_range", "The minimum salary cannot exceed the maximum.");
        }

        var type = EmploymentType.FullTime;
        if (!string.IsNullOrWhiteSpace(employmentType) && !EmploymentTypes.TryParse(employmentType, out type))
        {
            return Error.Validation("template.employment_type_invalid", "Employment type must be one of fulltime, parttime, contract, internship or temporary.");
        }

        var normalizedTags = (tags ?? [])
            .Select(t => t.Trim().ToLowerInvariant())
            .Where(t => t.Length is > 0 and <= 40)
            .Distinct(StringComparer.Ordinal)
            .ToList();

        return new JobTemplate(Guid.NewGuid())
        {
            Name = name.Trim(),
            Title = title.Trim(),
            City = string.IsNullOrWhiteSpace(city) ? null : city.Trim(),
            Positions = positions,
            SalaryMin = salaryMin,
            SalaryMax = salaryMax,
            Currency = string.IsNullOrWhiteSpace(currency) ? "NAD" : currency.Trim().ToUpperInvariant(),
            EmploymentType = type,
            Remote = remote,
            TagsCsv = string.Join(';', normalizedTags),
            CreatedAt = createdAt,
        };
    }
}

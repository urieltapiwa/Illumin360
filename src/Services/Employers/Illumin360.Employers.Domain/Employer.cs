using Illumin360.SharedKernel;

namespace Illumin360.Employers.Domain;

/// <summary>
/// Aggregate root for a hiring company on the marketplace: its public company profile. Owned +
/// migration-managed by the Employers service (database-per-service — charter Part 13).
/// </summary>
public sealed class Employer : Entity<EmployerId>
{
    private Employer(EmployerId id)
        : base(id)
    {
    }

    private Employer(EmployerId id, string companyName, string industry, string city, string? website, string? about)
        : base(id)
    {
        CompanyName = companyName;
        Industry = industry;
        City = city;
        Website = website;
        About = about;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>Company name.</summary>
    public string CompanyName { get; private set; } = string.Empty;

    /// <summary>Industry / sector.</summary>
    public string Industry { get; private set; } = string.Empty;

    /// <summary>Head-office city.</summary>
    public string City { get; private set; } = string.Empty;

    /// <summary>Company website, if any.</summary>
    public string? Website { get; private set; }

    /// <summary>Short "about" blurb, if any.</summary>
    public string? About { get; private set; }

    /// <summary>When the profile was created (UTC).</summary>
    public DateTimeOffset CreatedAt { get; private set; }

    /// <summary>Registers a new employer profile.</summary>
    /// <param name="companyName">Company name (required).</param>
    /// <param name="industry">Industry (required).</param>
    /// <param name="city">City (required).</param>
    /// <param name="website">Optional website.</param>
    /// <param name="about">Optional about blurb (≤ 1000 chars).</param>
    /// <returns>The employer, or a validation error.</returns>
    public static Result<Employer> Register(string companyName, string industry, string city, string? website, string? about)
    {
        if (string.IsNullOrWhiteSpace(companyName))
        {
            return Error.Validation("employer.company_required", "A company name is required.");
        }

        if (string.IsNullOrWhiteSpace(industry))
        {
            return Error.Validation("employer.industry_required", "An industry is required.");
        }

        if (string.IsNullOrWhiteSpace(city))
        {
            return Error.Validation("employer.city_required", "A city is required.");
        }

        if (about is { Length: > 1000 })
        {
            return Error.Validation("employer.about_too_long", "About must be 1000 characters or fewer.");
        }

        return new Employer(EmployerId.New(), companyName.Trim(), industry.Trim(), city.Trim(), Clean(website), Clean(about));
    }

    /// <summary>Rehydrates a fully-specified employer for demo seeding / import (raises no event).</summary>
    /// <param name="id">Identity.</param>
    /// <param name="companyName">Company name.</param>
    /// <param name="industry">Industry.</param>
    /// <param name="city">City.</param>
    /// <param name="website">Website.</param>
    /// <param name="about">About blurb.</param>
    /// <param name="createdAt">Creation timestamp (UTC).</param>
    /// <returns>The hydrated employer.</returns>
    public static Employer Seed(Guid id, string companyName, string industry, string city, string? website, string? about, DateTimeOffset createdAt)
        => new(new EmployerId(id))
        {
            CompanyName = companyName,
            Industry = industry,
            City = city,
            Website = website,
            About = about,
            CreatedAt = createdAt,
        };

    /// <summary>Updates the editable profile fields (company name is fixed after registration).</summary>
    /// <param name="industry">Industry (required).</param>
    /// <param name="city">City (required).</param>
    /// <param name="website">Optional website.</param>
    /// <param name="about">Optional about blurb (≤ 1000 chars).</param>
    /// <returns>Success, or a validation error.</returns>
    public Result<Employer> UpdateProfile(string industry, string city, string? website, string? about)
    {
        if (string.IsNullOrWhiteSpace(industry))
        {
            return Error.Validation("employer.industry_required", "An industry is required.");
        }

        if (string.IsNullOrWhiteSpace(city))
        {
            return Error.Validation("employer.city_required", "A city is required.");
        }

        if (about is { Length: > 1000 })
        {
            return Error.Validation("employer.about_too_long", "About must be 1000 characters or fewer.");
        }

        Industry = industry.Trim();
        City = city.Trim();
        Website = Clean(website);
        About = Clean(about);
        return this;
    }

    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}

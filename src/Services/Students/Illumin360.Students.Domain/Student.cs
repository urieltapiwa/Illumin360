using Illumin360.SharedKernel;

namespace Illumin360.Students.Domain;

/// <summary>
/// Aggregate root for a student on the Illumin Futures programme: their academic profile,
/// career-readiness score and engagement counters. Skills, learning modules, internship
/// matches, pipeline stages and activity are modelled as related read entities in the same
/// bounded context (queried by <see cref="StudentId"/>).
/// </summary>
public sealed class Student : Entity<StudentId>
{
    private Student(StudentId id)
        : base(id)
    {
    }

    private Student(
        StudentId id,
        string firstName,
        string lastName,
        string field,
        string school,
        string year,
        string graduating,
        string program,
        string city)
        : base(id)
    {
        FirstName = firstName;
        LastName = lastName;
        Field = field;
        School = school;
        Year = year;
        Graduating = graduating;
        Program = program;
        City = city;
        ViewsTrend = [];
        CreatedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>Given name.</summary>
    public string FirstName { get; private set; } = string.Empty;

    /// <summary>Family name.</summary>
    public string LastName { get; private set; } = string.Empty;

    /// <summary>Field of study (e.g. "Computer Science").</summary>
    public string Field { get; private set; } = string.Empty;

    /// <summary>Institution the student attends.</summary>
    public string School { get; private set; } = string.Empty;

    /// <summary>Year of study label (e.g. "Final year").</summary>
    public string Year { get; private set; } = string.Empty;

    /// <summary>Expected graduation year label.</summary>
    public string Graduating { get; private set; } = string.Empty;

    /// <summary>Sponsoring programme (e.g. "Illumin Futures (CSR)").</summary>
    public string Program { get; private set; } = string.Empty;

    /// <summary>Home city.</summary>
    public string City { get; private set; } = string.Empty;

    /// <summary>Availability label shown to employers (e.g. "Open to internships").</summary>
    public string Availability { get; private set; } = "Open to internships";

    /// <summary>Career-readiness score (0–100).</summary>
    public int Readiness { get; private set; }

    /// <summary>Total profile views by employers.</summary>
    public int ProfileViews { get; private set; }

    /// <summary>Percentage change in profile views over the trend window.</summary>
    public int ViewsDelta { get; private set; }

    /// <summary>Number of mentor sessions attended.</summary>
    public int MentorSessions { get; private set; }

    /// <summary>Number of applications the student has submitted.</summary>
    public int ApplicationsCount { get; private set; }

    /// <summary>Profile-view counts per period, oldest first (drives the sparkline).</summary>
    public IReadOnlyList<int> ViewsTrend { get; private set; } = [];

    /// <summary>When the student record was created (UTC).</summary>
    public DateTimeOffset CreatedAt { get; private set; }

    /// <summary>Storage object key of the uploaded CV, or null if none.</summary>
    public string? CvObjectKey { get; private set; }

    /// <summary>Original file name of the uploaded CV, or null if none.</summary>
    public string? CvFileName { get; private set; }

    /// <summary>MIME type of the uploaded CV, or null if none.</summary>
    public string? CvContentType { get; private set; }

    /// <summary>Size in bytes of the uploaded CV.</summary>
    public long CvSize { get; private set; }

    /// <summary>When the CV was last uploaded (UTC), or null if none.</summary>
    public DateTimeOffset? CvUploadedAt { get; private set; }

    /// <summary>Whether a CV has been uploaded.</summary>
    public bool HasCv => CvObjectKey is not null;

    /// <summary>The student's full display name.</summary>
    public string FullName => $"{FirstName} {LastName}".Trim();

    /// <summary>Updates the student's availability label.</summary>
    /// <param name="availability">New availability label.</param>
    public void SetAvailability(string availability) =>
        Availability = string.IsNullOrWhiteSpace(availability) ? Availability : availability.Trim();

    /// <summary>Records an uploaded CV's storage location and metadata.</summary>
    /// <param name="objectKey">Storage object key.</param>
    /// <param name="fileName">Original file name.</param>
    /// <param name="contentType">MIME type.</param>
    /// <param name="size">Size in bytes.</param>
    /// <param name="uploadedAt">Upload timestamp (UTC).</param>
    public void SetCv(string objectKey, string fileName, string contentType, long size, DateTimeOffset uploadedAt)
    {
        CvObjectKey = objectKey;
        CvFileName = fileName;
        CvContentType = contentType;
        CvSize = size;
        CvUploadedAt = uploadedAt;
    }

    /// <summary>Records that the student submitted an application (bumps the counter).</summary>
    public void RecordApplication() => ApplicationsCount++;

    /// <summary>
    /// Registers a new student. Engagement counters and readiness start at zero — they accrue
    /// as the student uses the platform.
    /// </summary>
    /// <param name="firstName">Given name.</param>
    /// <param name="lastName">Family name.</param>
    /// <param name="field">Field of study.</param>
    /// <param name="school">Institution.</param>
    /// <param name="year">Year-of-study label.</param>
    /// <param name="graduating">Expected graduation year label.</param>
    /// <param name="program">Sponsoring programme.</param>
    /// <param name="city">Home city.</param>
    /// <returns>A successful <see cref="Result{T}"/> with the student, or a validation error.</returns>
    public static Result<Student> Register(
        string firstName,
        string lastName,
        string field,
        string school,
        string year,
        string graduating,
        string program,
        string city)
    {
        if (string.IsNullOrWhiteSpace(firstName))
        {
            return Error.Validation("student.first_name_required", "First name is required.");
        }

        if (string.IsNullOrWhiteSpace(lastName))
        {
            return Error.Validation("student.last_name_required", "Last name is required.");
        }

        if (string.IsNullOrWhiteSpace(field))
        {
            return Error.Validation("student.field_required", "Field of study is required.");
        }

        if (string.IsNullOrWhiteSpace(city))
        {
            return Error.Validation("student.city_required", "City is required.");
        }

        var student = new Student(
            StudentId.New(),
            firstName.Trim(),
            lastName.Trim(),
            field.Trim(),
            school?.Trim() ?? string.Empty,
            year?.Trim() ?? string.Empty,
            graduating?.Trim() ?? string.Empty,
            program?.Trim() ?? string.Empty,
            city.Trim());

        student.Raise(new StudentRegistered(student.Id, student.FullName, student.CreatedAt));
        return student;
    }

    /// <summary>
    /// Rehydrates a fully-specified student for demo seeding / data import. Unlike
    /// <see cref="Register"/> this sets engagement counters and readiness directly and raises no
    /// domain event — it represents already-existing state, not a new registration.
    /// </summary>
    /// <param name="id">The student's identity.</param>
    /// <param name="firstName">Given name.</param>
    /// <param name="lastName">Family name.</param>
    /// <param name="field">Field of study.</param>
    /// <param name="school">Institution.</param>
    /// <param name="year">Year-of-study label.</param>
    /// <param name="graduating">Expected graduation year label.</param>
    /// <param name="program">Sponsoring programme.</param>
    /// <param name="city">Home city.</param>
    /// <param name="readiness">Career-readiness score (0–100).</param>
    /// <param name="profileViews">Total profile views.</param>
    /// <param name="viewsDelta">Percentage change in views.</param>
    /// <param name="mentorSessions">Mentor sessions attended.</param>
    /// <param name="applicationsCount">Applications submitted.</param>
    /// <param name="viewsTrend">Profile-view trend, oldest first.</param>
    /// <param name="createdAt">Creation timestamp (UTC).</param>
    /// <returns>The hydrated student.</returns>
    public static Student Seed(
        Guid id,
        string firstName,
        string lastName,
        string field,
        string school,
        string year,
        string graduating,
        string program,
        string city,
        int readiness,
        int profileViews,
        int viewsDelta,
        int mentorSessions,
        int applicationsCount,
        IReadOnlyList<int> viewsTrend,
        DateTimeOffset createdAt)
        => new(new StudentId(id))
        {
            FirstName = firstName,
            LastName = lastName,
            Field = field,
            School = school,
            Year = year,
            Graduating = graduating,
            Program = program,
            City = city,
            Readiness = readiness,
            ProfileViews = profileViews,
            ViewsDelta = viewsDelta,
            MentorSessions = mentorSessions,
            ApplicationsCount = applicationsCount,
            ViewsTrend = viewsTrend,
            CreatedAt = createdAt,
        };
}

/// <summary>Raised when a new <see cref="Student"/> is registered.</summary>
/// <param name="StudentId">The new student's identity.</param>
/// <param name="FullName">The student's full name.</param>
/// <param name="OccurredOn">When registration occurred (UTC).</param>
public sealed record StudentRegistered(StudentId StudentId, string FullName, DateTimeOffset OccurredOn) : IDomainEvent;

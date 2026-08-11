using Illumin360.Candidates.Application.Abstractions;
using Illumin360.Candidates.Domain;
using Illumin360.SharedKernel;

namespace Illumin360.Candidates.Application.Candidates;

/// <summary>The stored CV metadata included in a data export (never the file bytes).</summary>
/// <param name="HasCv">Whether a CV is on file.</param>
/// <param name="FileName">Original CV file name, if any.</param>
/// <param name="ContentType">CV MIME type, if any.</param>
/// <param name="UploadedAt">When the CV was uploaded (UTC), if any.</param>
public sealed record CandidateExportCv(bool HasCv, string? FileName, string? ContentType, DateTimeOffset? UploadedAt);

/// <summary>
/// A GDPR subject-access export of everything the Candidates service holds about one candidate:
/// their profile, recruiter notes, tags and CV metadata.
/// </summary>
/// <param name="Id">Candidate id.</param>
/// <param name="FirstName">Given name.</param>
/// <param name="LastName">Family name.</param>
/// <param name="City">City.</param>
/// <param name="Nationality">Nationality.</param>
/// <param name="Availability">Availability status.</param>
/// <param name="PublicHeadline">Public headline, if any.</param>
/// <param name="CreatedAt">When the record was created (UTC).</param>
/// <param name="Cv">Stored CV metadata.</param>
/// <param name="Notes">Recruiter notes held about the candidate.</param>
/// <param name="Tags">Tags applied to the candidate.</param>
/// <param name="GeneratedAt">When the export was generated (UTC).</param>
public sealed record CandidateExportDto(
    Guid Id,
    string FirstName,
    string LastName,
    string City,
    string Nationality,
    string Availability,
    string? PublicHeadline,
    DateTimeOffset CreatedAt,
    CandidateExportCv Cv,
    IReadOnlyList<CandidateNoteDto> Notes,
    IReadOnlyList<string> Tags,
    DateTimeOffset GeneratedAt);

/// <summary>Query: assemble a GDPR data export for a candidate.</summary>
/// <param name="CandidateId">The candidate id.</param>
public sealed record GetCandidateExportQuery(Guid CandidateId) : IQuery<CandidateExportDto>;

/// <summary>Handles <see cref="GetCandidateExportQuery"/>.</summary>
/// <param name="repository">The candidate repository.</param>
public sealed class GetCandidateExportQueryHandler(ICandidateRepository repository)
    : IQueryHandler<GetCandidateExportQuery, CandidateExportDto>
{
    private readonly ICandidateRepository _repository = repository;

    /// <inheritdoc />
    public async Task<Result<CandidateExportDto>> HandleAsync(GetCandidateExportQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var candidateId = new CandidateId(query.CandidateId);
        var candidate = await _repository.GetByIdAsync(candidateId, cancellationToken).ConfigureAwait(false);
        if (candidate is null)
        {
            return Error.NotFound("candidate.not_found", "No matching candidate was found.");
        }

        var notes = await _repository.ListNotesAsync(candidateId, cancellationToken).ConfigureAwait(false);
        var tags = await _repository.ListTagsAsync(candidateId, cancellationToken).ConfigureAwait(false);

        return new CandidateExportDto(
            candidate.Id.Value,
            candidate.FirstName,
            candidate.LastName,
            candidate.City,
            candidate.Nationality,
            candidate.Availability.ToString(),
            candidate.PublicHeadline,
            candidate.CreatedAt,
            new CandidateExportCv(candidate.HasCv, candidate.CvFileName, candidate.CvContentType, candidate.CvUploadedAt),
            notes.Select(CandidateNoteDto.FromDomain).ToList(),
            tags.Select(t => t.Label).ToList(),
            DateTimeOffset.UtcNow);
    }
}

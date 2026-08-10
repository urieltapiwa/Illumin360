using Illumin360.Candidates.Application.Abstractions;
using Illumin360.Candidates.Domain;
using Illumin360.SharedKernel;

namespace Illumin360.Candidates.Application.Candidates;

/// <summary>A recruiter note on a candidate.</summary>
/// <param name="Id">Note id.</param>
/// <param name="Author">Author display name.</param>
/// <param name="Body">Note body.</param>
/// <param name="CreatedAt">When created (UTC).</param>
public sealed record CandidateNoteDto(Guid Id, string Author, string Body, DateTimeOffset CreatedAt)
{
    /// <summary>Projects a domain <see cref="CandidateNote"/> into the transport DTO.</summary>
    /// <param name="n">The note.</param>
    /// <returns>The transport DTO.</returns>
    public static CandidateNoteDto FromDomain(CandidateNote n)
    {
        ArgumentNullException.ThrowIfNull(n);
        return new CandidateNoteDto(n.Id, n.Author, n.Body, n.CreatedAt);
    }
}

/// <summary>Lists a candidate's recruiter notes, newest first.</summary>
/// <param name="CandidateId">The candidate id.</param>
public sealed record GetCandidateNotesQuery(Guid CandidateId) : IQuery<IReadOnlyList<CandidateNoteDto>>;

/// <summary>Adds a recruiter note to a candidate.</summary>
/// <param name="CandidateId">The candidate id.</param>
/// <param name="Author">Author display name.</param>
/// <param name="Body">Note body.</param>
public sealed record AddCandidateNoteCommand(Guid CandidateId, string? Author, string Body) : ICommand<CandidateNoteDto>;

/// <summary>Removes a recruiter note.</summary>
/// <param name="NoteId">The note id.</param>
public sealed record RemoveCandidateNoteCommand(Guid NoteId) : ICommand<bool>;

/// <summary>Lists a candidate's tags.</summary>
/// <param name="CandidateId">The candidate id.</param>
public sealed record GetCandidateTagsQuery(Guid CandidateId) : IQuery<IReadOnlyList<string>>;

/// <summary>Adds a tag to a candidate (idempotent per label).</summary>
/// <param name="CandidateId">The candidate id.</param>
/// <param name="Label">The tag label.</param>
public sealed record AddCandidateTagCommand(Guid CandidateId, string Label) : ICommand<IReadOnlyList<string>>;

/// <summary>Removes a tag from a candidate.</summary>
/// <param name="CandidateId">The candidate id.</param>
/// <param name="Label">The tag label.</param>
public sealed record RemoveCandidateTagCommand(Guid CandidateId, string Label) : ICommand<IReadOnlyList<string>>;

/// <summary>Handles <see cref="GetCandidateNotesQuery"/>.</summary>
/// <param name="repository">The candidate repository.</param>
public sealed class GetCandidateNotesQueryHandler(ICandidateRepository repository)
    : IQueryHandler<GetCandidateNotesQuery, IReadOnlyList<CandidateNoteDto>>
{
    private readonly ICandidateRepository _repository = repository;

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<CandidateNoteDto>>> HandleAsync(GetCandidateNotesQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        var notes = await _repository.ListNotesAsync(new CandidateId(query.CandidateId), cancellationToken).ConfigureAwait(false);
        return notes.Select(CandidateNoteDto.FromDomain).ToList();
    }
}

/// <summary>Handles <see cref="AddCandidateNoteCommand"/>.</summary>
/// <param name="repository">The candidate repository.</param>
public sealed class AddCandidateNoteCommandHandler(ICandidateRepository repository)
    : ICommandHandler<AddCandidateNoteCommand, CandidateNoteDto>
{
    private readonly ICandidateRepository _repository = repository;

    /// <inheritdoc />
    public async Task<Result<CandidateNoteDto>> HandleAsync(AddCandidateNoteCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var candidateId = new CandidateId(command.CandidateId);
        var candidate = await _repository.GetByIdAsync(candidateId, cancellationToken).ConfigureAwait(false);
        if (candidate is null)
        {
            return Error.NotFound("candidate.not_found", "No matching candidate was found.");
        }

        var creation = CandidateNote.Create(candidateId, command.Author, command.Body, DateTimeOffset.UtcNow);
        if (creation.IsFailure)
        {
            return creation.Error!;
        }

        _repository.AddNote(creation.Value!);
        await _repository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return CandidateNoteDto.FromDomain(creation.Value!);
    }
}

/// <summary>Handles <see cref="RemoveCandidateNoteCommand"/>.</summary>
/// <param name="repository">The candidate repository.</param>
public sealed class RemoveCandidateNoteCommandHandler(ICandidateRepository repository)
    : ICommandHandler<RemoveCandidateNoteCommand, bool>
{
    private readonly ICandidateRepository _repository = repository;

    /// <inheritdoc />
    public async Task<Result<bool>> HandleAsync(RemoveCandidateNoteCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var note = await _repository.GetNoteAsync(command.NoteId, cancellationToken).ConfigureAwait(false);
        if (note is null)
        {
            return Error.NotFound("note.not_found", "No matching note was found.");
        }

        _repository.RemoveNote(note);
        await _repository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return true;
    }
}

/// <summary>Handles <see cref="GetCandidateTagsQuery"/>.</summary>
/// <param name="repository">The candidate repository.</param>
public sealed class GetCandidateTagsQueryHandler(ICandidateRepository repository)
    : IQueryHandler<GetCandidateTagsQuery, IReadOnlyList<string>>
{
    private readonly ICandidateRepository _repository = repository;

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<string>>> HandleAsync(GetCandidateTagsQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        var tags = await _repository.ListTagsAsync(new CandidateId(query.CandidateId), cancellationToken).ConfigureAwait(false);
        return tags.Select(t => t.Label).ToList();
    }
}

/// <summary>Handles <see cref="AddCandidateTagCommand"/>. Idempotent — re-adding an existing tag is a no-op.</summary>
/// <param name="repository">The candidate repository.</param>
public sealed class AddCandidateTagCommandHandler(ICandidateRepository repository)
    : ICommandHandler<AddCandidateTagCommand, IReadOnlyList<string>>
{
    private readonly ICandidateRepository _repository = repository;

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<string>>> HandleAsync(AddCandidateTagCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var candidateId = new CandidateId(command.CandidateId);
        var candidate = await _repository.GetByIdAsync(candidateId, cancellationToken).ConfigureAwait(false);
        if (candidate is null)
        {
            return Error.NotFound("candidate.not_found", "No matching candidate was found.");
        }

        var creation = CandidateTag.Create(candidateId, command.Label, DateTimeOffset.UtcNow);
        if (creation.IsFailure)
        {
            return creation.Error!;
        }

        var tag = creation.Value!;
        if (!await _repository.TagExistsAsync(candidateId, tag.Label, cancellationToken).ConfigureAwait(false))
        {
            _repository.AddTag(tag);
            await _repository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }

        var tags = await _repository.ListTagsAsync(candidateId, cancellationToken).ConfigureAwait(false);
        return tags.Select(t => t.Label).ToList();
    }
}

/// <summary>Handles <see cref="RemoveCandidateTagCommand"/>.</summary>
/// <param name="repository">The candidate repository.</param>
public sealed class RemoveCandidateTagCommandHandler(ICandidateRepository repository)
    : ICommandHandler<RemoveCandidateTagCommand, IReadOnlyList<string>>
{
    private readonly ICandidateRepository _repository = repository;

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<string>>> HandleAsync(RemoveCandidateTagCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var candidateId = new CandidateId(command.CandidateId);
        var label = (command.Label ?? string.Empty).Trim().ToLowerInvariant();
        var tag = await _repository.GetTagAsync(candidateId, label, cancellationToken).ConfigureAwait(false);
        if (tag is not null)
        {
            _repository.RemoveTag(tag);
            await _repository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }

        var tags = await _repository.ListTagsAsync(candidateId, cancellationToken).ConfigureAwait(false);
        return tags.Select(t => t.Label).ToList();
    }
}

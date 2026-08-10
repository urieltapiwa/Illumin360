using Illumin360.Recruitment.Application.Abstractions;
using Illumin360.Recruitment.Domain;
using Illumin360.SharedKernel;

namespace Illumin360.Recruitment.Application.Recruitment;

/// <summary>A talent's saved search.</summary>
/// <param name="Id">Saved-search id.</param>
/// <param name="Label">Label.</param>
/// <param name="City">Optional city filter.</param>
/// <param name="Keyword">Optional title keyword.</param>
/// <param name="AlertsEnabled">Whether job alerts are on.</param>
public sealed record SavedSearchDto(Guid Id, string Label, string? City, string? Keyword, bool AlertsEnabled)
{
    /// <summary>Projects a domain <see cref="SavedSearch"/> into the transport DTO.</summary>
    /// <param name="s">The saved search.</param>
    /// <returns>The transport DTO.</returns>
    public static SavedSearchDto FromDomain(SavedSearch s)
    {
        ArgumentNullException.ThrowIfNull(s);
        return new SavedSearchDto(s.Id.Value, s.Label, s.City, s.Keyword, s.AlertsEnabled);
    }
}

/// <summary>Creates a saved search for a talent.</summary>
public sealed record CreateSavedSearchCommand(Guid TalentId, string Label, string? City, string? Keyword, bool AlertsEnabled) : ICommand<SavedSearchDto>;

/// <summary>Deletes a saved search.</summary>
public sealed record DeleteSavedSearchCommand(Guid Id) : ICommand<bool>;

/// <summary>Toggles job alerts on a saved search.</summary>
public sealed record ToggleSavedSearchAlertsCommand(Guid Id, bool Enabled) : ICommand<SavedSearchDto>;

/// <summary>Lists a talent's saved searches.</summary>
public sealed record GetSavedSearchesQuery(Guid TalentId) : IQuery<IReadOnlyList<SavedSearchDto>>;

/// <summary>Runs a saved search and returns the currently-matching open roles.</summary>
public sealed record RunSavedSearchQuery(Guid Id) : IQuery<IReadOnlyList<RecruitmentRequestDto>>;

/// <summary>Handles <see cref="CreateSavedSearchCommand"/>.</summary>
/// <param name="repository">The recruitment repository.</param>
public sealed class CreateSavedSearchCommandHandler(IRecruitmentRepository repository)
    : ICommandHandler<CreateSavedSearchCommand, SavedSearchDto>
{
    private readonly IRecruitmentRepository _repository = repository;

    /// <inheritdoc />
    public async Task<Result<SavedSearchDto>> HandleAsync(CreateSavedSearchCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var creation = SavedSearch.Create(command.TalentId, command.Label, command.City, command.Keyword, command.AlertsEnabled, DateTimeOffset.UtcNow);
        if (creation.IsFailure)
        {
            return creation.Error!;
        }

        _repository.AddSavedSearch(creation.Value!);
        await _repository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return SavedSearchDto.FromDomain(creation.Value!);
    }
}

/// <summary>Handles <see cref="DeleteSavedSearchCommand"/>.</summary>
/// <param name="repository">The recruitment repository.</param>
public sealed class DeleteSavedSearchCommandHandler(IRecruitmentRepository repository)
    : ICommandHandler<DeleteSavedSearchCommand, bool>
{
    private readonly IRecruitmentRepository _repository = repository;

    /// <inheritdoc />
    public async Task<Result<bool>> HandleAsync(DeleteSavedSearchCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var search = await _repository.GetSavedSearchAsync(new SavedSearchId(command.Id), cancellationToken).ConfigureAwait(false);
        if (search is null)
        {
            return Error.NotFound("saved_search.not_found", "No matching saved search was found.");
        }

        _repository.RemoveSavedSearch(search);
        await _repository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return true;
    }
}

/// <summary>Handles <see cref="ToggleSavedSearchAlertsCommand"/>.</summary>
/// <param name="repository">The recruitment repository.</param>
public sealed class ToggleSavedSearchAlertsCommandHandler(IRecruitmentRepository repository)
    : ICommandHandler<ToggleSavedSearchAlertsCommand, SavedSearchDto>
{
    private readonly IRecruitmentRepository _repository = repository;

    /// <inheritdoc />
    public async Task<Result<SavedSearchDto>> HandleAsync(ToggleSavedSearchAlertsCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var search = await _repository.GetSavedSearchAsync(new SavedSearchId(command.Id), cancellationToken).ConfigureAwait(false);
        if (search is null)
        {
            return Error.NotFound("saved_search.not_found", "No matching saved search was found.");
        }

        search.SetAlerts(command.Enabled);
        await _repository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return SavedSearchDto.FromDomain(search);
    }
}

/// <summary>Handles <see cref="GetSavedSearchesQuery"/>.</summary>
/// <param name="repository">The recruitment repository.</param>
public sealed class GetSavedSearchesQueryHandler(IRecruitmentRepository repository)
    : IQueryHandler<GetSavedSearchesQuery, IReadOnlyList<SavedSearchDto>>
{
    private readonly IRecruitmentRepository _repository = repository;

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<SavedSearchDto>>> HandleAsync(GetSavedSearchesQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        var searches = await _repository.ListSavedSearchesForTalentAsync(query.TalentId, cancellationToken).ConfigureAwait(false);
        return searches.Select(SavedSearchDto.FromDomain).ToList();
    }
}

/// <summary>Handles <see cref="RunSavedSearchQuery"/> — the current open roles matching the saved criteria.</summary>
/// <param name="repository">The recruitment repository.</param>
public sealed class RunSavedSearchQueryHandler(IRecruitmentRepository repository)
    : IQueryHandler<RunSavedSearchQuery, IReadOnlyList<RecruitmentRequestDto>>
{
    private readonly IRecruitmentRepository _repository = repository;

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<RecruitmentRequestDto>>> HandleAsync(RunSavedSearchQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var search = await _repository.GetSavedSearchAsync(new SavedSearchId(query.Id), cancellationToken).ConfigureAwait(false);
        if (search is null)
        {
            return Error.NotFound("saved_search.not_found", "No matching saved search was found.");
        }

        var open = await _repository.ListAsync(search.City, "open", 0, 50, cancellationToken).ConfigureAwait(false);
        var matched = string.IsNullOrWhiteSpace(search.Keyword)
            ? open
            : open.Where(r => r.Title.Contains(search.Keyword, StringComparison.OrdinalIgnoreCase)).ToList();

        return matched.Select(RecruitmentRequestDto.FromDomain).ToList();
    }
}

using Illumin360.Recruitment.Application.Abstractions;
using Illumin360.Recruitment.Domain;
using Illumin360.SharedKernel;
using ApplicationId = Illumin360.Recruitment.Domain.ApplicationId;

namespace Illumin360.Recruitment.Application.Recruitment;

/// <summary>The arrival channel recorded for an application.</summary>
/// <param name="ApplicationId">The application id.</param>
/// <param name="Channel">The arrival channel (e.g. referral, campaign, careers, board, direct).</param>
public sealed record ApplicationSourceDto(Guid ApplicationId, string Channel)
{
    /// <summary>Projects a domain <see cref="ApplicationSource"/> into the transport DTO.</summary>
    /// <param name="s">The source.</param>
    /// <returns>The transport DTO.</returns>
    public static ApplicationSourceDto FromDomain(ApplicationSource s)
    {
        ArgumentNullException.ThrowIfNull(s);
        return new ApplicationSourceDto(s.ApplicationId, s.Channel);
    }
}

/// <summary>Gets an application's arrival channel (defaults to "direct" if none recorded).</summary>
/// <param name="ApplicationId">The application id.</param>
public sealed record GetApplicationSourceQuery(Guid ApplicationId) : IQuery<ApplicationSourceDto>;

/// <summary>Sets (or overrides) an application's arrival channel.</summary>
/// <param name="ApplicationId">The application id.</param>
/// <param name="Channel">The channel.</param>
public sealed record SetApplicationSourceCommand(Guid ApplicationId, string? Channel) : ICommand<ApplicationSourceDto>;

/// <summary>Applications + hires broken down by arrival channel.</summary>
public sealed record GetChannelBreakdownQuery : IQuery<IReadOnlyList<SourceMetric>>;

/// <summary>Handles <see cref="GetApplicationSourceQuery"/>.</summary>
/// <param name="repository">The recruitment repository.</param>
public sealed class GetApplicationSourceQueryHandler(IRecruitmentRepository repository)
    : IQueryHandler<GetApplicationSourceQuery, ApplicationSourceDto>
{
    private readonly IRecruitmentRepository _repository = repository;

    /// <inheritdoc />
    public async Task<Result<ApplicationSourceDto>> HandleAsync(GetApplicationSourceQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        var source = await _repository.GetApplicationSourceAsync(query.ApplicationId, cancellationToken).ConfigureAwait(false);
        return source is null
            ? new ApplicationSourceDto(query.ApplicationId, ApplicationSource.DefaultChannel)
            : ApplicationSourceDto.FromDomain(source);
    }
}

/// <summary>Handles <see cref="SetApplicationSourceCommand"/> — upserts the application's channel.</summary>
/// <param name="repository">The recruitment repository.</param>
public sealed class SetApplicationSourceCommandHandler(IRecruitmentRepository repository)
    : ICommandHandler<SetApplicationSourceCommand, ApplicationSourceDto>
{
    private readonly IRecruitmentRepository _repository = repository;

    /// <inheritdoc />
    public async Task<Result<ApplicationSourceDto>> HandleAsync(SetApplicationSourceCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var application = await _repository.GetApplicationAsync(new ApplicationId(command.ApplicationId), cancellationToken).ConfigureAwait(false);
        if (application is null)
        {
            return Error.NotFound("application.not_found", "No matching application was found.");
        }

        var existing = await _repository.GetApplicationSourceTrackedAsync(command.ApplicationId, cancellationToken).ConfigureAwait(false);
        if (existing is null)
        {
            var creation = ApplicationSource.Create(command.ApplicationId, command.Channel, DateTimeOffset.UtcNow);
            if (creation.IsFailure)
            {
                return creation.Error!;
            }

            _repository.AddApplicationSource(creation.Value!);
            await _repository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return ApplicationSourceDto.FromDomain(creation.Value!);
        }

        existing.SetChannel(command.Channel);
        await _repository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return ApplicationSourceDto.FromDomain(existing);
    }
}

/// <summary>Handles <see cref="GetChannelBreakdownQuery"/>.</summary>
/// <param name="repository">The recruitment repository.</param>
public sealed class GetChannelBreakdownQueryHandler(IRecruitmentRepository repository)
    : IQueryHandler<GetChannelBreakdownQuery, IReadOnlyList<SourceMetric>>
{
    private readonly IRecruitmentRepository _repository = repository;

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<SourceMetric>>> HandleAsync(GetChannelBreakdownQuery query, CancellationToken cancellationToken)
    {
        var breakdown = await _repository.GetChannelBreakdownAsync(cancellationToken).ConfigureAwait(false);
        return Result<IReadOnlyList<SourceMetric>>.Success(breakdown);
    }
}

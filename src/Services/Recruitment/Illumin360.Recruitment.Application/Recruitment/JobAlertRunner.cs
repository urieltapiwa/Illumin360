using Illumin360.Recruitment.Application.Abstractions;
using IntegrationEvents = Illumin360.Recruitment.IntegrationEvents;

namespace Illumin360.Recruitment.Application.Recruitment;

/// <summary>
/// Runs every alert-enabled saved search once and publishes a <see cref="IntegrationEvents.JobAlertDigest"/>
/// for each that currently has matching open roles. Invoked by a scheduled background service; extracted
/// as a plain service so the logic is unit-testable without a timer.
/// </summary>
/// <param name="repository">The recruitment repository.</param>
/// <param name="eventPublisher">Integration-event publisher (transactional outbox).</param>
public sealed class JobAlertRunner(IRecruitmentRepository repository, IIntegrationEventPublisher eventPublisher)
{
    private const int SampleSize = 3;

    private readonly IRecruitmentRepository _repository = repository;
    private readonly IIntegrationEventPublisher _eventPublisher = eventPublisher;

    /// <summary>Runs all alert-enabled searches once and publishes digests for those with matches.</summary>
    /// <param name="now">Timestamp stamped on the digests (UTC).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The number of digests published.</returns>
    public async Task<int> RunOnceAsync(DateTimeOffset now, CancellationToken cancellationToken)
    {
        var searches = await _repository.ListAlertEnabledSavedSearchesAsync(cancellationToken).ConfigureAwait(false);
        var published = 0;

        foreach (var search in searches)
        {
            var open = await _repository.ListAsync(search.City, "open", 0, 50, cancellationToken).ConfigureAwait(false);
            var matched = string.IsNullOrWhiteSpace(search.Keyword)
                ? open
                : open.Where(r => r.Title.Contains(search.Keyword, StringComparison.OrdinalIgnoreCase)).ToList();

            if (matched.Count == 0)
            {
                continue;
            }

            var sample = matched.Take(SampleSize).Select(r => r.Title).ToList();
            await _eventPublisher.PublishAsync(
                new IntegrationEvents.JobAlertDigest(search.TalentId, search.Label, matched.Count, sample, now),
                cancellationToken).ConfigureAwait(false);
            published++;
        }

        // Flush the bus outbox (Publish stages into the DbContext; SaveChanges persists + triggers delivery).
        if (published > 0)
        {
            await _repository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }

        return published;
    }
}

using Illumin360.Professionals.Application.Abstractions;
using Illumin360.Professionals.Domain;
using Illumin360.Recruitment.IntegrationEvents;
using MassTransit;

namespace Illumin360.Professionals.Infrastructure.Messaging;

/// <summary>
/// Stores an in-app notification for the professional when one of their applications changes status.
/// Only professional-type applicants are relevant here (the talent id maps to a professional).
/// </summary>
/// <param name="repository">The professional repository.</param>
public sealed class ApplicationStatusNotificationConsumer(IProfessionalRepository repository)
    : IConsumer<ApplicationStatusChanged>
{
    private readonly IProfessionalRepository _repository = repository;

    /// <inheritdoc />
    public async Task Consume(ConsumeContext<ApplicationStatusChanged> context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var msg = context.Message;
        if (!string.Equals(msg.TalentType, "professional", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        _repository.AddNotification(new ProfessionalNotification(Guid.NewGuid(), new ProfessionalId(msg.TalentId), "application", $"Your application is now {msg.Status}.", msg.OccurredOn));
        await _repository.SaveChangesAsync(context.CancellationToken).ConfigureAwait(false);
    }
}

/// <summary>Stores an in-app notification when a professional's saved-search alert has new matches.</summary>
/// <param name="repository">The professional repository.</param>
public sealed class JobAlertNotificationConsumer(IProfessionalRepository repository)
    : IConsumer<JobAlertDigest>
{
    private readonly IProfessionalRepository _repository = repository;

    /// <inheritdoc />
    public async Task Consume(ConsumeContext<JobAlertDigest> context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var msg = context.Message;
        _repository.AddNotification(new ProfessionalNotification(Guid.NewGuid(), new ProfessionalId(msg.TalentId), "job-alert", $"{msg.MatchCount} new role(s) match \"{msg.SearchLabel}\".", msg.OccurredOn));
        await _repository.SaveChangesAsync(context.CancellationToken).ConfigureAwait(false);
    }
}

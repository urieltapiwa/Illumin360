using Illumin360.Recruitment.Application.Abstractions;
using Illumin360.Recruitment.Domain;
using Illumin360.SharedKernel;
using IntegrationEvents = Illumin360.Recruitment.IntegrationEvents;

namespace Illumin360.Recruitment.Application.Recruitment;

/// <summary>A bulk email campaign.</summary>
/// <param name="Id">Campaign id.</param>
/// <param name="Name">Internal name.</param>
/// <param name="Subject">Email subject.</param>
/// <param name="Body">Email body.</param>
/// <param name="Status">draft/sent.</param>
/// <param name="RecipientCount">Recipients emailed (once sent).</param>
/// <param name="CreatedAt">When created (UTC).</param>
/// <param name="SentAt">When sent (UTC), if applicable.</param>
/// <param name="Recipients">Recipient emails (draft view).</param>
public sealed record CampaignDto(Guid Id, string Name, string Subject, string Body, string Status, int RecipientCount, DateTimeOffset CreatedAt, DateTimeOffset? SentAt, IReadOnlyList<string> Recipients)
{
    /// <summary>Projects a campaign + its recipients into the transport DTO.</summary>
    /// <param name="c">The campaign.</param>
    /// <param name="recipients">The recipient emails.</param>
    /// <returns>The transport DTO.</returns>
    public static CampaignDto FromDomain(EmailCampaign c, IReadOnlyList<string> recipients)
    {
        ArgumentNullException.ThrowIfNull(c);
        ArgumentNullException.ThrowIfNull(recipients);
        return new CampaignDto(c.Id, c.Name, c.Subject, c.Body, c.Status.ToString().ToLowerInvariant(), c.RecipientCount, c.CreatedAt, c.SentAt, recipients);
    }
}

/// <summary>Lists email campaigns, newest first.</summary>
public sealed record GetCampaignsQuery : IQuery<IReadOnlyList<CampaignDto>>;

/// <summary>Gets a campaign with its recipients.</summary>
/// <param name="Id">Campaign id.</param>
public sealed record GetCampaignQuery(Guid Id) : IQuery<CampaignDto>;

/// <summary>Creates a draft campaign.</summary>
public sealed record CreateCampaignCommand(string Name, string Subject, string Body) : ICommand<CampaignDto>;

/// <summary>Adds a recipient to a draft campaign (idempotent per email).</summary>
public sealed record AddCampaignRecipientCommand(Guid CampaignId, string Email) : ICommand<CampaignDto>;

/// <summary>Removes a recipient from a draft campaign.</summary>
public sealed record RemoveCampaignRecipientCommand(Guid CampaignId, string Email) : ICommand<CampaignDto>;

/// <summary>Sends a draft campaign to its recipients (publishes one email event each).</summary>
public sealed record SendCampaignCommand(Guid Id) : ICommand<CampaignDto>;

/// <summary>Handles <see cref="GetCampaignsQuery"/>.</summary>
/// <param name="repository">The recruitment repository.</param>
public sealed class GetCampaignsQueryHandler(IRecruitmentRepository repository)
    : IQueryHandler<GetCampaignsQuery, IReadOnlyList<CampaignDto>>
{
    private readonly IRecruitmentRepository _repository = repository;

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<CampaignDto>>> HandleAsync(GetCampaignsQuery query, CancellationToken cancellationToken)
    {
        var campaigns = await _repository.ListCampaignsAsync(cancellationToken).ConfigureAwait(false);
        var result = new List<CampaignDto>(campaigns.Count);
        foreach (var c in campaigns)
        {
            var recipients = await _repository.ListCampaignRecipientsAsync(c.Id, cancellationToken).ConfigureAwait(false);
            result.Add(CampaignDto.FromDomain(c, recipients.Select(r => r.Email).ToList()));
        }

        return result;
    }
}

/// <summary>Handles <see cref="GetCampaignQuery"/>.</summary>
/// <param name="repository">The recruitment repository.</param>
public sealed class GetCampaignQueryHandler(IRecruitmentRepository repository)
    : IQueryHandler<GetCampaignQuery, CampaignDto>
{
    private readonly IRecruitmentRepository _repository = repository;

    /// <inheritdoc />
    public async Task<Result<CampaignDto>> HandleAsync(GetCampaignQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        var campaign = await _repository.GetCampaignAsync(query.Id, cancellationToken).ConfigureAwait(false);
        if (campaign is null)
        {
            return Error.NotFound("campaign.not_found", "No matching campaign was found.");
        }

        var recipients = await _repository.ListCampaignRecipientsAsync(campaign.Id, cancellationToken).ConfigureAwait(false);
        return CampaignDto.FromDomain(campaign, recipients.Select(r => r.Email).ToList());
    }
}

/// <summary>Handles <see cref="CreateCampaignCommand"/>.</summary>
/// <param name="repository">The recruitment repository.</param>
public sealed class CreateCampaignCommandHandler(IRecruitmentRepository repository)
    : ICommandHandler<CreateCampaignCommand, CampaignDto>
{
    private readonly IRecruitmentRepository _repository = repository;

    /// <inheritdoc />
    public async Task<Result<CampaignDto>> HandleAsync(CreateCampaignCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var creation = EmailCampaign.Create(command.Name, command.Subject, command.Body, DateTimeOffset.UtcNow);
        if (creation.IsFailure)
        {
            return creation.Error!;
        }

        _repository.AddCampaign(creation.Value!);
        await _repository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return CampaignDto.FromDomain(creation.Value!, []);
    }
}

/// <summary>Handles <see cref="AddCampaignRecipientCommand"/>. Idempotent per email; draft-only.</summary>
/// <param name="repository">The recruitment repository.</param>
public sealed class AddCampaignRecipientCommandHandler(IRecruitmentRepository repository)
    : ICommandHandler<AddCampaignRecipientCommand, CampaignDto>
{
    private readonly IRecruitmentRepository _repository = repository;

    /// <inheritdoc />
    public async Task<Result<CampaignDto>> HandleAsync(AddCampaignRecipientCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var campaign = await _repository.GetCampaignAsync(command.CampaignId, cancellationToken).ConfigureAwait(false);
        if (campaign is null)
        {
            return Error.NotFound("campaign.not_found", "No matching campaign was found.");
        }

        if (campaign.Status == CampaignStatus.Sent)
        {
            return Error.Conflict("campaign.already_sent", "A sent campaign cannot be edited.");
        }

        var creation = CampaignRecipient.Create(command.CampaignId, command.Email);
        if (creation.IsFailure)
        {
            return creation.Error!;
        }

        if (!await _repository.CampaignRecipientExistsAsync(command.CampaignId, creation.Value!.Email, cancellationToken).ConfigureAwait(false))
        {
            _repository.AddCampaignRecipient(creation.Value!);
            await _repository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }

        var recipients = await _repository.ListCampaignRecipientsAsync(command.CampaignId, cancellationToken).ConfigureAwait(false);
        return CampaignDto.FromDomain(campaign, recipients.Select(r => r.Email).ToList());
    }
}

/// <summary>Handles <see cref="RemoveCampaignRecipientCommand"/>.</summary>
/// <param name="repository">The recruitment repository.</param>
public sealed class RemoveCampaignRecipientCommandHandler(IRecruitmentRepository repository)
    : ICommandHandler<RemoveCampaignRecipientCommand, CampaignDto>
{
    private readonly IRecruitmentRepository _repository = repository;

    /// <inheritdoc />
    public async Task<Result<CampaignDto>> HandleAsync(RemoveCampaignRecipientCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var campaign = await _repository.GetCampaignAsync(command.CampaignId, cancellationToken).ConfigureAwait(false);
        if (campaign is null)
        {
            return Error.NotFound("campaign.not_found", "No matching campaign was found.");
        }

        if (campaign.Status == CampaignStatus.Sent)
        {
            return Error.Conflict("campaign.already_sent", "A sent campaign cannot be edited.");
        }

        var email = (command.Email ?? string.Empty).Trim().ToLowerInvariant();
        var recipient = await _repository.GetCampaignRecipientAsync(command.CampaignId, email, cancellationToken).ConfigureAwait(false);
        if (recipient is not null)
        {
            _repository.RemoveCampaignRecipient(recipient);
            await _repository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }

        var recipients = await _repository.ListCampaignRecipientsAsync(command.CampaignId, cancellationToken).ConfigureAwait(false);
        return CampaignDto.FromDomain(campaign, recipients.Select(r => r.Email).ToList());
    }
}

/// <summary>Handles <see cref="SendCampaignCommand"/> — publishes one email event per recipient (outbox).</summary>
/// <param name="repository">The recruitment repository.</param>
/// <param name="eventPublisher">Integration-event publisher (transactional outbox).</param>
public sealed class SendCampaignCommandHandler(IRecruitmentRepository repository, IIntegrationEventPublisher eventPublisher)
    : ICommandHandler<SendCampaignCommand, CampaignDto>
{
    private readonly IRecruitmentRepository _repository = repository;
    private readonly IIntegrationEventPublisher _eventPublisher = eventPublisher;

    /// <inheritdoc />
    public async Task<Result<CampaignDto>> HandleAsync(SendCampaignCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var campaign = await _repository.GetCampaignAsync(command.Id, cancellationToken).ConfigureAwait(false);
        if (campaign is null)
        {
            return Error.NotFound("campaign.not_found", "No matching campaign was found.");
        }

        var recipients = await _repository.ListCampaignRecipientsAsync(command.Id, cancellationToken).ConfigureAwait(false);
        var now = DateTimeOffset.UtcNow;
        var marked = campaign.MarkSent(recipients.Count, now);
        if (marked.IsFailure)
        {
            return marked.Error!;
        }

        foreach (var recipient in recipients)
        {
            await _eventPublisher.PublishAsync(
                new IntegrationEvents.CampaignEmailRequested(campaign.Id, recipient.Email, campaign.Subject, campaign.Body, now),
                cancellationToken).ConfigureAwait(false);
        }

        await _repository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return CampaignDto.FromDomain(campaign, recipients.Select(r => r.Email).ToList());
    }
}

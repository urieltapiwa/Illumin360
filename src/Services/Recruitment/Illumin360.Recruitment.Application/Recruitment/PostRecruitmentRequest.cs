using Illumin360.Recruitment.Application.Abstractions;
using Illumin360.Recruitment.Domain;
using Illumin360.SharedKernel;
using IntegrationEvents = Illumin360.Recruitment.IntegrationEvents;

namespace Illumin360.Recruitment.Application.Recruitment;

/// <summary>Command: post a new recruitment request.</summary>
/// <param name="CompanyId">The hiring company's id (required).</param>
/// <param name="Title">Role title (required).</param>
/// <param name="City">City (required).</param>
/// <param name="Positions">Number of positions (at least 1).</param>
public sealed record PostRecruitmentRequestCommand(
    Guid CompanyId,
    string Title,
    string City,
    int Positions) : ICommand<RecruitmentRequestDto>;

/// <summary>Handles <see cref="PostRecruitmentRequestCommand"/> by creating and persisting a request.</summary>
public sealed class PostRecruitmentRequestCommandHandler(
    IRecruitmentRepository repository,
    IIntegrationEventPublisher eventPublisher)
    : ICommandHandler<PostRecruitmentRequestCommand, RecruitmentRequestDto>
{
    private readonly IRecruitmentRepository _repository = repository;
    private readonly IIntegrationEventPublisher _eventPublisher = eventPublisher;

    /// <inheritdoc />
    public async Task<Result<RecruitmentRequestDto>> HandleAsync(
        PostRecruitmentRequestCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var creation = RecruitmentRequest.Post(command.CompanyId, command.Title, command.City, command.Positions);
        if (creation.IsFailure)
        {
            return creation.Error!;
        }

        var request = creation.Value!;
        _repository.Add(request);

        // Publish integration events through the MassTransit bus outbox: written to the outbox table
        // inside the same SaveChanges transaction as the aggregate, delivered to the broker only after
        // commit (transactional outbox — charter Part 5/13).
        foreach (var domainEvent in request.DomainEvents)
        {
            if (domainEvent is RequestPosted posted)
            {
                await _eventPublisher.PublishAsync(
                    new IntegrationEvents.RecruitmentRequestPosted(posted.RequestId.Value, posted.CompanyId, posted.OccurredOn),
                    cancellationToken).ConfigureAwait(false);
            }
        }

        request.ClearDomainEvents();
        await _repository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return RecruitmentRequestDto.FromDomain(request);
    }
}

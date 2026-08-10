using Illumin360.Recruitment.Application.Abstractions;
using Illumin360.Recruitment.Domain;
using Illumin360.SharedKernel;

namespace Illumin360.Recruitment.Application.Recruitment;

/// <summary>An employment offer for an application.</summary>
/// <param name="Id">Offer id.</param>
/// <param name="ApplicationId">The application the offer is for.</param>
/// <param name="Title">Role title.</param>
/// <param name="SalaryAmount">Salary amount.</param>
/// <param name="Currency">Currency code.</param>
/// <param name="StartDate">Proposed start date (ISO yyyy-MM-dd).</param>
/// <param name="Status">Offer status (draft/sent/accepted/declined/withdrawn).</param>
/// <param name="Notes">Notes, if any.</param>
/// <param name="CreatedAt">When created (UTC).</param>
/// <param name="DecidedAt">When decided (UTC), if applicable.</param>
/// <param name="SignedByName">E-signature name, if signed.</param>
/// <param name="SignedAt">E-signature timestamp (UTC), if signed.</param>
public sealed record OfferDto(Guid Id, Guid ApplicationId, string Title, decimal SalaryAmount, string Currency, string StartDate, string Status, string? Notes, DateTimeOffset CreatedAt, DateTimeOffset? DecidedAt, string? SignedByName, DateTimeOffset? SignedAt)
{
    /// <summary>Projects a domain <see cref="Offer"/> into the transport DTO.</summary>
    /// <param name="o">The offer.</param>
    /// <returns>The transport DTO.</returns>
    public static OfferDto FromDomain(Offer o)
    {
        ArgumentNullException.ThrowIfNull(o);
        return new OfferDto(
            o.Id.Value,
            o.ApplicationId,
            o.Title,
            o.SalaryAmount,
            o.Currency,
            o.StartDate.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture),
            o.Status.ToString().ToLowerInvariant(),
            o.Notes,
            o.CreatedAt,
            o.DecidedAt,
            o.SignedByName,
            o.SignedAt);
    }
}

/// <summary>Drafts an offer for an application.</summary>
public sealed record CreateOfferCommand(Guid ApplicationId, string Title, decimal SalaryAmount, string? Currency, DateOnly StartDate, string? Notes) : ICommand<OfferDto>;

/// <summary>Transitions an offer (send/accept/decline/withdraw).</summary>
/// <param name="Id">Offer id.</param>
/// <param name="Action">The transition to apply.</param>
public sealed record TransitionOfferCommand(Guid Id, OfferAction Action) : ICommand<OfferDto>;

/// <summary>The set of offer lifecycle transitions.</summary>
public enum OfferAction
{
    /// <summary>Extend a draft offer to the candidate.</summary>
    Send,

    /// <summary>Candidate accepts a sent offer.</summary>
    Accept,

    /// <summary>Candidate declines a sent offer.</summary>
    Decline,

    /// <summary>Employer withdraws an offer before a decision.</summary>
    Withdraw,
}

/// <summary>Lists an application's offers, newest first.</summary>
/// <param name="ApplicationId">The application id.</param>
public sealed record GetOffersQuery(Guid ApplicationId) : IQuery<IReadOnlyList<OfferDto>>;

/// <summary>Gets a single offer by id.</summary>
/// <param name="Id">The offer id.</param>
public sealed record GetOfferQuery(Guid Id) : IQuery<OfferDto>;

/// <summary>Records the candidate e-signing (and thereby accepting) an offer.</summary>
/// <param name="Id">The offer id.</param>
/// <param name="SignerName">The typed signature name.</param>
public sealed record SignOfferCommand(Guid Id, string SignerName) : ICommand<OfferDto>;

/// <summary>Handles <see cref="CreateOfferCommand"/>.</summary>
/// <param name="repository">The recruitment repository.</param>
public sealed class CreateOfferCommandHandler(IRecruitmentRepository repository)
    : ICommandHandler<CreateOfferCommand, OfferDto>
{
    private readonly IRecruitmentRepository _repository = repository;

    /// <inheritdoc />
    public async Task<Result<OfferDto>> HandleAsync(CreateOfferCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var creation = Offer.Draft(command.ApplicationId, command.Title, command.SalaryAmount, command.Currency, command.StartDate, command.Notes, DateTimeOffset.UtcNow);
        if (creation.IsFailure)
        {
            return creation.Error!;
        }

        _repository.AddOffer(creation.Value!);
        await _repository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return OfferDto.FromDomain(creation.Value!);
    }
}

/// <summary>Handles <see cref="TransitionOfferCommand"/>.</summary>
/// <param name="repository">The recruitment repository.</param>
public sealed class TransitionOfferCommandHandler(IRecruitmentRepository repository)
    : ICommandHandler<TransitionOfferCommand, OfferDto>
{
    private readonly IRecruitmentRepository _repository = repository;

    /// <inheritdoc />
    public async Task<Result<OfferDto>> HandleAsync(TransitionOfferCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var offer = await _repository.GetOfferAsync(new OfferId(command.Id), cancellationToken).ConfigureAwait(false);
        if (offer is null)
        {
            return Error.NotFound("offer.not_found", "No matching offer was found.");
        }

        var now = DateTimeOffset.UtcNow;
        var transition = command.Action switch
        {
            OfferAction.Send => offer.Send(),
            OfferAction.Accept => offer.Accept(now),
            OfferAction.Decline => offer.Decline(now),
            OfferAction.Withdraw => offer.Withdraw(now),
            _ => Error.Validation("offer.action_invalid", "Unknown offer action."),
        };

        if (transition.IsFailure)
        {
            return transition.Error!;
        }

        await _repository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return OfferDto.FromDomain(offer);
    }
}

/// <summary>Handles <see cref="GetOffersQuery"/>.</summary>
/// <param name="repository">The recruitment repository.</param>
public sealed class GetOffersQueryHandler(IRecruitmentRepository repository)
    : IQueryHandler<GetOffersQuery, IReadOnlyList<OfferDto>>
{
    private readonly IRecruitmentRepository _repository = repository;

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<OfferDto>>> HandleAsync(GetOffersQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        var offers = await _repository.ListOffersForApplicationAsync(query.ApplicationId, cancellationToken).ConfigureAwait(false);
        return offers.Select(OfferDto.FromDomain).ToList();
    }
}

/// <summary>Handles <see cref="GetOfferQuery"/>.</summary>
/// <param name="repository">The recruitment repository.</param>
public sealed class GetOfferQueryHandler(IRecruitmentRepository repository)
    : IQueryHandler<GetOfferQuery, OfferDto>
{
    private readonly IRecruitmentRepository _repository = repository;

    /// <inheritdoc />
    public async Task<Result<OfferDto>> HandleAsync(GetOfferQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        var offer = await _repository.GetOfferAsync(new OfferId(query.Id), cancellationToken).ConfigureAwait(false);
        return offer is null
            ? Error.NotFound("offer.not_found", "No matching offer was found.")
            : OfferDto.FromDomain(offer);
    }
}

/// <summary>Handles <see cref="SignOfferCommand"/>.</summary>
/// <param name="repository">The recruitment repository.</param>
public sealed class SignOfferCommandHandler(IRecruitmentRepository repository)
    : ICommandHandler<SignOfferCommand, OfferDto>
{
    private readonly IRecruitmentRepository _repository = repository;

    /// <inheritdoc />
    public async Task<Result<OfferDto>> HandleAsync(SignOfferCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var offer = await _repository.GetOfferAsync(new OfferId(command.Id), cancellationToken).ConfigureAwait(false);
        if (offer is null)
        {
            return Error.NotFound("offer.not_found", "No matching offer was found.");
        }

        var signed = offer.Sign(command.SignerName, DateTimeOffset.UtcNow);
        if (signed.IsFailure)
        {
            return signed.Error!;
        }

        await _repository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return OfferDto.FromDomain(offer);
    }
}

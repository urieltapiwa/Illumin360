using Illumin360.Recruitment.Application.Abstractions;
using Illumin360.Recruitment.Domain;
using Illumin360.SharedKernel;

namespace Illumin360.Recruitment.Application.Recruitment;

/// <summary>A proposed self-schedule interview slot.</summary>
/// <param name="Id">Slot id.</param>
/// <param name="ApplicationId">The application.</param>
/// <param name="ProposedAt">Proposed start (UTC).</param>
/// <param name="DurationMinutes">Duration in minutes.</param>
/// <param name="Location">Location/mode.</param>
/// <param name="Status">Slot status (Offered/Booked/Expired).</param>
public sealed record BookingSlotDto(Guid Id, Guid ApplicationId, DateTimeOffset ProposedAt, int DurationMinutes, string Location, string Status);

/// <summary>Offers a self-schedule slot for an application.</summary>
/// <param name="ApplicationId">The application.</param>
/// <param name="ProposedAt">Proposed start (UTC).</param>
/// <param name="DurationMinutes">Duration in minutes.</param>
/// <param name="Location">Location/mode.</param>
public sealed record OfferBookingSlotCommand(Guid ApplicationId, DateTimeOffset ProposedAt, int DurationMinutes, string Location) : ICommand<BookingSlotDto>;

/// <summary>Books a proposed slot (schedules the interview + expires the siblings).</summary>
/// <param name="SlotId">The slot to book.</param>
public sealed record BookSlotCommand(Guid SlotId) : ICommand<BookingSlotDto>;

/// <summary>Lists an application's booking slots.</summary>
/// <param name="ApplicationId">The application.</param>
public sealed record ListBookingSlotsQuery(Guid ApplicationId) : IQuery<IReadOnlyList<BookingSlotDto>>;

/// <summary>Handles <see cref="OfferBookingSlotCommand"/>.</summary>
/// <param name="repository">The recruitment repository.</param>
public sealed class OfferBookingSlotCommandHandler(IRecruitmentRepository repository)
    : ICommandHandler<OfferBookingSlotCommand, BookingSlotDto>
{
    private readonly IRecruitmentRepository _repository = repository;

    /// <inheritdoc />
    public async Task<Result<BookingSlotDto>> HandleAsync(OfferBookingSlotCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var slot = InterviewBookingSlot.Offer(command.ApplicationId, command.ProposedAt, command.DurationMinutes, command.Location, DateTimeOffset.UtcNow);
        if (slot.IsFailure)
        {
            return slot.Error!;
        }

        _repository.AddBookingSlot(slot.Value!);
        await _repository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return ToDto(slot.Value!);
    }

    /// <summary>Projects a slot entity to its DTO.</summary>
    /// <param name="s">The slot.</param>
    /// <returns>The slot DTO.</returns>
    internal static BookingSlotDto ToDto(InterviewBookingSlot s)
        => new(s.Id, s.ApplicationId, s.ProposedAt, s.DurationMinutes, s.Location, s.Status.ToString());
}

/// <summary>
/// Handles <see cref="BookSlotCommand"/> — the candidate self-schedule action: books the chosen slot,
/// schedules the real <see cref="Interview"/> at that time, and expires the other offered slots.
/// </summary>
/// <param name="repository">The recruitment repository.</param>
public sealed class BookSlotCommandHandler(IRecruitmentRepository repository)
    : ICommandHandler<BookSlotCommand, BookingSlotDto>
{
    private readonly IRecruitmentRepository _repository = repository;

    /// <inheritdoc />
    public async Task<Result<BookingSlotDto>> HandleAsync(BookSlotCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var slot = await _repository.GetBookingSlotAsync(command.SlotId, cancellationToken).ConfigureAwait(false);
        if (slot is null)
        {
            return Error.NotFound("slot.not_found", "Booking slot not found.");
        }

        var now = DateTimeOffset.UtcNow;
        var booked = slot.Book(now);
        if (booked.IsFailure)
        {
            return booked.Error!;
        }

        // Schedule the real interview at the chosen time.
        var interview = Interview.Schedule(slot.ApplicationId, slot.ProposedAt, slot.DurationMinutes, slot.Location, now);
        if (interview.IsFailure)
        {
            return interview.Error!;
        }

        _repository.AddInterview(interview.Value!);

        // The candidate can only hold one slot — expire the rest offered for this application.
        var siblings = await _repository.ListOfferedSlotsForApplicationAsync(slot.ApplicationId, cancellationToken).ConfigureAwait(false);
        foreach (var s in siblings.Where(s => s.Id != slot.Id))
        {
            s.Expire();
        }

        await _repository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return OfferBookingSlotCommandHandler.ToDto(slot);
    }
}

/// <summary>Handles <see cref="ListBookingSlotsQuery"/>.</summary>
/// <param name="repository">The recruitment repository.</param>
public sealed class ListBookingSlotsQueryHandler(IRecruitmentRepository repository)
    : IQueryHandler<ListBookingSlotsQuery, IReadOnlyList<BookingSlotDto>>
{
    private readonly IRecruitmentRepository _repository = repository;

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<BookingSlotDto>>> HandleAsync(ListBookingSlotsQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var slots = await _repository.ListBookingSlotsForApplicationAsync(query.ApplicationId, cancellationToken).ConfigureAwait(false);
        return slots.Select(OfferBookingSlotCommandHandler.ToDto).ToList();
    }
}

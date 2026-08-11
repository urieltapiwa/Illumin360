using FluentAssertions;
using Illumin360.Recruitment.Application.Abstractions;
using Illumin360.Recruitment.Application.Recruitment;
using Illumin360.Recruitment.Domain;
using NSubstitute;
using Xunit;

namespace Illumin360.Recruitment.UnitTests;

public class InterviewBookingTests
{
    private static readonly Guid AppId = Guid.NewGuid();

    private static InterviewBookingSlot Slot(DateTimeOffset at)
        => InterviewBookingSlot.Offer(AppId, at, 45, "Video call", DateTimeOffset.UnixEpoch).Value!;

    [Fact]
    public void Offer_rejects_a_past_time()
    {
        var now = DateTimeOffset.UnixEpoch.AddYears(50);
        var result = InterviewBookingSlot.Offer(AppId, now.AddDays(-1), 45, "Video call", now);
        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("slot.past");
    }

    [Fact]
    public async Task Booking_schedules_the_interview_and_expires_the_siblings()
    {
        var chosen = Slot(DateTimeOffset.UnixEpoch.AddYears(60));
        var sibling = Slot(DateTimeOffset.UnixEpoch.AddYears(60).AddDays(1));

        var repo = Substitute.For<IRecruitmentRepository>();
        repo.GetBookingSlotAsync(chosen.Id, Arg.Any<CancellationToken>()).Returns(chosen);
        repo.ListOfferedSlotsForApplicationAsync(AppId, Arg.Any<CancellationToken>()).Returns(new[] { chosen, sibling });
        var handler = new BookSlotCommandHandler(repo);

        var result = await handler.HandleAsync(new BookSlotCommand(chosen.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Status.Should().Be("Booked");
        chosen.Status.Should().Be(BookingSlotStatus.Booked);
        sibling.Status.Should().Be(BookingSlotStatus.Expired);
        repo.Received(1).AddInterview(Arg.Is<Interview>(i => i.ApplicationId == AppId && i.ScheduledAt == chosen.ProposedAt));
    }

    [Fact]
    public async Task Booking_an_already_booked_slot_conflicts()
    {
        var slot = Slot(DateTimeOffset.UnixEpoch.AddYears(60));
        slot.Book(DateTimeOffset.UnixEpoch.AddYears(59));
        var repo = Substitute.For<IRecruitmentRepository>();
        repo.GetBookingSlotAsync(slot.Id, Arg.Any<CancellationToken>()).Returns(slot);
        var handler = new BookSlotCommandHandler(repo);

        var result = await handler.HandleAsync(new BookSlotCommand(slot.Id), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("slot.not_offered");
        repo.DidNotReceive().AddInterview(Arg.Any<Interview>());
    }

    [Fact]
    public async Task Booking_a_missing_slot_returns_not_found()
    {
        var repo = Substitute.For<IRecruitmentRepository>();
        repo.GetBookingSlotAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((InterviewBookingSlot?)null);
        var handler = new BookSlotCommandHandler(repo);

        var result = await handler.HandleAsync(new BookSlotCommand(Guid.NewGuid()), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("slot.not_found");
    }
}

using FluentAssertions;
using Illumin360.Admin.Domain;
using Illumin360.SharedKernel;
using Xunit;

namespace Illumin360.Admin.UnitTests;

public class TicketAndAccountTests
{
    [Fact]
    public void Ticket_AssignThenResolve_TransitionsAndRaisesEvents()
    {
        var t = Ticket.Seed(Guid.NewGuid(), "Cannot upload CV", "P1", "user@x.na", DateTimeOffset.UnixEpoch);

        t.Assign("dev.admin").IsSuccess.Should().BeTrue();
        t.Status.Should().Be(TicketStatus.Assigned);
        t.Assignee.Should().Be("dev.admin");

        t.Resolve("dev.admin").IsSuccess.Should().BeTrue();
        t.Status.Should().Be(TicketStatus.Resolved);
        t.DomainEvents.Should().Contain(e => e is TicketTriaged);
    }

    [Fact]
    public void Ticket_ResolveTwice_FailsWithConflict()
    {
        var t = Ticket.Seed(Guid.NewGuid(), "x", "P3", "u@x.na", DateTimeOffset.UnixEpoch);
        t.Resolve("dev.admin");

        var result = t.Resolve("dev.admin");

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Conflict);
    }

    [Fact]
    public void Account_SuspendThenActivate_TransitionsAndRaisesEvents()
    {
        var a = AdminAccount.Seed(Guid.NewGuid(), "Baobab", "Company", "hr@baobab.na", DateTimeOffset.UnixEpoch);

        a.Suspend("dev.admin").IsSuccess.Should().BeTrue();
        a.Status.Should().Be(AccountStatus.Suspended);

        a.Activate("dev.admin").IsSuccess.Should().BeTrue();
        a.Status.Should().Be(AccountStatus.Active);
        a.DomainEvents.Should().Contain(e => e is AccountStatusChanged);
    }

    [Fact]
    public void Account_SuspendWhenAlreadySuspended_FailsWithConflict()
    {
        var a = AdminAccount.Seed(Guid.NewGuid(), "Baobab", "Company", "hr@baobab.na", DateTimeOffset.UnixEpoch);
        a.Suspend("dev.admin");

        var result = a.Suspend("dev.admin");

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("account.no_change");
    }
}

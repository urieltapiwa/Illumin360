using FluentAssertions;
using Illumin360.Admin.Application.Abstractions;
using Illumin360.Admin.Application.Audit;
using Illumin360.Admin.Domain;
using NSubstitute;
using Xunit;

namespace Illumin360.Admin.UnitTests;

public class AuditLogTests
{
    [Fact]
    public void Record_defaults_actor_and_captures_fields()
    {
        var e = AuditEntry.Record(" ", "verification.approved", "verification", "abc", "Approved.", DateTimeOffset.UnixEpoch);
        e.Actor.Should().Be("system");
        e.Action.Should().Be("verification.approved");
        e.EntityType.Should().Be("verification");
        e.EntityId.Should().Be("abc");
    }

    [Fact]
    public async Task Query_pages_and_projects()
    {
        var repo = Substitute.For<IAuditRepository>();
        repo.ListAsync(null, 0, 50, Arg.Any<CancellationToken>())
            .Returns(new[] { AuditEntry.Record("rita", "ticket.resolved", "ticket", "t1", "Ticket resolved.", DateTimeOffset.UnixEpoch) });
        var handler = new GetAuditLogQueryHandler(repo);

        var result = await handler.HandleAsync(new GetAuditLogQuery(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Should().ContainSingle(e => e.Action == "ticket.resolved" && e.Actor == "rita");
    }

    [Fact]
    public async Task Query_clamps_page_size_and_forwards_filter()
    {
        var repo = Substitute.For<IAuditRepository>();
        repo.ListAsync(Arg.Any<string?>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns([]);
        var handler = new GetAuditLogQueryHandler(repo);

        await handler.HandleAsync(new GetAuditLogQuery("account", 2, 500), CancellationToken.None);

        // page 2 @ clamped size 100 → skip 100, take 100; filter forwarded.
        await repo.Received(1).ListAsync("account", 100, 100, Arg.Any<CancellationToken>());
    }
}

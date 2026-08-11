using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using Illumin360.TestSupport;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Hosting;
using Testcontainers.PostgreSql;
using Xunit;

namespace Illumin360.Recruitment.IntegrationTests;

/// <summary>
/// End-to-end test for self-schedule interview booking against the REAL repository + Testcontainers
/// PostgreSQL: a recruiter offers two slots, the candidate books one, and the booked slot is scheduled as a
/// real interview while the sibling expires. Exercises the booking write-path (AddInterview + expire
/// siblings) that unit tests mock. Requires a Docker daemon on the test host.
/// </summary>
public sealed class BookingApiTests : IAsyncLifetime
{
    private static readonly Guid AppId = Guid.NewGuid();

    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithImage("postgres:17-alpine")
        .WithDatabase("illumin360_recruitment")
        .WithUsername("illumin")
        .WithPassword("illumin_dev_pw")
        .Build();

    public Task InitializeAsync() => _postgres.StartAsync();

    public async Task DisposeAsync()
    {
        Environment.SetEnvironmentVariable("ConnectionStrings__recruitment", null);
        await _postgres.DisposeAsync();
    }

    [Fact]
    public async Task Offer_two_slots_then_book_one_schedules_an_interview_and_expires_the_sibling()
    {
        Environment.SetEnvironmentVariable("ConnectionStrings__recruitment", _postgres.GetConnectionString() + ";SSL Mode=Disable");

        await using var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(b =>
        {
            b.UseEnvironment("Development");
            b.UseSetting("JobAlerts:Enabled", "false");
            b.UseSetting("Nurture:Enabled", "false");
            b.UseTestAuth();
        });
        _ = factory.Server;

        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", TestJwt.ForRoles(["admin.superuser"]));

        var when1 = DateTimeOffset.UtcNow.AddDays(3);
        var when2 = DateTimeOffset.UtcNow.AddDays(4);
        var slot1 = await (await client.PostAsJsonAsync($"/v1/recruitment/applications/{AppId}/booking-slots", new { proposedAt = when1, durationMinutes = 45, location = "Video call" })).Content.ReadFromJsonAsync<Slot>();
        _ = await client.PostAsJsonAsync($"/v1/recruitment/applications/{AppId}/booking-slots", new { proposedAt = when2, durationMinutes = 45, location = "Video call" });

        var offered = await client.GetFromJsonAsync<List<Slot>>($"/v1/recruitment/applications/{AppId}/booking-slots");
        offered!.Should().HaveCount(2).And.OnlyContain(s => s.Status == "Offered");

        var bookResp = await client.PostAsync($"/v1/recruitment/booking-slots/{slot1!.Id}/book", content: null);
        bookResp.StatusCode.Should().Be(HttpStatusCode.OK);

        var after = await client.GetFromJsonAsync<List<Slot>>($"/v1/recruitment/applications/{AppId}/booking-slots");
        after!.Single(s => s.Id == slot1.Id).Status.Should().Be("Booked");
        after!.Single(s => s.Id != slot1.Id).Status.Should().Be("Expired");

        var interviews = await client.GetFromJsonAsync<List<Interview>>($"/v1/recruitment/applications/{AppId}/interviews");
        interviews!.Should().ContainSingle();

        // Re-booking the taken slot conflicts.
        var reBook = await client.PostAsync($"/v1/recruitment/booking-slots/{slot1.Id}/book", content: null);
        reBook.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    private sealed record Slot(Guid Id, Guid ApplicationId, DateTimeOffset ProposedAt, int DurationMinutes, string Location, string Status);

    private sealed record Interview(Guid Id, DateTimeOffset ScheduledAt, string Status);
}

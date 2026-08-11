using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using Illumin360.Candidates.Application.Candidates;
using Illumin360.Candidates.Infrastructure.Persistence;
using Illumin360.TestSupport;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Testcontainers.PostgreSql;
using Xunit;

namespace Illumin360.Candidates.IntegrationTests;

/// <summary>
/// Write-side component tests: register a candidate and read it back through the API,
/// against a real PostgreSQL via Testcontainers (charter Part 14). Requires Docker.
/// </summary>
public sealed class RegisterCandidateApiTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithImage("postgres:17-alpine")
        .WithDatabase("illumin360_candidates")
        .WithUsername("illumin")
        .WithPassword("illumin_dev_pw")
        .Build();

    private WebApplicationFactory<Program> _factory = null!;

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();

        // The host reads its connection string eagerly at DI-registration time, before
        // WebApplicationFactory's ConfigureAppConfiguration overrides apply; an environment variable is
        // folded in by CreateBuilder before that read and outranks appsettings.Development.json.
        Environment.SetEnvironmentVariable("ConnectionStrings__candidates", _postgres.GetConnectionString() + ";SSL Mode=Disable");

        _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(b =>
        {
            b.UseEnvironment("Development");

            // Registering a candidate requires an admin (write) role; trust the local HS256 test key.
            b.UseTestAuth();
        });

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CandidatesDbContext>();
        await db.Database.EnsureCreatedAsync();
    }

    public async Task DisposeAsync()
    {
        Environment.SetEnvironmentVariable("ConnectionStrings__candidates", null);
        await _factory.DisposeAsync();
        await _postgres.DisposeAsync();
    }

    [Fact]
    public async Task Talent_pool_add_then_list_members()
    {
        var admin = AdminClient();
        var reg = await admin.PostAsJsonAsync("/v1/candidates", new RegisterCandidateCommand("Pool", "Member", "Windhoek", "Namibian"));
        var candidate = await reg.Content.ReadFromJsonAsync<CandidateDto>();

        var poolResp = await admin.PostAsJsonAsync("/v1/candidates/pools", new { name = "Shortlist A" });
        poolResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var pool = await poolResp.Content.ReadFromJsonAsync<Pool>();

        var add = await admin.PostAsync($"/v1/candidates/pools/{pool!.Id}/members/{candidate!.Id}", content: null);
        add.StatusCode.Should().Be(HttpStatusCode.OK);
        // Re-adding conflicts.
        (await admin.PostAsync($"/v1/candidates/pools/{pool.Id}/members/{candidate.Id}", content: null)).StatusCode.Should().Be(HttpStatusCode.Conflict);

        var client = _factory.CreateClient();
        var members = await client.GetFromJsonAsync<List<PoolMember>>($"/v1/candidates/pools/{pool.Id}/members");
        members.Should().ContainSingle(m => m.CandidateId == candidate.Id && m.Name == "Pool Member");
    }

    private sealed record Pool(Guid Id, string Name, int MemberCount);

    private sealed record PoolMember(Guid CandidateId, string Name, string City);

    private sealed record RankedCandidate(Guid Id, string Name, string City, string? Headline, int Score);

    // A client carrying an admin (write) bearer token — required to POST a new candidate.
    private HttpClient AdminClient()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", TestJwt.ForRoles(["admin.write"]));
        return client;
    }

    [Fact]
    public async Task Register_then_get_round_trips_the_candidate()
    {
        var client = AdminClient();

        var register = await client.PostAsJsonAsync(
            "/v1/candidates",
            new RegisterCandidateCommand("Tariro", "Moyo", "Windhoek", "Namibian", "OpenToOpportunities", "Backend engineer"));

        register.StatusCode.Should().Be(HttpStatusCode.Created);
        register.Headers.Location.Should().NotBeNull();

        var created = await register.Content.ReadFromJsonAsync<CandidateDto>();
        created.Should().NotBeNull();
        created!.FirstName.Should().Be("Tariro");

        var fetch = await client.GetAsync(register.Headers.Location);
        fetch.StatusCode.Should().Be(HttpStatusCode.OK);

        var fetched = await fetch.Content.ReadFromJsonAsync<CandidateDto>();
        fetched!.Id.Should().Be(created.Id);
        fetched.City.Should().Be("Windhoek");
        fetched.Availability.Should().Be("OpenToOpportunities");
    }

    [Fact]
    public async Task Register_writes_a_message_to_the_transactional_outbox()
    {
        var client = AdminClient();

        var register = await client.PostAsJsonAsync(
            "/v1/candidates",
            new RegisterCandidateCommand("Anesu", "Chikore", "Harare", "Zimbabwean"));
        register.StatusCode.Should().Be(HttpStatusCode.Created);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CandidatesDbContext>();
        // The integration event is written to the MassTransit outbox table in the same transaction
        // as the aggregate; with no broker running in the test it stays there (count > 0).
        var outboxCount = (await db.Database
            .SqlQueryRaw<long>("SELECT COUNT(*) AS \"Value\" FROM candidates.\"OutboxMessage\"")
            .ToListAsync()).Single();

        outboxCount.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task Top_candidates_ranks_best_fit_first()
    {
        var admin = AdminClient();
        var dev = new RegisterCandidateCommand("Dev", "One", "Windhoek", "Namibian", "OpenToOpportunities", "Software Developer");
        var chef = new RegisterCandidateCommand("Chef", "Two", "Walvis Bay", "Namibian", "OpenToOpportunities", "Head Chef");
        (await admin.PostAsJsonAsync("/v1/candidates", dev)).StatusCode.Should().Be(HttpStatusCode.Created);
        (await admin.PostAsJsonAsync("/v1/candidates", chef)).StatusCode.Should().Be(HttpStatusCode.Created);

        var client = _factory.CreateClient();
        var response = await client.GetAsync("/v1/candidates/top?title=Software%20Developer&city=Windhoek&limit=5");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var ranked = await response.Content.ReadFromJsonAsync<List<RankedCandidate>>();
        ranked.Should().NotBeNullOrEmpty();
        ranked![0].Name.Should().Be("Dev One");
    }

    [Fact]
    public async Task Faceted_search_filters_and_returns_facets()
    {
        var admin = AdminClient();
        (await admin.PostAsJsonAsync("/v1/candidates", new RegisterCandidateCommand("Search", "Dev", "Windhoek", "Namibian", "OpenToOpportunities", "Senior backend engineer"))).EnsureSuccessStatusCode();
        (await admin.PostAsJsonAsync("/v1/candidates", new RegisterCandidateCommand("Search", "Chef", "Windhoek", "Namibian", "NotAvailable", "Head chef"))).EnsureSuccessStatusCode();
        (await admin.PostAsJsonAsync("/v1/candidates", new RegisterCandidateCommand("Search", "Analyst", "Swakopmund", "Namibian", "OpenToOpportunities", "Data analyst"))).EnsureSuccessStatusCode();

        var client = _factory.CreateClient();

        // Keyword facet: only the engineer matches "engineer".
        var byKeyword = await client.GetFromJsonAsync<SearchResult>("/v1/candidates/search?q=engineer");
        byKeyword!.Items.Should().OnlyContain(c => c.LastName == "Dev");

        // City + availability filter narrows to the Windhoek OpenToOpportunities candidate.
        var narrowed = await client.GetFromJsonAsync<SearchResult>("/v1/candidates/search?city=Windhoek&availability=OpenToOpportunities");
        narrowed!.Items.Should().OnlyContain(c => c.City == "Windhoek");
        narrowed.Facets.Availability.Should().Contain(f => f.Label == "OpenToOpportunities");

        // Availability facet excludes its own filter, so it still sees the NotAvailable Windhoek candidate.
        narrowed.Facets.Availability.Should().Contain(f => f.Label == "NotAvailable");

        // Invalid availability is a 400.
        (await client.GetAsync("/v1/candidates/search?availability=Whenever")).StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    private sealed record SearchResult(List<CandidateDto> Items, int Total, int Page, int PageSize, Facets Facets);

    private sealed record Facets(List<FacetCount> Cities, List<FacetCount> Availability);

    private sealed record FacetCount(string Label, int Count);

    [Fact]
    public async Task Notes_and_tags_round_trip()
    {
        var admin = AdminClient();
        var reg = await admin.PostAsJsonAsync("/v1/candidates", new RegisterCandidateCommand("Note", "Target", "Windhoek", "Namibian"));
        var candidate = await reg.Content.ReadFromJsonAsync<CandidateDto>();

        // Add a note, list it back.
        var addNote = await admin.PostAsJsonAsync($"/v1/candidates/{candidate!.Id}/notes", new { author = "Rita", body = "Strong second interview." });
        addNote.StatusCode.Should().Be(HttpStatusCode.Created);
        var note = await addNote.Content.ReadFromJsonAsync<Note>();

        var client = _factory.CreateClient();
        var notes = await client.GetFromJsonAsync<List<Note>>($"/v1/candidates/{candidate.Id}/notes");
        notes.Should().ContainSingle(n => n.Body == "Strong second interview." && n.Author == "Rita");

        // Deleting the note leaves none.
        (await admin.DeleteAsync($"/v1/candidates/notes/{note!.Id}")).StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await client.GetFromJsonAsync<List<Note>>($"/v1/candidates/{candidate.Id}/notes"))!.Should().BeEmpty();

        // Tags normalise + dedupe.
        await admin.PostAsJsonAsync($"/v1/candidates/{candidate.Id}/tags", new { label = "Backend" });
        var tags = await (await admin.PostAsJsonAsync($"/v1/candidates/{candidate.Id}/tags", new { label = "backend" })).Content.ReadFromJsonAsync<List<string>>();
        tags.Should().ContainSingle().Which.Should().Be("backend");

        // Removing the tag empties the list.
        var afterRemove = await (await admin.DeleteAsync($"/v1/candidates/{candidate.Id}/tags/backend")).Content.ReadFromJsonAsync<List<string>>();
        afterRemove.Should().BeEmpty();

        // Notes on a missing candidate are a 404.
        (await admin.PostAsJsonAsync($"/v1/candidates/{Guid.NewGuid()}/notes", new { body = "x" })).StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    private sealed record Note(Guid Id, string Author, string Body, DateTimeOffset CreatedAt);

    [Fact]
    public async Task Erase_removes_candidate_and_owned_data()
    {
        var admin = AdminClient();
        var reg = await admin.PostAsJsonAsync("/v1/candidates", new RegisterCandidateCommand("Erase", "Me", "Windhoek", "Namibian"));
        var candidate = await reg.Content.ReadFromJsonAsync<CandidateDto>();

        // Attach a note + a tag, confirm the export sees them.
        await admin.PostAsJsonAsync($"/v1/candidates/{candidate!.Id}/notes", new { author = "Rita", body = "note" });
        await admin.PostAsJsonAsync($"/v1/candidates/{candidate.Id}/tags", new { label = "vip" });

        // Erase (right to be forgotten).
        (await admin.DeleteAsync($"/v1/candidates/{candidate.Id}")).StatusCode.Should().Be(HttpStatusCode.NoContent);

        var client = _factory.CreateClient();
        (await client.GetAsync($"/v1/candidates/{candidate.Id}")).StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await client.GetFromJsonAsync<List<Note>>($"/v1/candidates/{candidate.Id}/notes"))!.Should().BeEmpty();
        (await client.GetFromJsonAsync<List<string>>($"/v1/candidates/{candidate.Id}/tags"))!.Should().BeEmpty();

        // Erasing again is a 404.
        (await admin.DeleteAsync($"/v1/candidates/{candidate.Id}")).StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Get_unknown_id_returns_404()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync($"/v1/candidates/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Register_with_invalid_availability_returns_400()
    {
        var client = AdminClient();

        var response = await client.PostAsJsonAsync(
            "/v1/candidates",
            new RegisterCandidateCommand("Tariro", "Moyo", "Windhoek", "Namibian", "Whenever"));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}

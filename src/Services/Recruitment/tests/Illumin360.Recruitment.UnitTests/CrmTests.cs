using FluentAssertions;
using Illumin360.Recruitment.Application.Abstractions;
using Illumin360.Recruitment.Application.Recruitment;
using Illumin360.Recruitment.Domain;
using Illumin360.SharedKernel;
using NSubstitute;
using Xunit;

namespace Illumin360.Recruitment.UnitTests;

public class CrmTests
{
    [Fact]
    public void Client_create_defaults_to_prospect_and_requires_name()
    {
        Client.Create("", null, null, null, DateTimeOffset.UnixEpoch).IsFailure.Should().BeTrue();

        var ok = Client.Create("Acme Ltd", "Tech", "Windhoek", "keen", DateTimeOffset.UnixEpoch);
        ok.IsSuccess.Should().BeTrue();
        ok.Value!.Status.Should().Be(ClientStatus.Prospect);
        ok.Value!.Name.Should().Be("Acme Ltd");
    }

    [Fact]
    public void Client_change_status_parses_and_rejects_unknown()
    {
        var c = Client.Create("Acme", null, null, null, DateTimeOffset.UnixEpoch).Value!;
        c.ChangeStatus("active").IsSuccess.Should().BeTrue();
        c.Status.Should().Be(ClientStatus.Active);
        c.ChangeStatus("nope").IsFailure.Should().BeTrue();
        c.Status.Should().Be(ClientStatus.Active); // unchanged on failure
    }

    [Theory]
    [InlineData("", "team.name?", false)]
    [InlineData("Jane", "not-an-email", false)]
    [InlineData("Jane", "jane@acme.na", true)]
    [InlineData("Jane", null, true)]
    public void Contact_create_validates_name_and_email(string name, string? email, bool shouldSucceed)
    {
        var result = ClientContact.Create(ClientId.New(), name, "Manager", email, "+264", false, DateTimeOffset.UnixEpoch);
        result.IsSuccess.Should().Be(shouldSucceed);
    }

    [Fact]
    public async Task Create_client_persists_and_returns_dto()
    {
        var repo = Substitute.For<IRecruitmentRepository>();
        var handler = new CreateClientCommandHandler(repo);

        var result = await handler.HandleAsync(new CreateClientCommand("Namib Mills", "Manufacturing", "Windhoek", null), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Name.Should().Be("Namib Mills");
        result.Value!.Status.Should().Be("prospect");
        repo.Received(1).AddClient(Arg.Any<Client>());
        await repo.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Add_contact_to_missing_client_returns_not_found()
    {
        var repo = Substitute.For<IRecruitmentRepository>();
        repo.GetClientAsync(Arg.Any<ClientId>(), Arg.Any<CancellationToken>()).Returns((Client?)null);
        var handler = new AddClientContactCommandHandler(repo);

        var result = await handler.HandleAsync(
            new AddClientContactCommand(Guid.NewGuid(), "Jane", "HR", "jane@acme.na", null, true), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.NotFound);
        repo.DidNotReceive().AddClientContact(Arg.Any<ClientContact>());
    }

    [Fact]
    public async Task Add_contact_to_existing_client_persists()
    {
        var client = Client.Create("Acme", null, null, null, DateTimeOffset.UnixEpoch).Value!;
        var repo = Substitute.For<IRecruitmentRepository>();
        repo.GetClientAsync(Arg.Any<ClientId>(), Arg.Any<CancellationToken>()).Returns(client);
        var handler = new AddClientContactCommandHandler(repo);

        var result = await handler.HandleAsync(
            new AddClientContactCommand(client.Id.Value, "Jane Doe", "Hiring Manager", "jane@acme.na", "+264 61 000", true), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Name.Should().Be("Jane Doe");
        result.Value!.IsPrimary.Should().BeTrue();
        repo.Received(1).AddClientContact(Arg.Any<ClientContact>());
    }

    [Fact]
    public async Task List_clients_rejects_bad_status_filter()
    {
        var repo = Substitute.For<IRecruitmentRepository>();
        var handler = new ListClientsQueryHandler(repo);

        var result = await handler.HandleAsync(new ListClientsQuery("banana"), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("client.status_invalid");
    }

    [Fact]
    public async Task Get_client_returns_detail_with_contacts()
    {
        var client = Client.Create("Acme", "Tech", "Windhoek", null, DateTimeOffset.UnixEpoch).Value!;
        var contact = ClientContact.Create(client.Id, "Jane", "HR", "jane@acme.na", null, true, DateTimeOffset.UnixEpoch).Value!;
        var repo = Substitute.For<IRecruitmentRepository>();
        repo.GetClientAsync(Arg.Any<ClientId>(), Arg.Any<CancellationToken>()).Returns(client);
        repo.ListContactsForClientAsync(Arg.Any<ClientId>(), Arg.Any<CancellationToken>()).Returns(new[] { contact });
        var handler = new GetClientQueryHandler(repo);

        var result = await handler.HandleAsync(new GetClientQuery(client.Id.Value), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Client.ContactCount.Should().Be(1);
        result.Value!.Contacts.Should().ContainSingle(c => c.Name == "Jane");
    }
}

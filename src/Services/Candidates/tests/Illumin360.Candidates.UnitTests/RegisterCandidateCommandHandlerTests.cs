using FluentAssertions;
using Illumin360.Candidates.Application.Abstractions;
using Illumin360.Candidates.Application.Candidates;
using Illumin360.Candidates.Domain;
using Illumin360.SharedKernel;
using NSubstitute;
using Xunit;
using IntegrationEvents = Illumin360.Candidates.IntegrationEvents;

namespace Illumin360.Candidates.UnitTests;

public class RegisterCandidateCommandHandlerTests
{
    private readonly ICandidateRepository _repo = Substitute.For<ICandidateRepository>();
    private readonly IIntegrationEventPublisher _eventPublisher = Substitute.For<IIntegrationEventPublisher>();

    private RegisterCandidateCommandHandler CreateHandler() => new(_repo, _eventPublisher);

    [Fact]
    public async Task HandleAsync_WithValidCommand_PersistsAndReturnsDto()
    {
        var result = await CreateHandler().HandleAsync(
            new RegisterCandidateCommand("Tariro", "Moyo", "Windhoek", "Namibian", "OpenToOpportunities"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.FirstName.Should().Be("Tariro");
        result.Value.Availability.Should().Be("OpenToOpportunities");
        _repo.Received(1).Add(Arg.Is<Candidate>(c => c.FirstName == "Tariro"));
        await _repo.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_WithValidCommand_PublishesIntegrationEvent()
    {
        var result = await CreateHandler().HandleAsync(
            new RegisterCandidateCommand("Tariro", "Moyo", "Windhoek", "Namibian"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _eventPublisher.Received(1).PublishAsync(
            Arg.Is<IntegrationEvents.CandidateRegistered>(e => e.CandidateId == result.Value!.Id),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_WithInvalidAvailability_FailsValidation_AndDoesNotPersist()
    {
        var result = await CreateHandler().HandleAsync(
            new RegisterCandidateCommand("Tariro", "Moyo", "Windhoek", "Namibian", "Whenever"),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("candidate.invalid_availability");
        _repo.DidNotReceive().Add(Arg.Any<Candidate>());
        await _repo.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_WithMissingName_PropagatesDomainValidationError()
    {
        var result = await CreateHandler().HandleAsync(
            new RegisterCandidateCommand("", "Moyo", "Windhoek", "Namibian"),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Validation);
        _repo.DidNotReceive().Add(Arg.Any<Candidate>());
    }

    [Fact]
    public async Task HandleAsync_WithNoAvailability_DefaultsToActivelyLooking()
    {
        var result = await CreateHandler().HandleAsync(
            new RegisterCandidateCommand("Tariro", "Moyo", "Windhoek", "Namibian"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Availability.Should().Be(nameof(AvailabilityStatus.ActivelyLooking));
    }
}

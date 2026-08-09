using FluentAssertions;
using Illumin360.Admin.Domain;
using Illumin360.SharedKernel;
using Xunit;

namespace Illumin360.Admin.UnitTests;

public class VerificationTests
{
    [Fact]
    public void Submit_WithValidInput_CreatesPending()
    {
        var result = Verification.Submit("Etosha Consulting", "Company verification", "Low", "34m ago");

        result.IsSuccess.Should().BeTrue();
        result.Value!.Status.Should().Be(VerificationStatus.Pending);
        result.Value!.DecidedBy.Should().BeNull();
    }

    [Fact]
    public void Submit_WithoutEntity_FailsValidation()
    {
        var result = Verification.Submit("", "Company verification", "Low", "now");

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Validation);
        result.Error!.Code.Should().Be("verification.entity_required");
    }

    [Fact]
    public void Approve_WhenPending_SucceedsAndRaisesEvent()
    {
        var v = Verification.Submit("Apex Namibia", "Recruiter ID", "Low", "12m ago").Value!;

        var result = v.Approve("dev.admin");

        result.IsSuccess.Should().BeTrue();
        v.Status.Should().Be(VerificationStatus.Approved);
        v.DecidedBy.Should().Be("dev.admin");
        v.DecidedAt.Should().NotBeNull();
        v.DomainEvents.Should().ContainSingle(e => e is VerificationDecided);
    }

    [Fact]
    public void Reject_WhenAlreadyDecided_FailsWithConflict()
    {
        var v = Verification.Submit("Meridian Group", "Company verification", "Medium", "5h ago").Value!;
        v.Approve("dev.admin");

        var result = v.Reject("dev.admin");

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Conflict);
        result.Error!.Code.Should().Be("verification.already_decided");
    }
}

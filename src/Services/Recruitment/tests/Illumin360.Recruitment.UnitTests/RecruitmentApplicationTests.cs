using FluentAssertions;
using Illumin360.Recruitment.Domain;
using Xunit;

namespace Illumin360.Recruitment.UnitTests;

public class RecruitmentApplicationTests
{
    [Fact]
    public void Apply_CreatesAppliedApplicationForTheTalent()
    {
        var requestId = RequestId.New();
        var talentId = Guid.NewGuid();
        var appliedAt = DateTimeOffset.UnixEpoch;

        var application = RecruitmentApplication.Apply(requestId, talentId, "professional", appliedAt);

        application.RequestId.Should().Be(requestId);
        application.TalentId.Should().Be(talentId);
        application.TalentType.Should().Be("professional");
        application.Status.Should().Be("applied");
        application.IsHire.Should().BeFalse();
        application.MatchScore.Should().Be(0m);
        application.AppliedAt.Should().Be(appliedAt);
        application.DecidedAt.Should().BeNull();
    }
}

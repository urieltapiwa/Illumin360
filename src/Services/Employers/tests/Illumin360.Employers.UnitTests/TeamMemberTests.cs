using FluentAssertions;
using Illumin360.Employers.Domain;
using Illumin360.SharedKernel;
using Xunit;

namespace Illumin360.Employers.UnitTests;

public class TeamMemberTests
{
    private static readonly EmployerId Emp = EmployerId.New();

    [Fact]
    public void Invite_with_valid_input_succeeds_and_normalizes_email()
    {
        var result = TeamMember.Invite(Emp, "  Jane@Acme.NA ", "Jane Doe", "recruiter");

        result.IsSuccess.Should().BeTrue();
        result.Value!.Email.Should().Be("jane@acme.na");
        result.Value!.DisplayName.Should().Be("Jane Doe");
        result.Value!.Role.Should().Be(EmployerRole.Recruiter);
        result.Value!.EmployerId.Should().Be(Emp);
    }

    [Theory]
    [InlineData("", "Jane", "owner", "team.email_invalid")]
    [InlineData("no-at-sign", "Jane", "owner", "team.email_invalid")]
    [InlineData("jane@acme.na", "", "owner", "team.name_required")]
    [InlineData("jane@acme.na", "Jane", "superuser", "team.role_invalid")]
    public void Invite_with_bad_input_fails(string email, string name, string role, string expectedCode)
    {
        var result = TeamMember.Invite(Emp, email, name, role);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Validation);
        result.Error!.Code.Should().Be(expectedCode);
    }

    [Fact]
    public void ChangeRole_updates_role()
    {
        var m = TeamMember.Invite(Emp, "jane@acme.na", "Jane", "viewer").Value!;

        m.ChangeRole("owner").IsSuccess.Should().BeTrue();
        m.Role.Should().Be(EmployerRole.Owner);
    }

    [Fact]
    public void ChangeRole_rejects_unknown_role()
    {
        var m = TeamMember.Invite(Emp, "jane@acme.na", "Jane", "viewer").Value!;

        var result = m.ChangeRole("boss");

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("team.role_invalid");
        m.Role.Should().Be(EmployerRole.Viewer);
    }

    [Theory]
    [InlineData(EmployerRole.Owner, "owner")]
    [InlineData(EmployerRole.Recruiter, "recruiter")]
    [InlineData(EmployerRole.Viewer, "viewer")]
    public void ToWire_is_lowercase(EmployerRole role, string expected)
        => role.ToWire().Should().Be(expected);
}

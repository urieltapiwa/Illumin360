using FluentAssertions;
using Illumin360.Employers.Domain;
using Illumin360.SharedKernel;
using Xunit;

namespace Illumin360.Employers.UnitTests;

public class EmployerTests
{
    [Fact]
    public void Register_with_valid_input_succeeds()
    {
        var result = Employer.Register("Namib Mills", "Manufacturing", "Windhoek", "https://x.na", "About us");

        result.IsSuccess.Should().BeTrue();
        result.Value!.CompanyName.Should().Be("Namib Mills");
        result.Value!.City.Should().Be("Windhoek");
    }

    [Theory]
    [InlineData("", "Tech", "Windhoek", "employer.company_required")]
    [InlineData("Acme", "", "Windhoek", "employer.industry_required")]
    [InlineData("Acme", "Tech", "", "employer.city_required")]
    public void Register_with_missing_field_fails(string company, string industry, string city, string expectedCode)
    {
        var result = Employer.Register(company, industry, city, null, null);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Validation);
        result.Error!.Code.Should().Be(expectedCode);
    }

    [Fact]
    public void UpdateProfile_changes_editable_fields()
    {
        var e = Employer.Register("Acme", "Tech", "Windhoek", null, null).Value!;

        var result = e.UpdateProfile("Fintech", "Walvis Bay", "https://acme.na", "New blurb");

        result.IsSuccess.Should().BeTrue();
        e.Industry.Should().Be("Fintech");
        e.City.Should().Be("Walvis Bay");
        e.Website.Should().Be("https://acme.na");
        e.CompanyName.Should().Be("Acme"); // name is fixed
    }

    [Fact]
    public void UpdateProfile_rejects_blank_city()
    {
        var e = Employer.Register("Acme", "Tech", "Windhoek", null, null).Value!;
        e.UpdateProfile("Tech", "  ", null, null).IsFailure.Should().BeTrue();
    }
}

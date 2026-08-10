using FluentAssertions;
using Illumin360.Email;
using Xunit;

namespace Illumin360.Email.UnitTests;

public class NotificationTemplatesTests
{
    [Fact]
    public void Welcome_uses_the_name_when_present()
    {
        var email = NotificationTemplates.Welcome("Panduleni");

        email.Subject.Should().Be("Welcome to Illumin360");
        email.HtmlBody.Should().Contain("Hi Panduleni");
    }

    [Fact]
    public void Welcome_falls_back_to_generic_greeting()
    {
        NotificationTemplates.Welcome("  ").HtmlBody.Should().Contain("Hi there");
    }

    [Fact]
    public void ApplicationStatusChanged_mentions_role_and_status()
    {
        var email = NotificationTemplates.ApplicationStatusChanged("Software Developer", "shortlisted");

        email.Subject.Should().Contain("Software Developer");
        email.HtmlBody.Should().Contain("shortlisted");
    }
}

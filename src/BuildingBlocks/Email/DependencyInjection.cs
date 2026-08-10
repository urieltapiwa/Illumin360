using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Illumin360.Email;

/// <summary>Wires the SMTP email sender into DI.</summary>
public static class DependencyInjection
{
    /// <summary>Registers <see cref="IEmailSender"/> (MailKit SMTP) from the <c>Email</c> configuration section.</summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">App configuration (reads the <c>Email</c> section).</param>
    /// <returns>The same collection for chaining.</returns>
    public static IServiceCollection AddIllumin360Email(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        services.Configure<EmailOptions>(configuration.GetSection("Email"));
        services.AddScoped<IEmailSender, MailKitEmailSender>();
        return services;
    }
}

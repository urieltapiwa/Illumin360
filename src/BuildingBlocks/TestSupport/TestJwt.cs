using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;

namespace Illumin360.TestSupport;

/// <summary>
/// Shared integration-test auth helper. Mints HS256 bearer tokens shaped like the Keycloak access
/// tokens the services expect (realm roles under a <c>realm_access</c> JSON claim) and reconfigures
/// JWT bearer to trust <see cref="SigningKey"/> instead of Keycloak's JWKS, so auth-gated endpoints
/// can be exercised offline (no live identity provider).
/// </summary>
public static class TestJwt
{
    /// <summary>Symmetric key shared between the token minter and the reconfigured JWT bearer handler.</summary>
    public static readonly SymmetricSecurityKey SigningKey =
        new(Encoding.UTF8.GetBytes("illumin360-integration-test-signing-key-0123456789"));

    /// <summary>Creates a signed bearer token carrying the given realm roles.</summary>
    /// <param name="roles">Realm roles to embed under <c>realm_access.roles</c>.</param>
    /// <param name="username">The <c>preferred_username</c> to embed.</param>
    /// <returns>A signed compact JWT.</returns>
    public static string ForRoles(string[] roles, string username = "dev.user")
    {
        ArgumentNullException.ThrowIfNull(roles);

        var realmAccess = JsonSerializer.Serialize(new { roles });

        var descriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(
            [
                new Claim("preferred_username", username),
                new Claim("realm_access", realmAccess, JsonClaimValueTypes.Json),
            ]),
            Expires = DateTime.UtcNow.AddMinutes(30),
            SigningCredentials = new SigningCredentials(SigningKey, SecurityAlgorithms.HmacSha256),
        };

        return new JsonWebTokenHandler().CreateToken(descriptor);
    }

    /// <summary>
    /// Reconfigures the JWT bearer handler to trust <see cref="SigningKey"/> and skip issuer/audience
    /// and metadata validation, so tokens from <see cref="ForRoles"/> validate without Keycloak.
    /// </summary>
    /// <param name="builder">The web host builder to configure.</param>
    /// <returns>The same builder, for chaining.</returns>
    public static IWebHostBuilder UseTestAuth(this IWebHostBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ConfigureTestServices(services =>
        {
            services.PostConfigure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, o =>
            {
                o.Authority = null;
                o.MetadataAddress = null!;
                o.RequireHttpsMetadata = false;
                o.Configuration = new OpenIdConnectConfiguration();
                o.TokenValidationParameters.ValidateIssuer = false;
                o.TokenValidationParameters.ValidateAudience = false;
                o.TokenValidationParameters.ValidateLifetime = true;
                o.TokenValidationParameters.ValidateIssuerSigningKey = true;
                o.TokenValidationParameters.IssuerSigningKey = SigningKey;
                o.TokenValidationParameters.NameClaimType = "preferred_username";
            });
        });

        return builder;
    }
}

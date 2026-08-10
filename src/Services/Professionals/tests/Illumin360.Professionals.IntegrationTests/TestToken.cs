using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Illumin360.Professionals.IntegrationTests;

/// <summary>
/// Mints HS256 bearer tokens for the integration tests, shaped like the Keycloak access tokens the
/// service expects: realm roles under a <c>realm_access</c> JSON claim, username under
/// <c>preferred_username</c>. The tests reconfigure JWT bearer to trust <see cref="SigningKey"/>.
/// </summary>
public static class TestToken
{
    /// <summary>Symmetric key shared between the token minter and the reconfigured JWT bearer handler.</summary>
    public static readonly SymmetricSecurityKey SigningKey =
        new(Encoding.UTF8.GetBytes("illumin360-integration-test-signing-key-0123456789"));

    /// <summary>Creates a signed bearer token carrying the given realm roles.</summary>
    /// <param name="roles">Realm roles to embed under <c>realm_access.roles</c>.</param>
    /// <param name="username">The <c>preferred_username</c> to embed.</param>
    /// <returns>A signed compact JWT.</returns>
    public static string ForRoles(string[] roles, string username = "dev.professional")
    {
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
}

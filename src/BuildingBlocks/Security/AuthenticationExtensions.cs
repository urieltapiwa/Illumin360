using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace Illumin360.Security;

/// <summary>
/// Shared service-layer authentication/authorization wiring. Validates Keycloak-issued JWT bearer
/// access tokens (relayed by the BFF) and projects the realm roles Keycloak emits under
/// <c>realm_access.roles</c> into ASP.NET <see cref="ClaimTypes.Role"/> claims so role policies work
/// (charter Part 7: authZ enforced at the service layer).
/// </summary>
public static class AuthenticationExtensions
{
    /// <summary>The authorization policy name for admin-tier access (any <c>admin.*</c> realm role).</summary>
    public const string AdminPolicy = "admin";

    /// <summary>The authorization policy name for mutating admin access (<c>admin.write</c>/<c>admin.superuser</c>).</summary>
    public const string AdminWritePolicy = "admin.write";

    private const string DefaultAuthority = "http://keycloak:8080/realms/illumin360";
    private const string DefaultFrontChannel = "http://localhost:8080/realms/illumin360";

    private static readonly string[] AdminRoles = ["admin.read", "admin.write", "admin.superuser"];
    private static readonly string[] AdminWriteRoles = ["admin.write", "admin.superuser"];

    /// <summary>
    /// Adds JWT bearer authentication (against Keycloak) and the admin authorization policies.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">
    /// App configuration. Reads <c>Oidc:Authority</c> (back-channel host used for JWKS/discovery, reachable
    /// in-cluster) and <c>Oidc:FrontChannelAuthority</c> (browser-facing host). Both are accepted as valid
    /// token issuers, because Keycloak stamps <c>iss</c> with whichever host the user authenticated against.
    /// </param>
    /// <returns>The same collection for chaining.</returns>
    public static IServiceCollection AddIllumin360Auth(
        this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var authority = configuration["Oidc:Authority"] ?? DefaultAuthority;
        var frontChannel = configuration["Oidc:FrontChannelAuthority"] ?? DefaultFrontChannel;

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                // JWKS + discovery come from the back-channel authority (reachable inside the cluster).
                options.Authority = authority;
                options.MapInboundClaims = false;

                // Keycloak runs over plain HTTP in the local/dev cluster; metadata is fetched in-network.
                options.RequireHttpsMetadata = false;

                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,

                    // Accept both hosts: the access token's `iss` is the front-channel host the user
                    // authenticated against, while discovery/JWKS is the back-channel host.
                    ValidIssuers = [authority, frontChannel],

                    // No audience mapper is configured in the realm yet, so the access token's `aud`
                    // does not carry the service name. Revisit once a client-scope audience mapper exists.
                    ValidateAudience = false,

                    ValidateLifetime = true,
                    RoleClaimType = ClaimTypes.Role,
                    NameClaimType = "preferred_username",
                };

                options.Events = new JwtBearerEvents
                {
                    OnTokenValidated = static context =>
                    {
                        ProjectRealmRoles(context.Principal);
                        return Task.CompletedTask;
                    },
                };
            });

        services.AddAuthorization(options =>
        {
            options.AddPolicy(AdminPolicy, policy => policy.RequireRole(AdminRoles));
            options.AddPolicy(AdminWritePolicy, policy => policy.RequireRole(AdminWriteRoles));
        });

        return services;
    }

    /// <summary>
    /// Projects Keycloak realm roles (emitted as a JSON object under the <c>realm_access</c> claim) into
    /// individual <see cref="ClaimTypes.Role"/> claims. ASP.NET's JWT handler does not flatten these
    /// automatically, so without this projection <c>RequireRole</c>/<c>[Authorize(Roles=…)]</c> never match.
    /// </summary>
    private static void ProjectRealmRoles(ClaimsPrincipal? principal)
    {
        if (principal?.Identity is not ClaimsIdentity identity)
        {
            return;
        }

        var realmAccess = principal.FindFirst("realm_access")?.Value;
        if (string.IsNullOrEmpty(realmAccess))
        {
            return;
        }

        using var document = JsonDocument.Parse(realmAccess);
        if (!document.RootElement.TryGetProperty("roles", out var roles)
            || roles.ValueKind != JsonValueKind.Array)
        {
            return;
        }

        foreach (var role in roles.EnumerateArray())
        {
            var name = role.GetString();
            if (!string.IsNullOrEmpty(name) && !identity.HasClaim(ClaimTypes.Role, name))
            {
                identity.AddClaim(new Claim(ClaimTypes.Role, name));
            }
        }
    }
}

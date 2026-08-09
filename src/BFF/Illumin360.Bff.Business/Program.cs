using System.Security.Claims;
using System.Threading.RateLimiting;
using Illumin360.Bff.Business;
using Illumin360.Observability;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Yarp.ReverseProxy.Transforms;

var builder = WebApplication.CreateBuilder(args);

// --- Cross-cutting: OpenTelemetry + Serilog (charter Part 10) ---
builder.AddProjectObservability("bff-business");

var oidc = builder.Configuration.GetSection("Oidc");

// --- Auth: server-side cookie session + OIDC authorization-code flow against Keycloak ---
// The BFF holds the tokens; the SPA only ever sees an HttpOnly session cookie (charter Part 2/7).
builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;

    // Default challenge = cookie so unauthenticated XHR/API calls receive 401 (the SPA then sends the user
    // to /bff/login, which explicitly challenges OIDC) rather than being redirected to the IdP.
    options.DefaultChallengeScheme = CookieAuthenticationDefaults.AuthenticationScheme;
})
.AddCookie(options =>
{
    options.Cookie.Name = "illumin360.bff";
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest; // production terminates TLS → Always
    options.ExpireTimeSpan = TimeSpan.FromHours(8);
    options.SlidingExpiration = true;

    // SPA-friendly: respond 401 to XHR instead of redirecting the API/user calls to the IdP.
    options.Events.OnRedirectToLogin = ctx =>
    {
        ctx.Response.StatusCode = StatusCodes.Status401Unauthorized;
        return Task.CompletedTask;
    };
    options.Events.OnRedirectToAccessDenied = ctx =>
    {
        ctx.Response.StatusCode = StatusCodes.Status403Forbidden;
        return Task.CompletedTask;
    };
})
.AddOpenIdConnect(options =>
{
    options.Authority = oidc["Authority"];
    options.ClientId = oidc["ClientId"];
    options.ClientSecret = oidc["ClientSecret"];
    options.ResponseType = OpenIdConnectResponseType.Code;
    options.UsePkce = true;

    // Authorization-code + PKCE is the secure baseline. PAR (Pushed Authorization Requests) adds
    // defence-in-depth but requires the confidential client registered in Keycloak with PAR enabled;
    // opt back in (UseIfAvailable) once that client exists.
    options.PushedAuthorizationBehavior = PushedAuthorizationBehavior.Disable;

    options.SaveTokens = true; // persist access/refresh/id tokens in the encrypted auth cookie

    // Read identity claims from the id_token (scopes openid/profile/email) rather than calling the
    // user-info endpoint. The user authenticates against the front-channel host, so the token `iss` is that
    // host — Keycloak's back-channel user-info endpoint rejects a token minted for a different host. Reading
    // claims from the already-validated id_token avoids that cross-host round-trip (and is leaner for a BFF).
    options.GetClaimsFromUserInfoEndpoint = false;
    options.CallbackPath = "/bff/signin-callback";
    options.SignedOutCallbackPath = "/bff/signout-callback";
    options.RequireHttpsMetadata = oidc.GetValue("RequireHttps", false);
    options.MapInboundClaims = false;
    options.Scope.Clear();
    options.Scope.Add("openid");
    options.Scope.Add("profile");
    options.Scope.Add("email");
    options.TokenValidationParameters.NameClaimType = "name";
    options.TokenValidationParameters.RoleClaimType = "roles";

    // --- Keycloak hostname alignment (charter Part 2/7) ---
    // In Docker the BFF reaches Keycloak by its in-network name (`keycloak:8080`), but the user's browser
    // must be sent to a host it can actually resolve (`localhost:8080`). We keep `Authority` on the
    // back-channel host so discovery, the code→token exchange and JWKS retrieval all stay internal, and
    // rewrite ONLY the redirects the browser follows — authorization + end-session — to the front-channel
    // host. The user authenticates at the front-channel host, so the issued token `iss` is that host — we
    // add it to ValidIssuers below (JWKS still comes from back-channel discovery). This avoids pinning a
    // fixed `KC_HOSTNAME` on the *shared* dev Keycloak, which would rewrite the issuer for the sibling
    // apps (SalesApp / StoreCatalogue) and break their back-channel token validation.
    // Unset (local dev, where both channels are already localhost) → no rewrite, behaviour unchanged.
    var frontChannel = oidc["FrontChannelAuthority"];
    if (!string.IsNullOrWhiteSpace(frontChannel))
    {
        var front = new Uri(frontChannel);
        static string ToFrontChannel(string? url, Uri front) =>
            string.IsNullOrEmpty(url)
                ? url ?? string.Empty
                : new UriBuilder(url) { Scheme = front.Scheme, Host = front.Host, Port = front.Port }.Uri.AbsoluteUri;

        // Keycloak stamps the token `iss` with the host the user *authenticated* against — the front-channel
        // host (localhost) — while discovery is fetched over the back-channel host (keycloak). Accept both so
        // issuer validation succeeds; the signing keys (JWKS) still come from the back-channel discovery.
        options.TokenValidationParameters.ValidIssuers = new[] { oidc["Authority"]!, frontChannel };

        options.Events ??= new OpenIdConnectEvents();
        options.Events.OnRedirectToIdentityProvider = ctx =>
        {
            ctx.ProtocolMessage.IssuerAddress = ToFrontChannel(ctx.ProtocolMessage.IssuerAddress, front);
            return Task.CompletedTask;
        };
        options.Events.OnRedirectToIdentityProviderForSignOut = ctx =>
        {
            ctx.ProtocolMessage.IssuerAddress = ToFrontChannel(ctx.ProtocolMessage.IssuerAddress, front);
            return Task.CompletedTask;
        };
    }
});

builder.Services.AddAuthorization(options =>
    options.AddPolicy("authenticated", policy => policy.RequireAuthenticatedUser()));

// --- YARP: token-relay proxy to the gateway + same-origin SPA passthrough ---
builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"))
    .AddTransforms(context =>
    {
        // Token relay: attach the signed-in user's access token to requests forwarded to the API gateway.
        if (context.Route.RouteId == "bff-api")
        {
            context.AddRequestTransform(async transform =>
            {
                var token = await transform.HttpContext.GetTokenAsync("access_token").ConfigureAwait(false);
                if (!string.IsNullOrEmpty(token))
                {
                    transform.ProxyRequest.Headers.Authorization =
                        new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
                }
            });
        }
    });

// Self-registration: Keycloak user provisioning + rate limiting (abuse control on a public endpoint).
builder.Services.AddHttpClient();
builder.Services.AddScoped<KeycloakRegistrar>();
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("register", httpContext => RateLimitPartition.GetFixedWindowLimiter(
        httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new FixedWindowRateLimiterOptions { PermitLimit = 5, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));
});

builder.Services.AddHealthChecks()
    .AddCheck("self", () => Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy(), tags: ["live", "ready"]);

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();

// --- Health probes (charter Part 11) ---
app.MapProjectHealthChecks();

// --- Self-registration (anonymous, rate-limited): provisions Keycloak identity + role + domain profile ---
app.MapPost("/register/{type}", async (string type, RegisterRequest req, KeycloakRegistrar registrar, CancellationToken ct) =>
    {
        var result = await registrar.RegisterAsync(type, req, ct);
        return Results.Json(new { code = result.Code, message = result.Message }, statusCode: result.StatusCode);
    })
    .RequireRateLimiting("register")
    .WithName("Register");

// --- BFF session endpoints ---
var bff = app.MapGroup("/bff");

bff.MapGet("/login", (string? returnUrl) => Results.Challenge(
        new AuthenticationProperties { RedirectUri = SafeReturn(returnUrl) },
        [OpenIdConnectDefaults.AuthenticationScheme]))
    .WithName("BffLogin");

bff.MapPost("/logout", () => Results.SignOut(
        new AuthenticationProperties { RedirectUri = "/" },
        [CookieAuthenticationDefaults.AuthenticationScheme, OpenIdConnectDefaults.AuthenticationScheme]))
    .RequireAuthorization("authenticated")
    .WithName("BffLogout");

// The SPA polls this on load to learn whether a session exists and who the user is (no tokens exposed).
bff.MapGet("/user", (ClaimsPrincipal user) =>
{
    if (user.Identity?.IsAuthenticated != true)
    {
        return Results.Json(new { authenticated = false });
    }

    return Results.Json(new
    {
        authenticated = true,
        name = user.FindFirstValue("name") ?? user.Identity.Name,
        email = user.FindFirstValue("email"),
        company = user.FindFirstValue("company") ?? user.FindFirstValue("organization"),
    });
}).WithName("BffUser");

// API calls require a session and are token-relayed to the gateway; everything else proxies to the SPA.
app.MapReverseProxy();

app.Run();

// Only allow local relative return URLs — never an attacker-supplied absolute URL (open-redirect guard).
static string SafeReturn(string? returnUrl)
    => !string.IsNullOrEmpty(returnUrl) && returnUrl.StartsWith('/') && !returnUrl.StartsWith("//", StringComparison.Ordinal)
        ? returnUrl
        : "/";

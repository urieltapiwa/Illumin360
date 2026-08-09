using System.Security.Claims;
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
    options.GetClaimsFromUserInfoEndpoint = true;
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

builder.Services.AddHealthChecks()
    .AddCheck("self", () => Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy(), tags: ["live", "ready"]);

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();

// --- Health probes (charter Part 11) ---
app.MapProjectHealthChecks();

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

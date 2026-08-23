using Illumin360.Portal.Components;
using Illumin360.Portal.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

var builder = WebApplication.CreateBuilder(args);

// --- Blazor SSR (static server rendering + interactive Server islands where needed) ---
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddHttpContextAccessor();
builder.Services.AddCascadingAuthenticationState();

// --- Auth: server-side cookie session + OIDC code+PKCE against Keycloak ---
// Mirrors Illumin360.Bff.Business: the server holds the tokens; the browser only ever sees an HttpOnly
// session cookie. The Blazor app subsumes the BFF role for its own (server-rendered) pages.
var oidc = builder.Configuration.GetSection("Oidc");

builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
})
.AddCookie(options =>
{
    options.Cookie.Name = "illumin360.portal";
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest; // production terminates TLS -> Always
    options.ExpireTimeSpan = TimeSpan.FromHours(8);
    options.SlidingExpiration = true;
})
.AddOpenIdConnect(options =>
{
    options.Authority = oidc["Authority"];
    options.ClientId = oidc["ClientId"];
    options.ClientSecret = oidc["ClientSecret"];
    options.ResponseType = OpenIdConnectResponseType.Code;
    options.UsePkce = true;
    options.SaveTokens = true; // access/refresh/id tokens persisted in the encrypted auth cookie
    options.GetClaimsFromUserInfoEndpoint = false;
    options.CallbackPath = "/signin-oidc";
    options.SignedOutCallbackPath = "/signout-callback-oidc";
    options.RequireHttpsMetadata = oidc.GetValue("RequireHttps", false);
    options.MapInboundClaims = false;
    options.Scope.Clear();
    options.Scope.Add("openid");
    options.Scope.Add("profile");
    options.Scope.Add("email");
    options.TokenValidationParameters.NameClaimType = "name";
    options.TokenValidationParameters.RoleClaimType = "roles";

    // Keycloak front/back-channel alignment (identical rationale to the BFF): keep Authority on the
    // back-channel host for discovery/token/JWKS, rewrite only the browser-facing redirects to the
    // front-channel host, and accept both issuers. Unset in local host-run dev (both are localhost).
    var frontChannel = oidc["FrontChannelAuthority"];
    if (!string.IsNullOrWhiteSpace(frontChannel))
    {
        var front = new Uri(frontChannel);
        static string ToFront(string? url, Uri f) =>
            string.IsNullOrEmpty(url) ? url ?? string.Empty
                : new UriBuilder(url) { Scheme = f.Scheme, Host = f.Host, Port = f.Port }.Uri.AbsoluteUri;

        options.TokenValidationParameters.ValidIssuers = new[] { oidc["Authority"]!, frontChannel };
        options.Events ??= new OpenIdConnectEvents();
        options.Events.OnRedirectToIdentityProvider = ctx =>
        {
            ctx.ProtocolMessage.IssuerAddress = ToFront(ctx.ProtocolMessage.IssuerAddress, front);
            return Task.CompletedTask;
        };
        options.Events.OnRedirectToIdentityProviderForSignOut = ctx =>
        {
            ctx.ProtocolMessage.IssuerAddress = ToFront(ctx.ProtocolMessage.IssuerAddress, front);
            return Task.CompletedTask;
        };
    }
});

builder.Services.AddAuthorization();

// --- Typed gateway client with server-side token relay ---
builder.Services.AddTransient<TokenRelayHandler>();
builder.Services.AddHttpClient<EmployersApiClient>(client =>
        client.BaseAddress = new Uri(builder.Configuration["Gateway:BaseAddress"] ?? "http://localhost:8088"))
    .AddHttpMessageHandler<TokenRelayHandler>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts(); // OWASP A05 / ASVS V9 — HSTS (prod, behind TLS)
}

app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);

// --- Security response headers (OWASP Top 10 A05, ASVS V14.4, NIST SP 800-53 SC-7/SC-18) ---
// Defence-in-depth: lock the browser down to same-origin resources, forbid framing (clickjacking),
// stop MIME-sniffing, and minimise referrer leakage. CSP allows 'unsafe-inline' for styles only
// (Blazor scoped styles) — never for scripts. Set on OnStarting with overwrite so we emit a single
// authoritative value even if a framework default also writes one.
app.Use((ctx, next) =>
{
    ctx.Response.OnStarting(() =>
    {
        var h = ctx.Response.Headers;
        h.ContentSecurityPolicy =
            "default-src 'self'; script-src 'self'; style-src 'self' 'unsafe-inline'; " +
            "img-src 'self' data:; connect-src 'self' ws: wss:; base-uri 'self'; " +
            "form-action 'self'; frame-ancestors 'none'; object-src 'none'";
        h.XContentTypeOptions = "nosniff";
        h.XFrameOptions = "DENY";
        h["Referrer-Policy"] = "no-referrer";
        h["Permissions-Policy"] = "camera=(), microphone=(), geolocation=()";
        return Task.CompletedTask;
    });
    return next();
});

app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

app.MapStaticAssets();

// --- Auth endpoints (sign in / out via the OIDC scheme) ---
app.MapGet("/auth/login", (string? returnUrl) => Results.Challenge(
    new AuthenticationProperties { RedirectUri = SafeReturn(returnUrl) },
    [OpenIdConnectDefaults.AuthenticationScheme]));

app.MapPost("/auth/logout", () => Results.SignOut(
    new AuthenticationProperties { RedirectUri = "/employer" },
    [CookieAuthenticationDefaults.AuthenticationScheme, OpenIdConnectDefaults.AuthenticationScheme]))
    .RequireAuthorization();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();

// Open-redirect guard: only local relative return URLs.
static string SafeReturn(string? returnUrl)
    => !string.IsNullOrEmpty(returnUrl) && returnUrl.StartsWith('/') && !returnUrl.StartsWith("//", StringComparison.Ordinal)
        ? returnUrl : "/employer";

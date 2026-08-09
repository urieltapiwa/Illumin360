using Illumin360.Observability;

var builder = WebApplication.CreateBuilder(args);

// Backend-for-Frontend for the Business (employer) portal (charter Parts 5 & 6).
// Holds the OIDC session server-side; the browser only gets an HttpOnly cookie.
builder.AddProjectObservability("business-bff");

builder.Services.AddAuthentication(options =>
    {
        options.DefaultScheme = "Cookies";
        options.DefaultChallengeScheme = "oidc";
    })
    .AddCookie("Cookies", o =>
    {
        o.Cookie.HttpOnly = true;
        o.Cookie.SecurePolicy = Microsoft.AspNetCore.Http.CookieSecurePolicy.Always;
    })
    .AddOpenIdConnect("oidc", options =>
    {
        // Keycloak realm "illumin360", client "business-web" (charter Part 8).
        options.Authority = builder.Configuration["Keycloak:Authority"] ?? "http://localhost:8080/realms/illumin360";
        options.ClientId = "business-web";
        options.ClientSecret = builder.Configuration["Keycloak:ClientSecret"]; // from Vault in non-dev
        options.ResponseType = "code";
        options.UsePkce = true;
        options.SaveTokens = true;
        options.Scope.Add("openid");
        options.Scope.Add("profile");
    });
builder.Services.AddAuthorization();

// Typed, resilient HTTP client to the Candidates domain service via the gateway (charter Part 12).
builder.Services.AddHttpClient("candidates", c =>
        c.BaseAddress = new Uri(builder.Configuration["Services:Candidates"] ?? "http://gateway:8080/api/candidates/"))
    .AddStandardResilienceHandler();

builder.Services.AddHealthChecks()
    .AddCheck("self", () => Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy(), tags: ["live", "ready"]);

var app = builder.Build();
app.UseAuthentication();
app.UseAuthorization();
app.MapProjectHealthChecks();

// Example aggregation endpoint shaped for exactly this frontend.
app.MapGet("/bff/candidates", async (IHttpClientFactory factory, CancellationToken ct) =>
    {
        var client = factory.CreateClient("candidates");
        var payload = await client.GetStringAsync("?pageSize=20", ct);
        return Results.Content(payload, "application/json");
    })
    .RequireAuthorization();

app.Run();

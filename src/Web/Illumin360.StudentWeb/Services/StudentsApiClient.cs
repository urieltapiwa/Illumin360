using System.Net.Http.Json;
using Microsoft.AspNetCore.Authentication;

namespace Illumin360.StudentWeb.Services;

// Live dashboard shape returned by GET /api/students/me (Students service).
public sealed record Persona(string? Name, string? Field, string? School, string? Year, string? Graduating, int Readiness, string? Program, string? City, string? Availability);

public sealed record StudentKpis(int ProfileViews, int ViewsDelta, int InternshipMatches, int Applications, int SkillsDone, int MentorSessions, int Readiness);

public sealed record Learning(string Name, int Progress, string? Tag);

public sealed record StudentDashboard(Persona? Persona, StudentKpis? Kpis, int[]? ViewsTrend, Learning[]? Learning);

// Attaches the signed-in user's access token to gateway calls (relay). GET /me is currently
// anonymous, but sending the token keeps parity with the protected endpoints.
public sealed class TokenRelayHandler(IHttpContextAccessor accessor) : DelegatingHandler
{
    private readonly IHttpContextAccessor _accessor = accessor;

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var http = _accessor.HttpContext;
        if (http?.User.Identity?.IsAuthenticated == true)
        {
            var token = await http.GetTokenAsync("access_token").ConfigureAwait(false);
            if (!string.IsNullOrEmpty(token))
            {
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            }
        }

        return await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
    }
}

// Typed client for the Students service via the gateway.
public sealed class StudentsApiClient(HttpClient http)
{
    private readonly HttpClient _http = http;

    public async Task<StudentDashboard?> GetDashboardAsync(CancellationToken ct = default)
    {
        try
        {
            return await _http.GetFromJsonAsync<StudentDashboard>("/api/students/me", ct).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or System.Text.Json.JsonException)
        {
            return null; // API unavailable — the view falls back to placeholder values.
        }
    }
}

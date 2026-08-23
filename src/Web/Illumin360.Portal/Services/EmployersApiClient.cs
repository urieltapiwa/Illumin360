using System.Net;
using System.Net.Http.Json;

namespace Illumin360.Portal.Services;

// DTOs mirror the Employers service contracts (Illumin360.Employers.Contracts).
public sealed record EmployerDto(Guid Id, string CompanyName, string Industry, string City, string? Website, string? About);

public sealed record TeamMemberDto(Guid Id, Guid EmployerId, string Email, string DisplayName, string Role, DateTimeOffset InvitedAt);

public sealed record InviteTeamMember(string Email, string DisplayName, string Role);

/// <summary>Result of an invite attempt, so pages can render role-aware outcomes (e.g. 403 = wrong role).</summary>
public sealed record InviteResult(bool Ok, HttpStatusCode Status, TeamMemberDto? Member, string? Message);

/// <summary>
/// Typed, server-side client for the Employers service, reached through the API gateway. The access
/// token (when present) is attached by <see cref="TokenRelayHandler"/>. All calls run on the server
/// during SSR — the browser only ever receives rendered HTML.
/// </summary>
public sealed class EmployersApiClient(HttpClient http)
{
    private readonly HttpClient _http = http;

    public async Task<EmployerDto?> GetMyProfileAsync(CancellationToken ct = default)
        => await _http.GetFromJsonAsync<EmployerDto>("/api/employers/me", ct).ConfigureAwait(false);

    public async Task<IReadOnlyList<TeamMemberDto>> GetTeamAsync(CancellationToken ct = default)
        => await _http.GetFromJsonAsync<IReadOnlyList<TeamMemberDto>>("/api/employers/me/team", ct)
               .ConfigureAwait(false) ?? [];

    public async Task<InviteResult> InviteAsync(InviteTeamMember invite, CancellationToken ct = default)
    {
        var res = await _http.PostAsJsonAsync("/api/employers/me/team", invite, ct).ConfigureAwait(false);
        if (res.IsSuccessStatusCode)
        {
            var member = await res.Content.ReadFromJsonAsync<TeamMemberDto>(ct).ConfigureAwait(false);
            return new InviteResult(true, res.StatusCode, member, null);
        }

        var message = res.StatusCode switch
        {
            HttpStatusCode.Unauthorized => "Not signed in — log in to invite team members.",
            HttpStatusCode.Forbidden => "Your role can't invite team members (requires admin write).",
            HttpStatusCode.Conflict => "That email is already on the team.",
            _ => $"Invite failed ({(int)res.StatusCode}).",
        };
        return new InviteResult(false, res.StatusCode, null, message);
    }
}

using Microsoft.AspNetCore.Authentication;

namespace Illumin360.Portal.Services;

/// <summary>
/// Attaches the signed-in user's access token to outgoing gateway calls. The token lives only in the
/// server-side encrypted auth cookie (OIDC SaveTokens) and is read here per-request — it never reaches
/// the browser. Anonymous requests (no session) simply go out without a bearer header; the API then
/// returns 401/403 for protected routes, exactly as the SPA+BFF did.
/// </summary>
public sealed class TokenRelayHandler(IHttpContextAccessor accessor) : DelegatingHandler
{
    private readonly IHttpContextAccessor _accessor = accessor;

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var http = _accessor.HttpContext;
        if (http?.User.Identity?.IsAuthenticated == true)
        {
            var token = await http.GetTokenAsync("access_token").ConfigureAwait(false);
            if (!string.IsNullOrEmpty(token))
            {
                request.Headers.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            }
        }

        return await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
    }
}

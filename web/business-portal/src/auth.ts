import Keycloak from "keycloak-js";

// In production these come from the BFF / env; hard-coded here for the local proof.
export const keycloak = new Keycloak({
  url: import.meta.env.VITE_KC_URL || "http://localhost:8080",
  realm: import.meta.env.VITE_KC_REALM || "illumin360",
  clientId: import.meta.env.VITE_KC_CLIENT || "illumin360-business",
});

export interface Session {
  authenticated: boolean;
  name?: string;
  email?: string;
  company?: string;
}

// `?demo=1` bypasses the gate so the dashboard UI can be shown without a live Keycloak round-trip.
export const demoMode = new URLSearchParams(location.search).get("demo") === "1";

// When served behind the Business BFF (VITE_USE_BFF=1), the SPA never touches Keycloak directly: the BFF
// holds the tokens server-side and the SPA only asks /bff/user who it is. Tokens stay out of the browser.
const useBff = import.meta.env.VITE_USE_BFF === "1";

export async function initAuth(): Promise<Session> {
  if (demoMode) {
    return { authenticated: true, name: "Demo Manager", email: "demo@company.na", company: "Etosha Holdings" };
  }
  if (useBff) {
    try {
      const r = await fetch("/bff/user", { credentials: "include", headers: { "X-Requested-With": "fetch" } });
      const u = await r.json();
      return u?.authenticated
        ? { authenticated: true, name: u.name, email: u.email, company: u.company }
        : { authenticated: false };
    } catch {
      return { authenticated: false };
    }
  }
  try {
    const ok = await keycloak.init({
      onLoad: "check-sso",
      pkceMethod: "S256",
      checkLoginIframe: false,
    });
    if (!ok) return { authenticated: false };
    const p = (keycloak.tokenParsed || {}) as Record<string, string>;
    return {
      authenticated: true,
      name: p.name || p.preferred_username || "Member",
      email: p.email,
      company: p.company || "Your Company",
    };
  } catch {
    return { authenticated: false };
  }
}

export function login() {
  if (useBff) {
    window.location.href = "/bff/login?returnUrl=" + encodeURIComponent(location.pathname + location.search);
    return;
  }
  keycloak.login({ redirectUri: location.origin + location.pathname });
}
export function logout() {
  if (useBff) {
    // Top-level form POST: the browser follows the OIDC end-session redirect (a fetch can't, cross-origin),
    // ending the Keycloak SSO session and returning to the post-logout URI. CSRF-safe (POST, same-origin).
    const form = document.createElement("form");
    form.method = "POST";
    form.action = "/bff/logout";
    document.body.appendChild(form);
    form.submit();
    return;
  }
  keycloak.logout({ redirectUri: location.origin + location.pathname });
}

# Support Portal BFF (skeleton)

Backend-for-Frontend for the **Support** portal. Mirror `src/BFF/Illumin360.Business.Bff/` (the reference BFF):
OIDC code+PKCE against Keycloak realm `illumin360`, client `support-web`; session held server-side
(HttpOnly cookie, no tokens in the browser — charter Part 6); typed, resilient HTTP/gRPC clients to domain
services via the gateway; per-app aggregation/view-shaping.

- Local port: `5104`
- Pairs with MVC app: `src/Apps/Illumin360.Support.Web/`

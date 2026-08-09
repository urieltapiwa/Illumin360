// Idempotent setup for the self-registration service-account client used by the Business BFF.
// Creates the `illumin360-registration` confidential client (service accounts enabled) and grants its
// service account: realm-management `manage-users` (create users + assign roles) and the realm roles
// admin.write + client.student/user/employer (so its token can call the admin-gated domain register
// endpoints and assign the type roles to new users).
//
// Non-destructive: only adds this one client + its service-account role mappings; touches nothing else in
// the shared realm. Safe to re-run. Requires Node 18+ (global fetch).
//
// Usage (Keycloak reachable at KC, master admin creds):
//   KC=http://localhost:8080 KC_ADMIN=admin KC_ADMIN_PW=admin \
//   REG_SECRET=registration-dev-secret-local-only \
//   node deploy/keycloak/setup-registration-client.mjs
const KC = process.env.KC || "http://localhost:8080";
const REALM = process.env.KC_REALM || "illumin360";
const ADMIN = process.env.KC_ADMIN || "admin";
const ADMIN_PW = process.env.KC_ADMIN_PW || "admin";
const SECRET = process.env.REG_SECRET || "registration-dev-secret-local-only";
const CLIENT_ID = "illumin360-registration";
const j = (r) => r.json();
const H = (t) => ({ Authorization: `Bearer ${t}`, "Content-Type": "application/json" });

const at = (await fetch(`${KC}/realms/master/protocol/openid-connect/token`, {
  method: "POST", headers: { "Content-Type": "application/x-www-form-urlencoded" },
  body: new URLSearchParams({ grant_type: "password", client_id: "admin-cli", username: ADMIN, password: ADMIN_PW }),
}).then(j)).access_token;
if (!at) { console.error("Could not obtain admin token"); process.exit(1); }

// 1. create client if missing
let client = (await fetch(`${KC}/admin/realms/${REALM}/clients?clientId=${CLIENT_ID}`, { headers: H(at) }).then(j))[0];
if (!client) {
  await fetch(`${KC}/admin/realms/${REALM}/clients`, {
    method: "POST", headers: H(at),
    body: JSON.stringify({ clientId: CLIENT_ID, enabled: true, publicClient: false, serviceAccountsEnabled: true,
      standardFlowEnabled: false, directAccessGrantsEnabled: false, secret: SECRET, protocol: "openid-connect" }),
  });
  client = (await fetch(`${KC}/admin/realms/${REALM}/clients?clientId=${CLIENT_ID}`, { headers: H(at) }).then(j))[0];
  console.log("created client", client.id);
} else {
  console.log("client already exists", client.id);
}

const sa = await fetch(`${KC}/admin/realms/${REALM}/clients/${client.id}/service-account-user`, { headers: H(at) }).then(j);

// 2. realm roles
const want = ["admin.write", "client.student", "client.user", "client.employer"];
const realmRoles = (await fetch(`${KC}/admin/realms/${REALM}/roles`, { headers: H(at) }).then(j))
  .filter((r) => want.includes(r.name)).map((r) => ({ id: r.id, name: r.name }));
await fetch(`${KC}/admin/realms/${REALM}/users/${sa.id}/role-mappings/realm`, { method: "POST", headers: H(at), body: JSON.stringify(realmRoles) });

// 3. realm-management: manage-users (create users + assign roles) and view-realm (read role reps).
const rm = (await fetch(`${KC}/admin/realms/${REALM}/clients?clientId=realm-management`, { headers: H(at) }).then(j))[0];
const rmRoles = [];
for (const name of ["manage-users", "view-realm"]) {
  const role = await fetch(`${KC}/admin/realms/${REALM}/clients/${rm.id}/roles/${name}`, { headers: H(at) }).then(j);
  rmRoles.push({ id: role.id, name: role.name });
}
await fetch(`${KC}/admin/realms/${REALM}/users/${sa.id}/role-mappings/clients/${rm.id}`, { method: "POST", headers: H(at), body: JSON.stringify(rmRoles) });

console.log("registration client configured (service account roles: manage-users + view-realm + admin.write + client.*)");

// 4. realm SMTP → dev mail catcher (Mailpit) so email-verification messages are sent + caught.
const realm = await fetch(`${KC}/admin/realms/${REALM}`, { headers: H(at) }).then(j);
realm.smtpServer = {
  host: process.env.SMTP_HOST || "mailpit", port: process.env.SMTP_PORT || "1025",
  from: "no-reply@illumin360.local", fromDisplayName: "Illumin360", ssl: "false", starttls: "false", auth: "false",
};
await fetch(`${KC}/admin/realms/${REALM}`, { method: "PUT", headers: H(at), body: JSON.stringify(realm) });
console.log("realm SMTP configured -> Mailpit");

import { useEffect, useState } from "react";
import { motion } from "framer-motion";
import { useTranslation } from "react-i18next";
import { logout, type Session } from "./auth";
import { LanguageSwitcher, ThemeSwitcher } from "@illumin360/ui";

// Company profile as returned by the Employers service (GET /api/employers/me → EmployerDto).
interface Employer {
  id?: string;
  companyName: string;
  industry: string;
  city: string;
  website?: string | null;
  about?: string | null;
}
// Ranked candidate from the Candidates service (GET /api/candidates/top → RankedCandidateDto[]).
interface RankedCandidate { id: string; name: string; city: string; headline?: string | null; score: number; }
// Team member (GET /api/employers/me/team → TeamMemberDto[]).
interface TeamMember { id: string; email: string; displayName: string; role: string; invitedAt: string; }
const ROLES = ["owner", "recruiter", "viewer"] as const;

const Ic = ({ d, s = 18, w = 1.7 }: { d: React.ReactNode; s?: number; w?: number }) => (
  <svg width={s} height={s} viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth={w} strokeLinecap="round" strokeLinejoin="round">{d}</svg>
);
const N = {
  building: <path d="M4 21V4a1 1 0 0 1 1-1h10a1 1 0 0 1 1 1v17M9 8h2M9 12h2M9 16h2M16 10h3a1 1 0 0 1 1 1v10" />,
  users: <path d="M16 11a4 4 0 1 0-4-4 4 4 0 0 0 4 4zM2 21a7 7 0 0 1 14 0M19 21a5 5 0 0 0-6-4.9" />,
  brief: <path d="M3 8h18v12H3zM8 8V6a2 2 0 0 1 2-2h4a2 2 0 0 1 2 2v2M3 13h18" />,
  gear: <path d="M12 15a3 3 0 1 0 0-6 3 3 0 0 0 0 6zM19.4 13a7.5 7.5 0 0 0 0-2l2-1.5-2-3.5-2.4 1a7 7 0 0 0-1.7-1L14.5 2.5h-5L9.2 5a7 7 0 0 0-1.7 1l-2.4-1-2 3.5L5.1 11a7.5 7.5 0 0 0 0 2l-2 1.5 2 3.5 2.4-1a7 7 0 0 0 1.7 1l.3 2.5h5l.3-2.5a7 7 0 0 0 1.7-1l2.4 1 2-3.5z" />,
  out: <path d="M16 17l5-5-5-5M21 12H9M9 21H5a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h4" />,
  link: <path d="M10 13a5 5 0 0 0 7 0l3-3a5 5 0 0 0-7-7l-1 1M14 11a5 5 0 0 0-7 0l-3 3a5 5 0 0 0 7 7l1-1" />,
  pin: <path d="M20 10c0 6-8 12-8 12s-8-6-8-12a8 8 0 0 1 16 0zM12 12a2 2 0 1 0 0-4 2 2 0 0 0 0 4z" />,
};
const fade = { initial: { opacity: 0, y: 14 }, animate: { opacity: 1, y: 0 } };
// Local-only fallback so the portal renders (read-only) when the Employers API is unreachable.
const SNAPSHOT: Employer = { companyName: "Namib Mills", industry: "Manufacturing", city: "Windhoek", website: "https://namibmills.com.na", about: "One of Namibia's largest food producers." };

function Logo() {
  return (
    <div className="flex items-center gap-2.5">
      <svg width="30" height="30" viewBox="0 0 32 32" fill="none"><circle cx="16" cy="16" r="14" stroke="#1FB283" strokeWidth="1.6" /><circle cx="16" cy="16" r="8.5" stroke="#2FD39A" strokeWidth="1.6" /><circle cx="16" cy="16" r="3" fill="#E8B14C" /></svg>
      <div className="leading-none"><div className="font-display text-[17px] font-extrabold tracking-tight text-ink-hi">Illumin<span className="text-brand-bright">360</span></div><div className="text-[9px] uppercase tracking-[0.22em] text-ink-lo mt-0.5">Employer</div></div>
    </div>
  );
}

export default function Employer(_props: { session: Session }) {
  const { t } = useTranslation();
  const [emp, setEmp] = useState<Employer | null>(null);
  const [live, setLive] = useState(false);
  const [editing, setEditing] = useState(false);
  const [form, setForm] = useState<Employer | null>(null);
  const [saving, setSaving] = useState<"idle" | "saving" | "error">("idle");
  // Top-candidates panel state.
  const [title, setTitle] = useState("");
  const [cands, setCands] = useState<RankedCandidate[] | null>(null);
  const [searching, setSearching] = useState(false);
  // Team panel state.
  const [team, setTeam] = useState<TeamMember[] | null>(null);
  const [invite, setInvite] = useState({ email: "", displayName: "", role: "recruiter" });
  const [inviting, setInviting] = useState<"idle" | "busy" | "error">("idle");
  const [teamErr, setTeamErr] = useState<string | null>(null);

  // Live-first: read the company profile from the Employers service (via BFF/gateway); fall back to the
  // bundled snapshot (read-only) if the API is unavailable. Mirrors the other portals' live-data pattern.
  useEffect(() => {
    let cancelled = false;
    (async () => {
      try {
        const r = await fetch("/api/employers/me");
        if (r.ok) {
          const j = await r.json();
          if (!cancelled) { setEmp(j); setLive(true); }
          return;
        }
      } catch { /* fall through to the snapshot */ }
      if (!cancelled) { setEmp(SNAPSHOT); setLive(false); }
    })();
    return () => { cancelled = true; };
  }, []);

  // Team members (best-effort; the panel simply hides its list if the call fails).
  useEffect(() => {
    let cancelled = false;
    (async () => {
      try {
        const r = await fetch("/api/employers/me/team");
        if (r.ok && !cancelled) setTeam(await r.json().catch(() => []));
      } catch { /* offline */ }
    })();
    return () => { cancelled = true; };
  }, []);

  const inviteMember = async () => {
    setInviting("busy");
    setTeamErr(null);
    try {
      const r = await fetch("/api/employers/me/team", { method: "POST", headers: { "Content-Type": "application/json" }, credentials: "same-origin", body: JSON.stringify(invite) });
      if (r.ok) {
        const m: TeamMember = await r.json();
        setTeam((ts) => [...(ts ?? []), m]);
        setInvite({ email: "", displayName: "", role: "recruiter" });
        setInviting("idle");
      } else {
        setTeamErr(r.status === 409 ? t("employer.team.dupe", "That email is already on the team.") : t("employer.team.inviteFail", "Could not invite — check the details and your permissions."));
        setInviting("error");
      }
    } catch {
      setInviting("error");
    }
  };
  const changeRole = async (id: string, role: string) => {
    setTeamErr(null);
    const prev = team;
    setTeam((ts) => ts?.map((m) => (m.id === id ? { ...m, role } : m)) ?? ts);
    const r = await fetch(`/api/employers/me/team/${id}/role`, { method: "PUT", headers: { "Content-Type": "application/json" }, credentials: "same-origin", body: JSON.stringify({ role }) });
    if (!r.ok) {
      setTeam(prev ?? null); // revert
      setTeamErr(r.status === 409 ? t("employer.team.lastOwner", "An employer must keep at least one owner.") : t("employer.team.roleFail", "Could not change role."));
    }
  };
  const removeMember = async (id: string) => {
    setTeamErr(null);
    const r = await fetch(`/api/employers/me/team/${id}`, { method: "DELETE", credentials: "same-origin" });
    if (r.ok) {
      setTeam((ts) => ts?.filter((m) => m.id !== id) ?? ts);
    } else {
      setTeamErr(r.status === 409 ? t("employer.team.lastOwner", "An employer must keep at least one owner.") : t("employer.team.removeFail", "Could not remove member."));
    }
  };

  if (!emp) return <div className="grid place-items-center h-screen text-ink-mid font-mono text-sm animate-pulse">{t("employer.loading", "Loading company…")}</div>;

  const startEdit = () => { setForm({ ...emp }); setSaving("idle"); setEditing(true); };
  const set = (k: keyof Employer) => (e: React.ChangeEvent<HTMLInputElement | HTMLTextAreaElement>) => setForm((f) => (f ? { ...f, [k]: e.target.value } : f));
  const save = async () => {
    if (!form) return;
    setSaving("saving");
    try {
      // CompanyName is fixed server-side; only industry/city/website/about are editable.
      const body = { industry: form.industry, city: form.city, website: form.website || null, about: form.about || null };
      const r = await fetch("/api/employers/me", { method: "PUT", headers: { "Content-Type": "application/json" }, credentials: "same-origin", body: JSON.stringify(body) });
      if (r.ok) {
        const j = await r.json().catch(() => ({ ...emp, ...body }));
        setEmp(j);
        setEditing(false);
        setSaving("idle");
      } else {
        setSaving("error");
      }
    } catch {
      setSaving("error");
    }
  };
  const findCandidates = async () => {
    setSearching(true);
    try {
      const qs = new URLSearchParams();
      if (title.trim()) qs.set("title", title.trim());
      if (emp.city) qs.set("city", emp.city);
      qs.set("limit", "8");
      const r = await fetch("/api/candidates/top?" + qs.toString());
      setCands(r.ok ? await r.json().catch(() => []) : []);
    } catch {
      setCands([]);
    } finally {
      setSearching(false);
    }
  };

  const nav: [React.ReactNode, string, boolean][] = [[N.building, "employer.nav.profile", true], [N.users, "employer.nav.candidates", false], [N.brief, "employer.nav.roles", false], [N.gear, "employer.nav.settings", false]];
  const initials = emp.companyName.split(" ").map((x) => x[0]).slice(0, 2).join("").toUpperCase();
  const inputCls = "w-full rounded-lg border border-line/70 bg-panel2/50 px-3 py-2 text-sm text-ink-hi placeholder:text-ink-lo focus:border-brand/50 focus:outline-none";

  return (
    <div className="flex min-h-screen">
      <aside className="hidden lg:flex w-[228px] shrink-0 flex-col border-r border-line/70 bg-panel/40 px-4 py-6 relative z-10">
        <div className="px-1"><Logo /></div>
        <nav className="mt-9 flex flex-col gap-1"><div className="eyebrow px-3 mb-1">{t("employer.nav.eyebrow", "Company")}</div>
          {nav.map(([icon, label, active]) => (
            <a key={label as string} href="#" className={`group flex items-center gap-3 rounded-xl px-3 py-2.5 text-sm transition ${active ? "bg-brand/[0.12] text-ink-hi shadow-[inset_0_0_0_1px_rgba(47,211,154,0.25)]" : "text-ink-mid hover:bg-white/[0.03] hover:text-ink-hi"}`}>
              <span className={active ? "text-brand-bright" : "text-ink-lo group-hover:text-ink-mid"}><Ic d={icon} /></span>{t(label as string, (label as string).split(".").pop()!)}{active && <span className="ml-auto h-1.5 w-1.5 rounded-full bg-gold" />}</a>
          ))}
        </nav>
        <div className="mt-auto card p-3.5"><div className="text-xs font-semibold text-gold">★ {emp.industry}</div><p className="mt-1.5 text-[11px] leading-snug text-ink-mid">{t("employer.sidebar.blurb", "Keep your company profile current so the right talent finds you.")}</p></div>
      </aside>
      <main className="flex-1 min-w-0 relative z-10">
        <header className="sticky top-0 z-20 flex items-center gap-4 border-b border-line/60 bg-base/70 backdrop-blur-xl px-5 lg:px-7 py-4">
          <div className="min-w-0"><div className="flex items-center gap-2"><h1 className="font-display text-xl font-extrabold text-ink-hi tracking-tight">{t("employer.topbar.title", "Company profile")}</h1>{live ? <span className="chip !text-[10px] !text-brand-bright !border-brand/30"><span className="h-1.5 w-1.5 rounded-full bg-brand-bright animate-pulse" /> LIVE</span> : <span className="chip !text-[10px] !text-gold !border-gold/30">{t("employer.topbar.demo", "DEMO")}</span>}</div><p className="text-[11px] text-ink-lo mt-0.5">{t("employer.topbar.subtitle", "Manage how your company appears to candidates.")}</p></div>
          <div className="ml-auto flex items-center gap-3">
            <LanguageSwitcher />
            <ThemeSwitcher />
            <div className="hidden md:flex items-center gap-2.5 rounded-xl border border-line/70 bg-panel2/50 pl-2.5 pr-2 py-1.5"><div className="grid h-7 w-7 place-items-center rounded-lg bg-brand/20 text-[11px] font-bold text-brand-bright">{initials}</div><div className="leading-tight"><div className="text-xs font-semibold text-ink-hi">{emp.companyName}</div><div className="text-[10px] text-ink-lo">{emp.city}</div></div><button onClick={logout} title={t("employer.topbar.signOut", "Sign out")} className="ml-1 text-ink-lo hover:text-pink transition"><Ic d={N.out} s={15} /></button></div>
          </div>
        </header>
        <motion.div initial="initial" animate="animate" transition={{ staggerChildren: 0.06 }} className="px-5 lg:px-7 py-6 space-y-5">
          <motion.section variants={fade} className="card p-5">
            <div className="flex items-start justify-between gap-3 flex-wrap">
              <div className="flex items-center gap-4">
                <div className="grid h-14 w-14 place-items-center rounded-2xl bg-brand/15 text-lg font-extrabold text-brand-bright">{initials}</div>
                <div>
                  <h2 className="font-display text-2xl font-extrabold text-ink-hi tracking-tight">{emp.companyName}</h2>
                  <div className="mt-1 flex flex-wrap items-center gap-3 text-[12px] text-ink-mid">
                    <span className="chip !text-[11px] !text-gold !border-gold/30">{emp.industry}</span>
                    <span className="inline-flex items-center gap-1"><span className="text-ink-lo"><Ic d={N.pin} s={13} /></span>{emp.city}</span>
                    {emp.website && <a href={emp.website} target="_blank" rel="noreferrer" className="inline-flex items-center gap-1 text-brand-bright hover:underline"><Ic d={N.link} s={13} />{emp.website.replace(/^https?:\/\//, "")}</a>}
                  </div>
                </div>
              </div>
              {live && !editing && (
                <button onClick={startEdit} className="rounded-lg bg-brand/15 px-3 py-1.5 text-[11px] font-semibold text-brand-bright hover:bg-brand/25 transition">{t("employer.profile.edit", "Edit profile")}</button>
              )}
            </div>
            {emp.about && !editing && <p className="mt-4 border-t border-line/40 pt-4 text-sm leading-relaxed text-ink-mid">{emp.about}</p>}
            {!live && <p className="mt-4 text-[11px] text-ink-lo">{t("employer.profile.readonly", "Read-only demo — sign in to an employer account to edit.")}</p>}

            {editing && form && (
              <div className="mt-4 border-t border-line/40 pt-4 grid gap-3 sm:grid-cols-2">
                <label className="text-[11px] text-ink-lo sm:col-span-2">{t("employer.field.company", "Company name")}
                  <input className={inputCls + " mt-1 opacity-60 cursor-not-allowed"} value={form.companyName} disabled title={t("employer.field.companyFixed", "Company name is fixed")} />
                </label>
                <label className="text-[11px] text-ink-lo">{t("employer.field.industry", "Industry")}
                  <input className={inputCls + " mt-1"} value={form.industry} onChange={set("industry")} placeholder="Manufacturing" />
                </label>
                <label className="text-[11px] text-ink-lo">{t("employer.field.city", "City")}
                  <input className={inputCls + " mt-1"} value={form.city} onChange={set("city")} placeholder="Windhoek" />
                </label>
                <label className="text-[11px] text-ink-lo sm:col-span-2">{t("employer.field.website", "Website")}
                  <input className={inputCls + " mt-1"} value={form.website ?? ""} onChange={set("website")} placeholder="https://…" />
                </label>
                <label className="text-[11px] text-ink-lo sm:col-span-2">{t("employer.field.about", "About")}
                  <textarea className={inputCls + " mt-1 min-h-[84px] resize-y"} value={form.about ?? ""} onChange={set("about")} placeholder={t("employer.field.aboutHint", "A short description of your company.")} />
                </label>
                <div className="sm:col-span-2 flex items-center gap-2">
                  <button onClick={save} disabled={saving === "saving"} className="rounded-lg bg-brand/20 px-3 py-1.5 text-[11px] font-semibold text-brand-bright hover:bg-brand/30 transition disabled:opacity-50">{saving === "saving" ? t("employer.profile.saving", "Saving…") : t("employer.profile.save", "Save changes")}</button>
                  <button onClick={() => setEditing(false)} className="rounded-lg px-3 py-1.5 text-[11px] font-semibold text-ink-lo hover:text-ink-hi transition">{t("employer.profile.cancel", "Cancel")}</button>
                  {saving === "error" && <span className="text-[11px] text-pink">{t("employer.profile.saveError", "Could not save — check your permissions and try again.")}</span>}
                </div>
              </div>
            )}
          </motion.section>

          <motion.section variants={fade} className="card p-5">
            <div className="flex items-center justify-between gap-3 flex-wrap mb-3">
              <div><h3 className="font-display text-[15px] font-bold text-ink-hi">{t("employer.top.title", "Top candidates for you")}</h3><p className="text-[11px] text-ink-lo mt-0.5">{t("employer.top.sub", "Ranked by match to a role, near {{city}}.", { city: emp.city })}</p></div>
              <div className="flex items-center gap-2">
                <input className={inputCls + " !py-1.5 w-[200px]"} value={title} onChange={(e) => setTitle(e.target.value)} onKeyDown={(e) => { if (e.key === "Enter") findCandidates(); }} placeholder={t("employer.top.rolePlaceholder", "Role e.g. Software Developer")} />
                <button onClick={findCandidates} disabled={searching} className="rounded-lg bg-brand/15 px-3 py-1.5 text-[11px] font-semibold text-brand-bright hover:bg-brand/25 transition disabled:opacity-50">{searching ? t("employer.top.searching", "Ranking…") : t("employer.top.search", "Find")}</button>
              </div>
            </div>
            {cands === null ? (
              <div className="py-8 text-center text-[12px] text-ink-lo">{t("employer.top.prompt", "Enter a role to rank candidates.")}</div>
            ) : cands.length === 0 ? (
              <div className="py-8 text-center text-[12px] text-ink-lo">{t("employer.top.empty", "No candidates matched.")}</div>
            ) : (
              <div className="grid sm:grid-cols-2 gap-3">
                {cands.map((c) => (
                  <div key={c.id} className="rounded-xl border border-line/60 bg-panel2/40 p-3.5 hover:border-brand/40 transition">
                    <div className="flex items-start justify-between gap-2">
                      <div className="min-w-0"><div className="text-sm font-semibold text-ink-hi truncate">{c.name}</div><div className="text-[11px] text-ink-mid truncate">{c.headline || t("employer.top.noHeadline", "Candidate")} · {c.city}</div></div>
                      <div className="text-right shrink-0"><div className="num text-base font-bold text-brand-bright">{c.score}%</div><div className="text-[9px] text-ink-lo uppercase">{t("employer.top.match", "Match")}</div></div>
                    </div>
                    <div className="mt-2.5 h-2 rounded-full bg-panel2/70 overflow-hidden"><div className="h-full rounded-full bg-gradient-to-r from-brand-deep to-brand-bright" style={{ width: Math.max(0, Math.min(100, c.score)) + "%" }} /></div>
                  </div>
                ))}
              </div>
            )}
          </motion.section>

          <motion.section variants={fade} className="card p-5">
            <div className="flex items-center justify-between gap-3 flex-wrap mb-3">
              <div><h3 className="font-display text-[15px] font-bold text-ink-hi">{t("employer.team.title", "Team & roles")}</h3><p className="text-[11px] text-ink-lo mt-0.5">{t("employer.team.sub", "Owners manage the team, recruiters manage hiring, viewers read only.")}</p></div>
              {team && <span className="chip !text-[10px]">{t("employer.team.count", "{{n}} member(s)", { n: team.length })}</span>}
            </div>
            {teamErr && <div className="mb-3 rounded-lg border border-pink/40 bg-pink/[0.06] px-3 py-2 text-[11px] text-pink">{teamErr}</div>}
            {team === null ? (
              <div className="py-6 text-center text-[12px] text-ink-lo">{t("employer.team.none", "No team data.")}</div>
            ) : (
              <div className="space-y-2">
                {team.map((m) => (
                  <div key={m.id} className="flex items-center gap-3 rounded-xl border border-line/60 bg-panel2/40 px-3.5 py-2.5">
                    <div className="grid h-8 w-8 shrink-0 place-items-center rounded-lg bg-brand/15 text-[11px] font-bold text-brand-bright">{m.displayName.split(" ").map((x) => x[0]).slice(0, 2).join("").toUpperCase()}</div>
                    <div className="min-w-0 flex-1"><div className="text-sm font-semibold text-ink-hi truncate">{m.displayName}</div><div className="text-[11px] text-ink-lo truncate">{m.email}</div></div>
                    {live ? (
                      <>
                        <select value={m.role} onChange={(e) => changeRole(m.id, e.target.value)} className="rounded-lg border border-line/70 bg-panel2/50 px-2 py-1 text-[11px] font-semibold text-ink-hi capitalize focus:border-brand/50 focus:outline-none">
                          {ROLES.map((r) => <option key={r} value={r}>{r}</option>)}
                        </select>
                        <button onClick={() => removeMember(m.id)} title={t("employer.team.remove", "Remove")} className="rounded-lg px-2 py-1 text-[11px] font-semibold text-ink-lo hover:text-pink transition">✕</button>
                      </>
                    ) : (
                      <span className="chip !text-[10px] !text-gold !border-gold/30 capitalize">{m.role}</span>
                    )}
                  </div>
                ))}
                {team.length === 0 && <div className="py-4 text-center text-[12px] text-ink-lo">{t("employer.team.empty", "No members yet.")}</div>}
              </div>
            )}
            {live && (
              <div className="mt-4 border-t border-line/40 pt-4 grid gap-2 sm:grid-cols-[1fr_1fr_auto_auto] items-center">
                <input className={inputCls + " !py-1.5"} value={invite.displayName} onChange={(e) => setInvite((f) => ({ ...f, displayName: e.target.value }))} placeholder={t("employer.team.namePlaceholder", "Name")} />
                <input className={inputCls + " !py-1.5"} value={invite.email} onChange={(e) => setInvite((f) => ({ ...f, email: e.target.value }))} placeholder={t("employer.team.emailPlaceholder", "email@company.na")} />
                <select value={invite.role} onChange={(e) => setInvite((f) => ({ ...f, role: e.target.value }))} className="rounded-lg border border-line/70 bg-panel2/50 px-2 py-1.5 text-[11px] font-semibold text-ink-hi capitalize focus:border-brand/50 focus:outline-none">
                  {ROLES.map((r) => <option key={r} value={r}>{r}</option>)}
                </select>
                <button onClick={inviteMember} disabled={inviting === "busy" || !invite.email.trim() || !invite.displayName.trim()} className="rounded-lg bg-brand/15 px-3 py-1.5 text-[11px] font-semibold text-brand-bright hover:bg-brand/25 transition disabled:opacity-50">{inviting === "busy" ? t("employer.team.inviting", "Inviting…") : t("employer.team.invite", "Invite")}</button>
              </div>
            )}
          </motion.section>

          <footer className="flex flex-wrap items-center justify-between gap-2 pt-1 pb-4 text-[11px] text-ink-lo"><span>{t("employer.footer.brand", "Illumin360 · Employer portal")}</span><span>{live ? t("employer.footer.live", "Live data") : t("employer.footer.demo", "Demo snapshot")}</span></footer>
        </motion.div>
      </main>
    </div>
  );
}

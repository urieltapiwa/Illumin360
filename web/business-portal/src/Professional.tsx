import { useEffect, useState } from "react";
import { motion } from "framer-motion";
import * as echarts from "echarts";
import { Chart, sparkOption, nf, C } from "@illumin360/ui";
import { logout, type Session } from "./auth";
import { useTranslation } from "react-i18next";
import { LanguageSwitcher, ThemeSwitcher } from "@illumin360/ui";

interface Match { role: string; company: string; city: string; industry: string; match: number; salaryLo: number; salaryHi: number; posted: string; type: string; id?: string; status?: string; }
interface Prof {
  id?: string;
  persona: { name: string; role: string; city: string; nationality: string; availability: string; headline: string; profileStrength: number; percentile: number; memberSince: string };
  kpis: { profileViews: number; viewsDelta: number; matchOpportunities: number; matchDelta: number; activeApplications: number; responseRate: number; avgMatch: number; interviews: number };
  viewsTrend: number[];
  matches: Match[];
  pipeline: { stage: string; value: number }[];
  skillDemand: { role: string; value: number }[];
  skills: { name: string; level: number; trend: string }[];
  salary: { role: string; p25: number; median: number; p75: number; you: number };
  activity: { icon: string; text: string; when: string }[];
}

const Ic = ({ d, s = 18, w = 1.7 }: { d: React.ReactNode; s?: number; w?: number }) => (
  <svg width={s} height={s} viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth={w} strokeLinecap="round" strokeLinejoin="round">{d}</svg>
);
const ICN = {
  user: <path d="M16 11a4 4 0 1 0-4-4 4 4 0 0 0 4 4zM2 21a7 7 0 0 1 14 0M19 21a5 5 0 0 0-6-4.9" />,
  spark2: <path d="M5 3v4M3 5h4M6 17v4M4 19h4M13 3l2.5 6.5L22 12l-6.5 2.5L13 21l-2.5-6.5L4 12l6.5-2.5z" />,
  brief: <path d="M3 8h18v12H3zM8 8V6a2 2 0 0 1 2-2h4a2 2 0 0 1 2 2v2M3 13h18" />,
  chart: <path d="M4 20V10M10 20V4M16 20v-7M22 20H2" />,
  gear: <path d="M12 15a3 3 0 1 0 0-6 3 3 0 0 0 0 6zM19.4 13a7.5 7.5 0 0 0 0-2l2-1.5-2-3.5-2.4 1a7 7 0 0 0-1.7-1L14.5 2.5h-5L9.2 5a7 7 0 0 0-1.7 1l-2.4-1-2 3.5L5.1 11a7.5 7.5 0 0 0 0 2l-2 1.5 2 3.5 2.4-1a7 7 0 0 0 1.7 1l.3 2.5h5l.3-2.5a7 7 0 0 0 1.7-1l2.4 1 2-3.5z" />,
  out: <path d="M16 17l5-5-5-5M21 12H9M9 21H5a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h4" />,
  loc: <path d="M12 21s7-5.2 7-11a7 7 0 1 0-14 0c0 5.8 7 11 7 11zM12 12a2.5 2.5 0 1 0 0-5 2.5 2.5 0 0 0 0 5z" />,
  eye: <path d="M2 12s4-7 10-7 10 7 10 7-4 7-10 7S2 12 2 12zM12 15a3 3 0 1 0 0-6 3 3 0 0 0 0 6z" />,
};

const kK = (n: number) => "N$" + Math.round(n / 1000) + "k";
const fade = { initial: { opacity: 0, y: 14 }, animate: { opacity: 1, y: 0 } };

function Logo() {
  return (
    <div className="flex items-center gap-2.5">
      <svg width="30" height="30" viewBox="0 0 32 32" fill="none">
        <circle cx="16" cy="16" r="14" stroke="#1FB283" strokeWidth="1.6" />
        <circle cx="16" cy="16" r="8.5" stroke="#2FD39A" strokeWidth="1.6" />
        <circle cx="16" cy="16" r="3" fill="#E8B14C" />
      </svg>
      <div className="leading-none">
        <div className="font-display text-[17px] font-extrabold tracking-tight text-ink-hi">Illumin<span className="text-brand-bright">360</span></div>
        <div className="text-[9px] uppercase tracking-[0.22em] text-ink-lo mt-0.5">Professional</div>
      </div>
    </div>
  );
}

function gaugeOption(value: number): echarts.EChartsOption {
  return {
    series: [{
      type: "gauge", startAngle: 210, endAngle: -30, min: 0, max: 100, radius: "96%", center: ["50%", "56%"],
      progress: { show: true, width: 11, roundCap: true, itemStyle: { color: "#2FD39A" } },
      axisLine: { lineStyle: { width: 11, color: [[1, "#15301F"]] } },
      pointer: { show: false }, axisTick: { show: false }, splitLine: { show: false }, axisLabel: { show: false }, anchor: { show: false },
      title: { show: false },
      detail: { valueAnimation: true, offsetCenter: [0, "0%"], formatter: "{v|{value}%}", rich: { v: { color: "#E8F2EC", fontSize: 30, fontFamily: "JetBrains Mono", fontWeight: 700 } } },
      data: [{ value }],
    }],
  };
}
function demandOption(rows: { role: string; value: number }[]): echarts.EChartsOption {
  const s = [...rows].sort((a, b) => a.value - b.value);
  return {
    grid: { left: 96, right: 30, top: 6, bottom: 6 },
    tooltip: { trigger: "item", backgroundColor: "#0B1A14", borderColor: C.line, textStyle: { color: "#E8F2EC" }, valueFormatter: (v) => nf(v as number) + " open roles" },
    xAxis: { type: "value", axisLine: { show: false }, axisTick: { show: false }, axisLabel: { show: false }, splitLine: { show: false } },
    yAxis: { type: "category", data: s.map((r) => r.role), axisLine: { lineStyle: { color: C.line } }, axisTick: { show: false }, axisLabel: { color: C.textHi, fontFamily: "Hanken Grotesk", fontSize: 11 } },
    series: [{ type: "bar", data: s.map((r) => r.value), barWidth: "58%", itemStyle: { color: "#1FB283", borderRadius: [0, 5, 5, 0] }, label: { show: true, position: "right", color: C.textHi, fontFamily: "JetBrains Mono", fontSize: 10, formatter: (p) => nf((p as { value: number }).value) } }],
  };
}

const trendColor: Record<string, string> = { hot: "text-gold", rising: "text-brand-bright", steady: "text-ink-mid" };

interface OpenRole { id: string; title: string; city: string; positions: number; createdAt: string; }

export default function Professional(_props: { session: Session }) {
  const [d, setD] = useState<Prof | null>(null);
  const [live, setLive] = useState(false);
  const [openRoles, setOpenRoles] = useState<OpenRole[] | null>(null);
  const [matchFilter, setMatchFilter] = useState<"all" | "saved" | "applied">("all");
  // Open-roles the professional has applied to this session (marketplace panel is otherwise stateless).
  const [appliedRoles, setAppliedRoles] = useState<Record<string, "pending" | "done" | "error">>({});
  // P2: live open roles from the Recruitment marketplace (real recruitment_requests).
  useEffect(() => {
    fetch("/api/recruitment/requests?status=open&pageSize=6")
      .then((r) => (r.ok ? r.json() : null))
      .then((v) => { if (Array.isArray(v)) setOpenRoles(v); })
      .catch(() => { /* marketplace unavailable — panel simply hidden */ });
  }, []);
  // Live-first: read the professional's dashboard from the Professionals service (via the BFF/gateway);
  // fall back to the bundled snapshot if the API is unavailable. Mirrors the other portals' live-data pattern.
  useEffect(() => {
    let cancelled = false;
    (async () => {
      try {
        const r = await fetch("/api/professionals/me");
        if (r.ok) {
          const j = await r.json();
          if (!cancelled) { setD(j); setLive(true); }
          return;
        }
      } catch { /* fall through to the bundled snapshot */ }
      const snap = await fetch(import.meta.env.BASE_URL + "professional.json").then((x) => x.json());
      if (!cancelled) { setD(snap); setLive(false); }
    })();
    return () => { cancelled = true; };
  }, []);
  const { t } = useTranslation();
  if (!d) return <div className="grid place-items-center h-screen text-ink-mid font-mono text-sm animate-pulse">{t("pro.loading")}</div>;

  const p = d.persona; const k = d.kpis;
  // Self-service actions (only meaningful when logged in / live; snapshot matches have no id).
  const act = async (matchId: string | undefined, action: "save" | "dismiss" | "apply") => {
    if (!matchId) return;
    const r = await fetch(`/api/professionals/me/matches/${matchId}/${action}`, { method: "POST", credentials: "same-origin" });
    if (r.ok) {
      const updated = await r.json().catch(() => null);
      setD((prev) => (prev ? { ...prev, matches: prev.matches.map((m) => (m.id === matchId ? { ...m, status: updated?.status ?? action + "d" } : m)) } : prev));
    }
  };
  const toggleAvailability = async () => {
    const next = /open/i.test(p.availability) ? "Not looking" : "Open to opportunities";
    const r = await fetch(`/api/professionals/me/availability`, { method: "POST", headers: { "Content-Type": "application/json" }, credentials: "same-origin", body: JSON.stringify({ availability: next }) });
    if (r.ok) { const v = await r.json().catch(() => next); setD((prev) => (prev ? { ...prev, persona: { ...prev.persona, availability: typeof v === "string" ? v : next } } : prev)); }
  };
  // Apply to a live marketplace open role — records a real application in the Recruitment service.
  const applyToRole = async (roleId: string) => {
    if (!d.id || appliedRoles[roleId]) return;
    setAppliedRoles((prev) => ({ ...prev, [roleId]: "pending" }));
    try {
      const r = await fetch(`/api/recruitment/requests/${roleId}/apply`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        credentials: "same-origin",
        body: JSON.stringify({ talentId: d.id, talentType: "professional" }),
      });
      // 409 (already applied) is a benign "already done" from our point of view.
      setAppliedRoles((prev) => ({ ...prev, [roleId]: r.ok || r.status === 409 ? "done" : "error" }));
    } catch {
      setAppliedRoles((prev) => ({ ...prev, [roleId]: "error" }));
    }
  };
  const navItems: [React.ReactNode, string, string][] = [[ICN.user, "pro.nav.profile", "pro-top"], [ICN.spark2, "pro.nav.matches", "pro-matches"], [ICN.brief, "pro.nav.applications", "pro-applications"], [ICN.chart, "pro.nav.insights", "pro-insights"], [ICN.gear, "pro.nav.settings", "pro-top"]];
  const goto = (id: string) => document.getElementById(id)?.scrollIntoView({ behavior: "smooth", block: "start" });
  const salaryPos = (v: number) => Math.max(2, Math.min(98, ((v - d.salary.p25) / (d.salary.p75 - d.salary.p25)) * 100));
  const initials = p.name.split(" ").map((x) => x[0]).slice(0, 2).join("");

  return (
    <div className="flex min-h-screen">
      <aside className="hidden lg:flex w-[228px] shrink-0 flex-col border-r border-line/70 bg-panel/40 px-4 py-6 relative z-10">
        <div className="px-1"><Logo /></div>
        <nav className="mt-9 flex flex-col gap-1">
          <div className="eyebrow px-3 mb-1">{t("pro.nav.eyebrow")}</div>
          {navItems.map(([icon, label, target], idx) => (
            <button key={label} onClick={() => goto(target)} className={`group flex items-center gap-3 rounded-xl px-3 py-2.5 text-sm text-left transition ${idx === 0 ? "bg-brand/[0.12] text-ink-hi shadow-[inset_0_0_0_1px_rgba(47,211,154,0.25)]" : "text-ink-mid hover:bg-white/[0.03] hover:text-ink-hi"}`}>
              <span className={idx === 0 ? "text-brand-bright" : "text-ink-lo group-hover:text-ink-mid"}><Ic d={icon} /></span>{t(label)}
            </button>
          ))}
        </nav>
        <div className="mt-auto card p-3.5">
          <div className="text-xs font-semibold text-ink-hi">{t("pro.boost.title")}</div>
          <p className="mt-1.5 text-[11px] leading-snug text-ink-mid">{t("pro.boost.body")}</p>
          <button className="mt-2.5 w-full rounded-lg bg-brand/[0.15] py-1.5 text-[11px] font-semibold text-brand-bright hover:bg-brand/25 transition">{t("pro.boost.cta")}</button>
        </div>
      </aside>

      <main className="flex-1 min-w-0 relative z-10">
        <header className="sticky top-0 z-20 flex items-center gap-4 border-b border-line/60 bg-base/70 backdrop-blur-xl px-5 lg:px-7 py-4">
          <div className="min-w-0">
            <div className="flex items-center gap-2">
              <h1 className="font-display text-xl font-extrabold text-ink-hi tracking-tight">{t("pro.header.title")}</h1>
              {live ? <span className="chip !text-[10px] !text-brand-bright !border-brand/30"><span className="h-1.5 w-1.5 rounded-full bg-brand-bright animate-pulse" /> LIVE</span> : <span className="chip !text-[10px] !text-gold !border-gold/30">{t("pro.header.demo")}</span>}
            </div>
            <p className="text-[11px] text-ink-lo mt-0.5">{t("pro.header.subtitle")}</p>
          </div>
          <div className="ml-auto flex items-center gap-3">
            <LanguageSwitcher />
            <ThemeSwitcher />
            {live ? (
              <button onClick={toggleAvailability} title="Toggle availability" className={`chip !text-[11px] hidden md:inline-flex transition ${/open/i.test(p.availability) ? "!text-brand-bright !border-brand/30" : "!text-ink-mid !border-line/70"}`}><span className={`h-1.5 w-1.5 rounded-full ${/open/i.test(p.availability) ? "bg-brand-bright" : "bg-ink-lo"}`} />{p.availability}</button>
            ) : (
              <span className="chip !text-[11px] !text-brand-bright !border-brand/30 hidden md:inline-flex"><span className="h-1.5 w-1.5 rounded-full bg-brand-bright" />{p.availability}</span>
            )}
            <div className="hidden md:flex items-center gap-2.5 rounded-xl border border-line/70 bg-panel2/50 pl-2.5 pr-2 py-1.5">
              <div className="grid h-7 w-7 place-items-center rounded-lg bg-brand/20 text-[11px] font-bold text-brand-bright">{initials}</div>
              <div className="leading-tight"><div className="text-xs font-semibold text-ink-hi">{p.name}</div><div className="text-[10px] text-ink-lo">{p.role}</div></div>
              <button onClick={logout} title={t("pro.header.signOut")} className="ml-1 text-ink-lo hover:text-pink transition"><Ic d={ICN.out} s={15} /></button>
            </div>
          </div>
        </header>

        <motion.div initial="initial" animate="animate" transition={{ staggerChildren: 0.06 }} className="px-5 lg:px-7 py-6 space-y-5">
          {/* hero: profile strength + identity + KPIs */}
          <div id="pro-top" className="grid grid-cols-1 xl:grid-cols-3 gap-5 scroll-mt-24">
            <motion.section variants={fade} className="card p-5 flex items-center gap-4">
              <div className="w-[150px] shrink-0"><Chart option={gaugeOption(p.profileStrength)} height={150} /></div>
              <div>
                <div className="eyebrow">{t("pro.strength.eyebrow")}</div>
                <div className="font-display text-2xl font-extrabold text-ink-hi mt-1">{t("pro.strength.topPercentile", { pct: p.percentile })}</div>
                <p className="text-xs text-ink-mid mt-1">{t("pro.strength.ofRole", { role: p.role, headline: p.headline })}</p>
                <div className="flex items-center gap-1.5 text-[11px] text-ink-lo mt-2"><span className="text-brand-bright"><Ic d={ICN.loc} s={13} /></span>{p.city} · {t("pro.strength.memberSince", { date: p.memberSince })}</div>
              </div>
            </motion.section>
            <motion.section variants={fade} className="card p-5 xl:col-span-2">
              <div className="grid grid-cols-2 sm:grid-cols-4 gap-4 h-full">
                {[
                  [t("pro.kpi.profileViews"), nf(k.profileViews), `+${k.viewsDelta}%`, true, d.viewsTrend],
                  [t("pro.kpi.matchOpportunities"), String(k.matchOpportunities), `+${k.matchDelta}%`, true, undefined],
                  [t("pro.kpi.activeApplications"), String(k.activeApplications), t("pro.kpi.interviews", { n: k.interviews }), true, undefined],
                  [t("pro.kpi.avgMatchScore"), k.avgMatch + "%", t("pro.kpi.response", { pct: k.responseRate }), true, undefined],
                ].map(([label, val, sub, up, spark], i) => (
                  <div key={i} className="flex flex-col justify-between">
                    <span className="eyebrow">{label as string}</span>
                    <div className="num text-[26px] font-bold text-ink-hi leading-none mt-1.5">{val as string}</div>
                    {spark ? <div className="h-8 -mx-1 mt-1"><Chart option={sparkOption(spark as number[], C.brand)} height={32} /></div>
                      : <span className={`mt-2 text-[11px] ${up ? "text-brand-bright" : "text-pink"}`}>{sub as string}</span>}
                  </div>
                ))}
              </div>
            </motion.section>
          </div>

          {/* matches + pipeline */}
          <div id="pro-matches" className="grid grid-cols-1 xl:grid-cols-3 gap-5 scroll-mt-24">
            <motion.section variants={fade} className="card p-5 xl:col-span-2">
              <div className="flex items-center justify-between mb-3">
                <div><h3 className="font-display text-[15px] font-bold text-ink-hi">{t("pro.matches.title")}</h3><p className="text-[11px] text-ink-lo mt-0.5">{t("pro.matches.subtitle")}</p></div>
                {live ? (
                  <div className="flex items-center gap-0.5 rounded-lg bg-panel2/50 p-0.5">
                    {(["all", "saved", "applied"] as const).map((f) => (
                      <button key={f} onClick={() => setMatchFilter(f)} className={`rounded-md px-2.5 py-1 text-[11px] font-semibold capitalize transition ${matchFilter === f ? "bg-brand/20 text-brand-bright" : "text-ink-lo hover:text-ink-hi"}`}>{f}</button>
                    ))}
                  </div>
                ) : (
                  <span className="chip !text-[10px]">{t("pro.matches.new", { n: d.matches.length })}</span>
                )}
              </div>
              {(() => {
                const visible = d.matches.filter((m) => m.status !== "dismissed").filter((m) => matchFilter === "all" || m.status === matchFilter);
                if (visible.length === 0) {
                  return <div className="py-8 text-center text-[12px] text-ink-lo">No {matchFilter === "all" ? "" : matchFilter + " "}matches right now.</div>;
                }
                return (
              <div className="grid sm:grid-cols-2 gap-3">
                {visible.map((m, i) => (
                  <div key={m.id ?? i} className={`rounded-xl border p-3.5 transition group ${m.status === "applied" ? "border-brand/50 bg-brand/[0.06]" : m.status === "saved" ? "border-gold/40 bg-panel2/40" : "border-line/60 bg-panel2/40 hover:border-brand/40"}`}>
                    <div className="flex items-start justify-between gap-2">
                      <div className="min-w-0">
                        <div className="text-sm font-semibold text-ink-hi truncate">{m.role}</div>
                        <div className="text-[11px] text-ink-mid truncate">{m.company} · {m.city}</div>
                      </div>
                      <div className="text-right shrink-0">
                        <div className="num text-base font-bold text-brand-bright">{m.match}%</div>
                        <div className="text-[9px] text-ink-lo uppercase tracking-wide">{t("pro.matches.match")}</div>
                      </div>
                    </div>
                    <div className="mt-2.5 flex items-center justify-between">
                      <span className="text-[11px] text-gold num">{kK(m.salaryLo)}–{kK(m.salaryHi)}</span>
                      <span className="text-[10px] text-ink-lo">{m.type} · {m.posted}</span>
                    </div>
                    {live && m.id && (
                      <div className="mt-2.5 flex items-center gap-1.5 border-t border-line/40 pt-2.5">
                        {m.status === "applied" ? (
                          <span className="chip !text-[10px] !text-brand-bright !border-brand/30">✓ Applied</span>
                        ) : (
                          <>
                            <button onClick={() => act(m.id, "apply")} className="rounded-lg bg-brand/15 px-2.5 py-1 text-[11px] font-semibold text-brand-bright hover:bg-brand/25 transition">Apply</button>
                            <button onClick={() => act(m.id, "save")} className={`rounded-lg px-2.5 py-1 text-[11px] font-semibold transition ${m.status === "saved" ? "bg-gold/20 text-gold" : "bg-panel2/70 text-ink-mid hover:text-ink-hi"}`}>{m.status === "saved" ? "Saved" : "Save"}</button>
                            <button onClick={() => act(m.id, "dismiss")} className="ml-auto rounded-lg px-2.5 py-1 text-[11px] font-semibold text-ink-lo hover:text-pink transition">Dismiss</button>
                          </>
                        )}
                      </div>
                    )}
                  </div>
                ))}
              </div>
                );
              })()}
            </motion.section>

            <motion.section variants={fade} id="pro-applications" className="card p-5 scroll-mt-24">
              <h3 className="font-display text-[15px] font-bold text-ink-hi">{t("pro.pipeline.title")}</h3>
              <p className="text-[11px] text-ink-lo mt-0.5 mb-4">{t("pro.pipeline.subtitle")}</p>
              <div className="space-y-2.5">
                {d.pipeline.map((s, i) => {
                  const pctw = (s.value / d.pipeline[0].value) * 100;
                  const cols = ["#1FB283", "#2FD39A", "#46C39A", "#E8B14C", "#E8B14C"];
                  return (
                    <div key={i}>
                      <div className="flex items-center justify-between text-xs mb-1"><span className="text-ink-mid">{s.stage}</span><span className="num text-ink-hi">{s.value}</span></div>
                      <div className="h-2 rounded-full bg-panel2/70 overflow-hidden"><div className="h-full rounded-full" style={{ width: pctw + "%", background: cols[i] }} /></div>
                    </div>
                  );
                })}
              </div>
              <div className="mt-4 rounded-xl bg-brand/[0.08] border border-brand/20 p-3 text-[11px] text-ink-mid">
                <span className="text-brand-bright font-semibold">{t("pro.pipeline.offerLead")}</span>{t("pro.pipeline.offerBody")}
              </div>
            </motion.section>
          </div>

          {/* P2: live open roles from the Recruitment marketplace */}
          {openRoles && openRoles.length > 0 && (
            <div className="grid grid-cols-1 gap-5">
              <motion.section variants={fade} className="card p-5">
                <div className="flex items-center justify-between mb-3">
                  <div><h3 className="font-display text-[15px] font-bold text-ink-hi">Open roles · marketplace</h3><p className="text-[11px] text-ink-lo mt-0.5">Live openings posted across Illumin360 right now.</p></div>
                  <span className="chip !text-[10px] !text-brand-bright !border-brand/30"><span className="h-1.5 w-1.5 rounded-full bg-brand-bright animate-pulse" /> LIVE · {openRoles.length}</span>
                </div>
                <div className="grid sm:grid-cols-2 lg:grid-cols-3 gap-3">
                  {openRoles.map((r) => {
                    const state = appliedRoles[r.id];
                    return (
                    <div key={r.id} className={`rounded-xl border p-3.5 transition ${state === "done" ? "border-brand/50 bg-brand/[0.06]" : "border-line/60 bg-panel2/40 hover:border-brand/40"}`}>
                      <div className="text-sm font-semibold text-ink-hi truncate">{r.title}</div>
                      <div className="text-[11px] text-ink-mid truncate">{r.city}</div>
                      <div className="mt-2 flex items-center justify-between text-[10px] text-ink-lo"><span>{r.positions} position{r.positions === 1 ? "" : "s"}</span><span className="text-brand-bright">Open</span></div>
                      {live && (
                        <div className="mt-2.5 border-t border-line/40 pt-2.5">
                          {state === "done" ? (
                            <span className="chip !text-[10px] !text-brand-bright !border-brand/30">✓ Applied</span>
                          ) : (
                            <button onClick={() => applyToRole(r.id)} disabled={state === "pending"} className="rounded-lg bg-brand/15 px-2.5 py-1 text-[11px] font-semibold text-brand-bright hover:bg-brand/25 transition disabled:opacity-50">{state === "pending" ? "Applying…" : state === "error" ? "Retry" : "Apply"}</button>
                          )}
                        </div>
                      )}
                    </div>
                    );
                  })}
                </div>
              </motion.section>
            </div>
          )}

          {/* market insights: demand + skills + salary + activity */}
          <div id="pro-insights" className="grid grid-cols-1 xl:grid-cols-3 gap-5 scroll-mt-24">
            <motion.section variants={fade} className="card p-5">
              <h3 className="font-display text-[15px] font-bold text-ink-hi">{t("pro.demand.title")}</h3>
              <p className="text-[11px] text-ink-lo mt-0.5">{t("pro.demand.subtitle")}</p>
              <div className="mt-2"><Chart option={demandOption(d.skillDemand)} height={210} /></div>
            </motion.section>

            <motion.section variants={fade} className="card p-5">
              <h3 className="font-display text-[15px] font-bold text-ink-hi">{t("pro.skills.title")}</h3>
              <p className="text-[11px] text-ink-lo mt-0.5 mb-3">{t("pro.skills.subtitle")}</p>
              <div className="space-y-3">
                {d.skills.map((s, i) => (
                  <div key={i}>
                    <div className="flex items-center justify-between text-xs mb-1">
                      <span className="text-ink-hi font-medium">{s.name}</span>
                      <span className={`text-[10px] uppercase tracking-wide ${trendColor[s.trend]}`}>{s.trend}</span>
                    </div>
                    <div className="h-2 rounded-full bg-panel2/70 overflow-hidden"><div className="h-full rounded-full bg-gradient-to-r from-brand-deep to-brand-bright" style={{ width: s.level + "%" }} /></div>
                  </div>
                ))}
              </div>
            </motion.section>

            <motion.section variants={fade} className="card p-5">
              <h3 className="font-display text-[15px] font-bold text-ink-hi">{t("pro.salary.title")}</h3>
              <p className="text-[11px] text-ink-lo mt-0.5">{t("pro.salary.subtitle", { role: d.salary.role })}</p>
              <div className="mt-7 mb-2 relative h-2.5 rounded-full bg-panel2/70">
                <div className="absolute inset-y-0 rounded-full bg-gradient-to-r from-brand-deep/60 to-brand/60" style={{ left: "0%", right: "0%" }} />
                <div className="absolute -top-6 -translate-x-1/2 text-[10px] num text-ink-mid" style={{ left: salaryPos(d.salary.median) + "%" }}>{kK(d.salary.median)}</div>
                <div className="absolute top-1/2 -translate-y-1/2 h-4 w-[3px] bg-ink-mid rounded" style={{ left: salaryPos(d.salary.median) + "%" }} />
                <div className="absolute -bottom-7 -translate-x-1/2 flex flex-col items-center" style={{ left: salaryPos(d.salary.you) + "%" }}>
                  <div className="h-5 w-5 rounded-full bg-gold border-2 border-base shadow-glow" />
                  <span className="text-[10px] num text-gold mt-0.5 whitespace-nowrap">{t("pro.salary.you", { value: kK(d.salary.you) })}</span>
                </div>
              </div>
              <div className="flex justify-between text-[10px] text-ink-lo num mt-9"><span>{kK(d.salary.p25)} · p25</span><span>p75 · {kK(d.salary.p75)}</span></div>
            </motion.section>
          </div>

          {/* activity */}
          <motion.section variants={fade} className="card p-5">
            <h3 className="font-display text-[15px] font-bold text-ink-hi mb-3">{t("pro.activity.title")}</h3>
            <div className="grid sm:grid-cols-2 gap-x-8 gap-y-2.5">
              {d.activity.map((a, i) => (
                <div key={i} className="flex items-center gap-3 text-sm">
                  <span className="grid h-7 w-7 shrink-0 place-items-center rounded-lg bg-brand/[0.12] text-brand-bright"><Ic d={ICN.eye} s={14} /></span>
                  <span className="text-ink-mid flex-1">{a.text}</span>
                  <span className="text-[11px] text-ink-lo num whitespace-nowrap">{a.when}</span>
                </div>
              ))}
            </div>
          </motion.section>

          <footer className="flex flex-wrap items-center justify-between gap-2 pt-1 pb-4 text-[11px] text-ink-lo">
            <span>Illumin360 · {t("pro.footer.portal")}</span><span>{t("pro.footer.disclaimer")}</span>
          </footer>
        </motion.div>
      </main>
    </div>
  );
}

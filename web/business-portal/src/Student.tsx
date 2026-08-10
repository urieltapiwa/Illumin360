import { useEffect, useState } from "react";
import { motion } from "framer-motion";
import * as echarts from "echarts";
import { useTranslation } from "react-i18next";
import { Chart, sparkOption, C } from "@illumin360/ui";
import { logout, type Session } from "./auth";
import { LanguageSwitcher, ThemeSwitcher } from "@illumin360/ui";

interface StudentData {
  persona: { name: string; field: string; school: string; year: string; graduating: string; readiness: number; program: string; city: string; availability?: string };
  kpis: { profileViews: number; viewsDelta: number; internshipMatches: number; applications: number; skillsDone: number; mentorSessions: number; readiness: number };
  viewsTrend: number[];
  matches: { role: string; company: string; city: string; match: number; stipendLo: number; stipendHi: number; type: string; posted: string; id?: string; status?: string }[];
  learning: { name: string; progress: number; tag: string }[];
  pipeline: { stage: string; value: number }[];
  skills: { name: string; level: number }[];
  activity: { text: string; when: string }[];
}
const Ic = ({ d, s = 18, w = 1.7 }: { d: React.ReactNode; s?: number; w?: number }) => (
  <svg width={s} height={s} viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth={w} strokeLinecap="round" strokeLinejoin="round">{d}</svg>
);
const N = {
  path: <path d="M3 12h4l3 8 4-16 3 8h4" />, cap: <path d="M22 10 12 5 2 10l10 5 10-5zM6 12v5c0 1 2.7 3 6 3s6-2 6-3v-5" />,
  book: <path d="M4 19V5a1 1 0 0 1 1-1h13v15H6a2 2 0 0 0-2 2zM18 4v17" />, chat: <path d="M21 15a2 2 0 0 1-2 2H7l-4 4V5a2 2 0 0 1 2-2h14a2 2 0 0 1 2 2z" />,
  gear: <path d="M12 15a3 3 0 1 0 0-6 3 3 0 0 0 0 6zM19.4 13a7.5 7.5 0 0 0 0-2l2-1.5-2-3.5-2.4 1a7 7 0 0 0-1.7-1L14.5 2.5h-5L9.2 5a7 7 0 0 0-1.7 1l-2.4-1-2 3.5L5.1 11a7.5 7.5 0 0 0 0 2l-2 1.5 2 3.5 2.4-1a7 7 0 0 0 1.7 1l.3 2.5h5l.3-2.5a7 7 0 0 0 1.7-1l2.4 1 2-3.5z" />,
  out: <path d="M16 17l5-5-5-5M21 12H9M9 21H5a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h4" />, eye: <path d="M2 12s4-7 10-7 10 7 10 7-4 7-10 7S2 12 2 12zM12 15a3 3 0 1 0 0-6 3 3 0 0 0 0 6z" />,
};
const fade = { initial: { opacity: 0, y: 14 }, animate: { opacity: 1, y: 0 } };
const kK = (n: number) => "N$" + Math.round(n / 1000) + "k";
function Logo() {
  return (
    <div className="flex items-center gap-2.5">
      <svg width="30" height="30" viewBox="0 0 32 32" fill="none"><circle cx="16" cy="16" r="14" stroke="#1FB283" strokeWidth="1.6" /><circle cx="16" cy="16" r="8.5" stroke="#2FD39A" strokeWidth="1.6" /><circle cx="16" cy="16" r="3" fill="#E8B14C" /></svg>
      <div className="leading-none"><div className="font-display text-[17px] font-extrabold tracking-tight text-ink-hi">Illumin<span className="text-brand-bright">360</span></div><div className="text-[9px] uppercase tracking-[0.22em] text-ink-lo mt-0.5">Student</div></div>
    </div>
  );
}
function gauge(value: number, color = "#2FD39A"): echarts.EChartsOption {
  return { series: [{ type: "gauge", startAngle: 210, endAngle: -30, min: 0, max: 100, radius: "96%", center: ["50%", "56%"], progress: { show: true, width: 11, roundCap: true, itemStyle: { color } }, axisLine: { lineStyle: { width: 11, color: [[1, "#15301F"]] } }, pointer: { show: false }, axisTick: { show: false }, splitLine: { show: false }, axisLabel: { show: false }, anchor: { show: false }, detail: { offsetCenter: [0, "0%"], formatter: "{v|{value}%}", rich: { v: { color: "#E8F2EC", fontSize: 28, fontFamily: "JetBrains Mono", fontWeight: 700 } } }, data: [{ value }] }] };
}
const tagColor: Record<string, string> = { done: "text-brand-bright", "in progress": "text-gold" };

export default function Student(_props: { session: Session }) {
  const [d, setD] = useState<StudentData | null>(null);
  const [live, setLive] = useState(false);
  const [matchFilter, setMatchFilter] = useState<"all" | "saved" | "applied">("all");
  const { t } = useTranslation();
  // Live-first: read the student's dashboard from the Students service (via the BFF/gateway); fall back to
  // the bundled snapshot if the API is unavailable. Mirrors the Business dashboard's live-data pattern.
  useEffect(() => {
    let cancelled = false;
    (async () => {
      try {
        const r = await fetch("/api/students/me");
        if (r.ok) {
          const j = await r.json();
          if (!cancelled) { setD(j); setLive(true); }
          return;
        }
      } catch { /* fall through to the bundled snapshot */ }
      const snap = await fetch(import.meta.env.BASE_URL + "student.json").then((x) => x.json());
      if (!cancelled) { setD(snap); setLive(false); }
    })();
    return () => { cancelled = true; };
  }, []);
  if (!d) return <div className="grid place-items-center h-screen text-ink-mid font-mono text-sm animate-pulse">{t("student.loading")}</div>;
  const p = d.persona, k = d.kpis;
  const availability = p.availability ?? "Open to internships";
  // Self-service actions (only meaningful when logged in / live; snapshot matches have no id).
  const act = async (matchId: string | undefined, action: "save" | "dismiss" | "apply") => {
    if (!matchId) return;
    const r = await fetch(`/api/students/me/matches/${matchId}/${action}`, { method: "POST", credentials: "same-origin" });
    if (r.ok) {
      const updated = await r.json().catch(() => null);
      setD((prev) => (prev ? { ...prev, matches: prev.matches.map((m) => (m.id === matchId ? { ...m, status: updated?.status ?? action + "d" } : m)) } : prev));
    }
  };
  const toggleAvailability = async () => {
    const next = /open/i.test(availability) ? "Not looking" : "Open to internships";
    const r = await fetch(`/api/students/me/availability`, { method: "POST", headers: { "Content-Type": "application/json" }, credentials: "same-origin", body: JSON.stringify({ availability: next }) });
    if (r.ok) { const v = await r.json().catch(() => next); setD((prev) => (prev ? { ...prev, persona: { ...prev.persona, availability: typeof v === "string" ? v : next } } : prev)); }
  };
  const nav: [React.ReactNode, string, boolean][] = [[N.path, "student.nav.path", true], [N.cap, "student.nav.internships", false], [N.book, "student.nav.learning", false], [N.chat, "student.nav.mentors", false], [N.gear, "student.nav.settings", false]];
  const initials = p.name.split(" ").map((x) => x[0]).slice(0, 2).join("");
  return (
    <div className="flex min-h-screen">
      <aside className="hidden lg:flex w-[228px] shrink-0 flex-col border-r border-line/70 bg-panel/40 px-4 py-6 relative z-10">
        <div className="px-1"><Logo /></div>
        <nav className="mt-9 flex flex-col gap-1"><div className="eyebrow px-3 mb-1">{t("student.nav.eyebrow")}</div>
          {nav.map(([icon, label, active]) => (
            <a key={label as string} href="#" className={`group flex items-center gap-3 rounded-xl px-3 py-2.5 text-sm transition ${active ? "bg-brand/[0.12] text-ink-hi shadow-[inset_0_0_0_1px_rgba(47,211,154,0.25)]" : "text-ink-mid hover:bg-white/[0.03] hover:text-ink-hi"}`}>
              <span className={active ? "text-brand-bright" : "text-ink-lo group-hover:text-ink-mid"}><Ic d={icon} /></span>{t(label as string)}{active && <span className="ml-auto h-1.5 w-1.5 rounded-full bg-gold" />}</a>
          ))}
        </nav>
        <div className="mt-auto card p-3.5"><div className="text-xs font-semibold text-gold">★ {p.program}</div><p className="mt-1.5 text-[11px] leading-snug text-ink-mid">{t("student.sidebar.blurb")}</p></div>
      </aside>
      <main className="flex-1 min-w-0 relative z-10">
        <header className="sticky top-0 z-20 flex items-center gap-4 border-b border-line/60 bg-base/70 backdrop-blur-xl px-5 lg:px-7 py-4">
          <div className="min-w-0"><div className="flex items-center gap-2"><h1 className="font-display text-xl font-extrabold text-ink-hi tracking-tight">{t("student.topbar.title")}</h1>{live ? <span className="chip !text-[10px] !text-brand-bright !border-brand/30"><span className="h-1.5 w-1.5 rounded-full bg-brand-bright animate-pulse" /> LIVE</span> : <span className="chip !text-[10px] !text-gold !border-gold/30">{t("student.topbar.demo")}</span>}</div><p className="text-[11px] text-ink-lo mt-0.5">{t("student.topbar.subtitle")}</p></div>
          <div className="ml-auto flex items-center gap-3">
            <LanguageSwitcher />
            <ThemeSwitcher />
            {live && (
              <button onClick={toggleAvailability} title="Toggle availability" className={`chip !text-[11px] hidden md:inline-flex transition ${/open/i.test(availability) ? "!text-brand-bright !border-brand/30" : "!text-ink-mid !border-line/70"}`}><span className={`h-1.5 w-1.5 rounded-full ${/open/i.test(availability) ? "bg-brand-bright" : "bg-ink-lo"}`} />{availability}</button>
            )}
            <span className="chip !text-[11px] !text-gold !border-gold/30 hidden md:inline-flex">★ {p.program}</span>
            <div className="hidden md:flex items-center gap-2.5 rounded-xl border border-line/70 bg-panel2/50 pl-2.5 pr-2 py-1.5"><div className="grid h-7 w-7 place-items-center rounded-lg bg-brand/20 text-[11px] font-bold text-brand-bright">{initials}</div><div className="leading-tight"><div className="text-xs font-semibold text-ink-hi">{p.name}</div><div className="text-[10px] text-ink-lo">{p.field}</div></div><button onClick={logout} title={t("student.topbar.signOut")} className="ml-1 text-ink-lo hover:text-pink transition"><Ic d={N.out} s={15} /></button></div>
          </div>
        </header>
        <motion.div initial="initial" animate="animate" transition={{ staggerChildren: 0.06 }} className="px-5 lg:px-7 py-6 space-y-5">
          <div className="grid grid-cols-1 xl:grid-cols-3 gap-5">
            <motion.section variants={fade} className="card p-5 flex items-center gap-4">
              <div className="w-[150px] shrink-0"><Chart option={gauge(p.readiness)} height={150} /></div>
              <div><div className="eyebrow">{t("student.readiness.eyebrow")}</div><div className="font-display text-2xl font-extrabold text-ink-hi mt-1">{t("student.readiness.ready", { n: p.readiness })}</div><p className="text-xs text-ink-mid mt-1">{p.field} · {p.year}</p><div className="text-[11px] text-ink-lo mt-2">{p.school}</div><div className="text-[11px] text-gold mt-0.5">{t("student.readiness.graduating", { program: p.program, year: p.graduating })}</div></div>
            </motion.section>
            <motion.section variants={fade} className="card p-5 xl:col-span-2">
              <div className="grid grid-cols-2 sm:grid-cols-4 gap-4 h-full">
                {[[t("student.kpi.profileViews"), k.profileViews, t("student.kpi.viewsDelta", { n: k.viewsDelta }), d.viewsTrend], [t("student.kpi.internshipMatches"), k.internshipMatches, t("student.kpi.openNow"), undefined], [t("student.kpi.applications"), k.applications, t("student.kpi.mentorChats", { n: k.mentorSessions }), undefined], [t("student.kpi.modulesDone"), k.skillsDone, t("student.kpi.keepGoing"), undefined]].map(([label, val, sub, spark], i) => (
                  <div key={i} className="flex flex-col justify-between"><span className="eyebrow">{label as string}</span><div className="num text-[26px] font-bold text-ink-hi leading-none mt-1.5">{val as number}</div>{spark ? <div className="h-8 -mx-1 mt-1"><Chart option={sparkOption(spark as number[], C.brand)} height={32} /></div> : <span className="mt-2 text-[11px] text-brand-bright">{sub as string}</span>}</div>
                ))}
              </div>
            </motion.section>
          </div>
          <div className="grid grid-cols-1 xl:grid-cols-3 gap-5">
            <motion.section variants={fade} className="card p-5 xl:col-span-2">
              <div className="flex items-center justify-between mb-3">
                <div><h3 className="font-display text-[15px] font-bold text-ink-hi">{t("student.matches.title")}</h3><p className="text-[11px] text-ink-lo mt-0.5">{t("student.matches.sub")}</p></div>
                {live ? (
                  <div className="flex items-center gap-0.5 rounded-lg bg-panel2/50 p-0.5">
                    {(["all", "saved", "applied"] as const).map((f) => (
                      <button key={f} onClick={() => setMatchFilter(f)} className={`rounded-md px-2.5 py-1 text-[11px] font-semibold capitalize transition ${matchFilter === f ? "bg-brand/20 text-brand-bright" : "text-ink-lo hover:text-ink-hi"}`}>{f}</button>
                    ))}
                  </div>
                ) : (
                  <span className="chip !text-[10px]">{t("student.matches.count", { n: d.matches.length })}</span>
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
                  <div key={m.id ?? i} className={`rounded-xl border p-3.5 transition ${m.status === "applied" ? "border-brand/50 bg-brand/[0.06]" : m.status === "saved" ? "border-gold/40 bg-panel2/40" : "border-line/60 bg-panel2/40 hover:border-brand/40"}`}>
                    <div className="flex items-start justify-between gap-2"><div className="min-w-0"><div className="text-sm font-semibold text-ink-hi truncate">{m.role}</div><div className="text-[11px] text-ink-mid truncate">{m.company} · {m.city}</div></div><div className="text-right shrink-0"><div className="num text-base font-bold text-brand-bright">{m.match}%</div><div className="text-[9px] text-ink-lo uppercase">{t("student.matches.match")}</div></div></div>
                    <div className="mt-2.5 flex items-center justify-between"><span className="text-[11px] text-gold num">{kK(m.stipendLo)}–{kK(m.stipendHi)}</span><span className="text-[10px] text-ink-lo">{m.type} · {m.posted}</span></div>
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
            <motion.section variants={fade} className="card p-5">
              <h3 className="font-display text-[15px] font-bold text-ink-hi">{t("student.learning.title")}</h3><p className="text-[11px] text-ink-lo mt-0.5 mb-3">{t("student.learning.sub")}</p>
              <div className="space-y-3">{d.learning.map((l, i) => (<div key={i}><div className="flex items-center justify-between text-xs mb-1"><span className="text-ink-hi font-medium">{l.name}</span><span className={`text-[10px] uppercase tracking-wide ${tagColor[l.tag]}`}>{l.tag}</span></div><div className="h-2 rounded-full bg-panel2/70 overflow-hidden"><div className="h-full rounded-full bg-gradient-to-r from-brand-deep to-brand-bright" style={{ width: l.progress + "%" }} /></div></div>))}</div>
            </motion.section>
          </div>
          <div className="grid grid-cols-1 xl:grid-cols-3 gap-5">
            <motion.section variants={fade} className="card p-5">
              <h3 className="font-display text-[15px] font-bold text-ink-hi">{t("student.pipeline.title")}</h3><p className="text-[11px] text-ink-lo mt-0.5 mb-4">{t("student.pipeline.sub")}</p>
              <div className="space-y-2.5">{d.pipeline.map((s, i) => { const w = (s.value / d.pipeline[0].value) * 100; const c = ["#1FB283", "#2FD39A", "#E8B14C", "#E8B14C"][i]; return (<div key={i}><div className="flex justify-between text-xs mb-1"><span className="text-ink-mid">{s.stage}</span><span className="num text-ink-hi">{s.value}</span></div><div className="h-2 rounded-full bg-panel2/70 overflow-hidden"><div className="h-full rounded-full" style={{ width: w + "%", background: c }} /></div></div>); })}</div>
            </motion.section>
            <motion.section variants={fade} className="card p-5">
              <h3 className="font-display text-[15px] font-bold text-ink-hi">{t("student.skills.title")}</h3><p className="text-[11px] text-ink-lo mt-0.5 mb-3">{t("student.skills.sub")}</p>
              <div className="space-y-3">{d.skills.map((s, i) => (<div key={i}><div className="flex justify-between text-xs mb-1"><span className="text-ink-hi font-medium">{s.name}</span><span className="num text-[11px] text-ink-mid">{s.level}%</span></div><div className="h-2 rounded-full bg-panel2/70 overflow-hidden"><div className="h-full rounded-full bg-gradient-to-r from-brand-deep to-brand-bright" style={{ width: s.level + "%" }} /></div></div>))}</div>
            </motion.section>
            <motion.section variants={fade} className="card p-5">
              <h3 className="font-display text-[15px] font-bold text-ink-hi mb-3">{t("student.activity.title")}</h3>
              <div className="space-y-2.5">{d.activity.map((a, i) => (<div key={i} className="flex items-center gap-3 text-sm"><span className="grid h-7 w-7 shrink-0 place-items-center rounded-lg bg-brand/[0.12] text-brand-bright"><Ic d={N.eye} s={14} /></span><span className="text-ink-mid flex-1 text-[13px]">{a.text}</span><span className="text-[11px] text-ink-lo num whitespace-nowrap">{a.when}</span></div>))}</div>
            </motion.section>
          </div>
          <footer className="flex flex-wrap items-center justify-between gap-2 pt-1 pb-4 text-[11px] text-ink-lo"><span>{t("student.footer.brand")}</span><span>{t("student.footer.demo")}</span></footer>
        </motion.div>
      </main>
    </div>
  );
}

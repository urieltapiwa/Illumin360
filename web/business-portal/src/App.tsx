import { useEffect, useMemo, useState } from "react";
import { motion } from "framer-motion";
import type { Dashboard, FunnelRow, Annual } from "./types";
import { initAuth, login, logout, type Session } from "./auth";
import Professional from "./Professional";
import Admin from "./Admin";
import Student from "./Student";
import Employer from "./Employer";
import Support from "./Support";
import { useTranslation } from "react-i18next";
import { LanguageSwitcher, ThemeSwitcher } from "@illumin360/ui";
import {
  Chart, talentGrowthOption, revenueOption, hiresOption, cityOption, donutOption, funnelOption,
  sparkOption, nf, compact, curC, C,
} from "@illumin360/ui";

/* ---------- tiny inline icon set ---------- */
const I = {
  grid: <path d="M3 3h7v7H3zM14 3h7v7h-7zM14 14h7v7h-7zM3 14h7v7H3z" />,
  users: <path d="M16 11a4 4 0 1 0-4-4 4 4 0 0 0 4 4zM2 21a7 7 0 0 1 14 0M19 21a5 5 0 0 0-6-4.9" />,
  building: <path d="M4 21V4a1 1 0 0 1 1-1h10a1 1 0 0 1 1 1v17M9 8h2M9 12h2M9 16h2M16 10h3a1 1 0 0 1 1 1v10" />,
  brief: <path d="M3 8h18v12H3zM8 8V6a2 2 0 0 1 2-2h4a2 2 0 0 1 2 2v2M3 13h18" />,
  chart: <path d="M4 20V10M10 20V4M16 20v-7M22 20H2" />,
  gear: <path d="M12 15a3 3 0 1 0 0-6 3 3 0 0 0 0 6zM19.4 13a7.5 7.5 0 0 0 0-2l2-1.5-2-3.5-2.4 1a7 7 0 0 0-1.7-1L14.5 2.5h-5L9.2 5a7 7 0 0 0-1.7 1l-2.4-1-2 3.5L5.1 11a7.5 7.5 0 0 0 0 2l-2 1.5 2 3.5 2.4-1a7 7 0 0 0 1.7 1l.3 2.5h5l.3-2.5a7 7 0 0 0 1.7-1l2.4 1 2-3.5z" />,
  spark: <path d="M13 2 3 14h7l-1 8 10-12h-7z" />,
  out: <path d="M16 17l5-5-5-5M21 12H9M9 21H5a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h4" />,
};
const Ic = ({ d, s = 18, c = "currentColor", w = 1.7 }: { d: React.ReactNode; s?: number; c?: string; w?: number }) => (
  <svg width={s} height={s} viewBox="0 0 24 24" fill="none" stroke={c} strokeWidth={w} strokeLinecap="round" strokeLinejoin="round">{d}</svg>
);

function useDashboard() {
  const [d, setD] = useState<Dashboard | null>(null);
  useEffect(() => { fetch(import.meta.env.BASE_URL + "dashboard.json").then((r) => r.json()).then(setD); }, []);
  return d;
}
interface LiveStats { total: number; byCity: { label: string; count: number }[]; byAvailability: { label: string; count: number }[]; }
function useLiveStats() {
  const [s, setS] = useState<LiveStats | null>(null);
  useEffect(() => { fetch("/api/candidates/stats").then((r) => (r.ok ? r.json() : null)).then(setS).catch(() => setS(null)); }, []);
  return s;
}

interface Labelled { label: string; count: number; }
interface RecruitmentStats {
  totalRequests: number; openRequests: number; filledRequests: number;
  totalApplications: number; totalHires: number; avgMatchScore: number;
  funnel: Labelled[]; byTalentType: Labelled[]; topCities: Labelled[];
  trend: { month: string; applications: number; hires: number }[];
}
// Live recruitment analytics, served by the Recruitment service over a decade of real requests/applications.
function useRecruitmentStats() {
  const [s, setS] = useState<RecruitmentStats | null>(null);
  useEffect(() => { fetch("/api/recruitment/stats").then((r) => (r.ok ? r.json() : null)).then(setS).catch(() => setS(null)); }, []);
  return s;
}
const bucket = (rows: Labelled[], key: string) => rows.find((x) => x.label === key)?.count ?? 0;

function useCountUp(target: number, dur = 1100) {
  // Ease from 0 to target — but snap straight to the value when animation can't run: reduced-motion is
  // requested, or the tab is hidden (browsers pause requestAnimationFrame for hidden pages, which would
  // otherwise leave the number frozen at 0 on a backgrounded load).
  const canAnimate = () =>
    typeof window !== "undefined" &&
    !window.matchMedia?.("(prefers-reduced-motion: reduce)").matches &&
    !document.hidden;
  const [v, setV] = useState(() => (canAnimate() ? 0 : target));
  useEffect(() => {
    if (!canAnimate()) { setV(target); return; }
    let raf = 0; const start = performance.now();
    const tick = (t: number) => { const p = Math.min(1, (t - start) / dur); setV(target * (1 - Math.pow(1 - p, 3))); if (p < 1) raf = requestAnimationFrame(tick); };
    raf = requestAnimationFrame(tick); return () => cancelAnimationFrame(raf);
  }, [target, dur]);
  return v;
}

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
        <div className="text-[9px] uppercase tracking-[0.22em] text-ink-lo mt-0.5">Business</div>
      </div>
    </div>
  );
}

function Sidebar() {
  const { t } = useTranslation();
  const items: [React.ReactNode, string, boolean][] = [
    [I.grid, "nav.overview", true], [I.users, "nav.talent", false], [I.building, "nav.companies", false],
    [I.brief, "nav.hiring", false], [I.chart, "nav.revenue", false], [I.gear, "nav.settings", false],
  ];
  return (
    <aside className="hidden lg:flex w-[228px] shrink-0 flex-col border-r border-line/70 bg-panel/40 px-4 py-6 relative z-10">
      <div className="px-1"><Logo /></div>
      <nav className="mt-9 flex flex-col gap-1">
        <div className="eyebrow px-3 mb-1">{t("nav.workspace")}</div>
        {items.map(([icon, label, active]) => (
          <a key={label} href="#" className={`group flex items-center gap-3 rounded-xl px-3 py-2.5 text-sm transition ${active ? "bg-brand/[0.12] text-ink-hi shadow-[inset_0_0_0_1px_rgba(47,211,154,0.25)]" : "text-ink-mid hover:bg-white/[0.03] hover:text-ink-hi"}`}>
            <span className={active ? "text-brand-bright" : "text-ink-lo group-hover:text-ink-mid"}><Ic d={icon} /></span>
            {t(label)}
            {active && <span className="ml-auto h-1.5 w-1.5 rounded-full bg-gold" />}
          </a>
        ))}
      </nav>
      <div className="mt-auto card p-3.5">
        <div className="flex items-center gap-2 text-brand-bright"><Ic d={I.spark} s={15} /><span className="text-xs font-semibold text-ink-hi">Insights Pro</span></div>
        <p className="mt-1.5 text-[11px] leading-snug text-ink-mid">Benchmark your hiring against the national talent market in real time.</p>
        <button className="mt-2.5 w-full rounded-lg bg-brand/[0.15] py-1.5 text-[11px] font-semibold text-brand-bright hover:bg-brand/25 transition">Explore</button>
      </div>
    </aside>
  );
}

function RangeToggle({ value, onChange }: { value: string; onChange: (v: string) => void }) {
  return (
    <div className="flex items-center rounded-xl border border-line/70 bg-panel2/50 p-0.5">
      {["3Y", "5Y", "10Y"].map((r) => (
        <button key={r} onClick={() => onChange(r)} className={`px-3 py-1.5 text-xs font-semibold rounded-lg transition ${value === r ? "bg-brand text-base shadow-glow" : "text-ink-mid hover:text-ink-hi"}`}>{r}</button>
      ))}
    </div>
  );
}

function Kpi({ label, value, format, spark, sparkColor, delta, suffix }: {
  label: string; value: number; format: (n: number) => string; spark: number[]; sparkColor: string; delta?: number; suffix?: string;
}) {
  const v = useCountUp(value);
  const up = (delta ?? 0) >= 0;
  return (
    <motion.div variants={fade} className="card p-4 relative overflow-hidden">
      <div className="flex items-start justify-between">
        <span className="eyebrow">{label}</span>
        {delta !== undefined && (
          <span className={`chip !px-2 !py-0.5 !text-[10px] ${up ? "!text-brand-bright !border-brand/30" : "!text-pink !border-pink/30"}`}>
            {up ? "▲" : "▼"} {Math.abs(delta).toFixed(0)}%
          </span>
        )}
      </div>
      <div className="mt-2 num text-[30px] font-bold text-ink-hi leading-none">{format(v)}{suffix}</div>
      <div className="mt-2 h-9 -mx-1"><Chart option={sparkOption(spark, sparkColor)} height={38} /></div>
    </motion.div>
  );
}

function Panel({ title, sub, children, className = "", action }: { title: string; sub?: string; children: React.ReactNode; className?: string; action?: React.ReactNode }) {
  return (
    <motion.section variants={fade} className={`card p-5 ${className}`}>
      <div className="flex items-start justify-between mb-1">
        <div>
          <h3 className="font-display text-[15px] font-bold text-ink-hi">{title}</h3>
          {sub && <p className="text-[11px] text-ink-lo mt-0.5">{sub}</p>}
        </div>
        {action}
      </div>
      {children}
    </motion.section>
  );
}

function yr(a: Dashboard["annual"], y: string) { return a.find((x) => x.year === y); }
function pct(cur?: number, prev?: number) { return cur && prev ? ((cur - prev) / prev) * 100 : 0; }

interface LiveCandidate { firstName: string; lastName: string; city: string; nationality: string; availability: string; publicHeadline?: string; }
function LiveTalent({ city, onClear }: { city?: string; onClear?: () => void }) {
  const [rows, setRows] = useState<LiveCandidate[] | null>(null);
  const [err, setErr] = useState(false);
  const { t } = useTranslation();
  useEffect(() => {
    // same-origin /api → Vite proxy → YARP gateway → Candidates API (live, Keycloak-secured in prod).
    // `city` drives the API's ILIKE filter — clicking a bar in "Talent by city" cross-filters this feed live.
    setRows(null); setErr(false);
    const q = city ? `&city=${encodeURIComponent(city)}` : "";
    fetch(`/api/candidates?pageSize=8${q}`).then((r) => { if (!r.ok) throw new Error(); return r.json(); }).then(setRows).catch(() => setErr(true));
  }, [city]);
  return (
    <motion.section variants={fade} className="card p-5">
      <div className="flex items-center justify-between mb-3 gap-2">
        <div className="flex items-center gap-2 min-w-0">
          <h3 className="font-display text-[15px] font-bold text-ink-hi">{t("panel.liveFeed")}</h3>
          <span className="chip !text-[10px] !text-brand-bright !border-brand/30"><span className="h-1.5 w-1.5 rounded-full bg-brand-bright animate-pulse" />{t("panel.live")}</span>
          {city && (
            <button onClick={onClear} className="chip !text-[10px] !text-gold !border-gold/40 hover:!border-gold transition inline-flex items-center gap-1" title={t("filter.clear")}>
              {city}<span className="text-[13px] leading-none">×</span>
            </button>
          )}
        </div>
        <span className="text-[11px] text-ink-lo hidden sm:inline">Candidates API · Postgres · via BFF gateway</span>
      </div>
      {err ? (
        <div className="text-[12px] text-ink-lo py-3">Candidates API unreachable — start the stack to stream live talent.</div>
      ) : !rows ? (
        <div className="text-[12px] text-ink-lo py-3 animate-pulse">Connecting to Candidates API…</div>
      ) : rows.length === 0 ? (
        <div className="text-[12px] text-ink-lo py-3">{t("filter.empty", { city })}</div>
      ) : (
        <div className="grid sm:grid-cols-2 lg:grid-cols-4 gap-3">
          {rows.map((c, i) => (
            <div key={i} className="rounded-xl border border-line/50 bg-panel2/40 p-3">
              <div className="text-sm font-semibold text-ink-hi truncate">{c.firstName} {c.lastName}</div>
              <div className="text-[11px] text-ink-mid truncate">{c.publicHeadline || c.nationality}</div>
              <div className="mt-2 flex items-center justify-between"><span className="text-[11px] text-ink-lo">{c.city}</span><span className="chip !text-[9px]">{c.availability}</span></div>
            </div>
          ))}
        </div>
      )}
    </motion.section>
  );
}

function DashboardView({ session }: { session: Session }) {
  const d = useDashboard();
  const [range, setRange] = useState("10Y");
  const [selectedCity, setSelectedCity] = useState<string | null>(null);
  const live = useLiveStats();
  const rec = useRecruitmentStats();
  const { t } = useTranslation();
  const sliced = useMemo(() => {
    if (!d) return [];
    const n = range === "3Y" ? 36 : range === "5Y" ? 60 : 120;
    return d.monthly.slice(-n);
  }, [d, range]);

  if (!d) return <div className="grid place-items-center h-screen text-ink-mid font-mono text-sm animate-pulse">Loading insights…</div>;

  const k = d.kpis;
  const tDelta = pct(yr(d.annual, "2025")?.newTalent, yr(d.annual, "2024")?.newTalent);
  const hDelta = pct(yr(d.annual, "2025")?.hires, yr(d.annual, "2024")?.hires);
  const rDelta = pct(yr(d.annual, "2025")?.revenue, yr(d.annual, "2024")?.revenue);
  const cDelta = pct(yr(d.annual, "2025")?.activeSubs, yr(d.annual, "2024")?.activeSubs);
  const every = (arr: number[], step: number) => arr.filter((_, i) => i % step === 0);
  const initials = (session.name || "U").split(" ").map((s) => s[0]).slice(0, 2).join("");

  // --- Live recruitment analytics (funnel + hires) derived from the Recruitment service, snapshot fallback ---
  const liveFunnel: FunnelRow[] | null = rec ? [
    { stage: "Applications", value: rec.totalApplications },
    { stage: "Shortlisted", value: bucket(rec.funnel, "shortlisted") + bucket(rec.funnel, "hired") },
    { stage: "Hires", value: rec.totalHires },
  ] : null;
  const liveAnnualHires: Annual[] | null = rec
    ? (Object.entries(rec.trend.reduce<Record<string, number>>((m, p) => { const y = p.month.slice(0, 4); m[y] = (m[y] || 0) + p.hires; return m; }, {}))
        .map(([year, hires]) => ({ year, hires })) as Annual[])
    : null;
  const liveFillRate = rec && rec.totalRequests > 0 ? (rec.filledRequests / rec.totalRequests) * 100 : null;
  const LiveChip = () => (
    <span className="chip !text-[10px] !text-brand-bright !border-brand/30"><span className="h-1.5 w-1.5 rounded-full bg-brand-bright animate-pulse" />{t("panel.live")}</span>
  );

  return (
    <div className="flex min-h-screen">
      <Sidebar />
      <main className="flex-1 min-w-0 relative z-10">
        <header className="sticky top-0 z-20 flex items-center gap-4 border-b border-line/60 bg-base/70 backdrop-blur-xl px-5 lg:px-7 py-4">
          <div className="min-w-0">
            <div className="flex items-center gap-2">
              <h1 className="font-display text-xl font-extrabold text-ink-hi tracking-tight">{t("topbar.title")}</h1>
              <span className="chip !text-[10px] !text-gold !border-gold/30">{t("topbar.demo")}</span>
            </div>
            <p className="text-[11px] text-ink-lo mt-0.5 truncate">{d.meta.scope}</p>
          </div>
          <div className="ml-auto flex items-center gap-3">
            <LanguageSwitcher />
            <ThemeSwitcher />
            <RangeToggle value={range} onChange={setRange} />
            <div className="hidden md:flex items-center gap-2.5 rounded-xl border border-line/70 bg-panel2/50 pl-2.5 pr-2 py-1.5">
              <div className="grid h-7 w-7 place-items-center rounded-lg bg-brand/20 text-[11px] font-bold text-brand-bright">{initials}</div>
              <div className="leading-tight">
                <div className="text-xs font-semibold text-ink-hi">{session.name}</div>
                <div className="text-[10px] text-ink-lo">{session.company}</div>
              </div>
              <button onClick={logout} title="Sign out" className="ml-1 text-ink-lo hover:text-pink transition"><Ic d={I.out} s={15} /></button>
            </div>
          </div>
        </header>

        <motion.div initial="initial" animate="animate" transition={{ staggerChildren: 0.06 }} className="px-5 lg:px-7 py-6 space-y-5">
          <div className="grid grid-cols-2 lg:grid-cols-5 gap-4">
            <Kpi label={t("kpi.totalTalent")} value={live?.total ?? k.totalTalent} format={nf} spark={every(d.monthly.map((m) => m.cumTalent), 4)} sparkColor={C.brand} delta={tDelta} />
            <Kpi label={t("kpi.activeCompanies")} value={k.companies} format={nf} spark={every(d.monthly.map((m) => m.cumCompanies), 4)} sparkColor={C.blue} delta={cDelta} />
            <Kpi label={t("kpi.hiresPlaced")} value={rec?.totalHires ?? k.totalHires} format={nf} spark={d.annual.map((a) => a.hires)} sparkColor={C.brandDeep} delta={hDelta} />
            <Kpi label={t("kpi.fillRate")} value={liveFillRate ?? k.fillRate} format={(n) => n.toFixed(1)} suffix="%" spark={d.annual.map((a) => a.hires)} sparkColor={C.violet} />
            <Kpi label={t("kpi.arr")} value={k.arr} format={curC} spark={every(d.monthly.map((m) => m.mrr), 4)} sparkColor={C.gold} delta={rDelta} />
          </div>

          <div className="grid grid-cols-1 xl:grid-cols-3 gap-5">
            <Panel className="xl:col-span-2" title="Talent pool growth" sub={`Cumulative registered talent · ${range}`} action={<span className="chip !text-[10px]"><span className="h-1.5 w-1.5 rounded-full bg-brand-bright" /> {compact(k.totalTalent)} total</span>}>
              <Chart option={talentGrowthOption(sliced)} height={300} />
            </Panel>
            <Panel title="Talent mix" sub="Professionals vs students">
              <Chart option={donutOption([{ name: "Professionals", value: k.professionals }, { name: "Students", value: k.students }], [C.brand, C.gold])} height={300} />
            </Panel>
          </div>

          <div className="grid grid-cols-1 xl:grid-cols-5 gap-5">
            <Panel className="xl:col-span-3" title="Recurring revenue" sub={`MRR & active subscribers · ${range}`} action={<span className="chip !text-[10px] !text-gold !border-gold/30">{curC(k.mrr)}/mo</span>}>
              <Chart option={revenueOption(sliced)} height={280} />
            </Panel>
            <Panel className="xl:col-span-2" title="Hiring funnel" sub="Applications → shortlist → hire (10y)" action={rec ? <LiveChip /> : undefined}>
              <Chart option={funnelOption(liveFunnel ?? d.funnel)} height={280} />
            </Panel>
          </div>

          <div className="grid grid-cols-1 xl:grid-cols-2 gap-5">
            <Panel title={t("panel.byCity")} sub={t("filter.tapCity")} action={live ? <span className="chip !text-[10px] !text-brand-bright !border-brand/30"><span className="h-1.5 w-1.5 rounded-full bg-brand-bright animate-pulse" />{t("panel.live")}</span> : undefined}>
              <Chart option={cityOption(live ? live.byCity.map((x) => ({ city: x.label, value: x.count })) : d.byCity)} height={300} onClick={(city) => setSelectedCity((prev) => (prev === city ? null : city))} />
            </Panel>
            <Panel title="Hires per year" sub="Successful placements" action={rec ? <LiveChip /> : <span className="chip !text-[10px] !text-brand-bright !border-brand/30">▲ {hDelta.toFixed(0)}% YoY</span>}>
              <Chart option={hiresOption(liveAnnualHires ?? d.annual)} height={300} />
            </Panel>
          </div>

          <LiveTalent city={selectedCity ?? undefined} onClear={() => setSelectedCity(null)} />

          <Panel title="Recent placements" sub="Latest successful hires across the network">
            <div className="overflow-x-auto -mx-1">
              <table className="w-full text-sm min-w-[680px]">
                <thead>
                  <tr className="text-left eyebrow border-b border-line/60">
                    <th className="py-2.5 pl-2 font-semibold">Candidate</th><th className="font-semibold">Role</th>
                    <th className="font-semibold">Company</th><th className="font-semibold">City</th>
                    <th className="font-semibold text-right">Match</th><th className="font-semibold text-right pr-2">Date</th>
                  </tr>
                </thead>
                <tbody>
                  {d.recentHires.map((h, i) => (
                    <tr key={i} className="border-b border-line/30 hover:bg-white/[0.02] transition">
                      <td className="py-2.5 pl-2">
                        <div className="flex items-center gap-2.5">
                          <span className={`h-2 w-2 rounded-full ${h.type === "student" ? "bg-gold" : "bg-brand-bright"}`} />
                          <span className="text-ink-hi font-medium">{h.name}</span>
                        </div>
                      </td>
                      <td className="text-ink-mid">{h.role}</td>
                      <td className="text-ink-mid">{h.company}</td>
                      <td className="text-ink-mid">{h.city}</td>
                      <td className="text-right num text-ink-hi">{h.score.toFixed(0)}</td>
                      <td className="text-right pr-2 num text-ink-lo">{h.date}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          </Panel>

          <footer className="flex flex-wrap items-center justify-between gap-2 pt-1 pb-4 text-[11px] text-ink-lo">
            <span>Illumin360 · {d.meta.scope}</span>
            <span>Synthetic demo data · for illustration only</span>
          </footer>
        </motion.div>
      </main>
    </div>
  );
}

function Login() {
  const { t } = useTranslation();
  const stats: [string, string][] = [["17K+", t("login.statTalent")], ["1,800+", t("login.statCompanies")], ["25K+", t("login.statHires")]];
  return (
    <div className="min-h-screen grid lg:grid-cols-2 relative">
      <div className="absolute top-4 right-4 z-30 flex items-center gap-2"><LanguageSwitcher /><ThemeSwitcher /></div>

      {/* brand panel — large screens */}
      <div className="relative hidden lg:flex flex-col justify-between p-10 xl:p-12 border-r border-line/60 overflow-hidden">
        <div className="absolute -right-24 -top-24 h-[460px] w-[460px] rounded-full border border-line/60" />
        <div className="absolute right-6 top-10 h-[320px] w-[320px] rounded-full border border-brand/30" />
        <div className="absolute right-32 top-36 h-[180px] w-[180px] rounded-full border border-gold/40" />
        <Logo />
        <div className="relative z-10 max-w-md">
          <h2 className="font-display text-[36px] xl:text-[40px] font-extrabold leading-[1.05] text-ink-hi">{t("login.headline1")}<br /><span className="text-brand-bright">{t("login.headline2")}</span></h2>
          <p className="mt-4 text-ink-mid leading-relaxed">{t("login.blurb")}</p>
          <div className="mt-8 flex gap-6">
            {stats.map(([n, l]) => (
              <div key={l}><div className="num text-2xl font-bold text-gold">{n}</div><div className="text-[11px] text-ink-lo mt-0.5">{l}</div></div>
            ))}
          </div>
        </div>
        <p className="relative z-10 text-[11px] text-ink-lo">{t("login.location")}</p>
      </div>

      {/* form column */}
      <div className="flex items-center justify-center px-5 py-16 sm:px-8">
        <div className="w-full max-w-sm">
          {/* compact brand — mobile & tablet only */}
          <div className="lg:hidden mb-8">
            <Logo />
            <h2 className="font-display text-[26px] sm:text-[32px] font-extrabold leading-[1.08] text-ink-hi mt-6">{t("login.headline1")} <span className="text-brand-bright">{t("login.headline2")}</span></h2>
            <div className="mt-5 flex gap-6">
              {stats.map(([n, l]) => (
                <div key={l}><div className="num text-lg font-bold text-gold">{n}</div><div className="text-[10px] text-ink-lo">{l}</div></div>
              ))}
            </div>
          </div>
          <span className="eyebrow">{t("login.portal")}</span>
          <h1 className="font-display text-3xl font-extrabold text-ink-hi mt-2">{t("login.welcome")}</h1>
          <p className="text-ink-mid mt-2 text-sm">{t("login.subtitle")}</p>
          <button onClick={login} className="mt-7 w-full rounded-xl bg-brand py-3 font-semibold text-base shadow-glow hover:bg-brand-bright transition flex items-center justify-center gap-2">
            <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round"><path d="M12 17v-3M8 11V7a4 4 0 0 1 8 0v4M6 11h12v9H6z" /></svg>
            {t("login.continue")}
          </button>
          <div className="my-5 flex items-center gap-3 text-[11px] text-ink-lo"><span className="h-px flex-1 bg-line/60" />{t("login.secured")}<span className="h-px flex-1 bg-line/60" /></div>
          <a href="?demo=1" className="block w-full rounded-xl border border-line/70 bg-panel2/40 py-3 text-center font-semibold text-ink-mid hover:text-ink-hi hover:border-brand/40 transition">{t("login.demo")}</a>
          <a href="?screen=register" className="mt-3 block text-center text-[12px] text-ink-mid hover:text-brand-bright transition">New to Illumin360? <span className="font-semibold text-brand-bright">Create an account</span></a>
          <p className="mt-6 text-center text-[11px] text-ink-lo">{t("login.oauth")}</p>
        </div>
      </div>
    </div>
  );
}

const REG_TYPES: [string, string][] = [["student", "Student"], ["professional", "Professional"], ["employer", "Employer"]];

function Register() {
  const [type, setType] = useState("student");
  const [form, setForm] = useState({ firstName: "", lastName: "", email: "", password: "", city: "", field: "", school: "", role: "", company: "" });
  const [status, setStatus] = useState<{ state: "idle" | "submitting" | "ok" | "error"; msg?: string }>({ state: "idle" });
  const set = (k: string) => (e: React.ChangeEvent<HTMLInputElement>) => setForm((f) => ({ ...f, [k]: e.target.value }));
  const submit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (form.password.length < 12) { setStatus({ state: "error", msg: "Password must be at least 12 characters." }); return; }
    setStatus({ state: "submitting" });
    try {
      const r = await fetch(`/register/${type}`, { method: "POST", headers: { "Content-Type": "application/json" }, credentials: "same-origin", body: JSON.stringify(form) });
      const body = await r.json().catch(() => ({}));
      setStatus(r.ok ? { state: "ok", msg: body.message || "Account created." } : { state: "error", msg: body.message || "Registration failed." });
    } catch { setStatus({ state: "error", msg: "Network error — please try again." }); }
  };
  const inputCls = "w-full rounded-xl border border-line/70 bg-panel2/40 px-3 py-2.5 text-sm text-ink-hi placeholder:text-ink-lo focus:border-brand/50 focus:outline-none";
  return (
    <div className="min-h-screen grid place-items-center px-5 py-12 relative">
      <div className="absolute top-4 right-4 z-30 flex items-center gap-2"><LanguageSwitcher /><ThemeSwitcher /></div>
      <div className="w-full max-w-md">
        <Logo />
        <h1 className="font-display text-3xl font-extrabold text-ink-hi mt-6">Create your account</h1>
        <p className="text-ink-mid mt-2 text-sm">Join Illumin360 as a student, professional, or employer.</p>
        {status.state === "ok" ? (
          <div className="mt-6 rounded-xl border border-brand/30 bg-brand/[0.08] p-5">
            <div className="text-brand-bright font-semibold">Account created ✓</div>
            <p className="text-[13px] text-ink-mid mt-1">{status.msg}</p>
            <button onClick={login} className="mt-4 w-full rounded-xl bg-brand py-2.5 font-semibold text-sm hover:bg-brand-bright transition">Sign in</button>
          </div>
        ) : (
          <form onSubmit={submit} className="mt-6 space-y-3">
            <div className="grid grid-cols-3 gap-1.5 rounded-xl bg-panel2/50 p-1">
              {REG_TYPES.map(([v, l]) => (
                <button type="button" key={v} onClick={() => setType(v)} className={`rounded-lg py-1.5 text-[12px] font-semibold transition ${type === v ? "bg-brand/20 text-brand-bright" : "text-ink-mid hover:text-ink-hi"}`}>{l}</button>
              ))}
            </div>
            <div className="grid grid-cols-2 gap-3">
              <input className={inputCls} placeholder="First name" value={form.firstName} onChange={set("firstName")} required />
              <input className={inputCls} placeholder="Last name" value={form.lastName} onChange={set("lastName")} required />
            </div>
            <input className={inputCls} type="email" placeholder="Email" value={form.email} onChange={set("email")} required />
            <input className={inputCls} type="password" placeholder="Password" value={form.password} onChange={set("password")} required />
            <input className={inputCls} placeholder="City" value={form.city} onChange={set("city")} required />
            {type === "student" && (<div className="grid grid-cols-2 gap-3"><input className={inputCls} placeholder="Field of study" value={form.field} onChange={set("field")} /><input className={inputCls} placeholder="School" value={form.school} onChange={set("school")} /></div>)}
            {type === "professional" && (<input className={inputCls} placeholder="Role (e.g. Software Developer)" value={form.role} onChange={set("role")} />)}
            {type === "employer" && (<input className={inputCls} placeholder="Company name" value={form.company} onChange={set("company")} />)}
            <p className="text-[11px] text-ink-lo">Password must be at least 12 characters.</p>
            {status.state === "error" && <div className="rounded-lg border border-pink/30 bg-pink/[0.08] px-3 py-2 text-[12px] text-pink">{status.msg}</div>}
            <button type="submit" disabled={status.state === "submitting"} className="w-full rounded-xl bg-brand py-3 font-semibold hover:bg-brand-bright transition disabled:opacity-60">{status.state === "submitting" ? "Creating account…" : "Create account"}</button>
          </form>
        )}
        <a href="?screen=login" className="mt-5 block text-center text-[12px] text-ink-lo hover:text-ink-hi">Already have an account? Sign in</a>
      </div>
    </div>
  );
}

export default function App() {
  const [session, setSession] = useState<Session | null>(null);
  useEffect(() => { initAuth().then(setSession); }, []);
  if (new URLSearchParams(location.search).get("screen") === "register") return <Register />;
  if (new URLSearchParams(location.search).get("screen") === "login") return <Login />;
  if (!session) return <div className="grid place-items-center h-screen text-ink-mid font-mono text-sm animate-pulse">Connecting…</div>;
  if (!session.authenticated) return <Login />;
  const portal = new URLSearchParams(location.search).get("portal");
  if (portal === "professional") return <Professional session={session} />;
  if (portal === "admin") return <Admin session={session} />;
  if (portal === "student") return <Student session={session} />;
  if (portal === "employer") return <Employer session={session} />;
  if (portal === "support") return <Support session={session} />;
  return <DashboardView session={session} />;
}

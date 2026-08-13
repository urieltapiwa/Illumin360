import { useEffect, useState } from "react";
import { motion } from "framer-motion";
import * as echarts from "echarts";
import { useTranslation } from "react-i18next";
import { LanguageSwitcher, ThemeSwitcher } from "@illumin360/ui";
import { Chart, nf, C } from "@illumin360/ui";
import { logout, type Session } from "./auth";
import { useScrollSpy, scrollToSection, SectionAnchor } from "./scrollnav";

interface SupportData {
  agent: { name: string; role: string; team: string };
  kpis: { openTickets: number; assignedToMe: number; resolvedToday: number; avgFirstResponse: number; csat: number; slaMet: number };
  volume: number[][];
  byCategory: { name: string; value: number }[];
  priority: { open: number; p1: number; p2: number; p3: number };
  tickets: { id: string; subject: string; requester: string; category: string; priority: string; status: string; channel: string; age: string }[];
  leaderboard: { name: string; resolved: number; csat: number }[];
  activity: { text: string; when: string }[];
}
const Ic = ({ d, s = 18, w = 1.7 }: { d: React.ReactNode; s?: number; w?: number }) => (
  <svg width={s} height={s} viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth={w} strokeLinecap="round" strokeLinejoin="round">{d}</svg>
);
const N = {
  inbox: <path d="M3 12h5l2 3h4l2-3h5M3 12V5a1 1 0 0 1 1-1h16a1 1 0 0 1 1 1v7M3 12v6a1 1 0 0 0 1 1h16a1 1 0 0 0 1-1v-6" />,
  tag: <path d="M3 3h7l11 11-7 7L3 10zM7.5 7.5h.01" />, chart: <path d="M4 20V10M10 20V4M16 20v-7M22 20H2" />,
  book: <path d="M4 19V5a1 1 0 0 1 1-1h13v15H6a2 2 0 0 0-2 2zM18 4v17" />,
  gear: <path d="M12 15a3 3 0 1 0 0-6 3 3 0 0 0 0 6zM19.4 13a7.5 7.5 0 0 0 0-2l2-1.5-2-3.5-2.4 1a7 7 0 0 0-1.7-1L14.5 2.5h-5L9.2 5a7 7 0 0 0-1.7 1l-2.4-1-2 3.5L5.1 11a7.5 7.5 0 0 0 0 2l-2 1.5 2 3.5 2.4-1a7 7 0 0 0 1.7 1l.3 2.5h5l.3-2.5a7 7 0 0 0 1.7-1l2.4 1 2-3.5z" />,
  out: <path d="M16 17l5-5-5-5M21 12H9M9 21H5a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h4" />, bolt: <path d="M13 2 3 14h7l-1 8 10-12h-7z" />,
};
const fade = { initial: { opacity: 0, y: 14 }, animate: { opacity: 1, y: 0 } };
const priColor: Record<string, string> = { P1: "!text-pink !border-pink/40", P2: "!text-gold !border-gold/30", P3: "!text-brand-bright !border-brand/30" };
function Logo() {
  return (<div className="flex items-center gap-2.5"><svg width="30" height="30" viewBox="0 0 32 32" fill="none"><circle cx="16" cy="16" r="14" stroke="#1FB283" strokeWidth="1.6" /><circle cx="16" cy="16" r="8.5" stroke="#2FD39A" strokeWidth="1.6" /><circle cx="16" cy="16" r="3" fill="#E8B14C" /></svg><div className="leading-none"><div className="font-display text-[17px] font-extrabold tracking-tight text-ink-hi">Illumin<span className="text-brand-bright">360</span></div><div className="text-[9px] uppercase tracking-[0.22em] text-ink-lo mt-0.5">Support</div></div></div>);
}
const axis = { axisLine: { lineStyle: { color: C.line } }, axisTick: { show: false }, axisLabel: { color: C.text, fontFamily: "JetBrains Mono", fontSize: 10 }, splitLine: { lineStyle: { color: C.grid } } } as const;
const tt = { trigger: "axis", backgroundColor: "#0B1A14", borderColor: C.line, textStyle: { color: "#E8F2EC", fontFamily: "Hanken Grotesk", fontSize: 12 } } as const;
function volumeOption(v: number[][]): echarts.EChartsOption {
  const cats = v.map((_, i) => "d-" + (v.length - i));
  return { grid: { left: 36, right: 14, top: 24, bottom: 24 }, legend: { data: ["Created", "Resolved"], top: 0, right: 0, textStyle: { color: C.textHi, fontFamily: "Hanken Grotesk" }, icon: "roundRect", itemWidth: 10, itemHeight: 10 }, tooltip: { ...tt }, xAxis: { type: "category", data: cats, boundaryGap: false, ...axis, axisLabel: { ...axis.axisLabel, show: false } }, yAxis: { type: "value", ...axis }, series: [{ name: "Created", type: "line", data: v.map((x) => x[0]), smooth: true, showSymbol: false, lineStyle: { color: C.blue, width: 2 }, areaStyle: { color: "rgba(90,183,255,0.12)" } }, { name: "Resolved", type: "line", data: v.map((x) => x[1]), smooth: true, showSymbol: false, lineStyle: { color: C.brand, width: 2.5 }, areaStyle: { color: "rgba(47,211,154,0.14)" } }] };
}
function catOption(rows: { name: string; value: number }[]): echarts.EChartsOption {
  const s = [...rows].sort((a, b) => a.value - b.value);
  return { grid: { left: 84, right: 28, top: 6, bottom: 6 }, tooltip: { trigger: "item", backgroundColor: "#0B1A14", borderColor: C.line, textStyle: { color: "#E8F2EC" } }, xAxis: { type: "value", axisLine: { show: false }, axisTick: { show: false }, axisLabel: { show: false }, splitLine: { show: false } }, yAxis: { type: "category", data: s.map((r) => r.name), axisLine: { lineStyle: { color: C.line } }, axisTick: { show: false }, axisLabel: { color: C.textHi, fontFamily: "Hanken Grotesk", fontSize: 11 } }, series: [{ type: "bar", data: s.map((r) => r.value), barWidth: "56%", itemStyle: { color: "#1FB283", borderRadius: [0, 5, 5, 0] }, label: { show: true, position: "right", color: C.textHi, fontFamily: "JetBrains Mono", fontSize: 10, formatter: (p) => nf((p as { value: number }).value) } }] };
}
function gauge(value: number): echarts.EChartsOption {
  return { series: [{ type: "gauge", startAngle: 210, endAngle: -30, min: 0, max: 100, radius: "94%", center: ["50%", "58%"], progress: { show: true, width: 9, roundCap: true, itemStyle: { color: "#2FD39A" } }, axisLine: { lineStyle: { width: 9, color: [[1, "#15301F"]] } }, pointer: { show: false }, axisTick: { show: false }, splitLine: { show: false }, axisLabel: { show: false }, anchor: { show: false }, detail: { offsetCenter: [0, "0%"], formatter: "{v|{value}%}", rich: { v: { color: "#E8F2EC", fontSize: 22, fontFamily: "JetBrains Mono", fontWeight: 700 } } }, data: [{ value }] }] };
}

export default function Support({ session }: { session: Session }) {
  const { t } = useTranslation();
  const [d, setD] = useState<SupportData | null>(null);
  useEffect(() => { fetch(import.meta.env.BASE_URL + "support.json").then((r) => r.json()).then(setD); }, []);
  // Scrollspy for the sidebar — must run before the early return to keep hook order stable.
  const activeSec = useScrollSpy(["sec-overview", "sec-queue", "sec-reports", "sec-activity"]);
  if (!d) return <div className="grid place-items-center h-screen text-ink-mid font-mono text-sm animate-pulse">{t("support.loading")}</div>;
  const k = d.kpis;
  // Sidebar sections map to real content anchors on this single-page dashboard (scrollspy).
  const nav: [React.ReactNode, string, string][] = [
    [N.inbox, t("support.nav.overview", "Overview"), "sec-overview"],
    [N.tag, t("support.nav.queue", "Queue"), "sec-queue"],
    [N.chart, t("support.nav.reports", "Reports"), "sec-reports"],
    [N.book, t("support.nav.activity", "Activity"), "sec-activity"],
  ];
  const initials = (session.name || d.agent.name).split(" ").map((x) => x[0]).slice(0, 2).join("");
  const kpis = [[t("support.kpi.openTickets"), nf(k.openTickets), t("support.kpi.inQueue"), C.gold], [t("support.kpi.assignedToMe"), nf(k.assignedToMe), t("support.kpi.active"), C.blue], [t("support.kpi.resolvedToday"), nf(k.resolvedToday), t("support.kpi.resolvedDelta"), C.brand], [t("support.kpi.avgFirstResponse"), k.avgFirstResponse + "m", t("support.kpi.avgTarget"), C.brandDeep], [t("support.kpi.csat"), k.csat + "%", t("support.kpi.csatPeriod"), C.violet], [t("support.kpi.slaMet"), k.slaMet + "%", t("support.kpi.slaPeriod"), C.brand]];
  return (
    <div className="flex min-h-screen">
      <aside className="hidden lg:flex w-[228px] shrink-0 flex-col border-r border-line/70 bg-panel/40 px-4 py-6 relative z-10 lg:sticky lg:top-0 lg:h-screen lg:self-start lg:overflow-y-auto">
        <div className="px-1"><Logo /></div>
        <nav className="mt-9 flex flex-col gap-1"><div className="eyebrow px-3 mb-1">{t("support.nav.section")}</div>
          {nav.map(([icon, label, id]) => { const active = activeSec === id; return (<a key={id} href={`#${id}`} onClick={(e) => { e.preventDefault(); scrollToSection(id); }} aria-current={active ? "true" : undefined} className={`group flex items-center gap-3 rounded-xl px-3 py-2.5 text-sm transition ${active ? "bg-brand/[0.12] text-ink-hi shadow-[inset_0_0_0_1px_rgba(47,211,154,0.25)]" : "text-ink-mid hover:bg-white/[0.03] hover:text-ink-hi"}`}><span className={active ? "text-brand-bright" : "text-ink-lo group-hover:text-ink-mid"}><Ic d={icon} /></span>{label}{active && <span className="ml-auto h-1.5 w-1.5 rounded-full bg-gold" />}</a>); })}
        </nav>
        <div className="mt-auto card p-3.5"><div className="flex items-center gap-2 text-brand-bright"><Ic d={N.bolt} s={15} /><span className="text-xs font-semibold text-ink-hi">{t("support.sidebar.slaLabel", { n: k.slaMet })}</span></div><p className="mt-1.5 text-[11px] leading-snug text-ink-mid">{t("support.sidebar.slaTip")}</p></div>
      </aside>
      <main className="flex-1 min-w-0 relative z-10">
        <header className="sticky top-0 z-20 flex items-center gap-4 border-b border-line/60 bg-base/70 backdrop-blur-xl px-5 lg:px-7 py-4">
          <div className="min-w-0"><div className="flex items-center gap-2"><h1 className="font-display text-xl font-extrabold text-ink-hi tracking-tight">{t("support.topbar.title")}</h1><span className="chip !text-[10px] !text-gold !border-gold/30">{t("support.topbar.demo")}</span></div><p className="text-[11px] text-ink-lo mt-0.5">{t("support.topbar.subtitle")}</p></div>
          <div className="ml-auto flex items-center gap-3"><span className="chip !text-[11px] !text-brand-bright !border-brand/30 hidden md:inline-flex"><span className="h-1.5 w-1.5 rounded-full bg-brand-bright" />{t("support.topbar.csat", { n: k.csat })}</span>
            <LanguageSwitcher />
            <ThemeSwitcher />
            <div className="hidden md:flex items-center gap-2.5 rounded-xl border border-line/70 bg-panel2/50 pl-2.5 pr-2 py-1.5"><div className="grid h-7 w-7 place-items-center rounded-lg bg-brand/20 text-[11px] font-bold text-brand-bright">{initials}</div><div className="leading-tight"><div className="text-xs font-semibold text-ink-hi">{d.agent.name}</div><div className="text-[10px] text-ink-lo">{d.agent.team}</div></div><button onClick={logout} title={t("support.topbar.signOut")} className="ml-1 text-ink-lo hover:text-pink transition"><Ic d={N.out} s={15} /></button></div>
          </div>
        </header>
        <motion.div initial="initial" animate="animate" transition={{ staggerChildren: 0.05 }} className="px-5 lg:px-7 py-6 space-y-5">
          <SectionAnchor id="sec-overview" />
          <div className="grid grid-cols-2 lg:grid-cols-6 gap-4">
            {kpis.map(([label, val, sub, color], i) => (<motion.div key={i} variants={fade} className="card p-4"><div className="flex items-center justify-between"><span className="eyebrow">{label as string}</span><span className="h-1.5 w-1.5 rounded-full" style={{ background: color as string }} /></div><div className="num text-[24px] font-bold text-ink-hi leading-none mt-2">{val as string}</div><div className="text-[10px] text-ink-lo mt-1.5">{sub as string}</div></motion.div>))}
          </div>
          <SectionAnchor id="sec-queue" />
          <div className="grid grid-cols-1 xl:grid-cols-3 gap-5">
            <motion.section variants={fade} className="card p-5 xl:col-span-2">
              <div className="flex items-center justify-between mb-3"><div><h3 className="font-display text-[15px] font-bold text-ink-hi">{t("support.queue.title")}</h3><p className="text-[11px] text-ink-lo mt-0.5">{t("support.queue.subtitle")}</p></div><span className="chip !text-[10px] !text-gold !border-gold/30">{t("support.queue.openBadge", { n: k.openTickets })}</span></div>
              <div className="overflow-x-auto -mx-1"><table className="w-full text-sm min-w-[640px]">
                <thead><tr className="text-left eyebrow border-b border-line/60"><th className="py-2 pl-1 font-semibold">{t("support.queue.colId")}</th><th className="font-semibold">{t("support.queue.colSubject")}</th><th className="font-semibold">{t("support.queue.colRequester")}</th><th className="font-semibold">{t("support.queue.colPriority")}</th><th className="font-semibold">{t("support.queue.colStatus")}</th><th className="font-semibold text-right pr-1">{t("support.queue.colAge")}</th></tr></thead>
                <tbody>{d.tickets.map((t, i) => (<tr key={i} className="border-b border-line/30 hover:bg-white/[0.02] transition"><td className="py-2.5 pl-1 num text-[12px] text-ink-lo">{t.id}</td><td className="text-ink-hi font-medium max-w-[230px] truncate">{t.subject}</td><td className="text-ink-mid truncate">{t.requester}</td><td><span className={`chip !text-[10px] ${priColor[t.priority]}`}>{t.priority}</span></td><td className="text-ink-mid text-[12px]">{t.status}</td><td className="text-right pr-1 num text-[12px] text-ink-lo">{t.age}</td></tr>))}</tbody>
              </table></div>
            </motion.section>
            <motion.section variants={fade} className="card p-5">
              <h3 className="font-display text-[15px] font-bold text-ink-hi">{t("support.sla.title")}</h3><p className="text-[11px] text-ink-lo mt-0.5">{t("support.sla.subtitle")}</p>
              <div className="h-[120px]"><Chart option={gauge(k.slaMet)} height={120} /></div>
              {[[t("support.sla.p1"), d.priority.p1, "#FF7E92"], [t("support.sla.p2"), d.priority.p2, "#E8B14C"], [t("support.sla.p3"), d.priority.p3, "#2FD39A"]].map(([l, v, c], i) => (<div key={i} className="mb-2"><div className="flex justify-between text-xs mb-1"><span className="text-ink-mid">{l as string}</span><span className="num text-ink-hi">{v as number}</span></div><div className="h-2 rounded-full bg-panel2/70 overflow-hidden"><div className="h-full rounded-full" style={{ width: ((v as number) / d.priority.open) * 100 + "%", background: c as string }} /></div></div>))}
            </motion.section>
          </div>
          <SectionAnchor id="sec-reports" />
          <div className="grid grid-cols-1 xl:grid-cols-3 gap-5">
            <motion.section variants={fade} className="card p-5 xl:col-span-2"><h3 className="font-display text-[15px] font-bold text-ink-hi">{t("support.volume.title")}</h3><p className="text-[11px] text-ink-lo mt-0.5">{t("support.volume.subtitle")}</p><Chart option={volumeOption(d.volume)} height={230} /></motion.section>
            <motion.section variants={fade} className="card p-5"><h3 className="font-display text-[15px] font-bold text-ink-hi">{t("support.category.title")}</h3><p className="text-[11px] text-ink-lo mt-0.5">{t("support.category.subtitle")}</p><div className="mt-1"><Chart option={catOption(d.byCategory)} height={210} /></div></motion.section>
          </div>
          <SectionAnchor id="sec-activity" />
          <div className="grid grid-cols-1 xl:grid-cols-3 gap-5">
            <motion.section variants={fade} className="card p-5 xl:col-span-2">
              <h3 className="font-display text-[15px] font-bold text-ink-hi mb-3">{t("support.leaderboard.title")}</h3>
              <div className="space-y-2">{d.leaderboard.map((a, i) => (<div key={i} className="flex items-center gap-3 rounded-xl border border-line/40 bg-panel2/30 px-3 py-2.5"><span className="num text-sm text-ink-lo w-5">{i + 1}</span><div className="grid h-7 w-7 place-items-center rounded-lg bg-brand/15 text-[11px] font-bold text-brand-bright">{a.name.split(" ").map((x) => x[0]).slice(0, 2).join("")}</div><span className="text-sm text-ink-hi flex-1">{a.name}</span><span className="num text-[12px] text-ink-mid">{t("support.leaderboard.resolved", { n: a.resolved })}</span><span className="chip !text-[10px] !text-brand-bright !border-brand/30">{t("support.leaderboard.csat", { n: a.csat })}</span></div>))}</div>
            </motion.section>
            <motion.section variants={fade} className="card p-5"><h3 className="font-display text-[15px] font-bold text-ink-hi mb-3">{t("support.activity.title")}</h3><div className="space-y-3">{d.activity.map((e, i) => (<div key={i} className="flex gap-3"><span className="mt-1 h-2 w-2 shrink-0 rounded-full bg-brand/60" /><div><div className="text-[13px] text-ink-mid leading-snug">{e.text}</div><div className="text-[10px] text-ink-lo mt-0.5 num">{e.when}</div></div></div>))}</div></motion.section>
          </div>
          <footer className="flex flex-wrap items-center justify-between gap-2 pt-1 pb-4 text-[11px] text-ink-lo"><span>{t("support.footer.brand")}</span><span>{t("support.footer.disclaimer")}</span></footer>
        </motion.div>
      </main>
    </div>
  );
}

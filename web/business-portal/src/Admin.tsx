import { useEffect, useState } from "react";
import { motion } from "framer-motion";
import * as echarts from "echarts";
import { Chart, donutOption, cityOption, nf, compact, curC, C } from "@illumin360/ui";
import { logout, type Session } from "./auth";
import { useTranslation } from "react-i18next";
import { LanguageSwitcher, ThemeSwitcher } from "@illumin360/ui";

interface AdminData {
  kpis: { totalUsers: number; talent: number; companies: number; mrr: number; arr: number; activeSubs: number; uptime: number; openTickets: number; ticketsResolved: number; pendingVerifications: number; dau: number; dauDelta: number };
  monthly: { month: string; talent: number; companies: number; mrr: number; activeSubs: number; applications: number }[];
  segments: { name: string; value: number }[];
  byCity: { city: string; value: number }[];
  services: { name: string; status: string; latency: number; uptime: number }[];
  verifications: { entity: string; kind: string; submitted: string; risk: string }[];
  events: { text: string; who: string; when: string }[];
  tickets: { open: number; p1: number; p2: number; p3: number; slaOk: number };
}

const Ic = ({ d, s = 18, w = 1.7 }: { d: React.ReactNode; s?: number; w?: number }) => (
  <svg width={s} height={s} viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth={w} strokeLinecap="round" strokeLinejoin="round">{d}</svg>
);
const ICN = {
  grid: <path d="M3 3h7v7H3zM14 3h7v7h-7zM14 14h7v7h-7zM3 14h7v7H3z" />,
  users: <path d="M16 11a4 4 0 1 0-4-4 4 4 0 0 0 4 4zM2 21a7 7 0 0 1 14 0M19 21a5 5 0 0 0-6-4.9" />,
  cash: <path d="M3 6h18v12H3zM12 15a3 3 0 1 0 0-6 3 3 0 0 0 0 6zM6 9v.01M18 15v.01" />,
  shield: <path d="M12 3l8 3v6c0 5-3.5 8-8 9-4.5-1-8-4-8-9V6z M9 12l2 2 4-4" />,
  server: <path d="M3 4h18v6H3zM3 14h18v6H3zM7 7v.01M7 17v.01" />,
  gear: <path d="M12 15a3 3 0 1 0 0-6 3 3 0 0 0 0 6zM19.4 13a7.5 7.5 0 0 0 0-2l2-1.5-2-3.5-2.4 1a7 7 0 0 0-1.7-1L14.5 2.5h-5L9.2 5a7 7 0 0 0-1.7 1l-2.4-1-2 3.5L5.1 11a7.5 7.5 0 0 0 0 2l-2 1.5 2 3.5 2.4-1a7 7 0 0 0 1.7 1l.3 2.5h5l.3-2.5a7 7 0 0 0 1.7-1l2.4 1 2-3.5z" />,
  out: <path d="M16 17l5-5-5-5M21 12H9M9 21H5a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h4" />,
  bolt: <path d="M13 2 3 14h7l-1 8 10-12h-7z" />,
};
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
        <div className="text-[9px] uppercase tracking-[0.22em] text-ink-lo mt-0.5">Admin</div>
      </div>
    </div>
  );
}

function ml(i: number, cats: string[]) { const m = cats[i]; return m && m.endsWith("-01") ? m.slice(0, 4) : ""; }
function lg(top: string) { return new echarts.graphic.LinearGradient(0, 0, 0, 1, [{ offset: 0, color: top }, { offset: 1, color: top + "00" }]); }
const axis = { axisLine: { lineStyle: { color: C.line } }, axisTick: { show: false }, axisLabel: { color: C.text, fontFamily: "JetBrains Mono", fontSize: 10 }, splitLine: { lineStyle: { color: C.grid } } } as const;
const tt = { trigger: "axis", backgroundColor: "#0B1A14", borderColor: C.line, textStyle: { color: "#E8F2EC", fontFamily: "Hanken Grotesk", fontSize: 12 } } as const;

function growthOption(m: AdminData["monthly"]): echarts.EChartsOption {
  const cats = m.map((d) => d.month);
  return {
    grid: { left: 46, right: 16, top: 24, bottom: 26 },
    legend: { data: ["Talent", "Companies"], top: 0, right: 0, textStyle: { color: C.textHi, fontFamily: "Hanken Grotesk" }, icon: "roundRect", itemWidth: 10, itemHeight: 10 },
    tooltip: { ...tt, valueFormatter: (v) => nf(v as number) },
    xAxis: { type: "category", data: cats, boundaryGap: false, ...axis, axisLabel: { ...axis.axisLabel, formatter: (_: string, i: number) => ml(i, cats) } },
    yAxis: { type: "value", ...axis, axisLabel: { ...axis.axisLabel, formatter: (v: number) => compact(v) } },
    series: [
      { name: "Talent", type: "line", stack: "u", data: m.map((d) => d.talent), smooth: true, showSymbol: false, lineStyle: { color: C.brand, width: 2 }, areaStyle: { color: lg("#2FD39A"), opacity: 0.7 } },
      { name: "Companies", type: "line", stack: "u", data: m.map((d) => d.companies), smooth: true, showSymbol: false, lineStyle: { color: C.blue, width: 2 }, areaStyle: { color: lg("#5AB7FF"), opacity: 0.6 } },
    ],
  };
}
function mrrOption(m: AdminData["monthly"]): echarts.EChartsOption {
  const cats = m.map((d) => d.month);
  return {
    grid: { left: 50, right: 16, top: 16, bottom: 26 },
    tooltip: { ...tt, valueFormatter: (v) => curC(v as number) },
    xAxis: { type: "category", data: cats, boundaryGap: false, ...axis, axisLabel: { ...axis.axisLabel, formatter: (_: string, i: number) => ml(i, cats) } },
    yAxis: { type: "value", ...axis, axisLabel: { ...axis.axisLabel, formatter: (v: number) => curC(v) } },
    series: [{ name: "MRR", type: "line", data: m.map((d) => d.mrr), smooth: true, showSymbol: false, lineStyle: { color: C.gold, width: 2.5, shadowColor: "rgba(232,177,76,0.4)", shadowBlur: 12 }, areaStyle: { color: lg("#E8B14C"), opacity: 0.45 } }],
  };
}

export default function Admin({ session }: { session: Session }) {
  const { t } = useTranslation();
  const [d, setD] = useState<AdminData | null>(null);
  const [talentTotal, setTalentTotal] = useState<number | null>(null);
  const [liveByCity, setLiveByCity] = useState<{ city: string; value: number }[] | null>(null);
  const [liveVers, setLiveVers] = useState<{ id: string; entity: string; kind: string; risk: string; submitted: string; status: string }[] | null>(null);
  const [liveTickets, setLiveTickets] = useState<{ id: string; subject: string; priority: string; requester: string; status: string; assignee: string | null }[] | null>(null);
  const [liveAccounts, setLiveAccounts] = useState<{ id: string; name: string; kind: string; email: string; status: string }[] | null>(null);
  const [pipelineReqs, setPipelineReqs] = useState<{ id: string; title: string; city: string }[] | null>(null);
  const [pipelineReqId, setPipelineReqId] = useState<string | null>(null);
  const [pipelineApps, setPipelineApps] = useState<{ id: string; talentType: string; matchScore: number; status: string }[] | null>(null);
  // Recruiter CRM (clients + contacts).
  type CrmClient = { id: string; name: string; industry: string | null; city: string | null; status: string; contactCount: number };
  type CrmContact = { id: string; name: string; title: string | null; email: string | null; phone: string | null; isPrimary: boolean };
  const [clients, setClients] = useState<CrmClient[] | null>(null);
  const [clientFilter, setClientFilter] = useState<string>("");
  const [selClient, setSelClient] = useState<string | null>(null);
  const [contacts, setContacts] = useState<CrmContact[] | null>(null);
  const [newClient, setNewClient] = useState({ name: "", industry: "", city: "" });
  const [newContact, setNewContact] = useState({ name: "", title: "", email: "", phone: "", isPrimary: false });
  // Offers (per selected pipeline application).
  type Offer = { id: string; title: string; salaryAmount: number; currency: string; startDate: string; status: string };
  const [offerAppId, setOfferAppId] = useState<string | null>(null);
  const [offers, setOffers] = useState<Offer[] | null>(null);
  const [newOffer, setNewOffer] = useState({ title: "", salaryAmount: "", startDate: "" });
  useEffect(() => { fetch(import.meta.env.BASE_URL + "admin.json").then((r) => r.json()).then(setD); }, []);
  // Live platform signals from the microservices (via BFF → gateway). Talent count and talent-by-city are
  // real (Candidates service); finance/ops tiles (MRR, subscriptions, tickets, verifications) have no backing
  // service yet, so they remain snapshot-driven. The LIVE chip reflects the live talent aggregate.
  useEffect(() => {
    fetch("/api/candidates/stats")
      .then((r) => (r.ok ? r.json() : null))
      .then((s: { total?: number; byCity?: { label: string; count: number }[] } | null) => {
        if (!s) return;
        if (typeof s.total === "number") setTalentTotal(s.total);
        if (Array.isArray(s.byCity)) setLiveByCity(s.byCity.map((x) => ({ city: x.label, value: x.count })));
      })
      .catch(() => { /* stack offline — keep snapshot */ });
  }, []);
  // Live verification queue from the Admin service (admin-role gated). Non-admins get 403 → snapshot.
  useEffect(() => {
    fetch("/api/admin/verifications", { credentials: "same-origin" })
      .then((r) => (r.ok ? r.json() : null))
      .then((v) => { if (Array.isArray(v)) setLiveVers(v); })
      .catch(() => { /* not authorized / offline — keep snapshot */ });
    fetch("/api/admin/tickets", { credentials: "same-origin" })
      .then((r) => (r.ok ? r.json() : null))
      .then((v) => { if (Array.isArray(v)) setLiveTickets(v); })
      .catch(() => { /* keep snapshot */ });
    fetch("/api/admin/accounts", { credentials: "same-origin" })
      .then((r) => (r.ok ? r.json() : null))
      .then((v) => { if (Array.isArray(v)) setLiveAccounts(v); })
      .catch(() => { /* keep snapshot */ });
  }, []);
  // Recruitment application pipeline (kanban): open roles + the selected role's applications.
  useEffect(() => {
    fetch("/api/recruitment/requests?status=open&pageSize=8")
      .then((r) => (r.ok ? r.json() : null))
      .then((v: { id: string; title: string; city: string }[] | null) => {
        if (Array.isArray(v) && v.length > 0) { setPipelineReqs(v.map((x) => ({ id: x.id, title: x.title, city: x.city }))); setPipelineReqId(v[0].id); }
      })
      .catch(() => { /* recruitment offline */ });
  }, []);
  useEffect(() => {
    if (!pipelineReqId) return;
    fetch(`/api/recruitment/requests/${pipelineReqId}/applications`)
      .then((r) => (r.ok ? r.json() : null))
      .then((v) => { if (Array.isArray(v)) setPipelineApps(v); })
      .catch(() => { /* keep empty */ });
  }, [pipelineReqId]);
  // Recruiter CRM: client list (re-fetched on status-filter change).
  useEffect(() => {
    fetch("/api/recruitment/clients" + (clientFilter ? `?status=${clientFilter}` : ""))
      .then((r) => (r.ok ? r.json() : null))
      .then((v) => { if (Array.isArray(v)) setClients(v); })
      .catch(() => { /* recruitment offline */ });
  }, [clientFilter]);
  // Selected client's contacts.
  useEffect(() => {
    if (!selClient) { setContacts(null); return; }
    fetch(`/api/recruitment/clients/${selClient}`)
      .then((r) => (r.ok ? r.json() : null))
      .then((v) => { if (v?.contacts) setContacts(v.contacts); })
      .catch(() => { /* keep empty */ });
  }, [selClient]);
  // Offers for the application selected on a pipeline card.
  useEffect(() => {
    if (!offerAppId) { setOffers(null); return; }
    fetch(`/api/recruitment/applications/${offerAppId}/offers`)
      .then((r) => (r.ok ? r.json() : null))
      .then((v) => { if (Array.isArray(v)) setOffers(v); })
      .catch(() => { /* keep empty */ });
  }, [offerAppId]);
  if (!d) return <div className="grid place-items-center h-screen text-ink-mid font-mono text-sm animate-pulse">{t("admin.loading")}</div>;

  const k = d.kpis;
  const live = talentTotal !== null;
  // Verification queue: live (from the Admin service) when authorized, else the bundled snapshot.
  const vers: { id?: string; entity: string; kind: string; risk: string; submitted: string }[] = liveVers ?? d.verifications;
  const versLive = liveVers !== null;
  const decide = async (id: string, action: "approve" | "reject") => {
    const r = await fetch(`/api/admin/verifications/${id}/${action}`, { method: "POST", credentials: "same-origin" });
    if (r.ok) setLiveVers((prev) => (prev ? prev.filter((v) => v.id !== id) : prev));
  };
  const assignTicket = async (id: string) => {
    const r = await fetch(`/api/admin/tickets/${id}/assign`, { method: "POST", credentials: "same-origin" });
    if (r.ok) {
      const updated = await r.json().catch(() => null);
      setLiveTickets((prev) => (prev ? prev.map((tk) => (tk.id === id ? { ...tk, status: updated?.status ?? "assigned", assignee: updated?.assignee ?? tk.assignee } : tk)) : prev));
    }
  };
  const resolveTicket = async (id: string) => {
    const r = await fetch(`/api/admin/tickets/${id}/resolve`, { method: "POST", credentials: "same-origin" });
    if (r.ok) setLiveTickets((prev) => (prev ? prev.filter((tk) => tk.id !== id) : prev));
  };
  const setAccount = async (id: string, action: "suspend" | "activate") => {
    const r = await fetch(`/api/admin/accounts/${id}/${action}`, { method: "POST", credentials: "same-origin" });
    if (r.ok) {
      const updated = await r.json().catch(() => null);
      setLiveAccounts((prev) => (prev ? prev.map((a) => (a.id === id ? { ...a, status: updated?.status ?? (action === "suspend" ? "suspended" : "active") } : a)) : prev));
    }
  };
  const transitionApp = async (id: string, action: "advance" | "reject") => {
    const r = await fetch(`/api/recruitment/applications/${id}/${action}`, { method: "POST", credentials: "same-origin" });
    if (r.ok) {
      const u = await r.json().catch(() => null);
      setPipelineApps((prev) => (prev ? prev.map((a) => (a.id === id ? { ...a, status: u?.status ?? (action === "reject" ? "rejected" : a.status) } : a)) : prev));
    }
  };
  const createClient = async () => {
    if (!newClient.name.trim()) return;
    const r = await fetch("/api/recruitment/clients", { method: "POST", headers: { "Content-Type": "application/json" }, credentials: "same-origin", body: JSON.stringify({ name: newClient.name, industry: newClient.industry || null, city: newClient.city || null, notes: null }) });
    if (r.ok) {
      const c: CrmClient = await r.json();
      setClients((cs) => [c, ...(cs ?? [])]);
      setNewClient({ name: "", industry: "", city: "" });
    }
  };
  const changeClientStatus = async (id: string, status: string) => {
    const r = await fetch(`/api/recruitment/clients/${id}/status`, { method: "POST", headers: { "Content-Type": "application/json" }, credentials: "same-origin", body: JSON.stringify({ status }) });
    if (r.ok) setClients((cs) => cs?.map((c) => (c.id === id ? { ...c, status } : c)) ?? cs);
  };
  const addContact = async () => {
    if (!selClient || !newContact.name.trim()) return;
    const r = await fetch(`/api/recruitment/clients/${selClient}/contacts`, { method: "POST", headers: { "Content-Type": "application/json" }, credentials: "same-origin", body: JSON.stringify({ name: newContact.name, title: newContact.title || null, email: newContact.email || null, phone: newContact.phone || null, isPrimary: newContact.isPrimary }) });
    if (r.ok) {
      const c: CrmContact = await r.json();
      setContacts((cs) => [...(cs ?? []), c]);
      setClients((cs) => cs?.map((cl) => (cl.id === selClient ? { ...cl, contactCount: cl.contactCount + 1 } : cl)) ?? cs);
      setNewContact({ name: "", title: "", email: "", phone: "", isPrimary: false });
    }
  };
  const removeContact = async (contactId: string) => {
    if (!selClient) return;
    const r = await fetch(`/api/recruitment/clients/${selClient}/contacts/${contactId}`, { method: "DELETE", credentials: "same-origin" });
    if (r.ok) {
      setContacts((cs) => cs?.filter((c) => c.id !== contactId) ?? cs);
      setClients((cs) => cs?.map((cl) => (cl.id === selClient ? { ...cl, contactCount: Math.max(0, cl.contactCount - 1) } : cl)) ?? cs);
    }
  };
  const createAndSendOffer = async () => {
    if (!offerAppId || !newOffer.title.trim() || !newOffer.salaryAmount || !newOffer.startDate) return;
    const r = await fetch(`/api/recruitment/applications/${offerAppId}/offers`, { method: "POST", headers: { "Content-Type": "application/json" }, credentials: "same-origin", body: JSON.stringify({ title: newOffer.title, salaryAmount: Number(newOffer.salaryAmount), currency: "NAD", startDate: newOffer.startDate, notes: null }) });
    if (!r.ok) return;
    const o: Offer = await r.json();
    // Immediately extend it to the candidate.
    const sent = await fetch(`/api/recruitment/offers/${o.id}/send`, { method: "POST", credentials: "same-origin" });
    const finalOffer: Offer = sent.ok ? await sent.json().catch(() => ({ ...o, status: "sent" })) : o;
    setOffers((os) => [finalOffer, ...(os ?? [])]);
    setNewOffer({ title: "", salaryAmount: "", startDate: "" });
  };
  const withdrawOffer = async (id: string) => {
    const r = await fetch(`/api/recruitment/offers/${id}/withdraw`, { method: "POST", credentials: "same-origin" });
    if (r.ok) { const u = await r.json().catch(() => null); setOffers((os) => os?.map((o) => (o.id === id ? { ...o, status: u?.status ?? "withdrawn" } : o)) ?? os); }
  };
  const pipelineStages = ["applied", "reviewed", "shortlisted", "hired", "rejected"];
  const degraded = d.services.filter((s) => s.status !== "operational").length;
  const nav: [React.ReactNode, string, boolean][] = [[ICN.grid, t("admin.nav.overview"), true], [ICN.users, t("admin.nav.users"), false], [ICN.cash, t("admin.nav.revenue"), false], [ICN.shield, t("admin.nav.moderation"), false], [ICN.server, t("admin.nav.system"), false], [ICN.gear, t("admin.nav.settings"), false]];
  const initials = (session.name || "Admin").split(" ").map((x) => x[0]).slice(0, 2).join("");
  const kpiCards = [
    [t("admin.kpi.totalUsers"), nf(live ? talentTotal! + k.companies : k.totalUsers), t("admin.kpi.dauDelta", { delta: k.dauDelta }), C.brand],
    [t("admin.kpi.companies"), nf(k.companies), t("admin.kpi.companiesSub"), C.blue],
    [t("admin.kpi.mrr"), curC(k.mrr), t("admin.kpi.arrSub", { arr: curC(k.arr) }), C.gold],
    [t("admin.kpi.activeSubs"), nf(k.activeSubs), t("admin.kpi.activeSubsSub"), C.brandDeep],
    [t("admin.kpi.uptime"), k.uptime + "%", degraded ? t("admin.kpi.uptimeDegraded", { n: degraded }) : t("admin.kpi.uptimeHealthy"), C.violet],
    [t("admin.kpi.openTickets"), nf(k.openTickets), t("admin.kpi.ticketsResolved", { n: k.ticketsResolved }), C.gold],
  ];

  return (
    <div className="flex min-h-screen">
      <aside className="hidden lg:flex w-[228px] shrink-0 flex-col border-r border-line/70 bg-panel/40 px-4 py-6 relative z-10">
        <div className="px-1"><Logo /></div>
        <nav className="mt-9 flex flex-col gap-1">
          <div className="eyebrow px-3 mb-1">{t("admin.nav.platform")}</div>
          {nav.map(([icon, label, active]) => (
            <a key={label} href="#" className={`group flex items-center gap-3 rounded-xl px-3 py-2.5 text-sm transition ${active ? "bg-brand/[0.12] text-ink-hi shadow-[inset_0_0_0_1px_rgba(47,211,154,0.25)]" : "text-ink-mid hover:bg-white/[0.03] hover:text-ink-hi"}`}>
              <span className={active ? "text-brand-bright" : "text-ink-lo group-hover:text-ink-mid"}><Ic d={icon} /></span>{label}
              {active && <span className="ml-auto h-1.5 w-1.5 rounded-full bg-gold" />}
            </a>
          ))}
        </nav>
        <div className="mt-auto card p-3.5">
          <div className="flex items-center gap-2 text-brand-bright"><Ic d={ICN.bolt} s={15} /><span className="text-xs font-semibold text-ink-hi">{t("admin.sidebar.online", { n: nf(k.dau) })}</span></div>
          <p className="mt-1.5 text-[11px] leading-snug text-ink-mid">{t("admin.sidebar.onlineSub")}</p>
        </div>
      </aside>

      <main className="flex-1 min-w-0 relative z-10">
        <header className="sticky top-0 z-20 flex items-center gap-4 border-b border-line/60 bg-base/70 backdrop-blur-xl px-5 lg:px-7 py-4">
          <div className="min-w-0">
            <div className="flex items-center gap-2">
              <h1 className="font-display text-xl font-extrabold text-ink-hi tracking-tight">{t("admin.topbar.title")}</h1>
              {live ? <span className="chip !text-[10px] !text-brand-bright !border-brand/30"><span className="h-1.5 w-1.5 rounded-full bg-brand-bright animate-pulse" /> LIVE</span> : <span className="chip !text-[10px] !text-gold !border-gold/30">{t("admin.topbar.demo")}</span>}
            </div>
            <p className="text-[11px] text-ink-lo mt-0.5">{t("admin.topbar.subtitle")}</p>
          </div>
          <div className="ml-auto flex items-center gap-3">
            <span className={`chip !text-[11px] ${degraded ? "!text-gold !border-gold/30" : "!text-brand-bright !border-brand/30"}`}>
              <span className={`h-1.5 w-1.5 rounded-full ${degraded ? "bg-gold animate-pulse" : "bg-brand-bright"}`} />
              {degraded ? t("admin.topbar.serviceDegraded", { n: degraded }) : t("admin.topbar.allOperational")}
            </span>
            <LanguageSwitcher />
            <ThemeSwitcher />
            <div className="hidden md:flex items-center gap-2.5 rounded-xl border border-line/70 bg-panel2/50 pl-2.5 pr-2 py-1.5">
              <div className="grid h-7 w-7 place-items-center rounded-lg bg-brand/20 text-[11px] font-bold text-brand-bright">{initials}</div>
              <div className="leading-tight"><div className="text-xs font-semibold text-ink-hi">{session.name}</div><div className="text-[10px] text-ink-lo">{t("admin.topbar.role")}</div></div>
              <button onClick={logout} title={t("admin.topbar.signOut")} className="ml-1 text-ink-lo hover:text-pink transition"><Ic d={ICN.out} s={15} /></button>
            </div>
          </div>
        </header>

        <motion.div initial="initial" animate="animate" transition={{ staggerChildren: 0.05 }} className="px-5 lg:px-7 py-6 space-y-5">
          <div className="grid grid-cols-2 lg:grid-cols-6 gap-4">
            {kpiCards.map(([label, val, sub, color], i) => (
              <motion.div key={i} variants={fade} className="card p-4">
                <div className="flex items-center justify-between"><span className="eyebrow">{label as string}</span><span className="h-1.5 w-1.5 rounded-full" style={{ background: color as string }} /></div>
                <div className="num text-[24px] font-bold text-ink-hi leading-none mt-2">{val as string}</div>
                <div className="text-[10px] text-ink-lo mt-1.5">{sub as string}</div>
              </motion.div>
            ))}
          </div>

          <div className="grid grid-cols-1 xl:grid-cols-3 gap-5">
            <motion.section variants={fade} className="card p-5 xl:col-span-2">
              <h3 className="font-display text-[15px] font-bold text-ink-hi">{t("admin.panel.growth")}</h3>
              <p className="text-[11px] text-ink-lo mt-0.5">{t("admin.panel.growthSub")}</p>
              <Chart option={growthOption(d.monthly)} height={280} />
            </motion.section>
            <motion.section variants={fade} className="card p-5">
              <h3 className="font-display text-[15px] font-bold text-ink-hi">{t("admin.panel.segments")}</h3>
              <p className="text-[11px] text-ink-lo mt-0.5">{t("admin.panel.segmentsSub")}</p>
              <Chart option={donutOption(d.segments, [C.brand, C.gold, C.blue])} height={280} />
            </motion.section>
          </div>

          <div className="grid grid-cols-1 xl:grid-cols-3 gap-5">
            <motion.section variants={fade} className="card p-5 xl:col-span-2">
              <h3 className="font-display text-[15px] font-bold text-ink-hi">{t("admin.panel.revenue")}</h3>
              <p className="text-[11px] text-ink-lo mt-0.5">{t("admin.panel.revenueSub")}</p>
              <Chart option={mrrOption(d.monthly)} height={230} />
            </motion.section>
            <motion.section variants={fade} className="card p-5">
              <div className="flex items-center justify-between mb-2"><h3 className="font-display text-[15px] font-bold text-ink-hi">{t("admin.panel.health")}</h3><span className="chip !text-[10px]">{t("admin.panel.healthLive", { n: compact(k.dau) })}</span></div>
              <div className="space-y-2">
                {d.services.map((s, i) => {
                  const ok = s.status === "operational";
                  return (
                    <div key={i} className="flex items-center gap-3 text-sm">
                      <span className={`h-2 w-2 rounded-full ${ok ? "bg-brand-bright" : "bg-gold animate-pulse"}`} />
                      <span className="text-ink-hi flex-1 truncate">{s.name}</span>
                      <span className="num text-[11px] text-ink-mid">{s.latency}ms</span>
                      <span className={`num text-[11px] ${ok ? "text-ink-lo" : "text-gold"}`}>{s.uptime}%</span>
                    </div>
                  );
                })}
              </div>
            </motion.section>
          </div>

          <div className="grid grid-cols-1 xl:grid-cols-3 gap-5">
            <motion.section variants={fade} className="card p-5 xl:col-span-2">
              <div className="flex items-center justify-between mb-3"><div><h3 className="font-display text-[15px] font-bold text-ink-hi">{t("admin.panel.verifications")}</h3><p className="text-[11px] text-ink-lo mt-0.5">{t("admin.panel.verificationsSub", { n: vers.length })}</p></div>{versLive ? <span className="chip !text-[10px] !text-brand-bright !border-brand/30"><span className="h-1.5 w-1.5 rounded-full bg-brand-bright animate-pulse" /> LIVE · {vers.length}</span> : <span className="chip !text-[10px] !text-gold !border-gold/30">{t("admin.panel.verificationsChip", { n: k.pendingVerifications })}</span>}</div>
              <table className="w-full text-sm">
                <thead><tr className="text-left eyebrow border-b border-line/60"><th className="py-2 pl-1 font-semibold">{t("admin.table.entity")}</th><th className="font-semibold">{t("admin.table.type")}</th><th className="font-semibold">{t("admin.table.submitted")}</th><th className="font-semibold">{t("admin.table.risk")}</th><th className="font-semibold text-right pr-1">{t("admin.table.action")}</th></tr></thead>
                <tbody>
                  {vers.length === 0 && (
                    <tr><td colSpan={5} className="py-6 text-center text-ink-lo text-[12px]">{t("admin.table.queueClear", "Queue clear — no pending verifications.")}</td></tr>
                  )}
                  {vers.map((v, i) => (
                    <tr key={v.id ?? i} className="border-b border-line/30">
                      <td className="py-2.5 pl-1 text-ink-hi font-medium">{v.entity}</td>
                      <td className="text-ink-mid">{v.kind}</td>
                      <td className="text-ink-lo num text-[12px]">{v.submitted}</td>
                      <td><span className={`chip !text-[10px] ${v.risk === "Medium" ? "!text-gold !border-gold/30" : "!text-brand-bright !border-brand/30"}`}>{v.risk}</span></td>
                      <td className="text-right pr-1">
                        {versLive ? (
                          <div className="inline-flex gap-1.5">
                            <button onClick={() => decide(v.id!, "approve")} className="rounded-lg bg-brand/15 px-2.5 py-1 text-[11px] font-semibold text-brand-bright hover:bg-brand/25 transition">{t("admin.table.approve", "Approve")}</button>
                            <button onClick={() => decide(v.id!, "reject")} className="rounded-lg bg-pink/15 px-2.5 py-1 text-[11px] font-semibold text-pink hover:bg-pink/25 transition">{t("admin.table.reject", "Reject")}</button>
                          </div>
                        ) : (
                          <button className="rounded-lg bg-brand/15 px-2.5 py-1 text-[11px] font-semibold text-brand-bright hover:bg-brand/25 transition">{t("admin.table.review")}</button>
                        )}
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </motion.section>

            <motion.section variants={fade} className="card p-5">
              <h3 className="font-display text-[15px] font-bold text-ink-hi mb-3">{t("admin.panel.audit")}</h3>
              <div className="space-y-3">
                {d.events.map((e, i) => (
                  <div key={i} className="flex gap-3">
                    <span className="mt-1 h-2 w-2 shrink-0 rounded-full bg-brand/60" />
                    <div className="min-w-0">
                      <div className="text-[13px] text-ink-mid leading-snug">{e.text}</div>
                      <div className="text-[10px] text-ink-lo mt-0.5 num">{e.who} · {e.when}</div>
                    </div>
                  </div>
                ))}
              </div>
            </motion.section>
          </div>

          <div className="grid grid-cols-1 xl:grid-cols-3 gap-5">
            <motion.section variants={fade} className="card p-5 xl:col-span-2">
              <h3 className="font-display text-[15px] font-bold text-ink-hi">{t("admin.panel.byRegion")}</h3>
              <p className="text-[11px] text-ink-lo mt-0.5">{t("admin.panel.byRegionSub")}</p>
              <div className="mt-1"><Chart option={cityOption(liveByCity ?? d.byCity)} height={230} /></div>
            </motion.section>
            <motion.section variants={fade} className="card p-5">
              <div className="flex items-center justify-between mb-1"><h3 className="font-display text-[15px] font-bold text-ink-hi">{t("admin.panel.support")}</h3>{liveTickets !== null && <span className="chip !text-[10px] !text-brand-bright !border-brand/30"><span className="h-1.5 w-1.5 rounded-full bg-brand-bright animate-pulse" /> LIVE · {liveTickets.length}</span>}</div>
              <p className="text-[11px] text-ink-lo mt-0.5 mb-3">{t("admin.panel.supportSub")}</p>
              {liveTickets !== null ? (
                <div className="space-y-2">
                  {liveTickets.length === 0 && <div className="py-4 text-center text-ink-lo text-[12px]">{t("admin.tickets.queueClear", "No open tickets.")}</div>}
                  {liveTickets.map((tk) => (
                    <div key={tk.id} className="flex items-center gap-2 rounded-xl border border-line/60 bg-panel2/40 px-3 py-2">
                      <span className={`chip !text-[9px] ${tk.priority === "P1" ? "!text-pink !border-pink/30" : tk.priority === "P2" ? "!text-gold !border-gold/30" : "!text-brand-bright !border-brand/30"}`}>{tk.priority}</span>
                      <div className="min-w-0 flex-1"><div className="text-[12px] text-ink-hi truncate">{tk.subject}</div><div className="text-[10px] text-ink-lo truncate">{tk.requester}{tk.assignee ? ` · ${t("admin.tickets.assignedTo", "assigned to")} ${tk.assignee}` : ""}</div></div>
                      <div className="shrink-0 inline-flex gap-1.5">
                        {tk.status !== "assigned" && <button onClick={() => assignTicket(tk.id)} className="rounded-lg bg-panel2/70 px-2.5 py-1 text-[11px] font-semibold text-ink-mid hover:text-ink-hi transition">{t("admin.tickets.assign", "Assign to me")}</button>}
                        <button onClick={() => resolveTicket(tk.id)} className="rounded-lg bg-brand/15 px-2.5 py-1 text-[11px] font-semibold text-brand-bright hover:bg-brand/25 transition">{t("admin.tickets.resolve", "Resolve")}</button>
                      </div>
                    </div>
                  ))}
                </div>
              ) : (
                <>
                  {[[t("admin.tickets.p1"), d.tickets.p1, "#FF7E92"], [t("admin.tickets.p2"), d.tickets.p2, "#E8B14C"], [t("admin.tickets.p3"), d.tickets.p3, "#2FD39A"]].map(([l, v, c], i) => (
                    <div key={i} className="mb-2.5">
                      <div className="flex justify-between text-xs mb-1"><span className="text-ink-mid">{l as string}</span><span className="num text-ink-hi">{v as number}</span></div>
                      <div className="h-2 rounded-full bg-panel2/70 overflow-hidden"><div className="h-full rounded-full" style={{ width: ((v as number) / d.tickets.open) * 100 + "%", background: c as string }} /></div>
                    </div>
                  ))}
                  <div className="mt-3 rounded-xl bg-brand/[0.08] border border-brand/20 p-3 text-[11px] text-ink-mid"><span className="text-brand-bright font-semibold">{t("admin.tickets.slaHighlight", { pct: d.tickets.slaOk })}</span> {t("admin.tickets.slaMet")}</div>
                </>
              )}
            </motion.section>
          </div>

          {liveAccounts !== null && (
            <div className="grid grid-cols-1 gap-5">
              <motion.section variants={fade} className="card p-5">
                <div className="flex items-center justify-between mb-3">
                  <div><h3 className="font-display text-[15px] font-bold text-ink-hi">{t("admin.panel.users", "User management")}</h3><p className="text-[11px] text-ink-lo mt-0.5">{t("admin.panel.usersSub", "Suspend or reactivate platform accounts.")}</p></div>
                  <span className="chip !text-[10px] !text-brand-bright !border-brand/30"><span className="h-1.5 w-1.5 rounded-full bg-brand-bright animate-pulse" /> LIVE · {liveAccounts.length}</span>
                </div>
                <table className="w-full text-sm">
                  <thead><tr className="text-left eyebrow border-b border-line/60"><th className="py-2 pl-1 font-semibold">{t("admin.users.name", "Account")}</th><th className="font-semibold">{t("admin.users.kind", "Type")}</th><th className="font-semibold">{t("admin.users.email", "Email")}</th><th className="font-semibold">{t("admin.users.status", "Status")}</th><th className="font-semibold text-right pr-1">{t("admin.table.action")}</th></tr></thead>
                  <tbody>
                    {liveAccounts.map((a) => (
                      <tr key={a.id} className="border-b border-line/30">
                        <td className="py-2.5 pl-1 text-ink-hi font-medium">{a.name}</td>
                        <td className="text-ink-mid">{a.kind}</td>
                        <td className="text-ink-lo text-[12px]">{a.email}</td>
                        <td><span className={`chip !text-[10px] ${a.status === "suspended" ? "!text-pink !border-pink/30" : "!text-brand-bright !border-brand/30"}`}>{a.status}</span></td>
                        <td className="text-right pr-1">
                          {a.status === "suspended"
                            ? <button onClick={() => setAccount(a.id, "activate")} className="rounded-lg bg-brand/15 px-2.5 py-1 text-[11px] font-semibold text-brand-bright hover:bg-brand/25 transition">{t("admin.users.activate", "Activate")}</button>
                            : <button onClick={() => setAccount(a.id, "suspend")} className="rounded-lg bg-pink/15 px-2.5 py-1 text-[11px] font-semibold text-pink hover:bg-pink/25 transition">{t("admin.users.suspend", "Suspend")}</button>}
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </motion.section>
            </div>
          )}

          {pipelineReqs && pipelineReqs.length > 0 && (
            <motion.section variants={fade} className="card p-5">
              <div className="flex items-center justify-between mb-3 gap-3 flex-wrap">
                <div><h3 className="font-display text-[15px] font-bold text-ink-hi">{t("admin.pipeline.title", "Application pipeline")}</h3><p className="text-[11px] text-ink-lo mt-0.5">{t("admin.pipeline.sub", "Move applicants through the hiring stages.")}</p></div>
                <div className="flex flex-wrap gap-1.5">
                  {pipelineReqs.map((r) => (
                    <button key={r.id} onClick={() => setPipelineReqId(r.id)} className={`rounded-lg px-2.5 py-1 text-[11px] font-semibold transition ${pipelineReqId === r.id ? "bg-brand/20 text-brand-bright" : "bg-panel2/60 text-ink-lo hover:text-ink-hi"}`}>{r.title}</button>
                  ))}
                </div>
              </div>
              <div className="grid grid-cols-2 md:grid-cols-3 xl:grid-cols-5 gap-3">
                {pipelineStages.map((stage) => {
                  const cards = (pipelineApps ?? []).filter((a) => a.status === stage);
                  const terminal = stage === "hired" || stage === "rejected";
                  return (
                    <div key={stage} className="rounded-xl border border-line/60 bg-panel2/30 p-2.5">
                      <div className="flex items-center justify-between mb-2"><span className="eyebrow capitalize">{stage}</span><span className="num text-[11px] text-ink-lo">{cards.length}</span></div>
                      <div className="space-y-2">
                        {cards.map((a) => (
                          <div key={a.id} className={`rounded-lg border p-2 ${stage === "hired" ? "border-brand/40 bg-brand/[0.06]" : stage === "rejected" ? "border-pink/30 bg-pink/[0.05]" : "border-line/50 bg-panel/40"}`}>
                            <div className="flex items-center justify-between"><span className="text-[11px] text-ink-hi capitalize">{a.talentType}</span><span className="num text-[11px] text-brand-bright">{Math.round(a.matchScore)}%</span></div>
                            {!terminal && (
                              <div className="mt-1.5 flex gap-1.5">
                                <button onClick={() => transitionApp(a.id, "advance")} className="rounded bg-brand/15 px-2 py-0.5 text-[10px] font-semibold text-brand-bright hover:bg-brand/25 transition">{t("admin.pipeline.advance", "Advance")}</button>
                                <button onClick={() => transitionApp(a.id, "reject")} className="rounded bg-pink/15 px-2 py-0.5 text-[10px] font-semibold text-pink hover:bg-pink/25 transition">{t("admin.pipeline.reject", "Reject")}</button>
                              </div>
                            )}
                            {(stage === "shortlisted" || stage === "hired") && (
                              <button onClick={() => setOfferAppId(a.id)} className={`mt-1.5 w-full rounded px-2 py-0.5 text-[10px] font-semibold transition ${offerAppId === a.id ? "bg-gold/25 text-gold" : "bg-panel/60 text-ink-lo hover:text-ink-hi"}`}>{t("admin.offer.manage", "Offer")}</button>
                            )}
                          </div>
                        ))}
                        {cards.length === 0 && <div className="text-[10px] text-ink-lo py-1">—</div>}
                      </div>
                    </div>
                  );
                })}
              </div>
              {offerAppId && (
                <div className="mt-4 border-t border-line/40 pt-4">
                  <div className="flex items-center justify-between mb-2">
                    <span className="eyebrow">{t("admin.offer.title", "Offers for selected applicant")}</span>
                    <button onClick={() => setOfferAppId(null)} className="text-[11px] text-ink-lo hover:text-ink-hi transition">{t("admin.offer.close", "Close")}</button>
                  </div>
                  <div className="space-y-2">
                    {(offers ?? []).map((o) => (
                      <div key={o.id} className="flex items-center gap-3 rounded-lg border border-line/50 bg-panel2/40 px-3 py-2">
                        <div className="min-w-0 flex-1"><div className="text-[13px] font-semibold text-ink-hi truncate">{o.title}</div><div className="text-[11px] text-ink-lo">{o.currency} {o.salaryAmount.toLocaleString()} · starts {o.startDate}</div></div>
                        <span className={`chip !text-[10px] capitalize ${o.status === "accepted" ? "!text-brand-bright !border-brand/30" : o.status === "declined" || o.status === "withdrawn" ? "!text-pink !border-pink/30" : "!text-gold !border-gold/30"}`}>{o.status}</span>
                        {(o.status === "draft" || o.status === "sent") && <button onClick={() => withdrawOffer(o.id)} className="rounded px-2 py-1 text-[10px] font-semibold text-ink-lo hover:text-pink transition">{t("admin.offer.withdraw", "Withdraw")}</button>}
                      </div>
                    ))}
                    {offers && offers.length === 0 && <div className="py-2 text-center text-[12px] text-ink-lo">{t("admin.offer.none", "No offers yet.")}</div>}
                  </div>
                  <div className="mt-3 grid gap-2 sm:grid-cols-[2fr_1fr_1fr_auto] items-center">
                    <input className="rounded-lg border border-line/70 bg-panel2/50 px-2.5 py-1.5 text-[12px] text-ink-hi placeholder:text-ink-lo focus:border-brand/50 focus:outline-none" value={newOffer.title} onChange={(e) => setNewOffer((f) => ({ ...f, title: e.target.value }))} placeholder={t("admin.offer.roleTitle", "Role title")} />
                    <input type="number" className="rounded-lg border border-line/70 bg-panel2/50 px-2.5 py-1.5 text-[12px] text-ink-hi placeholder:text-ink-lo focus:border-brand/50 focus:outline-none" value={newOffer.salaryAmount} onChange={(e) => setNewOffer((f) => ({ ...f, salaryAmount: e.target.value }))} placeholder={t("admin.offer.salary", "Salary (N$)")} />
                    <input type="date" className="rounded-lg border border-line/70 bg-panel2/50 px-2.5 py-1.5 text-[12px] text-ink-hi focus:border-brand/50 focus:outline-none" value={newOffer.startDate} onChange={(e) => setNewOffer((f) => ({ ...f, startDate: e.target.value }))} />
                    <button onClick={createAndSendOffer} disabled={!newOffer.title.trim() || !newOffer.salaryAmount || !newOffer.startDate} className="rounded-lg bg-brand/15 px-3 py-1.5 text-[11px] font-semibold text-brand-bright hover:bg-brand/25 transition disabled:opacity-50">{t("admin.offer.send", "Draft & send")}</button>
                  </div>
                </div>
              )}
            </motion.section>
          )}

          {clients && (
            <motion.section variants={fade} className="card p-5">
              <div className="flex items-center justify-between mb-3 gap-3 flex-wrap">
                <div><h3 className="font-display text-[15px] font-bold text-ink-hi">{t("admin.crm.title", "Client CRM")}</h3><p className="text-[11px] text-ink-lo mt-0.5">{t("admin.crm.sub", "Companies you recruit for and their contacts.")}</p></div>
                <div className="flex items-center gap-0.5 rounded-lg bg-panel2/50 p-0.5">
                  {["", "prospect", "active", "inactive"].map((s) => (
                    <button key={s || "all"} onClick={() => setClientFilter(s)} className={`rounded-md px-2.5 py-1 text-[11px] font-semibold capitalize transition ${clientFilter === s ? "bg-brand/20 text-brand-bright" : "text-ink-lo hover:text-ink-hi"}`}>{s || t("admin.crm.all", "all")}</button>
                  ))}
                </div>
              </div>
              <div className="grid gap-4 lg:grid-cols-2">
                <div>
                  <div className="space-y-2">
                    {clients.length === 0 && <div className="py-4 text-center text-[12px] text-ink-lo">{t("admin.crm.empty", "No clients yet.")}</div>}
                    {clients.map((c) => (
                      <div key={c.id} className={`rounded-xl border p-3 transition cursor-pointer ${selClient === c.id ? "border-brand/50 bg-brand/[0.06]" : "border-line/60 bg-panel2/40 hover:border-brand/40"}`} onClick={() => setSelClient(c.id)}>
                        <div className="flex items-center justify-between gap-2">
                          <div className="min-w-0"><div className="text-sm font-semibold text-ink-hi truncate">{c.name}</div><div className="text-[11px] text-ink-lo truncate">{[c.industry, c.city].filter(Boolean).join(" · ") || "—"} · {c.contactCount} {t("admin.crm.contacts", "contacts")}</div></div>
                          <select value={c.status} onClick={(e) => e.stopPropagation()} onChange={(e) => changeClientStatus(c.id, e.target.value)} className="rounded-lg border border-line/70 bg-panel2/50 px-2 py-1 text-[11px] font-semibold text-ink-hi capitalize focus:border-brand/50 focus:outline-none">
                            {["prospect", "active", "inactive"].map((s) => <option key={s} value={s}>{s}</option>)}
                          </select>
                        </div>
                      </div>
                    ))}
                  </div>
                  <div className="mt-3 border-t border-line/40 pt-3 grid gap-2 sm:grid-cols-[1fr_1fr_auto] items-center">
                    <input className="rounded-lg border border-line/70 bg-panel2/50 px-2.5 py-1.5 text-[12px] text-ink-hi placeholder:text-ink-lo focus:border-brand/50 focus:outline-none" value={newClient.name} onChange={(e) => setNewClient((f) => ({ ...f, name: e.target.value }))} placeholder={t("admin.crm.namePlaceholder", "Company name")} />
                    <input className="rounded-lg border border-line/70 bg-panel2/50 px-2.5 py-1.5 text-[12px] text-ink-hi placeholder:text-ink-lo focus:border-brand/50 focus:outline-none" value={newClient.city} onChange={(e) => setNewClient((f) => ({ ...f, city: e.target.value }))} placeholder={t("admin.crm.cityPlaceholder", "City")} />
                    <button onClick={createClient} disabled={!newClient.name.trim()} className="rounded-lg bg-brand/15 px-3 py-1.5 text-[11px] font-semibold text-brand-bright hover:bg-brand/25 transition disabled:opacity-50">{t("admin.crm.addClient", "Add")}</button>
                  </div>
                </div>
                <div className="rounded-xl border border-line/60 bg-panel2/30 p-3.5">
                  {!selClient ? (
                    <div className="py-8 text-center text-[12px] text-ink-lo">{t("admin.crm.pick", "Select a client to see contacts.")}</div>
                  ) : (
                    <>
                      <div className="eyebrow mb-2">{t("admin.crm.contactsTitle", "Contacts")}</div>
                      <div className="space-y-2">
                        {(contacts ?? []).map((c) => (
                          <div key={c.id} className="flex items-center gap-3 rounded-lg border border-line/50 bg-panel/40 px-3 py-2">
                            <div className="min-w-0 flex-1"><div className="text-[13px] font-semibold text-ink-hi truncate">{c.name}{c.isPrimary && <span className="ml-1.5 chip !text-[9px] !text-gold !border-gold/30">{t("admin.crm.primary", "primary")}</span>}</div><div className="text-[11px] text-ink-lo truncate">{[c.title, c.email, c.phone].filter(Boolean).join(" · ") || "—"}</div></div>
                            <button onClick={() => removeContact(c.id)} className="rounded-lg px-2 py-1 text-[11px] font-semibold text-ink-lo hover:text-pink transition">✕</button>
                          </div>
                        ))}
                        {contacts && contacts.length === 0 && <div className="py-3 text-center text-[12px] text-ink-lo">{t("admin.crm.noContacts", "No contacts yet.")}</div>}
                      </div>
                      <div className="mt-3 border-t border-line/40 pt-3 grid gap-2 sm:grid-cols-2">
                        <input className="rounded-lg border border-line/70 bg-panel2/50 px-2.5 py-1.5 text-[12px] text-ink-hi placeholder:text-ink-lo focus:border-brand/50 focus:outline-none" value={newContact.name} onChange={(e) => setNewContact((f) => ({ ...f, name: e.target.value }))} placeholder={t("admin.crm.contactName", "Name")} />
                        <input className="rounded-lg border border-line/70 bg-panel2/50 px-2.5 py-1.5 text-[12px] text-ink-hi placeholder:text-ink-lo focus:border-brand/50 focus:outline-none" value={newContact.title} onChange={(e) => setNewContact((f) => ({ ...f, title: e.target.value }))} placeholder={t("admin.crm.contactTitle", "Title")} />
                        <input className="rounded-lg border border-line/70 bg-panel2/50 px-2.5 py-1.5 text-[12px] text-ink-hi placeholder:text-ink-lo focus:border-brand/50 focus:outline-none" value={newContact.email} onChange={(e) => setNewContact((f) => ({ ...f, email: e.target.value }))} placeholder={t("admin.crm.contactEmail", "email@company.na")} />
                        <input className="rounded-lg border border-line/70 bg-panel2/50 px-2.5 py-1.5 text-[12px] text-ink-hi placeholder:text-ink-lo focus:border-brand/50 focus:outline-none" value={newContact.phone} onChange={(e) => setNewContact((f) => ({ ...f, phone: e.target.value }))} placeholder={t("admin.crm.contactPhone", "Phone")} />
                        <label className="flex items-center gap-2 text-[11px] text-ink-mid"><input type="checkbox" checked={newContact.isPrimary} onChange={(e) => setNewContact((f) => ({ ...f, isPrimary: e.target.checked }))} />{t("admin.crm.primaryLabel", "Primary contact")}</label>
                        <button onClick={addContact} disabled={!newContact.name.trim()} className="rounded-lg bg-brand/15 px-3 py-1.5 text-[11px] font-semibold text-brand-bright hover:bg-brand/25 transition disabled:opacity-50">{t("admin.crm.addContact", "Add contact")}</button>
                      </div>
                    </>
                  )}
                </div>
              </div>
            </motion.section>
          )}

          <footer className="flex flex-wrap items-center justify-between gap-2 pt-1 pb-4 text-[11px] text-ink-lo">
            <span>{t("admin.footer.console")}</span><span>{t("admin.footer.synthetic")}</span>
          </footer>
        </motion.div>
      </main>
    </div>
  );
}

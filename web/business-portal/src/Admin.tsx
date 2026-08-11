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
  const [pipelineApps, setPipelineApps] = useState<{ id: string; talentType: string; matchScore: number; status: string; rejectReason?: string | null }[] | null>(null);
  const [selectedApps, setSelectedApps] = useState<Set<string>>(new Set());
  // Kanban drag-and-drop: the card being dragged + the column currently hovered.
  const [dragAppId, setDragAppId] = useState<string | null>(null);
  const [dragOverStage, setDragOverStage] = useState<string | null>(null);
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
  // Onboarding checklist (per selected pipeline application).
  type OnbTask = { id: string; label: string; isDone: boolean; sortOrder: number };
  type Onboarding = { id: string; applicationId: string; roleTitle: string; completed: number; total: number; tasks: OnbTask[] };
  const [onboarding, setOnboarding] = useState<Onboarding | "none" | null>(null);
  // Candidate↔employer messaging (per selected application).
  type Message = { id: string; sender: string; senderName: string; body: string; sentAt: string; read: boolean };
  const [messages, setMessages] = useState<Message[]>([]);
  const [msgDraft, setMsgDraft] = useState("");
  // Interviews + panel attendees (per selected application).
  type Interview = { id: string; applicationId: string; scheduledAt: string; durationMinutes: number; location: string; status: string; feedbackRating: number | null; feedbackComment: string | null; round: string | null; requiredSkills: string[] };
  type Attendee = { id: string; name: string; email: string | null; role: string };
  const [interviews, setInterviews] = useState<Interview[]>([]);
  const [newInterview, setNewInterview] = useState({ scheduledAt: "", durationMinutes: 45, location: "", round: "", skills: "" });
  const [ivOpen, setIvOpen] = useState<string | null>(null);
  const [attendees, setAttendees] = useState<Attendee[]>([]);
  const [newAttendee, setNewAttendee] = useState({ name: "", email: "", role: "interviewer" });
  const [ivBusy, setIvBusy] = useState(false);
  // Per-round skill ratings (for the expanded interview) + the application-wide aggregated summary.
  const [skillRatings, setSkillRatings] = useState<Record<string, number>>({});
  type IvSummary = { rounds: { interviewId: string; round: string | null; scheduledAt: string; status: string; overallRating: number | null }[]; skillAverages: { skill: string; average: number; count: number }[] };
  const [ivSummary, setIvSummary] = useState<IvSummary | null>(null);
  // Faceted candidate search.
  type SearchCandidate = { id: string; firstName: string; lastName: string; city: string; availability: string; publicHeadline: string | null };
  type FacetCount = { label: string; count: number };
  type SearchResult = { items: SearchCandidate[]; total: number; facets: { cities: FacetCount[]; availability: FacetCount[] } };
  const [csQuery, setCsQuery] = useState("");
  const [csCity, setCsCity] = useState("");
  const [csAvailability, setCsAvailability] = useState("");
  const [csBlind, setCsBlind] = useState(false);
  const [csResult, setCsResult] = useState<SearchResult | null>(null);
  // Notes + tags for a candidate expanded in the search results.
  type CandNote = { id: string; author: string; body: string; createdAt: string };
  const [csOpen, setCsOpen] = useState<string | null>(null);
  const [csNotes, setCsNotes] = useState<CandNote[]>([]);
  const [csTags, setCsTags] = useState<string[]>([]);
  const [csNoteDraft, setCsNoteDraft] = useState("");
  const [csTagDraft, setCsTagDraft] = useState("");
  // "Similar candidates" for the expanded candidate.
  type SimilarCandidate = { id: string; name: string; city: string; headline: string | null; availability: string; score: number };
  const [csSimilar, setCsSimilar] = useState<SimilarCandidate[]>([]);
  // Semantic "more like this" (flag-gated server-side; empty when the flag is off, so this self-hides).
  const [csSemantic, setCsSemantic] = useState<SimilarCandidate[]>([]);
  // Duplicate-candidate detection.
  type DupGroup = { name: string; count: number; candidates: SearchCandidate[] };
  const [dupes, setDupes] = useState<DupGroup[] | null>(null);
  // Talent pools / shortlists (recruiter).
  type Pool = { id: string; name: string; memberCount: number };
  type PoolMember = { candidateId: string; name: string; city: string };
  const [pools, setPools] = useState<Pool[] | null>(null);
  const [newPoolName, setNewPoolName] = useState("");
  const [poolOpen, setPoolOpen] = useState<string | null>(null);
  const [poolMembers, setPoolMembers] = useState<PoolMember[]>([]);
  const [poolBusy, setPoolBusy] = useState(false);
  // Admin-defined candidate custom fields.
  type CustomField = { id: string; key: string; label: string; kind: string; options: string[]; sortOrder: number };
  type CustomValue = { definitionId: string; key: string; label: string; kind: string; value: string };
  const [customFields, setCustomFields] = useState<CustomField[] | null>(null);
  const [newCustomField, setNewCustomField] = useState({ label: "", kind: "text", options: "" });
  const [csCustomValues, setCsCustomValues] = useState<Record<string, string>>({});
  // Bulk CSV candidate import.
  const [importCsv, setImportCsv] = useState("");
  const [importBusy, setImportBusy] = useState(false);
  const [importResult, setImportResult] = useState<{ created: number; skipped: number; errors: string[] } | null>(null);
  // Requisition enrichment (salary/type/remote/tags) for the selected pipeline role.
  type ReqDetail = { salaryMin: number | null; salaryMax: number | null; currency: string; employmentType: string; remote: boolean; internal: boolean; featuredUntil: string | null; tags: string[] };
  const [reqDetail, setReqDetail] = useState<ReqDetail | null>(null);
  const [reqTagDraft, setReqTagDraft] = useState("");
  // Employee referrals for the selected role.
  type Referral = { id: string; referrerName: string; candidateName: string; candidateEmail: string; note: string | null; createdAt: string };
  const [referrals, setReferrals] = useState<Referral[]>([]);
  const [newReferral, setNewReferral] = useState({ referrerName: "", candidateName: "", candidateEmail: "", note: "" });
  type Approval = { status: string; approver: string | null; reason: string | null };
  const [approval, setApproval] = useState<Approval | null>(null);
  // Application form / screening questions for the selected pipeline role.
  type FormQuestion = { id: string; label: string; kind: string; options: string[]; required: boolean; sortOrder: number };
  const [formQuestions, setFormQuestions] = useState<FormQuestion[]>([]);
  const [newQuestion, setNewQuestion] = useState({ label: "", kind: "text", required: false, optionsCsv: "" });
  // Candidate answers for the application open in the drawer.
  type Answer = { questionId: string; label: string; value: string };
  const [appAnswers, setAppAnswers] = useState<Answer[]>([]);
  // Source / channel attribution: the drawer application's channel + the org-wide breakdown.
  const [appSource, setAppSource] = useState<string>("direct");
  type ChannelMetric = { source: string; applications: number; hires: number };
  const [channels, setChannels] = useState<ChannelMetric[] | null>(null);
  // Per-role careers-page view analytics.
  type CareerView = { requestId: string; title: string; city: string; views: number; lastViewedAt: string | null };
  const [careerViews, setCareerViews] = useState<CareerView[] | null>(null);
  // Hiring-outcome training set (unlocks future learning-to-rank).
  type OutcomeSummary = { total: number; hired: number; rejected: number; avgScoreHired: number; avgScoreRejected: number };
  const [outcomes, setOutcomes] = useState<OutcomeSummary | null>(null);
  const SOURCE_CHANNELS = ["direct", "careers", "referral", "campaign", "board", "agency", "walk-in"];
  // Job templates.
  type JobTemplate = { id: string; name: string; title: string; city: string | null; positions: number; employmentType: string; remote: boolean; tags: string[] };
  const [templates, setTemplates] = useState<JobTemplate[] | null>(null);
  const [newTemplate, setNewTemplate] = useState({ name: "", title: "", city: "", positions: 1 });
  // Bulk email campaigns.
  type Campaign = { id: string; name: string; subject: string; body: string; status: string; recipientCount: number; recipients: string[] };
  const [campaigns, setCampaigns] = useState<Campaign[] | null>(null);
  const [newCampaign, setNewCampaign] = useState({ name: "", subject: "", body: "" });
  const [campaignRecipient, setCampaignRecipient] = useState<Record<string, string>>({});
  // Audit trail.
  type AuditEntry = { id: string; actor: string; action: string; entityType: string; entityId: string | null; summary: string; occurredAt: string };
  const [audit, setAudit] = useState<AuditEntry[] | null>(null);
  // Hiring metrics (time-to-hire + source-of-hire).
  type HiringMetrics = { hires: number; avgTimeToHireDays: number; medianTimeToHireDays: number; bySource: { source: string; applications: number; hires: number }[] };
  const [hiring, setHiring] = useState<HiringMetrics | null>(null);
  // Diversity / EEO report.
  type Diversity = { total: number; byNationality: { label: string; count: number }[]; byCity: { label: string; count: number }[] };
  const [diversity, setDiversity] = useState<Diversity | null>(null);
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
    fetch(`/api/recruitment/requests/${pipelineReqId}/details`)
      .then((r) => (r.ok ? r.json() : null))
      .then((v) => { if (v) setReqDetail(v); })
      .catch(() => { /* keep empty */ });
    fetch(`/api/recruitment/requests/${pipelineReqId}/approval`)
      .then((r) => (r.ok ? r.json() : null))
      .then((v) => { if (v) setApproval(v); })
      .catch(() => { /* keep empty */ });
    fetch(`/api/recruitment/requests/${pipelineReqId}/form`)
      .then((r) => (r.ok ? r.json() : null))
      .then((v) => { if (Array.isArray(v)) setFormQuestions(v); })
      .catch(() => { /* keep empty */ });
    fetch(`/api/recruitment/requests/${pipelineReqId}/referrals`, { credentials: "same-origin" })
      .then((r) => (r.ok ? r.json() : null))
      .then((v) => { if (Array.isArray(v)) setReferrals(v); })
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
  // Faceted candidate search — re-run whenever a filter changes.
  useEffect(() => {
    const qs = new URLSearchParams();
    if (csQuery.trim()) qs.set("q", csQuery.trim());
    if (csCity) qs.set("city", csCity);
    if (csAvailability) qs.set("availability", csAvailability);
    if (csBlind) qs.set("blind", "true");
    qs.set("pageSize", "10");
    const id = setTimeout(() => {
      fetch("/api/candidates/search?" + qs.toString())
        .then((r) => (r.ok ? r.json() : null))
        .then((v) => { if (v?.items) setCsResult(v); })
        .catch(() => { /* offline */ });
    }, 250);
    return () => clearTimeout(id);
  }, [csQuery, csCity, csAvailability, csBlind]);

  // Diversity / EEO report.
  useEffect(() => {
    fetch("/api/candidates/diversity", { credentials: "same-origin" })
      .then((r) => (r.ok ? r.json() : null))
      .then((v) => { if (v) setDiversity(v); })
      .catch(() => { /* offline / unauthorised */ });
  }, []);

  // Hiring metrics.
  useEffect(() => {
    fetch("/api/recruitment/metrics/hiring")
      .then((r) => (r.ok ? r.json() : null))
      .then((v) => { if (v) setHiring(v); })
      .catch(() => { /* offline */ });
  }, []);

  // Audit trail.
  useEffect(() => {
    fetch("/api/admin/audit", { credentials: "same-origin" })
      .then((r) => (r.ok ? r.json() : null))
      .then((v) => { if (Array.isArray(v)) setAudit(v); })
      .catch(() => { /* offline / unauthorised */ });
  }, []);

  // Bulk email campaigns.
  useEffect(() => {
    fetch("/api/recruitment/campaigns", { credentials: "same-origin" })
      .then((r) => (r.ok ? r.json() : null))
      .then((v) => { if (Array.isArray(v)) setCampaigns(v); })
      .catch(() => { /* offline / unauthorised */ });
  }, []);

  // Job templates.
  useEffect(() => {
    fetch("/api/recruitment/templates")
      .then((r) => (r.ok ? r.json() : null))
      .then((v) => { if (Array.isArray(v)) setTemplates(v); })
      .catch(() => { /* offline */ });
  }, []);

  // Suspected-duplicate candidates.
  useEffect(() => {
    fetch("/api/candidates/duplicates")
      .then((r) => (r.ok ? r.json() : null))
      .then((v) => { if (Array.isArray(v)) setDupes(v); })
      .catch(() => { /* offline */ });
  }, []);

  // Talent pools (recruiter shortlists).
  useEffect(() => {
    fetch("/api/candidates/pools")
      .then((r) => (r.ok ? r.json() : null))
      .then((v) => { if (Array.isArray(v)) setPools(v); })
      .catch(() => { /* offline */ });
    fetch("/api/candidates/custom-fields")
      .then((r) => (r.ok ? r.json() : null))
      .then((v) => { if (Array.isArray(v)) setCustomFields(v); })
      .catch(() => { /* offline */ });
  }, []);

  // Source / channel breakdown (admin).
  useEffect(() => {
    fetch("/api/recruitment/metrics/channels", { credentials: "same-origin" })
      .then((r) => (r.ok ? r.json() : null))
      .then((v) => { if (Array.isArray(v)) setChannels(v); })
      .catch(() => { /* offline */ });
    fetch("/api/recruitment/metrics/careers-views", { credentials: "same-origin" })
      .then((r) => (r.ok ? r.json() : null))
      .then((v) => { if (Array.isArray(v)) setCareerViews(v); })
      .catch(() => { /* offline */ });
    fetch("/api/recruitment/metrics/outcomes", { credentials: "same-origin" })
      .then((r) => (r.ok ? r.json() : null))
      .then((v) => { if (v && typeof v.total === "number") setOutcomes(v); })
      .catch(() => { /* offline */ });
  }, []);

  // Members of the expanded pool.
  useEffect(() => {
    if (!poolOpen) { setPoolMembers([]); return; }
    fetch(`/api/candidates/pools/${poolOpen}/members`)
      .then((r) => (r.ok ? r.json() : []))
      .then((v) => { if (Array.isArray(v)) setPoolMembers(v); })
      .catch(() => { /* offline */ });
  }, [poolOpen]);

  // Notes + tags for the candidate expanded in search results.
  useEffect(() => {
    if (!csOpen) { setCsNotes([]); setCsTags([]); setCsCustomValues({}); setCsSimilar([]); setCsSemantic([]); return; }
    fetch(`/api/candidates/${csOpen}/similar?take=5`, { credentials: "same-origin" })
      .then((r) => (r.ok ? r.json() : []))
      .then((v) => { if (Array.isArray(v)) setCsSimilar(v); })
      .catch(() => { /* offline */ });
    fetch(`/api/candidates/${csOpen}/semantic-similar?take=5`, { credentials: "same-origin" })
      .then((r) => (r.ok ? r.json() : []))
      .then((v) => { if (Array.isArray(v)) setCsSemantic(v); })
      .catch(() => { /* offline / flag off */ });
    fetch(`/api/candidates/${csOpen}/notes`).then((r) => (r.ok ? r.json() : [])).then((v) => Array.isArray(v) && setCsNotes(v)).catch(() => { /* offline */ });
    fetch(`/api/candidates/${csOpen}/tags`).then((r) => (r.ok ? r.json() : [])).then((v) => Array.isArray(v) && setCsTags(v)).catch(() => { /* offline */ });
    fetch(`/api/candidates/${csOpen}/custom-values`, { credentials: "same-origin" })
      .then((r) => (r.ok ? r.json() : []))
      .then((v) => { if (Array.isArray(v)) setCsCustomValues(Object.fromEntries(v.map((x: CustomValue) => [x.definitionId, x.value]))); })
      .catch(() => { /* offline */ });
  }, [csOpen]);

  // Offers + onboarding + interviews for the application selected on a pipeline card.
  useEffect(() => {
    if (!offerAppId) { setOffers(null); setOnboarding(null); setMessages([]); setInterviews([]); setIvOpen(null); setAppAnswers([]); setAppSource("direct"); return; }
    fetch(`/api/recruitment/applications/${offerAppId}/answers`, { credentials: "same-origin" })
      .then((r) => (r.ok ? r.json() : null))
      .then((v) => { if (Array.isArray(v)) setAppAnswers(v); })
      .catch(() => { /* keep empty */ });
    fetch(`/api/recruitment/applications/${offerAppId}/source`, { credentials: "same-origin" })
      .then((r) => (r.ok ? r.json() : null))
      .then((v) => { if (v?.channel) setAppSource(v.channel); })
      .catch(() => { /* keep default */ });
    fetch(`/api/recruitment/applications/${offerAppId}/messages`, { credentials: "same-origin" })
      .then((r) => (r.ok ? r.json() : null))
      .then((v) => { if (Array.isArray(v)) setMessages(v); })
      .catch(() => { /* keep empty */ });
    fetch(`/api/recruitment/applications/${offerAppId}/offers`)
      .then((r) => (r.ok ? r.json() : null))
      .then((v) => { if (Array.isArray(v)) setOffers(v); })
      .catch(() => { /* keep empty */ });
    fetch(`/api/recruitment/applications/${offerAppId}/onboarding`)
      .then((r) => (r.ok ? r.json() : r.status === 404 ? "none" : null))
      .then((v) => { if (v === "none") setOnboarding("none"); else if (v?.id) setOnboarding(v); })
      .catch(() => { /* keep null */ });
    fetch(`/api/recruitment/applications/${offerAppId}/interviews`)
      .then((r) => (r.ok ? r.json() : null))
      .then((v) => { if (Array.isArray(v)) setInterviews(v); })
      .catch(() => { /* keep empty */ });
    fetch(`/api/recruitment/applications/${offerAppId}/interview-summary`, { credentials: "same-origin" })
      .then((r) => (r.ok ? r.json() : null))
      .then((v) => { if (v?.skillAverages) setIvSummary(v); })
      .catch(() => { /* keep empty */ });
  }, [offerAppId]);

  // Panel attendees + per-skill ratings for the expanded interview.
  useEffect(() => {
    if (!ivOpen) { setAttendees([]); setSkillRatings({}); return; }
    fetch(`/api/recruitment/interviews/${ivOpen}/attendees`)
      .then((r) => (r.ok ? r.json() : []))
      .then((v) => { if (Array.isArray(v)) setAttendees(v); })
      .catch(() => { /* offline */ });
    fetch(`/api/recruitment/interviews/${ivOpen}/skill-ratings`)
      .then((r) => (r.ok ? r.json() : []))
      .then((v) => { if (Array.isArray(v)) setSkillRatings(Object.fromEntries(v.map((x: { skill: string; rating: number }) => [x.skill, x.rating]))); })
      .catch(() => { /* offline */ });
  }, [ivOpen]);
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
    let init: RequestInit = { method: "POST", credentials: "same-origin" };
    if (action === "reject") {
      const reason = window.prompt(t("admin.pipeline.rejectReason", "Rejection reason (optional):")) ?? "";
      init = { ...init, headers: { "Content-Type": "application/json" }, body: JSON.stringify({ reason: reason.trim() || null, rejectedBy: session.name || "Recruiter" }) };
    }
    const r = await fetch(`/api/recruitment/applications/${id}/${action}`, init);
    if (r.ok) {
      const u = await r.json().catch(() => null);
      setPipelineApps((prev) => (prev ? prev.map((a) => (a.id === id ? { ...a, status: u?.status ?? (action === "reject" ? "rejected" : a.status), rejectReason: u?.rejectReason ?? a.rejectReason } : a)) : prev));
    }
  };
  // Kanban drop: move an application to a target stage using the available transitions. The backend
  // advances strictly one stage forward (applied→reviewed→shortlisted→hired) or rejects (terminal),
  // so a forward drop chains the right number of advances; dropping on "rejected" rejects; backward or
  // same-stage drops are unsupported and ignored.
  const advanceOrder = ["applied", "reviewed", "shortlisted", "hired"];
  const moveApp = async (id: string, target: string) => {
    const app = (pipelineApps ?? []).find((a) => a.id === id);
    if (!app || app.status === target) return;
    if (target === "rejected") { await transitionApp(id, "reject"); return; }
    const from = advanceOrder.indexOf(app.status);
    const to = advanceOrder.indexOf(target);
    if (from < 0 || to < 0 || to <= from) return; // only forward moves are supported
    for (let i = from; i < to; i++) {
      const r = await fetch(`/api/recruitment/applications/${id}/advance`, { method: "POST", credentials: "same-origin" });
      if (!r.ok) break;
      const u = await r.json().catch(() => null);
      if (u?.status) setPipelineApps((prev) => (prev ? prev.map((a) => (a.id === id ? { ...a, status: u.status } : a)) : prev));
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
  const sendMessage = async () => {
    if (!offerAppId || !msgDraft.trim()) return;
    const r = await fetch(`/api/recruitment/applications/${offerAppId}/messages`, { method: "POST", headers: { "Content-Type": "application/json" }, credentials: "same-origin", body: JSON.stringify({ sender: "recruiter", senderName: session.name || "Recruiter", body: msgDraft.trim() }) });
    if (r.ok) { const m: Message = await r.json(); setMessages((ms) => [...ms, m]); setMsgDraft(""); }
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
  // Set/override the drawer application's arrival channel.
  const setApplicationSource = async (channel: string) => {
    if (!offerAppId) return;
    setAppSource(channel);
    await fetch(`/api/recruitment/applications/${offerAppId}/source`, { method: "PUT", headers: { "Content-Type": "application/json" }, credentials: "same-origin", body: JSON.stringify({ channel }) });
  };
  // Interviews + panel attendees.
  const scheduleInterview = async () => {
    if (!offerAppId || !newInterview.scheduledAt || !newInterview.location.trim()) return;
    setIvBusy(true);
    try {
      // datetime-local yields "yyyy-MM-ddTHH:mm" (no zone); send as an ISO instant.
      const skills = newInterview.skills.split(",").map((s) => s.trim()).filter(Boolean);
      const r = await fetch(`/api/recruitment/applications/${offerAppId}/interviews`, { method: "POST", headers: { "Content-Type": "application/json" }, credentials: "same-origin", body: JSON.stringify({ scheduledAt: new Date(newInterview.scheduledAt).toISOString(), durationMinutes: Number(newInterview.durationMinutes) || 45, location: newInterview.location.trim(), round: newInterview.round.trim() || null, requiredSkills: skills.length ? skills : null }) });
      if (r.ok) { const iv: Interview = await r.json(); setInterviews((xs) => [...xs, iv]); setNewInterview({ scheduledAt: "", durationMinutes: 45, location: "", round: "", skills: "" }); }
    } finally { setIvBusy(false); }
  };
  const cancelInterview = async (id: string) => {
    const r = await fetch(`/api/recruitment/interviews/${id}/cancel`, { method: "POST", credentials: "same-origin" });
    if (r.ok) { const u = await r.json().catch(() => null); setInterviews((xs) => xs.map((iv) => (iv.id === id ? { ...iv, status: u?.status ?? "cancelled" } : iv))); }
  };
  const addAttendee = async (interviewId: string) => {
    const name = newAttendee.name.trim();
    if (!name) return;
    const r = await fetch(`/api/recruitment/interviews/${interviewId}/attendees`, { method: "POST", headers: { "Content-Type": "application/json" }, credentials: "same-origin", body: JSON.stringify({ name, email: newAttendee.email.trim() || null, role: newAttendee.role.trim() || "interviewer" }) });
    if (r.ok) { const at: Attendee = await r.json(); setAttendees((as) => [...as, at]); setNewAttendee({ name: "", email: "", role: "interviewer" }); }
  };
  const removeAttendee = async (attendeeId: string) => {
    const r = await fetch(`/api/recruitment/interviews/attendees/${attendeeId}`, { method: "DELETE", credentials: "same-origin" });
    if (r.ok || r.status === 204) setAttendees((as) => as.filter((a) => a.id !== attendeeId));
  };
  // Save (replace) the expanded interview's per-skill ratings, then refresh the application summary.
  const saveSkillRatings = async (interviewId: string) => {
    const ratings = Object.entries(skillRatings).filter(([, v]) => v >= 1 && v <= 5).map(([skill, rating]) => ({ skill, rating }));
    const r = await fetch(`/api/recruitment/interviews/${interviewId}/skill-ratings`, { method: "POST", headers: { "Content-Type": "application/json" }, credentials: "same-origin", body: JSON.stringify({ ratings }) });
    if (r.ok && offerAppId) {
      const s = await fetch(`/api/recruitment/applications/${offerAppId}/interview-summary`, { credentials: "same-origin" });
      if (s.ok) setIvSummary(await s.json());
    }
  };
  const startOnboarding = async (roleTitle: string) => {
    if (!offerAppId) return;
    const r = await fetch(`/api/recruitment/applications/${offerAppId}/onboarding`, { method: "POST", headers: { "Content-Type": "application/json" }, credentials: "same-origin", body: JSON.stringify({ roleTitle: roleTitle || "New hire" }) });
    if (r.ok) setOnboarding(await r.json());
  };
  const toggleTask = async (taskId: string, done: boolean) => {
    const r = await fetch(`/api/recruitment/onboarding/tasks/${taskId}/toggle`, { method: "POST", headers: { "Content-Type": "application/json" }, credentials: "same-origin", body: JSON.stringify({ done }) });
    if (r.ok) {
      setOnboarding((ob) => (ob && ob !== "none" ? { ...ob, tasks: ob.tasks.map((t) => (t.id === taskId ? { ...t, isDone: done } : t)), completed: ob.tasks.reduce((n, t) => n + (t.id === taskId ? (done ? 1 : 0) : t.isDone ? 1 : 0), 0) } : ob));
    }
  };
  const eraseCandidate = async (id: string) => {
    if (!window.confirm(t("admin.gdpr.eraseConfirm", "Permanently erase this candidate and all their data? This cannot be undone."))) return;
    const r = await fetch(`/api/candidates/${id}`, { method: "DELETE", credentials: "same-origin" });
    if (r.ok) {
      setCsResult((res) => (res ? { ...res, items: res.items.filter((c) => c.id !== id), total: Math.max(0, res.total - 1) } : res));
      if (csOpen === id) setCsOpen(null);
    }
  };
  const addCandNote = async () => {
    if (!csOpen || !csNoteDraft.trim()) return;
    const r = await fetch(`/api/candidates/${csOpen}/notes`, { method: "POST", headers: { "Content-Type": "application/json" }, credentials: "same-origin", body: JSON.stringify({ author: session.name || "Recruiter", body: csNoteDraft.trim() }) });
    if (r.ok) { const n: CandNote = await r.json(); setCsNotes((ns) => [n, ...ns]); setCsNoteDraft(""); }
  };
  const removeCandNote = async (noteId: string) => {
    const r = await fetch(`/api/candidates/notes/${noteId}`, { method: "DELETE", credentials: "same-origin" });
    if (r.ok) setCsNotes((ns) => ns.filter((n) => n.id !== noteId));
  };
  const addCandTag = async () => {
    if (!csOpen || !csTagDraft.trim()) return;
    const r = await fetch(`/api/candidates/${csOpen}/tags`, { method: "POST", headers: { "Content-Type": "application/json" }, credentials: "same-origin", body: JSON.stringify({ label: csTagDraft.trim() }) });
    if (r.ok) { setCsTags(await r.json()); setCsTagDraft(""); }
  };
  const removeCandTag = async (label: string) => {
    const r = await fetch(`/api/candidates/${csOpen}/tags/${encodeURIComponent(label)}`, { method: "DELETE", credentials: "same-origin" });
    if (r.ok) setCsTags(await r.json());
  };
  // Talent pools (recruiter shortlists).
  const createPool = async () => {
    const name = newPoolName.trim();
    if (!name) return;
    setPoolBusy(true);
    try {
      const r = await fetch("/api/candidates/pools", { method: "POST", headers: { "Content-Type": "application/json" }, credentials: "same-origin", body: JSON.stringify({ name }) });
      if (r.ok) { const p: Pool = await r.json(); setPools((ps) => [...(ps ?? []), p]); setNewPoolName(""); }
    } finally { setPoolBusy(false); }
  };
  // Candidate custom-field definitions.
  const addCustomField = async () => {
    if (!newCustomField.label.trim()) return;
    const options = newCustomField.kind === "select" ? newCustomField.options.split(",").map((o) => o.trim()).filter(Boolean) : null;
    const r = await fetch("/api/candidates/custom-fields", { method: "POST", headers: { "Content-Type": "application/json" }, credentials: "same-origin", body: JSON.stringify({ label: newCustomField.label.trim(), kind: newCustomField.kind, options }) });
    if (r.ok) { const f: CustomField = await r.json(); setCustomFields((fs) => [...(fs ?? []), f]); setNewCustomField({ label: "", kind: "text", options: "" }); }
  };
  const removeCustomField = async (id: string) => {
    const r = await fetch(`/api/candidates/custom-fields/${id}`, { method: "DELETE", credentials: "same-origin" });
    if (r.ok || r.status === 204) setCustomFields((fs) => (fs ? fs.filter((f) => f.id !== id) : fs));
  };
  const saveCustomValues = async () => {
    if (!csOpen) return;
    const values = Object.entries(csCustomValues).filter(([, v]) => (v ?? "").trim()).map(([definitionId, value]) => ({ definitionId, value }));
    await fetch(`/api/candidates/${csOpen}/custom-values`, { method: "PUT", headers: { "Content-Type": "application/json" }, credentials: "same-origin", body: JSON.stringify({ values }) });
  };
  // Bulk-import candidates from pasted/uploaded CSV.
  const runImport = async () => {
    if (!importCsv.trim()) return;
    setImportBusy(true);
    setImportResult(null);
    try {
      const r = await fetch("/api/candidates/import", { method: "POST", headers: { "Content-Type": "application/json" }, credentials: "same-origin", body: JSON.stringify({ csv: importCsv }) });
      if (r.ok) { setImportResult(await r.json()); }
      else { setImportResult({ created: 0, skipped: 0, errors: [t("admin.import.failed", "Import failed — check your permissions and try again.")] }); }
    } finally { setImportBusy(false); }
  };
  const addToPool = async (poolId: string, candidateId: string) => {
    const r = await fetch(`/api/candidates/pools/${poolId}/members/${candidateId}`, { method: "POST", credentials: "same-origin" });
    // 200 = added, 409 = already a member; both mean "in the pool" for our count purposes.
    if (r.ok) {
      setPools((ps) => (ps ? ps.map((p) => (p.id === poolId ? { ...p, memberCount: p.memberCount + 1 } : p)) : ps));
      if (poolOpen === poolId) {
        const c = csResult?.items.find((x) => x.id === candidateId);
        if (c && !poolMembers.some((m) => m.candidateId === candidateId)) setPoolMembers((ms) => [...ms, { candidateId, name: `${c.firstName} ${c.lastName}`, city: c.city }]);
      }
    }
  };
  const removeFromPool = async (poolId: string, candidateId: string) => {
    const r = await fetch(`/api/candidates/pools/${poolId}/members/${candidateId}`, { method: "DELETE", credentials: "same-origin" });
    if (r.ok) {
      setPoolMembers((ms) => ms.filter((m) => m.candidateId !== candidateId));
      setPools((ps) => (ps ? ps.map((p) => (p.id === poolId ? { ...p, memberCount: Math.max(0, p.memberCount - 1) } : p)) : ps));
    }
  };
  const toggleAppSelect = (id: string) => setSelectedApps((prev) => {
    const next = new Set(prev);
    if (next.has(id)) next.delete(id); else next.add(id);
    return next;
  });
  const bulkTransition = async (action: "advance" | "reject") => {
    const ids = [...selectedApps];
    if (ids.length === 0) return;
    const r = await fetch("/api/recruitment/applications/bulk", { method: "POST", headers: { "Content-Type": "application/json" }, credentials: "same-origin", body: JSON.stringify({ applicationIds: ids, action }) });
    if (r.ok) {
      const res = await r.json().catch(() => null);
      const ok = new Set<string>((res?.items ?? []).filter((i: { ok: boolean }) => i.ok).map((i: { applicationId: string }) => i.applicationId));
      setPipelineApps((prev) => (prev ? prev.map((a) => (ok.has(a.id) ? { ...a, status: action === "reject" ? "rejected" : nextStage(a.status) } : a)) : prev));
      setSelectedApps(new Set());
    }
  };
  const nextStage = (s: string) => ({ applied: "reviewed", reviewed: "shortlisted", shortlisted: "hired" }[s] ?? s);
  const saveReqDetail = async (patch: Partial<ReqDetail>) => {
    if (!pipelineReqId || !reqDetail) return;
    const next = { ...reqDetail, ...patch };
    setReqDetail(next);
    const r = await fetch(`/api/recruitment/requests/${pipelineReqId}/details`, { method: "PUT", headers: { "Content-Type": "application/json" }, credentials: "same-origin", body: JSON.stringify({ salaryMin: next.salaryMin, salaryMax: next.salaryMax, currency: next.currency || "NAD", employmentType: next.employmentType, remote: next.remote }) });
    if (r.ok) setReqDetail(await r.json());
  };
  const addReqTag = async () => {
    if (!pipelineReqId || !reqTagDraft.trim()) return;
    const r = await fetch(`/api/recruitment/requests/${pipelineReqId}/tags`, { method: "POST", headers: { "Content-Type": "application/json" }, credentials: "same-origin", body: JSON.stringify({ label: reqTagDraft.trim() }) });
    if (r.ok) { const tags = await r.json(); setReqDetail((d) => (d ? { ...d, tags } : d)); setReqTagDraft(""); }
  };
  const removeReqTag = async (label: string) => {
    const r = await fetch(`/api/recruitment/requests/${pipelineReqId}/tags/${encodeURIComponent(label)}`, { method: "DELETE", credentials: "same-origin" });
    if (r.ok) { const tags = await r.json(); setReqDetail((d) => (d ? { ...d, tags } : d)); }
  };
  // Application-form / screening questions (per requisition).
  const addFormQuestion = async () => {
    if (!pipelineReqId || !newQuestion.label.trim()) return;
    const options = newQuestion.kind === "select" ? newQuestion.optionsCsv.split(",").map((o) => o.trim()).filter(Boolean) : null;
    const r = await fetch(`/api/recruitment/requests/${pipelineReqId}/form`, { method: "POST", headers: { "Content-Type": "application/json" }, credentials: "same-origin", body: JSON.stringify({ label: newQuestion.label.trim(), kind: newQuestion.kind, options, required: newQuestion.required }) });
    if (r.ok) { const q: FormQuestion = await r.json(); setFormQuestions((qs) => [...qs, q]); setNewQuestion({ label: "", kind: "text", required: false, optionsCsv: "" }); }
  };
  const removeFormQuestion = async (questionId: string) => {
    const r = await fetch(`/api/recruitment/form/questions/${questionId}`, { method: "DELETE", credentials: "same-origin" });
    if (r.ok || r.status === 204) setFormQuestions((qs) => qs.filter((q) => q.id !== questionId));
  };
  // Internal-only visibility toggle (separate from the salary/type/remote details PUT).
  const setInternal = async (value: boolean) => {
    if (!pipelineReqId) return;
    setReqDetail((d) => (d ? { ...d, internal: value } : d));
    const r = await fetch(`/api/recruitment/requests/${pipelineReqId}/internal`, { method: "PUT", headers: { "Content-Type": "application/json" }, credentials: "same-origin", body: JSON.stringify({ internal: value }) });
    if (r.ok) setReqDetail(await r.json());
  };
  // Feature (promote) the selected role for N days, or clear with 0.
  const setFeatured = async (days: number) => {
    if (!pipelineReqId) return;
    const r = await fetch(`/api/recruitment/requests/${pipelineReqId}/feature`, { method: "PUT", headers: { "Content-Type": "application/json" }, credentials: "same-origin", body: JSON.stringify({ days }) });
    if (r.ok) setReqDetail(await r.json());
  };
  // Employee referrals.
  const submitReferral = async () => {
    if (!pipelineReqId || !newReferral.referrerName.trim() || !newReferral.candidateName.trim() || !newReferral.candidateEmail.trim()) return;
    const r = await fetch(`/api/recruitment/requests/${pipelineReqId}/referrals`, { method: "POST", headers: { "Content-Type": "application/json" }, credentials: "same-origin", body: JSON.stringify({ referrerName: newReferral.referrerName.trim(), referrerEmail: null, candidateName: newReferral.candidateName.trim(), candidateEmail: newReferral.candidateEmail.trim(), note: newReferral.note.trim() || null }) });
    if (r.ok) { const ref: Referral = await r.json(); setReferrals((rs) => [ref, ...rs]); setNewReferral({ referrerName: "", candidateName: "", candidateEmail: "", note: "" }); }
  };
  const approvalAction = async (action: "submit" | "approve" | "reject") => {
    if (!pipelineReqId) return;
    let body: Record<string, string> | undefined;
    if (action === "approve") body = { approver: session.name || "Approver" };
    if (action === "reject") {
      const reason = window.prompt(t("admin.approval.reasonPrompt", "Rejection reason:")) || "";
      if (!reason.trim()) return;
      body = { approver: session.name || "Approver", reason };
    }
    const r = await fetch(`/api/recruitment/requests/${pipelineReqId}/approval/${action}`, { method: "POST", headers: { "Content-Type": "application/json" }, credentials: "same-origin", body: body ? JSON.stringify(body) : undefined });
    if (r.ok) setApproval(await r.json());
  };
  const createCampaign = async () => {
    if (!newCampaign.name.trim() || !newCampaign.subject.trim() || !newCampaign.body.trim()) return;
    const r = await fetch("/api/recruitment/campaigns", { method: "POST", headers: { "Content-Type": "application/json" }, credentials: "same-origin", body: JSON.stringify(newCampaign) });
    if (r.ok) { const c: Campaign = await r.json(); setCampaigns((cs) => [c, ...(cs ?? [])]); setNewCampaign({ name: "", subject: "", body: "" }); }
  };
  const patchCampaign = (c: Campaign) => setCampaigns((cs) => cs?.map((x) => (x.id === c.id ? c : x)) ?? cs);
  const addCampaignRecipient = async (id: string) => {
    const email = (campaignRecipient[id] ?? "").trim();
    if (!email) return;
    const r = await fetch(`/api/recruitment/campaigns/${id}/recipients`, { method: "POST", headers: { "Content-Type": "application/json" }, credentials: "same-origin", body: JSON.stringify({ email }) });
    if (r.ok) { patchCampaign(await r.json()); setCampaignRecipient((m) => ({ ...m, [id]: "" })); }
  };
  const removeCampaignRecipient = async (id: string, email: string) => {
    const r = await fetch(`/api/recruitment/campaigns/${id}/recipients/${encodeURIComponent(email)}`, { method: "DELETE", credentials: "same-origin" });
    if (r.ok) patchCampaign(await r.json());
  };
  const sendCampaign = async (id: string) => {
    const r = await fetch(`/api/recruitment/campaigns/${id}/send`, { method: "POST", credentials: "same-origin" });
    if (r.ok) patchCampaign(await r.json());
  };
  const createTemplate = async () => {
    if (!newTemplate.name.trim() || !newTemplate.title.trim()) return;
    const r = await fetch("/api/recruitment/templates", { method: "POST", headers: { "Content-Type": "application/json" }, credentials: "same-origin", body: JSON.stringify({ name: newTemplate.name, title: newTemplate.title, city: newTemplate.city || null, positions: Number(newTemplate.positions) || 1, salaryMin: null, salaryMax: null, currency: "NAD", employmentType: "fulltime", remote: false, tags: [] }) });
    if (r.ok) { const tpl: JobTemplate = await r.json(); setTemplates((ts) => [tpl, ...(ts ?? [])]); setNewTemplate({ name: "", title: "", city: "", positions: 1 }); }
  };
  const deleteTemplate = async (id: string) => {
    const r = await fetch(`/api/recruitment/templates/${id}`, { method: "DELETE", credentials: "same-origin" });
    if (r.ok) setTemplates((ts) => ts?.filter((t) => t.id !== id) ?? ts);
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
                <div><h3 className="font-display text-[15px] font-bold text-ink-hi">{t("admin.pipeline.title", "Application pipeline")}</h3><p className="text-[11px] text-ink-lo mt-0.5">{t("admin.pipeline.sub", "Drag a card to a later stage — or use the buttons.")}</p></div>
                <div className="flex flex-wrap gap-1.5">
                  {pipelineReqs.map((r) => (
                    <button key={r.id} onClick={() => setPipelineReqId(r.id)} className={`rounded-lg px-2.5 py-1 text-[11px] font-semibold transition ${pipelineReqId === r.id ? "bg-brand/20 text-brand-bright" : "bg-panel2/60 text-ink-lo hover:text-ink-hi"}`}>{r.title}</button>
                  ))}
                </div>
              </div>
              {selectedApps.size > 0 && (
                <div className="mb-3 flex items-center gap-2 rounded-xl border border-brand/30 bg-brand/[0.06] px-3.5 py-2">
                  <span className="text-[12px] text-ink-hi">{t("admin.bulk.selected", "{{n}} selected", { n: selectedApps.size })}</span>
                  <button onClick={() => bulkTransition("advance")} className="rounded-lg bg-brand/15 px-2.5 py-1 text-[11px] font-semibold text-brand-bright hover:bg-brand/25 transition">{t("admin.bulk.advance", "Advance all")}</button>
                  <button onClick={() => bulkTransition("reject")} className="rounded-lg bg-pink/15 px-2.5 py-1 text-[11px] font-semibold text-pink hover:bg-pink/25 transition">{t("admin.bulk.reject", "Reject all")}</button>
                  <button onClick={() => setSelectedApps(new Set())} className="ml-auto text-[11px] text-ink-lo hover:text-ink-hi transition">{t("admin.bulk.clear", "Clear")}</button>
                </div>
              )}
              {reqDetail && (
                <div className="mb-4 rounded-xl border border-line/60 bg-panel2/30 p-3.5">
                  <div className="flex flex-wrap items-end gap-3">
                    <label className="text-[11px] text-ink-lo">{t("admin.req.salaryMin", "Salary min (N$)")}
                      <input type="number" value={reqDetail.salaryMin ?? ""} onChange={(e) => setReqDetail((d) => (d ? { ...d, salaryMin: e.target.value ? Number(e.target.value) : null } : d))} onBlur={() => saveReqDetail({})} className="mt-1 block w-28 rounded-lg border border-line/70 bg-panel2/50 px-2 py-1 text-[12px] text-ink-hi focus:border-brand/50 focus:outline-none" />
                    </label>
                    <label className="text-[11px] text-ink-lo">{t("admin.req.salaryMax", "Salary max (N$)")}
                      <input type="number" value={reqDetail.salaryMax ?? ""} onChange={(e) => setReqDetail((d) => (d ? { ...d, salaryMax: e.target.value ? Number(e.target.value) : null } : d))} onBlur={() => saveReqDetail({})} className="mt-1 block w-28 rounded-lg border border-line/70 bg-panel2/50 px-2 py-1 text-[12px] text-ink-hi focus:border-brand/50 focus:outline-none" />
                    </label>
                    <label className="text-[11px] text-ink-lo">{t("admin.req.type", "Type")}
                      <select value={reqDetail.employmentType} onChange={(e) => saveReqDetail({ employmentType: e.target.value })} className="mt-1 block rounded-lg border border-line/70 bg-panel2/50 px-2 py-1 text-[12px] text-ink-hi capitalize focus:border-brand/50 focus:outline-none">
                        {["fulltime", "parttime", "contract", "internship", "temporary"].map((tt) => <option key={tt} value={tt}>{tt}</option>)}
                      </select>
                    </label>
                    <label className="flex items-center gap-2 text-[11px] text-ink-mid"><input type="checkbox" checked={reqDetail.remote} onChange={(e) => saveReqDetail({ remote: e.target.checked })} />{t("admin.req.remote", "Remote")}</label>
                    <label className="flex items-center gap-2 text-[11px] text-ink-mid" title={t("admin.req.internalHint", "Hidden from the public careers site; open to referrals only.")}><input type="checkbox" checked={reqDetail.internal} onChange={(e) => setInternal(e.target.checked)} />{t("admin.req.internal", "Internal only")}</label>
                    <div className="flex items-center gap-1.5" title={t("admin.req.featureHint", "Promote this role to the top of the public careers site (payment handled out-of-band).")}>
                      {reqDetail.featuredUntil && new Date(reqDetail.featuredUntil) > new Date()
                        ? <span className="chip !text-[10px] !text-gold !border-gold/30">★ {t("admin.req.featuredUntil", "Featured to {{date}}", { date: new Date(reqDetail.featuredUntil).toLocaleDateString() })}</span>
                        : <span className="text-[11px] text-ink-lo">{t("admin.req.notFeatured", "Not featured")}</span>}
                      <button onClick={() => setFeatured(7)} className="rounded bg-gold/15 px-2 py-0.5 text-[10px] font-semibold text-gold hover:bg-gold/25 transition">{t("admin.req.feature7", "Feature 7d")}</button>
                      <button onClick={() => setFeatured(30)} className="rounded bg-gold/15 px-2 py-0.5 text-[10px] font-semibold text-gold hover:bg-gold/25 transition">{t("admin.req.feature30", "30d")}</button>
                      {reqDetail.featuredUntil && new Date(reqDetail.featuredUntil) > new Date() && <button onClick={() => setFeatured(0)} className="rounded px-2 py-0.5 text-[10px] font-semibold text-ink-lo hover:text-pink transition">{t("admin.req.unfeature", "Unfeature")}</button>}
                    </div>
                  </div>
                  <div className="mt-3 flex flex-wrap items-center gap-1.5">
                    {reqDetail.tags.map((tag) => (
                      <span key={tag} className="chip !text-[10px] !text-brand-bright !border-brand/30">{tag} <button onClick={() => removeReqTag(tag)} className="ml-1 hover:text-pink">✕</button></span>
                    ))}
                    <input value={reqTagDraft} onChange={(e) => setReqTagDraft(e.target.value)} onKeyDown={(e) => { if (e.key === "Enter") addReqTag(); }} placeholder={t("admin.req.addTag", "Add tag")} className="w-28 rounded-lg border border-line/70 bg-panel2/50 px-2 py-1 text-[11px] text-ink-hi placeholder:text-ink-lo focus:border-brand/50 focus:outline-none" />
                  </div>
                  {approval && (
                    <div className="mt-3 border-t border-line/40 pt-3 flex flex-wrap items-center gap-2">
                      <span className="eyebrow">{t("admin.approval.title", "Approval")}</span>
                      <span className={`chip !text-[10px] capitalize ${approval.status === "approved" ? "!text-brand-bright !border-brand/30" : approval.status === "rejected" ? "!text-pink !border-pink/30" : approval.status === "submitted" ? "!text-gold !border-gold/30" : "!text-ink-lo !border-line/70"}`}>{approval.status}</span>
                      {approval.approver && <span className="text-[11px] text-ink-lo">by {approval.approver}</span>}
                      {approval.reason && <span className="text-[11px] text-pink" title={approval.reason}>— {approval.reason}</span>}
                      <div className="ml-auto flex gap-1.5">
                        {(approval.status === "draft" || approval.status === "rejected") && <button onClick={() => approvalAction("submit")} className="rounded-lg bg-brand/15 px-2.5 py-1 text-[11px] font-semibold text-brand-bright hover:bg-brand/25 transition">{t("admin.approval.submit", "Submit")}</button>}
                        {approval.status === "submitted" && <>
                          <button onClick={() => approvalAction("approve")} className="rounded-lg bg-brand/15 px-2.5 py-1 text-[11px] font-semibold text-brand-bright hover:bg-brand/25 transition">{t("admin.approval.approve", "Approve")}</button>
                          <button onClick={() => approvalAction("reject")} className="rounded-lg bg-pink/15 px-2.5 py-1 text-[11px] font-semibold text-pink hover:bg-pink/25 transition">{t("admin.approval.reject", "Reject")}</button>
                        </>}
                      </div>
                    </div>
                  )}
                </div>
              )}
              {/* Application form / screening questions for the selected role */}
              <div className="mb-4 rounded-xl border border-line/60 bg-panel2/30 p-3">
                <div className="eyebrow mb-2">{t("admin.form.title", "Application form / screening questions")}</div>
                <div className="space-y-1.5 mb-2">
                  {formQuestions.map((q) => (
                    <div key={q.id} className="flex items-center gap-2 rounded-lg border border-line/50 bg-panel/40 px-2.5 py-1.5">
                      <div className="min-w-0 flex-1">
                        <div className="text-[12px] text-ink-hi truncate">{q.label} {q.required && <span className="text-pink" title={t("admin.form.required", "Required")}>*</span>}</div>
                        <div className="text-[10px] text-ink-lo">{q.kind}{q.options.length > 0 ? ` · ${q.options.join(" / ")}` : ""}</div>
                      </div>
                      <button onClick={() => removeFormQuestion(q.id)} className="text-ink-lo hover:text-pink text-[11px]" title={t("admin.form.remove", "Remove")}>✕</button>
                    </div>
                  ))}
                  {formQuestions.length === 0 && <div className="text-[11px] text-ink-lo">{t("admin.form.none", "No questions — applicants apply without a form.")}</div>}
                </div>
                <div className="flex flex-wrap items-center gap-2">
                  <input value={newQuestion.label} onChange={(e) => setNewQuestion((f) => ({ ...f, label: e.target.value }))} placeholder={t("admin.form.question", "Question")} className="flex-1 min-w-[160px] rounded-lg border border-line/70 bg-panel2/50 px-2.5 py-1 text-[12px] text-ink-hi placeholder:text-ink-lo focus:border-brand/50 focus:outline-none" />
                  <select value={newQuestion.kind} onChange={(e) => setNewQuestion((f) => ({ ...f, kind: e.target.value }))} className="rounded-lg border border-line/70 bg-panel2/50 px-2 py-1 text-[12px] text-ink-hi capitalize focus:border-brand/50 focus:outline-none">
                    {["text", "textarea", "boolean", "number", "select"].map((k) => <option key={k} value={k}>{k}</option>)}
                  </select>
                  {newQuestion.kind === "select" && <input value={newQuestion.optionsCsv} onChange={(e) => setNewQuestion((f) => ({ ...f, optionsCsv: e.target.value }))} placeholder={t("admin.form.options", "Options, comma-separated")} className="flex-1 min-w-[140px] rounded-lg border border-line/70 bg-panel2/50 px-2.5 py-1 text-[12px] text-ink-hi placeholder:text-ink-lo focus:border-brand/50 focus:outline-none" />}
                  <label className="flex items-center gap-1.5 text-[11px] text-ink-mid"><input type="checkbox" checked={newQuestion.required} onChange={(e) => setNewQuestion((f) => ({ ...f, required: e.target.checked }))} />{t("admin.form.requiredLabel", "Required")}</label>
                  <button onClick={addFormQuestion} disabled={!newQuestion.label.trim()} className="rounded-lg bg-brand/15 px-3 py-1 text-[11px] font-semibold text-brand-bright hover:bg-brand/25 transition disabled:opacity-50">{t("admin.form.add", "Add question")}</button>
                </div>
              </div>
              {/* Employee referrals for the selected role */}
              <div className="mb-4 rounded-xl border border-line/60 bg-panel2/30 p-3">
                <div className="eyebrow mb-2">{t("admin.ref.title", "Employee referrals")}</div>
                <div className="space-y-1.5 mb-2">
                  {referrals.map((ref) => (
                    <div key={ref.id} className="rounded-lg border border-line/50 bg-panel/40 px-2.5 py-1.5">
                      <div className="text-[12px] text-ink-hi">{ref.candidateName} <span className="text-[10px] text-ink-lo">· {ref.candidateEmail}</span></div>
                      <div className="text-[10px] text-ink-lo">{t("admin.ref.by", "referred by {{name}}", { name: ref.referrerName })}{ref.note ? ` — ${ref.note}` : ""}</div>
                    </div>
                  ))}
                  {referrals.length === 0 && <div className="text-[11px] text-ink-lo">{t("admin.ref.none", "No referrals yet.")}</div>}
                </div>
                <div className="flex flex-wrap items-center gap-2">
                  <input value={newReferral.referrerName} onChange={(e) => setNewReferral((f) => ({ ...f, referrerName: e.target.value }))} placeholder={t("admin.ref.referrer", "Referrer name")} className="flex-1 min-w-[120px] rounded-lg border border-line/70 bg-panel2/50 px-2.5 py-1 text-[12px] text-ink-hi placeholder:text-ink-lo focus:border-brand/50 focus:outline-none" />
                  <input value={newReferral.candidateName} onChange={(e) => setNewReferral((f) => ({ ...f, candidateName: e.target.value }))} placeholder={t("admin.ref.candidate", "Candidate name")} className="flex-1 min-w-[120px] rounded-lg border border-line/70 bg-panel2/50 px-2.5 py-1 text-[12px] text-ink-hi placeholder:text-ink-lo focus:border-brand/50 focus:outline-none" />
                  <input value={newReferral.candidateEmail} onChange={(e) => setNewReferral((f) => ({ ...f, candidateEmail: e.target.value }))} placeholder={t("admin.ref.email", "Candidate email")} className="flex-1 min-w-[120px] rounded-lg border border-line/70 bg-panel2/50 px-2.5 py-1 text-[12px] text-ink-hi placeholder:text-ink-lo focus:border-brand/50 focus:outline-none" />
                  <input value={newReferral.note} onChange={(e) => setNewReferral((f) => ({ ...f, note: e.target.value }))} placeholder={t("admin.ref.note", "Note (optional)")} className="flex-1 min-w-[120px] rounded-lg border border-line/70 bg-panel2/50 px-2.5 py-1 text-[12px] text-ink-hi placeholder:text-ink-lo focus:border-brand/50 focus:outline-none" />
                  <button onClick={submitReferral} disabled={!newReferral.referrerName.trim() || !newReferral.candidateName.trim() || !newReferral.candidateEmail.trim()} className="rounded-lg bg-brand/15 px-3 py-1 text-[11px] font-semibold text-brand-bright hover:bg-brand/25 transition disabled:opacity-50">{t("admin.ref.add", "Refer")}</button>
                </div>
              </div>
              <div className="grid grid-cols-2 md:grid-cols-3 xl:grid-cols-5 gap-3">
                {pipelineStages.map((stage) => {
                  const cards = (pipelineApps ?? []).filter((a) => a.status === stage);
                  const terminal = stage === "hired" || stage === "rejected";
                  // Is `stage` a legal drop target for the card currently being dragged?
                  const dragging = dragAppId ? (pipelineApps ?? []).find((a) => a.id === dragAppId) : null;
                  const validDrop = !!dragging && dragging.status !== stage && (stage === "rejected"
                    ? advanceOrder.includes(dragging.status)
                    : advanceOrder.indexOf(stage) > advanceOrder.indexOf(dragging.status));
                  return (
                    <div
                      key={stage}
                      onDragOver={(e) => { if (validDrop) { e.preventDefault(); setDragOverStage(stage); } }}
                      onDragLeave={() => setDragOverStage((s) => (s === stage ? null : s))}
                      onDrop={(e) => { e.preventDefault(); const id = dragAppId ?? e.dataTransfer.getData("text/plain"); if (id && validDrop) moveApp(id, stage); setDragAppId(null); setDragOverStage(null); }}
                      className={`rounded-xl border p-2.5 transition ${dragOverStage === stage && validDrop ? "border-brand/60 bg-brand/[0.08] ring-1 ring-brand/40" : validDrop ? "border-dashed border-brand/40 bg-panel2/30" : "border-line/60 bg-panel2/30"}`}
                    >
                      <div className="flex items-center justify-between mb-2"><span className="eyebrow capitalize">{stage}</span><span className="num text-[11px] text-ink-lo">{cards.length}</span></div>
                      <div className="space-y-2">
                        {cards.map((a) => (
                          <div
                            key={a.id}
                            draggable={!terminal}
                            onDragStart={(e) => { setDragAppId(a.id); e.dataTransfer.effectAllowed = "move"; e.dataTransfer.setData("text/plain", a.id); }}
                            onDragEnd={() => { setDragAppId(null); setDragOverStage(null); }}
                            className={`rounded-lg border p-2 ${!terminal ? "cursor-grab active:cursor-grabbing" : ""} ${dragAppId === a.id ? "opacity-50" : ""} ${stage === "hired" ? "border-brand/40 bg-brand/[0.06]" : stage === "rejected" ? "border-pink/30 bg-pink/[0.05]" : "border-line/50 bg-panel/40"}`}>
                            <div className="flex items-center justify-between gap-1"><label className="flex items-center gap-1.5 min-w-0"><input type="checkbox" checked={selectedApps.has(a.id)} onChange={() => toggleAppSelect(a.id)} /><span className="text-[11px] text-ink-hi capitalize truncate">{a.talentType}</span></label><span className="num text-[11px] text-brand-bright">{Math.round(a.matchScore)}%</span></div>
                            {stage === "rejected" && a.rejectReason && <div className="mt-1 text-[10px] text-ink-lo italic">“{a.rejectReason}”</div>}
                            {!terminal && (
                              <div className="mt-1.5 flex gap-1.5">
                                <button onClick={() => transitionApp(a.id, "advance")} className="rounded bg-brand/15 px-2 py-0.5 text-[10px] font-semibold text-brand-bright hover:bg-brand/25 transition">{t("admin.pipeline.advance", "Advance")}</button>
                                <button onClick={() => transitionApp(a.id, "reject")} className="rounded bg-pink/15 px-2 py-0.5 text-[10px] font-semibold text-pink hover:bg-pink/25 transition">{t("admin.pipeline.reject", "Reject")}</button>
                              </div>
                            )}
                            {(stage === "reviewed" || stage === "shortlisted" || stage === "hired") && (
                              <button onClick={() => setOfferAppId(offerAppId === a.id ? null : a.id)} className={`mt-1.5 w-full rounded px-2 py-0.5 text-[10px] font-semibold transition ${offerAppId === a.id ? "bg-gold/25 text-gold" : "bg-panel/60 text-ink-lo hover:text-ink-hi"}`}>{t("admin.manage.open", "Manage")}</button>
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
                  <div className="flex items-center justify-between mb-2 gap-3 flex-wrap">
                    <span className="eyebrow">{t("admin.offer.title", "Offers for selected applicant")}</span>
                    <div className="flex items-center gap-2">
                      <span className="text-[10px] text-ink-lo uppercase tracking-wide">{t("admin.source.label", "Source")}</span>
                      <select value={SOURCE_CHANNELS.includes(appSource) ? appSource : "direct"} onChange={(e) => setApplicationSource(e.target.value)} className="rounded-lg border border-line/70 bg-panel2/50 px-2 py-1 text-[11px] text-ink-hi capitalize focus:border-brand/50 focus:outline-none">
                        {SOURCE_CHANNELS.map((s) => <option key={s} value={s}>{s}</option>)}
                      </select>
                      <button onClick={() => setOfferAppId(null)} className="text-[11px] text-ink-lo hover:text-ink-hi transition">{t("admin.offer.close", "Close")}</button>
                    </div>
                  </div>
                  <div className="space-y-2">
                    {(offers ?? []).map((o) => (
                      <div key={o.id} className="flex items-center gap-3 rounded-lg border border-line/50 bg-panel2/40 px-3 py-2">
                        <div className="min-w-0 flex-1"><div className="text-[13px] font-semibold text-ink-hi truncate">{o.title}</div><div className="text-[11px] text-ink-lo">{o.currency} {o.salaryAmount.toLocaleString()} · starts {o.startDate}</div></div>
                        <span className={`chip !text-[10px] capitalize ${o.status === "accepted" ? "!text-brand-bright !border-brand/30" : o.status === "declined" || o.status === "withdrawn" ? "!text-pink !border-pink/30" : "!text-gold !border-gold/30"}`}>{o.status}</span>
                        <a href={`/api/recruitment/offers/${o.id}/letter`} target="_blank" rel="noreferrer" className="rounded px-2 py-1 text-[10px] font-semibold text-ink-lo hover:text-brand-bright transition">{t("admin.offer.letter", "Letter")}</a>
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
                  <div className="mt-4 border-t border-line/40 pt-4">
                    <div className="flex items-center justify-between mb-2">
                      <span className="eyebrow">{t("admin.onboarding.title", "Onboarding checklist")}</span>
                      {onboarding && onboarding !== "none" && <span className="num text-[11px] text-ink-lo">{onboarding.completed}/{onboarding.total} {t("admin.onboarding.done", "done")}</span>}
                    </div>
                    {onboarding === "none" ? (
                      <button onClick={() => startOnboarding(offers?.[0]?.title ?? "New hire")} className="rounded-lg bg-brand/15 px-3 py-1.5 text-[11px] font-semibold text-brand-bright hover:bg-brand/25 transition">{t("admin.onboarding.start", "Start onboarding")}</button>
                    ) : onboarding ? (
                      <>
                        <div className="h-2 rounded-full bg-panel2/70 overflow-hidden mb-3"><div className="h-full rounded-full bg-gradient-to-r from-brand-deep to-brand-bright" style={{ width: (onboarding.total ? (onboarding.completed / onboarding.total) * 100 : 0) + "%" }} /></div>
                        <div className="space-y-1.5">
                          {onboarding.tasks.map((tk) => (
                            <label key={tk.id} className="flex items-center gap-2.5 text-[13px] text-ink-mid cursor-pointer">
                              <input type="checkbox" checked={tk.isDone} onChange={(e) => toggleTask(tk.id, e.target.checked)} />
                              <span className={tk.isDone ? "line-through text-ink-lo" : ""}>{tk.label}</span>
                            </label>
                          ))}
                        </div>
                      </>
                    ) : (
                      <div className="text-[12px] text-ink-lo">{t("admin.onboarding.loading", "…")}</div>
                    )}
                  </div>
                  {appAnswers.length > 0 && (
                    <div className="mt-4 border-t border-line/40 pt-4">
                      <div className="eyebrow mb-2">{t("admin.answers.title", "Application answers")}</div>
                      <div className="space-y-1.5">
                        {appAnswers.map((a) => (
                          <div key={a.questionId} className="rounded-lg border border-line/50 bg-panel/40 px-2.5 py-1.5">
                            <div className="text-[10px] text-ink-lo">{a.label}</div>
                            <div className="text-[12px] text-ink-hi">{a.value}</div>
                          </div>
                        ))}
                      </div>
                    </div>
                  )}
                  <div className="mt-4 border-t border-line/40 pt-4">
                    <div className="eyebrow mb-2">{t("admin.msg.title", "Messages with candidate")}</div>
                    <div className="space-y-1.5 mb-2 max-h-48 overflow-y-auto">
                      {messages.map((m) => (
                        <div key={m.id} className={`rounded-lg px-2.5 py-1.5 text-[12px] ${m.sender === "recruiter" ? "bg-brand/[0.08] ml-8" : "bg-panel2/50 mr-8"}`}>
                          <div className="flex items-center justify-between gap-2"><span className="text-[10px] font-semibold text-ink-lo capitalize">{m.senderName} · {m.sender}</span><span className="text-[10px] text-ink-lo">{new Date(m.sentAt).toLocaleDateString()}</span></div>
                          <div className="text-ink-mid">{m.body}</div>
                        </div>
                      ))}
                      {messages.length === 0 && <div className="py-2 text-center text-[12px] text-ink-lo">{t("admin.msg.none", "No messages yet.")}</div>}
                    </div>
                    <div className="flex gap-2">
                      <input value={msgDraft} onChange={(e) => setMsgDraft(e.target.value)} onKeyDown={(e) => { if (e.key === "Enter") sendMessage(); }} placeholder={t("admin.msg.placeholder", "Message the candidate…")} className="flex-1 rounded-lg border border-line/70 bg-panel2/50 px-2.5 py-1.5 text-[12px] text-ink-hi placeholder:text-ink-lo focus:border-brand/50 focus:outline-none" />
                      <button onClick={sendMessage} disabled={!msgDraft.trim()} className="rounded-lg bg-brand/15 px-3 py-1.5 text-[11px] font-semibold text-brand-bright hover:bg-brand/25 transition disabled:opacity-50">{t("admin.msg.send", "Send")}</button>
                    </div>
                  </div>
                  {/* Interviews + panel attendees */}
                  <div className="mt-4 border-t border-line/40 pt-4">
                    <div className="eyebrow mb-2">{t("admin.iv.title", "Interviews & panel")}</div>
                    <div className="flex flex-wrap items-end gap-2 mb-3">
                      <input type="datetime-local" value={newInterview.scheduledAt} onChange={(e) => setNewInterview((f) => ({ ...f, scheduledAt: e.target.value }))} className="rounded-lg border border-line/70 bg-panel2/50 px-2.5 py-1.5 text-[12px] text-ink-hi focus:border-brand/50 focus:outline-none" />
                      <input type="number" min={15} step={15} value={newInterview.durationMinutes} onChange={(e) => setNewInterview((f) => ({ ...f, durationMinutes: Number(e.target.value) }))} title={t("admin.iv.duration", "Duration (min)")} className="w-20 rounded-lg border border-line/70 bg-panel2/50 px-2 py-1.5 text-[12px] text-ink-hi focus:border-brand/50 focus:outline-none" />
                      <input value={newInterview.location} onChange={(e) => setNewInterview((f) => ({ ...f, location: e.target.value }))} placeholder={t("admin.iv.location", "Location / link")} className="flex-1 min-w-[140px] rounded-lg border border-line/70 bg-panel2/50 px-2.5 py-1.5 text-[12px] text-ink-hi placeholder:text-ink-lo focus:border-brand/50 focus:outline-none" />
                      <input value={newInterview.round} onChange={(e) => setNewInterview((f) => ({ ...f, round: e.target.value }))} placeholder={t("admin.iv.round", "Round (e.g. Technical)")} className="w-40 rounded-lg border border-line/70 bg-panel2/50 px-2.5 py-1.5 text-[12px] text-ink-hi placeholder:text-ink-lo focus:border-brand/50 focus:outline-none" />
                      <input value={newInterview.skills} onChange={(e) => setNewInterview((f) => ({ ...f, skills: e.target.value }))} placeholder={t("admin.iv.skills", "Skills to assess (comma-sep)")} className="flex-1 min-w-[140px] rounded-lg border border-line/70 bg-panel2/50 px-2.5 py-1.5 text-[12px] text-ink-hi placeholder:text-ink-lo focus:border-brand/50 focus:outline-none" />
                      <button onClick={scheduleInterview} disabled={ivBusy || !newInterview.scheduledAt || !newInterview.location.trim()} className="rounded-lg bg-brand/15 px-3 py-1.5 text-[11px] font-semibold text-brand-bright hover:bg-brand/25 transition disabled:opacity-50">{t("admin.iv.schedule", "Schedule")}</button>
                    </div>
                    <div className="space-y-2">
                      {interviews.map((iv) => (
                        <div key={iv.id} className="rounded-lg border border-line/50 bg-panel2/40">
                          <div className="flex items-center gap-3 px-3 py-2">
                            <button onClick={() => setIvOpen(ivOpen === iv.id ? null : iv.id)} className="min-w-0 flex-1 text-left">
                              <div className="text-[12px] text-ink-hi truncate">{iv.round ? <span className="text-brand-bright">{iv.round} · </span> : null}{new Date(iv.scheduledAt).toLocaleString()} · {iv.durationMinutes}m</div>
                              <div className="text-[10px] text-ink-lo truncate">{iv.location}{iv.requiredSkills.length > 0 ? ` · ${iv.requiredSkills.join(", ")}` : ""}</div>
                            </button>
                            <span className={`chip !text-[10px] capitalize ${iv.status === "cancelled" ? "!text-pink !border-pink/30" : iv.status === "completed" ? "!text-brand-bright !border-brand/30" : "!text-gold !border-gold/30"}`}>{iv.status}</span>
                            <a href={`/api/recruitment/interviews/${iv.id}/ics`} target="_blank" rel="noreferrer" className="text-[10px] font-semibold text-ink-lo hover:text-brand-bright transition">{t("admin.iv.ics", ".ics")}</a>
                            {iv.status !== "cancelled" && iv.status !== "completed" && <button onClick={() => cancelInterview(iv.id)} className="text-[10px] font-semibold text-ink-lo hover:text-pink transition">{t("admin.iv.cancel", "Cancel")}</button>}
                            <button onClick={() => setIvOpen(ivOpen === iv.id ? null : iv.id)} className="text-ink-lo text-[11px]">{ivOpen === iv.id ? "▲" : "▼"}</button>
                          </div>
                          {ivOpen === iv.id && (
                            <div className="border-t border-line/40 px-3 py-2.5">
                              <div className="eyebrow mb-1.5">{t("admin.iv.panel", "Panel attendees")}</div>
                              <div className="space-y-1.5 mb-2">
                                {attendees.map((at) => (
                                  <div key={at.id} className="flex items-center gap-2 rounded-lg border border-line/50 bg-panel/40 px-2.5 py-1.5">
                                    <div className="min-w-0 flex-1"><div className="text-[12px] text-ink-hi truncate">{at.name} <span className="text-[10px] text-ink-lo capitalize">· {at.role}</span></div>{at.email && <div className="text-[10px] text-ink-lo truncate">{at.email}</div>}</div>
                                    <button onClick={() => removeAttendee(at.id)} className="text-ink-lo hover:text-pink text-[11px]" title={t("admin.iv.removeAttendee", "Remove")}>✕</button>
                                  </div>
                                ))}
                                {attendees.length === 0 && <div className="text-[11px] text-ink-lo">{t("admin.iv.noPanel", "No panellists yet.")}</div>}
                              </div>
                              <div className="flex flex-wrap items-center gap-2">
                                <input value={newAttendee.name} onChange={(e) => setNewAttendee((f) => ({ ...f, name: e.target.value }))} placeholder={t("admin.iv.attName", "Name")} className="flex-1 min-w-[120px] rounded-lg border border-line/70 bg-panel2/50 px-2.5 py-1 text-[12px] text-ink-hi placeholder:text-ink-lo focus:border-brand/50 focus:outline-none" />
                                <input value={newAttendee.email} onChange={(e) => setNewAttendee((f) => ({ ...f, email: e.target.value }))} placeholder={t("admin.iv.attEmail", "Email (optional)")} className="flex-1 min-w-[120px] rounded-lg border border-line/70 bg-panel2/50 px-2.5 py-1 text-[12px] text-ink-hi placeholder:text-ink-lo focus:border-brand/50 focus:outline-none" />
                                <input value={newAttendee.role} onChange={(e) => setNewAttendee((f) => ({ ...f, role: e.target.value }))} placeholder={t("admin.iv.attRole", "Role")} className="w-28 rounded-lg border border-line/70 bg-panel2/50 px-2.5 py-1 text-[12px] text-ink-hi placeholder:text-ink-lo focus:border-brand/50 focus:outline-none" />
                                <button onClick={() => addAttendee(iv.id)} disabled={!newAttendee.name.trim()} className="rounded-lg bg-brand/15 px-3 py-1 text-[11px] font-semibold text-brand-bright hover:bg-brand/25 transition disabled:opacity-50">{t("admin.iv.addAttendee", "Add")}</button>
                              </div>
                              {(() => {
                                const skills = Array.from(new Set([...iv.requiredSkills, ...Object.keys(skillRatings)]));
                                return (
                                  <div className="mt-3 border-t border-line/40 pt-2.5">
                                    <div className="eyebrow mb-1.5">{t("admin.iv.skillRatings", "Skill ratings (1–5)")}</div>
                                    {skills.length === 0 ? (
                                      <div className="text-[11px] text-ink-lo">{t("admin.iv.noSkills", "No skills set for this round — add them when scheduling.")}</div>
                                    ) : (
                                      <div className="space-y-1.5">
                                        {skills.map((sk) => (
                                          <div key={sk} className="flex items-center gap-2">
                                            <span className="text-[12px] text-ink-hi capitalize flex-1">{sk}</span>
                                            {[1, 2, 3, 4, 5].map((n) => (
                                              <button key={n} onClick={() => setSkillRatings((m) => ({ ...m, [sk]: n }))} className={`h-6 w-6 rounded text-[11px] font-semibold transition ${(skillRatings[sk] ?? 0) >= n ? "bg-brand/25 text-brand-bright" : "bg-panel2/60 text-ink-lo hover:text-ink-hi"}`}>{n}</button>
                                            ))}
                                          </div>
                                        ))}
                                        <button onClick={() => saveSkillRatings(iv.id)} className="mt-1 rounded-lg bg-brand/15 px-3 py-1 text-[11px] font-semibold text-brand-bright hover:bg-brand/25 transition">{t("admin.iv.saveRatings", "Save ratings")}</button>
                                      </div>
                                    )}
                                  </div>
                                );
                              })()}
                            </div>
                          )}
                        </div>
                      ))}
                      {interviews.length === 0 && <div className="text-[12px] text-ink-lo">{t("admin.iv.none", "No interviews scheduled.")}</div>}
                    </div>
                    {ivSummary && ivSummary.skillAverages.length > 0 && (
                      <div className="mt-3 border-t border-line/40 pt-2.5">
                        <div className="eyebrow mb-1.5">{t("admin.iv.summary", "Skill averages across rounds")}</div>
                        <div className="space-y-1.5">
                          {ivSummary.skillAverages.map((s) => (
                            <div key={s.skill}>
                              <div className="flex justify-between text-xs mb-1"><span className="text-ink-hi font-medium capitalize">{s.skill}</span><span className="num text-[11px] text-ink-mid">{s.average.toFixed(1)} / 5 · {s.count}×</span></div>
                              <div className="h-2 rounded-full bg-panel2/70 overflow-hidden"><div className="h-full rounded-full bg-gradient-to-r from-brand-deep to-brand-bright" style={{ width: Math.round((s.average / 5) * 100) + "%" }} /></div>
                            </div>
                          ))}
                        </div>
                      </div>
                    )}
                  </div>
                </div>
              )}
            </motion.section>
          )}

          {diversity && (
            <motion.section variants={fade} className="card p-5">
              <div className="mb-3"><h3 className="font-display text-[15px] font-bold text-ink-hi">{t("admin.div.title", "Diversity report")}</h3><p className="text-[11px] text-ink-lo mt-0.5">{t("admin.div.sub", "Anonymised candidate-pool breakdown ({{n}} total).", { n: diversity.total })}</p></div>
              <div className="grid gap-4 sm:grid-cols-2">
                {([["admin.div.nationality", "By nationality", diversity.byNationality], ["admin.div.city", "By city", diversity.byCity]] as const).map(([key, label, rows]) => (
                  <div key={key}>
                    <div className="eyebrow mb-2">{t(key, label)}</div>
                    <div className="space-y-2">
                      {rows.slice(0, 8).map((r) => {
                        const pct = diversity.total > 0 ? Math.round((r.count / diversity.total) * 100) : 0;
                        return (
                          <div key={r.label}>
                            <div className="flex justify-between text-xs mb-1"><span className="text-ink-hi font-medium">{r.label}</span><span className="num text-[11px] text-ink-mid">{r.count} · {pct}%</span></div>
                            <div className="h-2 rounded-full bg-panel2/70 overflow-hidden"><div className="h-full rounded-full bg-gradient-to-r from-brand-deep to-brand-bright" style={{ width: pct + "%" }} /></div>
                          </div>
                        );
                      })}
                    </div>
                  </div>
                ))}
              </div>
            </motion.section>
          )}

          {hiring && (
            <motion.section variants={fade} className="card p-5">
              <div className="flex items-center justify-between mb-3 gap-3 flex-wrap">
                <div><h3 className="font-display text-[15px] font-bold text-ink-hi">{t("admin.hire.title", "Hiring metrics")}</h3><p className="text-[11px] text-ink-lo mt-0.5">{t("admin.hire.sub", "Time-to-hire and source-of-hire.")}</p></div>
                <div className="flex items-center gap-3">
                  <a href="/api/recruitment/reports/source-of-hire.csv" className="text-[11px] text-ink-lo hover:text-brand-bright transition">{t("admin.hire.csvSource", "Source CSV")}</a>
                  <a href="/api/recruitment/reports/source-of-hire.pdf" className="text-[11px] text-ink-lo hover:text-brand-bright transition">{t("admin.hire.pdfSource", "Source PDF")}</a>
                  <a href="/api/recruitment/reports/funnel.csv" className="text-[11px] text-ink-lo hover:text-brand-bright transition">{t("admin.hire.csvFunnel", "Funnel CSV")}</a>
                  <a href="/api/recruitment/reports/funnel.pdf" className="text-[11px] text-ink-lo hover:text-brand-bright transition">{t("admin.hire.pdfFunnel", "Funnel PDF")}</a>
                </div>
              </div>
              <div className="grid grid-cols-3 gap-4 mb-4">
                {[[t("admin.hire.avg", "Avg time-to-hire"), `${hiring.avgTimeToHireDays}d`], [t("admin.hire.median", "Median"), `${hiring.medianTimeToHireDays}d`], [t("admin.hire.hires", "Hires"), `${hiring.hires}`]].map(([label, val], i) => (
                  <div key={i}><span className="eyebrow">{label as string}</span><div className="num text-[26px] font-bold text-ink-hi leading-none mt-1.5">{val as string}</div></div>
                ))}
              </div>
              <div className="eyebrow mb-2">{t("admin.hire.source", "By source")}</div>
              <div className="space-y-2">
                {hiring.bySource.map((s) => {
                  const rate = s.applications > 0 ? Math.round((s.hires / s.applications) * 100) : 0;
                  return (
                    <div key={s.source}>
                      <div className="flex justify-between text-xs mb-1"><span className="text-ink-hi font-medium capitalize">{s.source}</span><span className="num text-[11px] text-ink-mid">{s.hires}/{s.applications} · {rate}%</span></div>
                      <div className="h-2 rounded-full bg-panel2/70 overflow-hidden"><div className="h-full rounded-full bg-gradient-to-r from-brand-deep to-brand-bright" style={{ width: rate + "%" }} /></div>
                    </div>
                  );
                })}
                {hiring.bySource.length === 0 && <div className="text-[12px] text-ink-lo">{t("admin.hire.none", "No application data.")}</div>}
              </div>
            </motion.section>
          )}

          {channels && channels.length > 0 && (
            <motion.section variants={fade} className="card p-5">
              <div className="mb-3"><h3 className="font-display text-[15px] font-bold text-ink-hi">{t("admin.channels.title", "Source of applications")}</h3><p className="text-[11px] text-ink-lo mt-0.5">{t("admin.channels.sub", "Applications and hires by arrival channel.")}</p></div>
              <div className="space-y-2">
                {(() => {
                  const max = Math.max(...channels.map((c) => c.applications), 1);
                  return channels.map((c) => (
                    <div key={c.source}>
                      <div className="flex justify-between text-xs mb-1"><span className="text-ink-hi font-medium capitalize">{c.source}</span><span className="num text-[11px] text-ink-mid">{c.applications} {t("admin.channels.apps", "apps")} · {c.hires} {t("admin.channels.hires", "hires")}</span></div>
                      <div className="h-2 rounded-full bg-panel2/70 overflow-hidden"><div className="h-full rounded-full bg-gradient-to-r from-brand-deep to-brand-bright" style={{ width: Math.round((c.applications / max) * 100) + "%" }} /></div>
                    </div>
                  ));
                })()}
              </div>
            </motion.section>
          )}

          {careerViews && careerViews.length > 0 && (
            <motion.section variants={fade} className="card p-5">
              <div className="mb-3"><h3 className="font-display text-[15px] font-bold text-ink-hi">{t("admin.views.title", "Careers page views")}</h3><p className="text-[11px] text-ink-lo mt-0.5">{t("admin.views.sub", "How often each role's public careers page was viewed.")}</p></div>
              <div className="space-y-2">
                {(() => {
                  const max = Math.max(...careerViews.map((v) => v.views), 1);
                  return careerViews.slice(0, 12).map((v) => (
                    <div key={v.requestId}>
                      <div className="flex justify-between text-xs mb-1"><span className="text-ink-hi font-medium truncate">{v.title}<span className="text-ink-lo"> · {v.city}</span></span><span className="num text-[11px] text-ink-mid whitespace-nowrap">{v.views} {t("admin.views.views", "views")}{v.lastViewedAt ? ` · ${new Date(v.lastViewedAt).toLocaleDateString()}` : ""}</span></div>
                      <div className="h-2 rounded-full bg-panel2/70 overflow-hidden"><div className="h-full rounded-full bg-gradient-to-r from-brand-deep to-brand-bright" style={{ width: Math.round((v.views / max) * 100) + "%" }} /></div>
                    </div>
                  ));
                })()}
              </div>
            </motion.section>
          )}

          {outcomes && outcomes.total > 0 && (
            <motion.section variants={fade} className="card p-5">
              <div className="flex items-center justify-between mb-3 gap-3 flex-wrap">
                <div><h3 className="font-display text-[15px] font-bold text-ink-hi">{t("admin.outcomes.title", "Hiring outcomes")}</h3><p className="text-[11px] text-ink-lo mt-0.5">{t("admin.outcomes.sub", "Captured decisions — the labelled dataset that will train smarter ranking. A higher avg score for hires than rejections means the current ranker is separating well.")}</p></div>
                <a href="/api/recruitment/metrics/outcomes/export.csv" className="text-[11px] text-ink-lo hover:text-brand-bright transition whitespace-nowrap">{t("admin.outcomes.export", "Export CSV")}</a>
              </div>
              <div className="grid grid-cols-3 gap-4 mb-3">
                {[[t("admin.outcomes.total", "Decisions"), `${outcomes.total}`], [t("admin.outcomes.hired", "Hired"), `${outcomes.hired}`], [t("admin.outcomes.rejected", "Rejected"), `${outcomes.rejected}`]].map(([label, val], i) => (
                  <div key={i}><span className="eyebrow">{label as string}</span><div className="num text-[26px] font-bold text-ink-hi leading-none mt-1.5">{val as string}</div></div>
                ))}
              </div>
              <div className="grid grid-cols-2 gap-4">
                <div><span className="eyebrow">{t("admin.outcomes.avgHired", "Avg score · hired")}</span><div className="num text-[20px] font-bold text-brand-bright leading-none mt-1.5">{outcomes.avgScoreHired}%</div></div>
                <div><span className="eyebrow">{t("admin.outcomes.avgRejected", "Avg score · rejected")}</span><div className="num text-[20px] font-bold text-pink leading-none mt-1.5">{outcomes.avgScoreRejected}%</div></div>
              </div>
            </motion.section>
          )}

          {audit && audit.length > 0 && (
            <motion.section variants={fade} className="card p-5">
              <div className="flex items-center justify-between mb-3 gap-3 flex-wrap">
                <div><h3 className="font-display text-[15px] font-bold text-ink-hi">{t("admin.audit.title", "Audit trail")}</h3><p className="text-[11px] text-ink-lo mt-0.5">{t("admin.audit.sub", "Recent administrative actions.")}</p></div>
              </div>
              <div className="overflow-x-auto">
                <table className="w-full text-sm">
                  <thead><tr className="text-left eyebrow border-b border-line/60"><th className="py-2 pl-1 font-semibold">{t("admin.audit.when", "When")}</th><th className="font-semibold">{t("admin.audit.actor", "Actor")}</th><th className="font-semibold">{t("admin.audit.action", "Action")}</th><th className="font-semibold">{t("admin.audit.summary", "Summary")}</th></tr></thead>
                  <tbody>
                    {audit.map((e) => (
                      <tr key={e.id} className="border-b border-line/30">
                        <td className="py-2 pl-1 text-[11px] text-ink-lo num whitespace-nowrap">{new Date(e.occurredAt).toLocaleString()}</td>
                        <td className="text-[12px] text-ink-mid">{e.actor}</td>
                        <td><span className="chip !text-[10px] !text-brand-bright !border-brand/30">{e.action}</span></td>
                        <td className="text-[12px] text-ink-mid">{e.summary}</td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            </motion.section>
          )}

          {campaigns && (
            <motion.section variants={fade} className="card p-5">
              <div className="flex items-center justify-between mb-3 gap-3 flex-wrap">
                <div><h3 className="font-display text-[15px] font-bold text-ink-hi">{t("admin.camp.title", "Email campaigns")}</h3><p className="text-[11px] text-ink-lo mt-0.5">{t("admin.camp.sub", "Compose and send bulk emails to candidates.")}</p></div>
                <span className="chip !text-[10px]">{campaigns.length}</span>
              </div>
              <div className="space-y-3">
                {campaigns.map((c) => (
                  <div key={c.id} className="rounded-xl border border-line/60 bg-panel2/40 p-3.5">
                    <div className="flex items-center justify-between gap-2 mb-1">
                      <div className="min-w-0"><div className="text-sm font-semibold text-ink-hi truncate">{c.name}</div><div className="text-[11px] text-ink-lo truncate">{c.subject}</div></div>
                      <span className={`chip !text-[10px] capitalize ${c.status === "sent" ? "!text-brand-bright !border-brand/30" : "!text-gold !border-gold/30"}`}>{c.status}{c.status === "sent" ? ` · ${c.recipientCount}` : ""}</span>
                    </div>
                    {c.status === "draft" ? (
                      <>
                        <div className="flex flex-wrap items-center gap-1.5 my-2">
                          {c.recipients.map((e) => <span key={e} className="chip !text-[10px]">{e} <button onClick={() => removeCampaignRecipient(c.id, e)} className="ml-1 hover:text-pink">✕</button></span>)}
                          {c.recipients.length === 0 && <span className="text-[11px] text-ink-lo">{t("admin.camp.noRecipients", "No recipients")}</span>}
                        </div>
                        <div className="flex gap-2">
                          <input value={campaignRecipient[c.id] ?? ""} onChange={(e) => setCampaignRecipient((m) => ({ ...m, [c.id]: e.target.value }))} onKeyDown={(e) => { if (e.key === "Enter") addCampaignRecipient(c.id); }} placeholder={t("admin.camp.addEmail", "email@company.na")} className="flex-1 rounded-lg border border-line/70 bg-panel2/50 px-2.5 py-1 text-[12px] text-ink-hi placeholder:text-ink-lo focus:border-brand/50 focus:outline-none" />
                          <button onClick={() => addCampaignRecipient(c.id)} className="rounded-lg bg-panel2/70 px-2.5 py-1 text-[11px] font-semibold text-ink-mid hover:text-ink-hi transition">{t("admin.camp.add", "Add")}</button>
                          <button onClick={() => sendCampaign(c.id)} disabled={c.recipients.length === 0} className="rounded-lg bg-brand/15 px-2.5 py-1 text-[11px] font-semibold text-brand-bright hover:bg-brand/25 transition disabled:opacity-50">{t("admin.camp.send", "Send")}</button>
                        </div>
                      </>
                    ) : (
                      <div className="text-[11px] text-ink-lo mt-1">{t("admin.camp.sentTo", "Sent to {{n}} recipient(s).", { n: c.recipientCount })}</div>
                    )}
                  </div>
                ))}
                {campaigns.length === 0 && <div className="py-3 text-center text-[12px] text-ink-lo">{t("admin.camp.empty", "No campaigns yet.")}</div>}
              </div>
              <div className="mt-3 border-t border-line/40 pt-3 grid gap-2">
                <div className="grid gap-2 sm:grid-cols-2">
                  <input value={newCampaign.name} onChange={(e) => setNewCampaign((f) => ({ ...f, name: e.target.value }))} placeholder={t("admin.camp.name", "Campaign name")} className="rounded-lg border border-line/70 bg-panel2/50 px-2.5 py-1.5 text-[12px] text-ink-hi placeholder:text-ink-lo focus:border-brand/50 focus:outline-none" />
                  <input value={newCampaign.subject} onChange={(e) => setNewCampaign((f) => ({ ...f, subject: e.target.value }))} placeholder={t("admin.camp.subject", "Subject")} className="rounded-lg border border-line/70 bg-panel2/50 px-2.5 py-1.5 text-[12px] text-ink-hi placeholder:text-ink-lo focus:border-brand/50 focus:outline-none" />
                </div>
                <textarea value={newCampaign.body} onChange={(e) => setNewCampaign((f) => ({ ...f, body: e.target.value }))} placeholder={t("admin.camp.body", "Message body…")} className="min-h-[64px] resize-y rounded-lg border border-line/70 bg-panel2/50 px-2.5 py-1.5 text-[12px] text-ink-hi placeholder:text-ink-lo focus:border-brand/50 focus:outline-none" />
                <button onClick={createCampaign} disabled={!newCampaign.name.trim() || !newCampaign.subject.trim() || !newCampaign.body.trim()} className="justify-self-start rounded-lg bg-brand/15 px-3 py-1.5 text-[11px] font-semibold text-brand-bright hover:bg-brand/25 transition disabled:opacity-50">{t("admin.camp.create", "Create draft")}</button>
              </div>
            </motion.section>
          )}

          {templates && (
            <motion.section variants={fade} className="card p-5">
              <div className="flex items-center justify-between mb-3 gap-3 flex-wrap">
                <div><h3 className="font-display text-[15px] font-bold text-ink-hi">{t("admin.tpl.title", "Job templates")}</h3><p className="text-[11px] text-ink-lo mt-0.5">{t("admin.tpl.sub", "Reusable requisition presets.")}</p></div>
                <span className="chip !text-[10px]">{templates.length}</span>
              </div>
              <div className="space-y-2">
                {templates.map((tpl) => (
                  <div key={tpl.id} className="flex items-center gap-3 rounded-xl border border-line/60 bg-panel2/40 px-3.5 py-2.5">
                    <div className="min-w-0 flex-1"><div className="text-sm font-semibold text-ink-hi truncate">{tpl.name}</div><div className="text-[11px] text-ink-lo truncate">{tpl.title}{tpl.city ? ` · ${tpl.city}` : ""} · {tpl.positions} pos · <span className="capitalize">{tpl.employmentType}</span>{tpl.remote ? " · remote" : ""}{tpl.tags.length ? ` · ${tpl.tags.join(", ")}` : ""}</div></div>
                    <button onClick={() => deleteTemplate(tpl.id)} className="rounded-lg px-2 py-1 text-[11px] font-semibold text-ink-lo hover:text-pink transition">✕</button>
                  </div>
                ))}
                {templates.length === 0 && <div className="py-3 text-center text-[12px] text-ink-lo">{t("admin.tpl.empty", "No templates yet.")}</div>}
              </div>
              <div className="mt-3 border-t border-line/40 pt-3 grid gap-2 sm:grid-cols-[1fr_1fr_1fr_auto_auto] items-center">
                <input value={newTemplate.name} onChange={(e) => setNewTemplate((f) => ({ ...f, name: e.target.value }))} placeholder={t("admin.tpl.name", "Template name")} className="rounded-lg border border-line/70 bg-panel2/50 px-2.5 py-1.5 text-[12px] text-ink-hi placeholder:text-ink-lo focus:border-brand/50 focus:outline-none" />
                <input value={newTemplate.title} onChange={(e) => setNewTemplate((f) => ({ ...f, title: e.target.value }))} placeholder={t("admin.tpl.role", "Role title")} className="rounded-lg border border-line/70 bg-panel2/50 px-2.5 py-1.5 text-[12px] text-ink-hi placeholder:text-ink-lo focus:border-brand/50 focus:outline-none" />
                <input value={newTemplate.city} onChange={(e) => setNewTemplate((f) => ({ ...f, city: e.target.value }))} placeholder={t("admin.tpl.city", "City")} className="rounded-lg border border-line/70 bg-panel2/50 px-2.5 py-1.5 text-[12px] text-ink-hi placeholder:text-ink-lo focus:border-brand/50 focus:outline-none" />
                <input type="number" min={1} value={newTemplate.positions} onChange={(e) => setNewTemplate((f) => ({ ...f, positions: Number(e.target.value) }))} className="w-16 rounded-lg border border-line/70 bg-panel2/50 px-2 py-1.5 text-[12px] text-ink-hi focus:border-brand/50 focus:outline-none" />
                <button onClick={createTemplate} disabled={!newTemplate.name.trim() || !newTemplate.title.trim()} className="rounded-lg bg-brand/15 px-3 py-1.5 text-[11px] font-semibold text-brand-bright hover:bg-brand/25 transition disabled:opacity-50">{t("admin.tpl.add", "Add")}</button>
              </div>
            </motion.section>
          )}

          {dupes && dupes.length > 0 && (
            <motion.section variants={fade} className="card p-5">
              <div className="flex items-center justify-between mb-3 gap-3 flex-wrap">
                <div><h3 className="font-display text-[15px] font-bold text-ink-hi">{t("admin.dupes.title", "Possible duplicates")}</h3><p className="text-[11px] text-ink-lo mt-0.5">{t("admin.dupes.sub", "Candidate records sharing a name — review and merge.")}</p></div>
                <span className="chip !text-[10px] !text-gold !border-gold/30">{t("admin.dupes.count", "{{n}} group(s)", { n: dupes.length })}</span>
              </div>
              <div className="space-y-2">
                {dupes.map((g) => (
                  <div key={g.name} className="rounded-xl border border-gold/30 bg-gold/[0.05] px-3.5 py-2.5">
                    <div className="flex items-center justify-between mb-1.5"><span className="text-sm font-semibold text-ink-hi">{g.name}</span><span className="chip !text-[10px] !text-gold !border-gold/30">{g.count}×</span></div>
                    <div className="space-y-1">
                      {g.candidates.map((c) => (
                        <div key={c.id} className="text-[11px] text-ink-mid">{c.publicHeadline || "—"} · {c.city} · <span className="capitalize text-ink-lo">{c.availability}</span></div>
                      ))}
                    </div>
                  </div>
                ))}
              </div>
            </motion.section>
          )}

          {customFields && (
            <motion.section variants={fade} className="card p-5">
              <div className="mb-3"><h3 className="font-display text-[15px] font-bold text-ink-hi">{t("admin.cf.title", "Candidate custom fields")}</h3><p className="text-[11px] text-ink-lo mt-0.5">{t("admin.cf.sub", "Define extra fields captured on every candidate. Set values from a candidate in search.")}</p></div>
              <div className="space-y-1.5 mb-2">
                {customFields.map((f) => (
                  <div key={f.id} className="flex items-center gap-2 rounded-lg border border-line/50 bg-panel2/40 px-2.5 py-1.5">
                    <div className="min-w-0 flex-1"><div className="text-[12px] text-ink-hi truncate">{f.label}</div><div className="text-[10px] text-ink-lo">{f.kind}{f.options.length > 0 ? ` · ${f.options.join(" / ")}` : ""}</div></div>
                    <button onClick={() => removeCustomField(f.id)} className="text-ink-lo hover:text-pink text-[11px]" title={t("admin.cf.remove", "Remove")}>✕</button>
                  </div>
                ))}
                {customFields.length === 0 && <div className="text-[11px] text-ink-lo">{t("admin.cf.none", "No custom fields defined.")}</div>}
              </div>
              <div className="flex flex-wrap items-center gap-2">
                <input value={newCustomField.label} onChange={(e) => setNewCustomField((f) => ({ ...f, label: e.target.value }))} placeholder={t("admin.cf.label", "Field label")} className="flex-1 min-w-[140px] rounded-lg border border-line/70 bg-panel2/50 px-2.5 py-1 text-[12px] text-ink-hi placeholder:text-ink-lo focus:border-brand/50 focus:outline-none" />
                <select value={newCustomField.kind} onChange={(e) => setNewCustomField((f) => ({ ...f, kind: e.target.value }))} className="rounded-lg border border-line/70 bg-panel2/50 px-2 py-1 text-[12px] text-ink-hi capitalize focus:border-brand/50 focus:outline-none">
                  {["text", "number", "boolean", "select"].map((k) => <option key={k} value={k}>{k}</option>)}
                </select>
                {newCustomField.kind === "select" && <input value={newCustomField.options} onChange={(e) => setNewCustomField((f) => ({ ...f, options: e.target.value }))} placeholder={t("admin.cf.options", "Options, comma-separated")} className="flex-1 min-w-[140px] rounded-lg border border-line/70 bg-panel2/50 px-2.5 py-1 text-[12px] text-ink-hi placeholder:text-ink-lo focus:border-brand/50 focus:outline-none" />}
                <button onClick={addCustomField} disabled={!newCustomField.label.trim()} className="rounded-lg bg-brand/15 px-3 py-1 text-[11px] font-semibold text-brand-bright hover:bg-brand/25 transition disabled:opacity-50">{t("admin.cf.add", "Add field")}</button>
              </div>
            </motion.section>
          )}

          <motion.section variants={fade} className="card p-5">
            <div className="mb-3"><h3 className="font-display text-[15px] font-bold text-ink-hi">{t("admin.import.title", "Bulk import candidates")}</h3><p className="text-[11px] text-ink-lo mt-0.5">{t("admin.import.sub", "Paste or upload CSV — header: firstName,lastName,city,nationality[,availability,headline]. Duplicates (name+city) are skipped.")}</p></div>
            <div className="flex items-center gap-3 mb-2">
              <label className="rounded-lg bg-panel2/70 px-2.5 py-1 text-[11px] font-semibold text-ink-mid hover:text-ink-hi transition cursor-pointer">
                {t("admin.import.file", "Choose CSV file")}
                <input type="file" accept=".csv,text/csv" className="hidden" onChange={async (e) => { const f = e.target.files?.[0]; if (f) setImportCsv(await f.text()); }} />
              </label>
              <span className="text-[10px] text-ink-lo">{t("admin.import.or", "or paste below")}</span>
            </div>
            <textarea value={importCsv} onChange={(e) => setImportCsv(e.target.value)} placeholder={"firstName,lastName,city,nationality,availability,headline"} className="w-full min-h-[90px] resize-y rounded-lg border border-line/70 bg-panel2/50 px-2.5 py-1.5 text-[12px] font-mono text-ink-hi placeholder:text-ink-lo focus:border-brand/50 focus:outline-none" />
            <div className="mt-2 flex items-center gap-3">
              <button onClick={runImport} disabled={importBusy || !importCsv.trim()} className="rounded-lg bg-brand/15 px-3 py-1.5 text-[12px] font-semibold text-brand-bright hover:bg-brand/25 transition disabled:opacity-50">{importBusy ? t("admin.import.importing", "Importing…") : t("admin.import.run", "Import")}</button>
              {importResult && (
                <span className="text-[11px] text-ink-mid">{t("admin.import.result", "{{created}} created · {{skipped}} skipped", { created: importResult.created, skipped: importResult.skipped })}{importResult.errors.length > 0 ? ` · ${importResult.errors.length} ${t("admin.import.issues", "issue(s)")}` : ""}</span>
              )}
            </div>
            {importResult && importResult.errors.length > 0 && (
              <ul className="mt-2 space-y-0.5 max-h-32 overflow-y-auto">
                {importResult.errors.map((e, i) => <li key={i} className="text-[11px] text-pink">{e}</li>)}
              </ul>
            )}
          </motion.section>

          {pools && (
            <motion.section variants={fade} className="card p-5">
              <div className="flex items-center justify-between mb-3 gap-3 flex-wrap">
                <div><h3 className="font-display text-[15px] font-bold text-ink-hi">{t("admin.pools.title", "Talent pools")}</h3><p className="text-[11px] text-ink-lo mt-0.5">{t("admin.pools.sub", "Named shortlists of candidates. Add members from candidate search.")}</p></div>
                <span className="chip !text-[10px]">{t("admin.pools.count", "{{n}} pool(s)", { n: pools.length })}</span>
              </div>
              <div className="flex flex-wrap gap-2 mb-3">
                <input value={newPoolName} onChange={(e) => setNewPoolName(e.target.value)} onKeyDown={(e) => { if (e.key === "Enter") createPool(); }} placeholder={t("admin.pools.name", "New pool name…")} className="flex-1 min-w-[180px] rounded-lg border border-line/70 bg-panel2/50 px-3 py-1.5 text-[12px] text-ink-hi placeholder:text-ink-lo focus:border-brand/50 focus:outline-none" />
                <button onClick={createPool} disabled={poolBusy || !newPoolName.trim()} className="rounded-lg bg-brand/15 px-3 py-1.5 text-[12px] font-semibold text-brand-bright hover:bg-brand/25 transition disabled:opacity-50">{t("admin.pools.create", "Create")}</button>
              </div>
              {pools.length === 0 ? (
                <div className="py-4 text-center text-[12px] text-ink-lo">{t("admin.pools.none", "No pools yet — create one, then add candidates from search.")}</div>
              ) : (
                <div className="space-y-2">
                  {pools.map((p) => (
                    <div key={p.id} className="rounded-xl border border-line/60 bg-panel2/40">
                      <button onClick={() => setPoolOpen(poolOpen === p.id ? null : p.id)} className="flex w-full items-center gap-3 px-3.5 py-2.5 text-left">
                        <div className="min-w-0 flex-1"><div className="text-sm font-semibold text-ink-hi truncate">{p.name}</div></div>
                        <span className="chip !text-[10px] !text-brand-bright !border-brand/30">{t("admin.pools.members", "{{n}} member(s)", { n: p.memberCount })}</span>
                        <span className="text-ink-lo text-[11px]">{poolOpen === p.id ? "▲" : "▼"}</span>
                      </button>
                      {poolOpen === p.id && (
                        <div className="border-t border-line/40 px-3.5 py-3 space-y-1.5">
                          {poolMembers.map((m) => (
                            <div key={m.candidateId} className="flex items-center gap-2 rounded-lg border border-line/50 bg-panel/40 px-2.5 py-1.5">
                              <div className="min-w-0 flex-1"><div className="text-[12px] text-ink-hi truncate">{m.name}</div><div className="text-[10px] text-ink-lo truncate">{m.city}</div></div>
                              <button onClick={() => removeFromPool(p.id, m.candidateId)} className="text-ink-lo hover:text-pink text-[11px]" title={t("admin.pools.remove", "Remove")}>✕</button>
                            </div>
                          ))}
                          {poolMembers.length === 0 && <div className="text-[11px] text-ink-lo">{t("admin.pools.empty", "No members yet. Add candidates from search results below.")}</div>}
                        </div>
                      )}
                    </div>
                  ))}
                </div>
              )}
            </motion.section>
          )}

          {csResult && (
            <motion.section variants={fade} className="card p-5">
              <div className="flex items-center justify-between mb-3 gap-3 flex-wrap">
                <div><h3 className="font-display text-[15px] font-bold text-ink-hi">{t("admin.search.title", "Candidate search")}</h3><p className="text-[11px] text-ink-lo mt-0.5">{t("admin.search.sub", "Filter the talent pool by keyword, city and availability.")}</p></div>
                <span className="chip !text-[10px]">{t("admin.search.total", "{{n}} match(es)", { n: csResult.total })}</span>
              </div>
              <div className="flex flex-wrap gap-2 mb-3">
                <input className="flex-1 min-w-[180px] rounded-lg border border-line/70 bg-panel2/50 px-3 py-1.5 text-[12px] text-ink-hi placeholder:text-ink-lo focus:border-brand/50 focus:outline-none" value={csQuery} onChange={(e) => setCsQuery(e.target.value)} placeholder={t("admin.search.keyword", "Name or headline…")} />
                <select value={csAvailability} onChange={(e) => setCsAvailability(e.target.value)} className="rounded-lg border border-line/70 bg-panel2/50 px-2 py-1.5 text-[12px] text-ink-hi focus:border-brand/50 focus:outline-none">
                  <option value="">{t("admin.search.anyAvail", "Any availability")}</option>
                  {["ActivelyLooking", "OpenToOpportunities", "NotAvailable"].map((a) => <option key={a} value={a}>{a}</option>)}
                </select>
                {csCity && <button onClick={() => setCsCity("")} className="chip !text-[11px] !text-brand-bright !border-brand/30">{csCity} ✕</button>}
                <label className="flex items-center gap-1.5 text-[11px] text-ink-mid ml-auto" title={t("admin.search.blindHint", "Hide names + nationality so you assess on merit (blind screening).")}><input type="checkbox" checked={csBlind} onChange={(e) => setCsBlind(e.target.checked)} />{t("admin.search.blind", "Blind screening")}</label>
              </div>
              <div className="grid gap-4 lg:grid-cols-[1fr_200px]">
                <div className="space-y-2">
                  {csResult.items.length === 0 && <div className="py-4 text-center text-[12px] text-ink-lo">{t("admin.search.empty", "No candidates match.")}</div>}
                  {csResult.items.map((c) => (
                    <div key={c.id} className="rounded-xl border border-line/60 bg-panel2/40">
                      <button onClick={() => setCsOpen(csOpen === c.id ? null : c.id)} className="flex w-full items-center gap-3 px-3.5 py-2.5 text-left">
                        <div className="min-w-0 flex-1"><div className="text-sm font-semibold text-ink-hi truncate">{c.firstName} {c.lastName}</div><div className="text-[11px] text-ink-lo truncate">{c.publicHeadline || "—"} · {c.city}</div></div>
                        <span className="chip !text-[10px] !text-gold !border-gold/30">{c.availability}</span>
                        <span className="text-ink-lo text-[11px]">{csOpen === c.id ? "▲" : "▼"}</span>
                      </button>
                      {csOpen === c.id && (
                        <div className="border-t border-line/40 px-3.5 py-3 space-y-3">
                          <div>
                            <div className="flex flex-wrap items-center gap-1.5 mb-2">
                              {csTags.map((tag) => (
                                <span key={tag} className="chip !text-[10px] !text-brand-bright !border-brand/30">{tag} <button onClick={() => removeCandTag(tag)} className="ml-1 hover:text-pink">✕</button></span>
                              ))}
                              {csTags.length === 0 && <span className="text-[11px] text-ink-lo">{t("admin.notes.noTags", "No tags")}</span>}
                            </div>
                            <div className="flex gap-2">
                              <input value={csTagDraft} onChange={(e) => setCsTagDraft(e.target.value)} onKeyDown={(e) => { if (e.key === "Enter") addCandTag(); }} placeholder={t("admin.notes.addTag", "Add tag")} className="flex-1 rounded-lg border border-line/70 bg-panel2/50 px-2.5 py-1 text-[12px] text-ink-hi placeholder:text-ink-lo focus:border-brand/50 focus:outline-none" />
                              <button onClick={addCandTag} disabled={!csTagDraft.trim()} className="rounded-lg bg-brand/15 px-2.5 py-1 text-[11px] font-semibold text-brand-bright hover:bg-brand/25 transition disabled:opacity-50">{t("admin.notes.tag", "Tag")}</button>
                            </div>
                          </div>
                          {pools && pools.length > 0 && (
                            <div>
                              <div className="eyebrow mb-1.5">{t("admin.pools.addTo", "Add to shortlist")}</div>
                              <div className="flex flex-wrap gap-1.5">
                                {pools.map((p) => (
                                  <button key={p.id} onClick={() => addToPool(p.id, c.id)} className="rounded-lg bg-panel2/60 px-2.5 py-1 text-[11px] font-semibold text-ink-mid hover:text-brand-bright hover:bg-brand/10 transition">+ {p.name}</button>
                                ))}
                              </div>
                            </div>
                          )}
                          {customFields && customFields.length > 0 && (
                            <div>
                              <div className="eyebrow mb-1.5">{t("admin.cf.values", "Custom fields")}</div>
                              <div className="space-y-2">
                                {customFields.map((f) => (
                                  <div key={f.id} className="flex items-center gap-2">
                                    <label className="text-[12px] text-ink-mid flex-1 min-w-0 truncate">{f.label}</label>
                                    {f.kind === "boolean" ? (
                                      <input type="checkbox" checked={csCustomValues[f.id] === "true"} onChange={(e) => setCsCustomValues((m) => ({ ...m, [f.id]: e.target.checked ? "true" : "false" }))} />
                                    ) : f.kind === "select" ? (
                                      <select value={csCustomValues[f.id] ?? ""} onChange={(e) => setCsCustomValues((m) => ({ ...m, [f.id]: e.target.value }))} className="rounded-lg border border-line/70 bg-panel2/50 px-2 py-1 text-[12px] text-ink-hi focus:border-brand/50 focus:outline-none"><option value="">—</option>{f.options.map((o) => <option key={o} value={o}>{o}</option>)}</select>
                                    ) : (
                                      <input type={f.kind === "number" ? "number" : "text"} value={csCustomValues[f.id] ?? ""} onChange={(e) => setCsCustomValues((m) => ({ ...m, [f.id]: e.target.value }))} className="w-48 rounded-lg border border-line/70 bg-panel2/50 px-2.5 py-1 text-[12px] text-ink-hi focus:border-brand/50 focus:outline-none" />
                                    )}
                                  </div>
                                ))}
                                <button onClick={saveCustomValues} className="rounded-lg bg-brand/15 px-2.5 py-1 text-[11px] font-semibold text-brand-bright hover:bg-brand/25 transition">{t("admin.cf.save", "Save custom fields")}</button>
                              </div>
                            </div>
                          )}
                          {csSimilar.length > 0 && (
                            <div>
                              <div className="eyebrow mb-1.5">{t("admin.similar.title", "Similar candidates")}</div>
                              <div className="space-y-1.5">
                                {csSimilar.map((s) => (
                                  <button key={s.id} onClick={() => setCsOpen(s.id)} className="flex w-full items-center gap-2 rounded-lg border border-line/50 bg-panel/40 px-2.5 py-1.5 text-left hover:border-brand/40 transition">
                                    <div className="min-w-0 flex-1"><div className="text-[12px] text-ink-hi truncate">{s.name}</div><div className="text-[10px] text-ink-lo truncate">{s.headline || "—"} · {s.city}</div></div>
                                    <span className="num text-[11px] text-brand-bright shrink-0">{s.score}%</span>
                                  </button>
                                ))}
                              </div>
                            </div>
                          )}
                          {csSemantic.length > 0 && (
                            <div>
                              <div className="eyebrow mb-1.5">{t("admin.semantic.title", "Semantically similar")}</div>
                              <div className="space-y-1.5">
                                {csSemantic.map((s) => (
                                  <button key={s.id} onClick={() => setCsOpen(s.id)} className="flex w-full items-center gap-2 rounded-lg border border-line/50 bg-panel/40 px-2.5 py-1.5 text-left hover:border-brand/40 transition">
                                    <div className="min-w-0 flex-1"><div className="text-[12px] text-ink-hi truncate">{s.name}</div><div className="text-[10px] text-ink-lo truncate">{s.headline || "—"} · {s.city}</div></div>
                                    <span className="num text-[11px] text-gold shrink-0">{s.score}%</span>
                                  </button>
                                ))}
                              </div>
                            </div>
                          )}
                          <div>
                            <div className="flex items-center justify-between mb-1.5">
                            <span className="eyebrow">{t("admin.notes.title", "Recruiter notes")}</span>
                            <div className="flex items-center gap-3">
                              <a href={`/api/candidates/${c.id}/export`} target="_blank" rel="noreferrer" className="text-[11px] text-ink-lo hover:text-brand-bright transition">{t("admin.gdpr.export", "Export data (GDPR)")}</a>
                              <button onClick={() => eraseCandidate(c.id)} className="text-[11px] text-ink-lo hover:text-pink transition">{t("admin.gdpr.erase", "Erase (GDPR)")}</button>
                            </div>
                          </div>
                            <div className="space-y-1.5 mb-2">
                              {csNotes.map((n) => (
                                <div key={n.id} className="flex items-start gap-2 rounded-lg border border-line/50 bg-panel/40 px-2.5 py-1.5">
                                  <div className="min-w-0 flex-1"><div className="text-[12px] text-ink-mid">{n.body}</div><div className="text-[10px] text-ink-lo">{n.author} · {new Date(n.createdAt).toLocaleDateString()}</div></div>
                                  <button onClick={() => removeCandNote(n.id)} className="text-ink-lo hover:text-pink text-[11px]">✕</button>
                                </div>
                              ))}
                              {csNotes.length === 0 && <div className="text-[11px] text-ink-lo">{t("admin.notes.none", "No notes yet.")}</div>}
                            </div>
                            <div className="flex gap-2">
                              <input value={csNoteDraft} onChange={(e) => setCsNoteDraft(e.target.value)} onKeyDown={(e) => { if (e.key === "Enter") addCandNote(); }} placeholder={t("admin.notes.addNote", "Add a private note…")} className="flex-1 rounded-lg border border-line/70 bg-panel2/50 px-2.5 py-1 text-[12px] text-ink-hi placeholder:text-ink-lo focus:border-brand/50 focus:outline-none" />
                              <button onClick={addCandNote} disabled={!csNoteDraft.trim()} className="rounded-lg bg-brand/15 px-2.5 py-1 text-[11px] font-semibold text-brand-bright hover:bg-brand/25 transition disabled:opacity-50">{t("admin.notes.note", "Note")}</button>
                            </div>
                          </div>
                        </div>
                      )}
                    </div>
                  ))}
                </div>
                <div>
                  <div className="eyebrow mb-2">{t("admin.search.cities", "Cities")}</div>
                  <div className="flex flex-wrap gap-1.5">
                    {csResult.facets.cities.map((f) => (
                      <button key={f.label} onClick={() => setCsCity(f.label === csCity ? "" : f.label)} className={`rounded-lg px-2 py-1 text-[11px] font-semibold transition ${csCity === f.label ? "bg-brand/20 text-brand-bright" : "bg-panel2/60 text-ink-lo hover:text-ink-hi"}`}>{f.label} <span className="num text-ink-lo">{f.count}</span></button>
                    ))}
                  </div>
                </div>
              </div>
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

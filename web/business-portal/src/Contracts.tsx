import { useCallback, useEffect, useState } from "react";
import { useTranslation } from "react-i18next";

// Payments service DTOs (GET/POST /api/payments/**).
interface ContractDto { id: string; clientId: string; talentId: string; requestId: string | null; title: string; currency: string; status: string; createdAt: string; }
interface MilestoneDto { id: string; order: number; title: string; amountMinor: number; status: string; fundedAt: string | null; submittedAt: string | null; decidedAt: string | null; }
interface MovementDto { id: string; milestoneId: string; kind: string; amountMinor: number; currency: string; createdAt: string; }
interface ContractDetail { contract: ContractDto; milestones: MilestoneDto[]; movements: MovementDto[]; }

const money = (minor: number, currency: string) => `${currency} ${(minor / 100).toLocaleString(undefined, { minimumFractionDigits: 2, maximumFractionDigits: 2 })}`;

const statusChip = (s: string) => {
  switch (s) {
    case "Active": case "Funded": case "Submitted": return "!text-gold !border-gold/30";
    case "Completed": case "Approved": return "!text-brand-bright !border-brand/30";
    case "Cancelled": case "Refunded": return "!text-pink !border-pink/30";
    default: return "";
  }
};

/// A contracts + milestones panel for the marketplace transaction layer. The client (employer) drives the
/// lifecycle (create, add milestones, activate, fund, approve, refund); the talent submits funded milestones.
export default function Contracts({ role, partyId, live }: { role: "client" | "talent"; partyId: string; live: boolean }) {
  const { t } = useTranslation();
  const [contracts, setContracts] = useState<ContractDto[] | null>(null);
  const [openId, setOpenId] = useState<string | null>(null);
  const [detail, setDetail] = useState<Record<string, ContractDetail>>({});
  const [newContract, setNewContract] = useState({ talentId: "", title: "", currency: "NAD" });
  const [ms, setMs] = useState<Record<string, { title: string; amount: string }>>({});
  const [busy, setBusy] = useState<string | null>(null);

  const query = role === "client" ? `clientId=${partyId}` : `talentId=${partyId}`;

  const load = useCallback(async () => {
    const r = await fetch(`/api/payments/contracts?${query}`, { credentials: "same-origin" });
    if (r.ok) setContracts(await r.json());
  }, [query]);

  useEffect(() => { if (partyId) load(); }, [partyId, load]);

  const openDetail = async (id: string) => {
    if (openId === id) { setOpenId(null); return; }
    setOpenId(id);
    const r = await fetch(`/api/payments/contracts/${id}`, { credentials: "same-origin" });
    if (r.ok) { const dt: ContractDetail = await r.json(); setDetail((p) => ({ ...p, [id]: dt })); }
  };
  const refresh = async (id: string) => {
    const r = await fetch(`/api/payments/contracts/${id}`, { credentials: "same-origin" });
    if (r.ok) { const d: ContractDetail = await r.json(); setDetail((p) => ({ ...p, [id]: d })); setContracts((cs) => cs?.map((c) => (c.id === id ? d.contract : c)) ?? cs); }
  };

  const createContract = async () => {
    if (!newContract.talentId.trim() || !newContract.title.trim()) return;
    setBusy("create");
    try {
      const r = await fetch("/api/payments/contracts", { method: "POST", headers: { "Content-Type": "application/json" }, credentials: "same-origin", body: JSON.stringify({ clientId: partyId, talentId: newContract.talentId.trim(), requestId: null, title: newContract.title.trim(), currency: newContract.currency.trim().toUpperCase() }) });
      if (r.ok) { const c: ContractDto = await r.json(); setContracts((cs) => [c, ...(cs ?? [])]); setNewContract({ talentId: "", title: "", currency: "NAD" }); }
    } finally { setBusy(null); }
  };
  const addMilestone = async (id: string) => {
    const draft = ms[id] ?? { title: "", amount: "" };
    const amount = Math.round(Number(draft.amount) * 100);
    if (!draft.title.trim() || !(amount > 0)) return;
    setBusy(id + "ms");
    try {
      const r = await fetch(`/api/payments/contracts/${id}/milestones`, { method: "POST", headers: { "Content-Type": "application/json" }, credentials: "same-origin", body: JSON.stringify({ title: draft.title.trim(), amountMinor: amount }) });
      if (r.ok) { setMs((p) => ({ ...p, [id]: { title: "", amount: "" } })); await refresh(id); }
    } finally { setBusy(null); }
  };
  const contractAction = async (id: string, action: "activate" | "cancel") => {
    setBusy(id + action);
    try { const r = await fetch(`/api/payments/contracts/${id}/${action}`, { method: "POST", credentials: "same-origin" }); if (r.ok) await refresh(id); } finally { setBusy(null); }
  };
  const milestoneAction = async (contractId: string, mid: string, action: "fund" | "submit" | "approve" | "refund") => {
    setBusy(mid + action);
    try { const r = await fetch(`/api/payments/milestones/${mid}/${action}`, { method: "POST", credentials: "same-origin" }); if (r.ok) await refresh(contractId); } finally { setBusy(null); }
  };

  if (!contracts) return null;

  return (
    <section className="card p-5">
      <div className="flex items-center justify-between mb-3 gap-3 flex-wrap">
        <div>
          <h3 className="font-display text-[15px] font-bold text-ink-hi">{t("contracts.title", "Contracts & escrow")}</h3>
          <p className="text-[11px] text-ink-lo mt-0.5">{role === "client" ? t("contracts.subClient", "Agree fixed-price work, fund milestones, release on approval.") : t("contracts.subTalent", "Your contracts — submit funded milestones to get paid.")}</p>
        </div>
        <span className="chip !text-[10px]">{contracts.length}</span>
      </div>

      <div className="space-y-2">
        {contracts.map((c) => {
          const d = detail[c.id];
          return (
            <div key={c.id} className="rounded-xl border border-line/60 bg-panel2/40 p-3.5">
              <button onClick={() => openDetail(c.id)} className="w-full flex items-center justify-between gap-2 text-left">
                <div className="min-w-0"><div className="text-sm font-semibold text-ink-hi truncate">{c.title}</div><div className="text-[11px] text-ink-lo">{c.currency}</div></div>
                <div className="flex items-center gap-2 shrink-0"><span className={`chip !text-[10px] ${statusChip(c.status)}`}>{c.status}</span><span className="text-ink-lo text-xs">{openId === c.id ? "▲" : "▼"}</span></div>
              </button>

              {openId === c.id && d && (
                <div className="mt-3 border-t border-line/40 pt-3 space-y-3">
                  {/* Milestones */}
                  <div className="space-y-1.5">
                    {d.milestones.map((m) => (
                      <div key={m.id} className="flex items-center justify-between gap-2 rounded-lg border border-line/50 bg-base/40 px-3 py-2">
                        <div className="min-w-0"><div className="text-[12px] text-ink-hi truncate">{m.order}. {m.title}</div><div className="text-[10px] text-ink-lo num">{money(m.amountMinor, c.currency)}</div></div>
                        <div className="flex items-center gap-1.5 shrink-0">
                          <span className={`chip !text-[10px] ${statusChip(m.status)}`}>{m.status}</span>
                          {live && role === "client" && m.status === "Pending" && c.status === "Active" && <button onClick={() => milestoneAction(c.id, m.id, "fund")} disabled={!!busy} className="rounded bg-brand/15 px-2 py-0.5 text-[10px] font-semibold text-brand-bright hover:bg-brand/25 disabled:opacity-50">{t("contracts.fund", "Fund")}</button>}
                          {live && role === "talent" && m.status === "Funded" && <button onClick={() => milestoneAction(c.id, m.id, "submit")} disabled={!!busy} className="rounded bg-brand/15 px-2 py-0.5 text-[10px] font-semibold text-brand-bright hover:bg-brand/25 disabled:opacity-50">{t("contracts.submit", "Submit")}</button>}
                          {live && role === "client" && m.status === "Submitted" && <button onClick={() => milestoneAction(c.id, m.id, "approve")} disabled={!!busy} className="rounded bg-brand/15 px-2 py-0.5 text-[10px] font-semibold text-brand-bright hover:bg-brand/25 disabled:opacity-50">{t("contracts.approve", "Approve & pay")}</button>}
                          {live && role === "client" && (m.status === "Funded" || m.status === "Submitted") && <button onClick={() => milestoneAction(c.id, m.id, "refund")} disabled={!!busy} className="rounded px-2 py-0.5 text-[10px] font-semibold text-ink-lo hover:text-pink disabled:opacity-50">{t("contracts.refund", "Refund")}</button>}
                        </div>
                      </div>
                    ))}
                    {d.milestones.length === 0 && <div className="text-[11px] text-ink-lo">{t("contracts.noMs", "No milestones yet.")}</div>}
                  </div>

                  {/* Client: add milestone + activate (draft only) */}
                  {live && role === "client" && c.status === "Draft" && (
                    <div className="space-y-2 border-t border-line/40 pt-2.5">
                      <div className="flex gap-2">
                        <input value={ms[c.id]?.title ?? ""} onChange={(e) => setMs((p) => ({ ...p, [c.id]: { title: e.target.value, amount: p[c.id]?.amount ?? "" } }))} placeholder={t("contracts.msTitle", "Milestone")} className="flex-1 min-w-0 rounded-lg border border-line/70 bg-panel2/50 px-2.5 py-1 text-[12px] text-ink-hi placeholder:text-ink-lo focus:border-brand/50 focus:outline-none" />
                        <input value={ms[c.id]?.amount ?? ""} onChange={(e) => setMs((p) => ({ ...p, [c.id]: { title: p[c.id]?.title ?? "", amount: e.target.value } }))} type="number" min={0} placeholder={c.currency} className="w-24 rounded-lg border border-line/70 bg-panel2/50 px-2.5 py-1 text-[12px] text-ink-hi placeholder:text-ink-lo focus:border-brand/50 focus:outline-none" />
                        <button onClick={() => addMilestone(c.id)} disabled={busy === c.id + "ms"} className="rounded-lg bg-panel2/70 px-2.5 py-1 text-[11px] font-semibold text-ink-mid hover:text-ink-hi disabled:opacity-50">{t("contracts.addMs", "Add")}</button>
                      </div>
                      <div className="flex gap-2">
                        <button onClick={() => contractAction(c.id, "activate")} disabled={d.milestones.length === 0 || !!busy} className="rounded-lg bg-brand/15 px-3 py-1 text-[11px] font-semibold text-brand-bright hover:bg-brand/25 disabled:opacity-50">{t("contracts.activate", "Activate contract")}</button>
                        <button onClick={() => contractAction(c.id, "cancel")} disabled={!!busy} className="rounded-lg px-3 py-1 text-[11px] font-semibold text-ink-lo hover:text-pink disabled:opacity-50">{t("contracts.cancel", "Cancel")}</button>
                      </div>
                    </div>
                  )}

                  {/* Ledger */}
                  {d.movements.length > 0 && (
                    <div className="border-t border-line/40 pt-2.5">
                      <div className="eyebrow mb-1.5">{t("contracts.ledger", "Ledger")}</div>
                      <div className="space-y-1">
                        {d.movements.map((mv) => (
                          <div key={mv.id} className="flex items-center justify-between text-[11px]"><span className="text-ink-mid">{mv.kind}</span><span className="num text-ink-lo">{money(mv.amountMinor, mv.currency)}</span></div>
                        ))}
                      </div>
                    </div>
                  )}
                </div>
              )}
            </div>
          );
        })}
        {contracts.length === 0 && <div className="py-3 text-center text-[12px] text-ink-lo">{t("contracts.empty", "No contracts yet.")}</div>}
      </div>

      {/* Client: create a new contract */}
      {live && role === "client" && (
        <div className="mt-3 border-t border-line/40 pt-3 grid gap-2 sm:grid-cols-[1fr_1fr_auto]">
          <input value={newContract.talentId} onChange={(e) => setNewContract((f) => ({ ...f, talentId: e.target.value }))} placeholder={t("contracts.talentId", "Talent id")} className="rounded-lg border border-line/70 bg-panel2/50 px-2.5 py-1.5 text-[12px] text-ink-hi placeholder:text-ink-lo focus:border-brand/50 focus:outline-none" />
          <input value={newContract.title} onChange={(e) => setNewContract((f) => ({ ...f, title: e.target.value }))} placeholder={t("contracts.newTitle", "Contract title")} className="rounded-lg border border-line/70 bg-panel2/50 px-2.5 py-1.5 text-[12px] text-ink-hi placeholder:text-ink-lo focus:border-brand/50 focus:outline-none" />
          <div className="flex gap-2">
            <input value={newContract.currency} onChange={(e) => setNewContract((f) => ({ ...f, currency: e.target.value }))} className="w-16 rounded-lg border border-line/70 bg-panel2/50 px-2 py-1.5 text-[12px] text-ink-hi focus:border-brand/50 focus:outline-none" />
            <button onClick={createContract} disabled={busy === "create" || !newContract.talentId.trim() || !newContract.title.trim()} className="rounded-lg bg-brand/15 px-3 py-1.5 text-[11px] font-semibold text-brand-bright hover:bg-brand/25 disabled:opacity-50">{t("contracts.create", "Draft contract")}</button>
          </div>
        </div>
      )}
    </section>
  );
}

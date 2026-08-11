import { useCallback, useEffect, useState } from "react";
import { motion } from "framer-motion";
import { useTranslation } from "react-i18next";

// Talent-side (Professional / Student) view of the candidate's own applications, with the
// employer's offers (accept / decline / e-sign) and the two-way conversation surfaced inline.
// All endpoints are the same ones the admin/recruiter drawer uses; the talent acts as the
// "talent" side. Accept/decline/sign require a signed-in talent (client.user role, shared by
// both portals); messaging requires any signed-in user.

const fade = { initial: { opacity: 0, y: 14 }, animate: { opacity: 1, y: 0 } };

interface TalentApp { id: string; roleTitle: string; city: string; status: string; appliedAt: string; decidedAt: string | null; }
interface Offer { id: string; applicationId: string; title: string; salaryAmount: number; currency: string; startDate: string; status: string; notes: string | null; signedByName: string | null; signedAt: string | null; }
interface Message { id: string; sender: string; senderName: string; body: string; sentAt: string; read: boolean; }

const statusChip = (status: string) =>
  status === "hired" || status === "accepted" ? "!text-brand-bright !border-brand/30"
    : status === "rejected" || status === "declined" || status === "withdrawn" ? "!text-pink !border-pink/30"
    : status === "shortlisted" || status === "reviewed" || status === "sent" ? "!text-gold !border-gold/30"
    : "!text-ink-mid !border-line/70";

export default function TalentApplications({ talentId, senderName, live }: { talentId: string; senderName: string; live: boolean }) {
  const { t } = useTranslation();
  const [apps, setApps] = useState<TalentApp[] | null>(null);
  const [openId, setOpenId] = useState<string | null>(null);
  // Per-application offers + conversation, loaded lazily on expand.
  const [offers, setOffers] = useState<Record<string, Offer[]>>({});
  const [threads, setThreads] = useState<Record<string, Message[]>>({});
  const [drafts, setDrafts] = useState<Record<string, string>>({});
  const [signNames, setSignNames] = useState<Record<string, string>>({});
  const [busy, setBusy] = useState<string | null>(null);

  useEffect(() => {
    if (!talentId) return;
    fetch(`/api/recruitment/talents/${talentId}/applications`)
      .then((r) => (r.ok ? r.json() : null))
      .then((v) => { if (Array.isArray(v)) setApps(v); })
      .catch(() => { /* recruitment unavailable */ });
  }, [talentId]);

  const loadDetail = useCallback(async (appId: string) => {
    try {
      const [o, m] = await Promise.all([
        fetch(`/api/recruitment/applications/${appId}/offers`, { credentials: "same-origin" }).then((r) => (r.ok ? r.json() : [])),
        fetch(`/api/recruitment/applications/${appId}/messages`, { credentials: "same-origin" }).then((r) => (r.ok ? r.json() : [])),
      ]);
      if (Array.isArray(o)) setOffers((prev) => ({ ...prev, [appId]: o }));
      if (Array.isArray(m)) {
        setThreads((prev) => ({ ...prev, [appId]: m }));
        // Mark the recruiter's messages as read now that the talent is looking at them.
        if (m.some((x: Message) => x.sender === "recruiter" && !x.read)) {
          fetch(`/api/recruitment/applications/${appId}/messages/read`, { method: "POST", headers: { "Content-Type": "application/json" }, credentials: "same-origin", body: JSON.stringify({ reader: "talent" }) })
            .then(() => setThreads((prev) => ({ ...prev, [appId]: (prev[appId] ?? m).map((x) => (x.sender === "recruiter" ? { ...x, read: true } : x)) })))
            .catch(() => { /* best-effort */ });
        }
      }
    } catch { /* offline — panel stays empty */ }
  }, []);

  const toggle = (appId: string) => {
    const next = openId === appId ? null : appId;
    setOpenId(next);
    if (next && !(next in offers)) loadDetail(next);
  };

  // Offer lifecycle (talent side): accept / decline / e-sign.
  const decideOffer = async (appId: string, offerId: string, action: "accept" | "decline") => {
    setBusy(offerId + action);
    try {
      const r = await fetch(`/api/recruitment/offers/${offerId}/${action}`, { method: "POST", credentials: "same-origin" });
      if (r.ok) {
        const updated: Offer = await r.json();
        setOffers((prev) => ({ ...prev, [appId]: (prev[appId] ?? []).map((o) => (o.id === offerId ? updated : o)) }));
        if (action === "accept") setApps((prev) => (prev ? prev.map((a) => (a.id === appId ? { ...a, status: "hired" } : a)) : prev));
      }
    } finally { setBusy(null); }
  };
  const signOffer = async (appId: string, offerId: string) => {
    const name = (signNames[offerId] ?? "").trim();
    if (!name) return;
    setBusy(offerId + "sign");
    try {
      const r = await fetch(`/api/recruitment/offers/${offerId}/sign`, { method: "POST", headers: { "Content-Type": "application/json" }, credentials: "same-origin", body: JSON.stringify({ signerName: name }) });
      if (r.ok) {
        const updated: Offer = await r.json();
        setOffers((prev) => ({ ...prev, [appId]: (prev[appId] ?? []).map((o) => (o.id === offerId ? updated : o)) }));
        setApps((prev) => (prev ? prev.map((a) => (a.id === appId ? { ...a, status: "hired" } : a)) : prev));
      }
    } finally { setBusy(null); }
  };
  const sendMessage = async (appId: string) => {
    const body = (drafts[appId] ?? "").trim();
    if (!body) return;
    setBusy(appId + "msg");
    try {
      const r = await fetch(`/api/recruitment/applications/${appId}/messages`, { method: "POST", headers: { "Content-Type": "application/json" }, credentials: "same-origin", body: JSON.stringify({ sender: "talent", senderName, body }) });
      if (r.ok) {
        const m: Message = await r.json();
        setThreads((prev) => ({ ...prev, [appId]: [...(prev[appId] ?? []), m] }));
        setDrafts((prev) => ({ ...prev, [appId]: "" }));
      }
    } finally { setBusy(null); }
  };

  if (!apps || apps.length === 0) return null;

  return (
    <div className="grid grid-cols-1 gap-5">
      <motion.section variants={fade} className="card p-5">
        <div className="flex items-center justify-between mb-3">
          <div>
            <h3 className="font-display text-[15px] font-bold text-ink-hi">{t("talent.apps.title", "My applications")}</h3>
            <p className="text-[11px] text-ink-lo mt-0.5">{t("talent.apps.sub", "Live status, offers, and messages for the roles you've applied to.")}</p>
          </div>
          <span className="chip !text-[10px] !text-brand-bright !border-brand/30"><span className="h-1.5 w-1.5 rounded-full bg-brand-bright animate-pulse" /> LIVE · {apps.length}</span>
        </div>
        <div className="space-y-2">
          {apps.map((a) => {
            const isOpen = openId === a.id;
            const appOffers = (offers[a.id] ?? []).filter((o) => o.status !== "draft");
            const thread = threads[a.id] ?? [];
            const unread = thread.filter((m) => m.sender === "recruiter" && !m.read).length;
            const pendingOffer = appOffers.some((o) => o.status === "sent");
            return (
              <div key={a.id} className="rounded-xl border border-line/60 bg-panel2/40 overflow-hidden">
                <button onClick={() => toggle(a.id)} className="w-full flex items-center gap-3 px-3 py-2 text-left hover:bg-white/[0.02] transition">
                  <div className="min-w-0 flex-1">
                    <div className="text-[13px] text-ink-hi truncate">{a.roleTitle}</div>
                    <div className="text-[10px] text-ink-lo truncate">{a.city} · {t("talent.apps.appliedOn", "applied")} {new Date(a.appliedAt).toLocaleDateString()}</div>
                  </div>
                  {pendingOffer && <span className="chip !text-[10px] !text-gold !border-gold/30">{t("talent.apps.offer", "Offer")}</span>}
                  {unread > 0 && <span className="chip !text-[10px] !text-brand-bright !border-brand/30">{unread} {t("talent.apps.new", "new")}</span>}
                  <span className={`chip !text-[10px] capitalize ${statusChip(a.status)}`}>{a.status}</span>
                  <span className={`text-ink-lo text-[11px] transition-transform ${isOpen ? "rotate-90" : ""}`}>▸</span>
                </button>

                {isOpen && (
                  <div className="border-t border-line/40 px-3 py-3 space-y-4">
                    {/* Offers */}
                    {appOffers.length > 0 && (
                      <div>
                        <div className="eyebrow mb-2">{t("talent.offer.title", "Offers")}</div>
                        <div className="space-y-2">
                          {appOffers.map((o) => (
                            <div key={o.id} className="rounded-lg border border-line/60 bg-base/40 p-3">
                              <div className="flex items-start justify-between gap-2">
                                <div className="min-w-0">
                                  <div className="text-[13px] font-semibold text-ink-hi truncate">{o.title}</div>
                                  <div className="text-[11px] text-ink-mid num">{o.currency} {o.salaryAmount.toLocaleString()} · {t("talent.offer.starts", "starts")} {o.startDate}</div>
                                  {o.notes && <div className="text-[11px] text-ink-lo mt-1">{o.notes}</div>}
                                  {o.signedByName && <div className="text-[10px] text-brand-bright mt-1">✓ {t("talent.offer.signedBy", "e-signed by {{name}}", { name: o.signedByName })}{o.signedAt ? ` · ${new Date(o.signedAt).toLocaleDateString()}` : ""}</div>}
                                </div>
                                <span className={`chip !text-[10px] capitalize shrink-0 ${statusChip(o.status)}`}>{o.status}</span>
                              </div>
                              <div className="mt-2.5 flex flex-wrap items-center gap-1.5 border-t border-line/40 pt-2.5">
                                <a href={`/api/recruitment/offers/${o.id}/letter`} target="_blank" rel="noreferrer" className="rounded-lg bg-panel2/70 px-2.5 py-1 text-[11px] font-semibold text-ink-mid hover:text-ink-hi transition">{t("talent.offer.letter", "View letter")}</a>
                                {live && o.status === "sent" && (
                                  <>
                                    <button onClick={() => decideOffer(a.id, o.id, "accept")} disabled={!!busy} className="rounded-lg bg-brand/15 px-2.5 py-1 text-[11px] font-semibold text-brand-bright hover:bg-brand/25 transition disabled:opacity-50">{busy === o.id + "accept" ? t("talent.offer.accepting", "Accepting…") : t("talent.offer.accept", "Accept")}</button>
                                    <button onClick={() => decideOffer(a.id, o.id, "decline")} disabled={!!busy} className="rounded-lg px-2.5 py-1 text-[11px] font-semibold text-ink-lo hover:text-pink transition disabled:opacity-50">{busy === o.id + "decline" ? t("talent.offer.declining", "Declining…") : t("talent.offer.decline", "Decline")}</button>
                                    <span className="mx-1 text-[10px] text-ink-lo">{t("talent.offer.or", "or e-sign:")}</span>
                                    <input value={signNames[o.id] ?? ""} onChange={(e) => setSignNames((p) => ({ ...p, [o.id]: e.target.value }))} placeholder={t("talent.offer.yourName", "Type your full name")} className="min-w-0 flex-1 rounded-lg border border-line/70 bg-panel2/50 px-2.5 py-1 text-[11px] text-ink-hi placeholder:text-ink-lo focus:border-brand/50 focus:outline-none" />
                                    <button onClick={() => signOffer(a.id, o.id)} disabled={!!busy || !(signNames[o.id] ?? "").trim()} className="rounded-lg bg-gold/20 px-2.5 py-1 text-[11px] font-semibold text-gold hover:bg-gold/30 transition disabled:opacity-50">{busy === o.id + "sign" ? t("talent.offer.signing", "Signing…") : t("talent.offer.sign", "Sign & accept")}</button>
                                  </>
                                )}
                              </div>
                            </div>
                          ))}
                        </div>
                      </div>
                    )}

                    {/* Conversation */}
                    <div>
                      <div className="eyebrow mb-2">{t("talent.msg.title", "Messages with the employer")}</div>
                      <div className="space-y-1.5 max-h-56 overflow-y-auto">
                        {thread.map((m) => (
                          <div key={m.id} className={`rounded-lg px-2.5 py-1.5 text-[12px] ${m.sender === "talent" ? "bg-brand/[0.08] ml-8" : "bg-panel2/50 mr-8"}`}>
                            <div className="flex items-center justify-between gap-2"><span className="text-[10px] font-semibold text-ink-lo">{m.sender === "talent" ? t("talent.msg.you", "You") : m.senderName}</span><span className="text-[10px] text-ink-lo">{new Date(m.sentAt).toLocaleDateString()}</span></div>
                            <div className="text-ink-hi">{m.body}</div>
                          </div>
                        ))}
                        {thread.length === 0 && <div className="py-2 text-center text-[12px] text-ink-lo">{t("talent.msg.none", "No messages yet.")}</div>}
                      </div>
                      {live && (
                        <div className="mt-2 flex items-center gap-2">
                          <input value={drafts[a.id] ?? ""} onChange={(e) => setDrafts((p) => ({ ...p, [a.id]: e.target.value }))} onKeyDown={(e) => { if (e.key === "Enter") sendMessage(a.id); }} placeholder={t("talent.msg.placeholder", "Write a message…")} className="flex-1 min-w-0 rounded-lg border border-line/70 bg-panel2/50 px-2.5 py-1.5 text-[12px] text-ink-hi placeholder:text-ink-lo focus:border-brand/50 focus:outline-none" />
                          <button onClick={() => sendMessage(a.id)} disabled={busy === a.id + "msg" || !(drafts[a.id] ?? "").trim()} className="rounded-lg bg-brand/15 px-3 py-1.5 text-[11px] font-semibold text-brand-bright hover:bg-brand/25 transition disabled:opacity-50">{t("talent.msg.send", "Send")}</button>
                        </div>
                      )}
                    </div>
                  </div>
                )}
              </div>
            );
          })}
        </div>
      </motion.section>
    </div>
  );
}

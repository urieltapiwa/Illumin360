import { useState } from "react";
import { useTranslation } from "react-i18next";

// Apply-time screening form. Shown when a marketplace role has application-form questions: it collects
// the candidate's answers, applies to the role (POST /requests/{id}/apply → application id), then posts
// the answers (POST /applications/{id}/answers). Roles with no questions never open this — the caller
// applies directly, preserving one-click apply.

export interface FormQuestion { id: string; label: string; kind: string; options: string[]; required: boolean; sortOrder: number; }

export default function ApplyForm({
  roleId,
  roleTitle,
  talentId,
  talentType,
  questions,
  features,
  onClose,
  onApplied,
}: {
  roleId: string;
  roleTitle: string;
  talentId: string;
  talentType: string;
  questions: FormQuestion[];
  features?: { citySignal?: number; roleSignal?: number; skillSignal?: number };
  onClose: () => void;
  onApplied: (state: "done" | "error") => void;
}) {
  const { t } = useTranslation();
  const [answers, setAnswers] = useState<Record<string, string>>({});
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const set = (id: string, value: string) => setAnswers((a) => ({ ...a, [id]: value }));
  const missingRequired = questions.some((q) => q.required && !(answers[q.id] ?? "").trim());

  const submit = async () => {
    if (missingRequired) return;
    setBusy(true);
    setError(null);
    try {
      const r = await fetch(`/api/recruitment/requests/${roleId}/apply`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        credentials: "same-origin",
        body: JSON.stringify({ talentId, talentType, source: "careers", ...features }),
      });
      if (!r.ok && r.status !== 409) { setError(t("apply.failed", "Could not apply — please try again.")); return; }
      // On 409 (already applied) we have no fresh application id, so answers are skipped.
      if (r.ok) {
        const app = await r.json().catch(() => null);
        const items = questions
          .filter((q) => (answers[q.id] ?? "").trim())
          .map((q) => ({ questionId: q.id, value: answers[q.id] }));
        if (app?.id && items.length) {
          await fetch(`/api/recruitment/applications/${app.id}/answers`, {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            credentials: "same-origin",
            body: JSON.stringify({ answers: items }),
          }).catch(() => { /* answers best-effort; the application still stands */ });
        }
      }
      onApplied("done");
      onClose();
    } catch {
      setError(t("apply.failed", "Could not apply — please try again."));
    } finally {
      setBusy(false);
    }
  };

  return (
    <div className="fixed inset-0 z-50 grid place-items-center bg-black/60 backdrop-blur-sm p-4" onClick={onClose}>
      <div className="w-full max-w-lg rounded-2xl border border-line/70 bg-base shadow-2xl" onClick={(e) => e.stopPropagation()}>
        <div className="flex items-center justify-between border-b border-line/60 px-5 py-3.5">
          <div className="min-w-0">
            <h3 className="font-display text-[15px] font-bold text-ink-hi truncate">{t("apply.title", "Apply — {{role}}", { role: roleTitle })}</h3>
            <p className="text-[11px] text-ink-lo mt-0.5">{t("apply.sub", "A few questions from the employer before you apply.")}</p>
          </div>
          <button onClick={onClose} className="ml-3 text-ink-lo hover:text-ink-hi transition text-lg leading-none">✕</button>
        </div>
        <div className="px-5 py-4 space-y-3 max-h-[60vh] overflow-y-auto">
          {questions.map((q) => (
            <div key={q.id}>
              <label className="block text-[12px] font-medium text-ink-hi mb-1">{q.label}{q.required && <span className="text-pink ml-0.5">*</span>}</label>
              {q.kind === "textarea" ? (
                <textarea rows={3} value={answers[q.id] ?? ""} onChange={(e) => set(q.id, e.target.value)} className="w-full rounded-lg border border-line/70 bg-panel2/50 px-2.5 py-1.5 text-[12px] text-ink-hi placeholder:text-ink-lo focus:border-brand/50 focus:outline-none" />
              ) : q.kind === "select" ? (
                <select value={answers[q.id] ?? ""} onChange={(e) => set(q.id, e.target.value)} className="w-full rounded-lg border border-line/70 bg-panel2/50 px-2.5 py-1.5 text-[12px] text-ink-hi focus:border-brand/50 focus:outline-none">
                  <option value="">{t("apply.choose", "Choose…")}</option>
                  {q.options.map((o) => <option key={o} value={o}>{o}</option>)}
                </select>
              ) : q.kind === "boolean" ? (
                <label className="flex items-center gap-2 text-[12px] text-ink-mid"><input type="checkbox" checked={answers[q.id] === "true"} onChange={(e) => set(q.id, e.target.checked ? "true" : "false")} />{t("apply.yes", "Yes")}</label>
              ) : (
                <input type={q.kind === "number" ? "number" : "text"} value={answers[q.id] ?? ""} onChange={(e) => set(q.id, e.target.value)} className="w-full rounded-lg border border-line/70 bg-panel2/50 px-2.5 py-1.5 text-[12px] text-ink-hi placeholder:text-ink-lo focus:border-brand/50 focus:outline-none" />
              )}
            </div>
          ))}
          {error && <div className="text-[12px] text-pink">{error}</div>}
        </div>
        <div className="flex items-center justify-end gap-2 border-t border-line/60 px-5 py-3.5">
          <button onClick={onClose} className="rounded-lg px-3 py-1.5 text-[12px] font-semibold text-ink-lo hover:text-ink-hi transition">{t("apply.cancel", "Cancel")}</button>
          <button onClick={submit} disabled={busy || missingRequired} className="rounded-lg bg-brand/15 px-3.5 py-1.5 text-[12px] font-semibold text-brand-bright hover:bg-brand/25 transition disabled:opacity-50" title={missingRequired ? t("apply.fillRequired", "Answer the required questions first.") : undefined}>{busy ? t("apply.submitting", "Applying…") : t("apply.submit", "Submit application")}</button>
        </div>
      </div>
    </div>
  );
}

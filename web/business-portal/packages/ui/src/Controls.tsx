import { useTranslation } from "react-i18next";
import { LANGS } from "./langs";
import { THEMES, useTheme } from "./theme";

export function LanguageSwitcher() {
  const { i18n } = useTranslation();
  const cur = i18n.resolvedLanguage || "en";
  return (
    <div className="flex items-center rounded-xl border border-line/70 bg-panel2/50 p-0.5" title="Language">
      {LANGS.map((l) => (
        <button
          key={l.code}
          onClick={() => i18n.changeLanguage(l.code)}
          className={`px-2.5 py-1.5 text-[11px] font-semibold rounded-lg transition ${cur.startsWith(l.code) ? "bg-brand text-base" : "text-ink-mid hover:text-ink-hi"}`}
        >
          {l.short}
        </button>
      ))}
    </div>
  );
}

export function ThemeSwitcher() {
  const [theme, setTheme] = useTheme();
  return (
    <div className="flex items-center gap-1.5 rounded-xl border border-line/70 bg-panel2/50 px-2 py-1.5" title="Theme">
      {THEMES.map((t) => (
        <button
          key={t.id}
          title={t.label}
          aria-label={t.label}
          onClick={() => setTheme(t.id)}
          className={`h-4 w-4 rounded-full transition ${theme === t.id ? "ring-2 ring-ink-hi ring-offset-2 ring-offset-panel2" : "ring-1 ring-line hover:scale-110"}`}
          style={{ background: t.swatch }}
        />
      ))}
    </div>
  );
}

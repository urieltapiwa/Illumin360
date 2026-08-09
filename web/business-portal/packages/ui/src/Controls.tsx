import type { ReactNode } from "react";
import { useTranslation } from "react-i18next";
import { LANGS } from "./langs";
import { THEMES, useTheme, useMode, type Mode } from "./theme";

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

const MODE_ICON: Record<Mode, ReactNode> = {
  // sun
  light: <path d="M12 4V2M12 22v-2M4 12H2m20 0h-2M5.6 5.6 4.2 4.2m15.6 15.6-1.4-1.4M18.4 5.6l1.4-1.4M4.2 19.8l1.4-1.4M12 8a4 4 0 1 0 0 8 4 4 0 0 0 0-8z" />,
  // moon
  dark: <path d="M21 12.8A9 9 0 1 1 11.2 3a7 7 0 0 0 9.8 9.8z" />,
  // monitor (system)
  system: <path d="M3 5h18v11H3zM8 20h8M12 16v4" />,
};
const MODES: Mode[] = ["system", "light", "dark"];

export function ThemeSwitcher() {
  const [theme, setTheme] = useTheme();
  const [mode, setMode] = useMode();
  return (
    <div className="flex items-center gap-2 rounded-xl border border-line/70 bg-panel2/50 px-2 py-1.5" title="Appearance">
      {/* light / dark / system */}
      <div className="flex items-center gap-0.5">
        {MODES.map((m) => (
          <button
            key={m}
            title={m}
            aria-label={`${m} mode`}
            aria-pressed={mode === m}
            onClick={() => setMode(m)}
            className={`grid h-6 w-6 place-items-center rounded-lg transition ${mode === m ? "bg-brand/20 text-brand-bright" : "text-ink-lo hover:text-ink-hi"}`}
          >
            <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.8" strokeLinecap="round" strokeLinejoin="round">{MODE_ICON[m]}</svg>
          </button>
        ))}
      </div>
      <span className="h-4 w-px bg-line/70" />
      {/* accent presets */}
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

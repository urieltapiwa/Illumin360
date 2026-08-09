import { useState } from "react";

export interface ThemePreset { id: string; label: string; swatch: string; }
export const THEMES: ThemePreset[] = [
  { id: "default", label: "Forest", swatch: "#1FB283" },
  { id: "ocean", label: "Ocean", swatch: "#636EF1" },
  { id: "amber", label: "Amber", swatch: "#EA8E3A" },
];

const KEY = "illumin-theme";

export function applyTheme(id: string) {
  if (id === "default") delete document.documentElement.dataset.theme;
  else document.documentElement.dataset.theme = id;
  try { localStorage.setItem(KEY, id); } catch { /* ignore */ }
}

export function initTheme(): string {
  let saved = "default";
  try { saved = localStorage.getItem(KEY) || "default"; } catch { /* ignore */ }
  applyTheme(saved);
  return saved;
}

export function useTheme() {
  const [theme, setTheme] = useState<string>(() => {
    try { return localStorage.getItem(KEY) || "default"; } catch { return "default"; }
  });
  const set = (id: string) => { applyTheme(id); setTheme(id); };
  return [theme, set] as const;
}

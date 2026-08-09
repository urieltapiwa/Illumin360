/** @type {import('tailwindcss').Config} */
const v = (name) => `rgb(var(${name}) / <alpha-value>)`;
export default {
  content: ["./index.html", "./src/**/*.{ts,tsx}", "./packages/ui/src/**/*.{ts,tsx}"],
  theme: {
    extend: {
      colors: {
        base: v("--c-base"),
        panel: v("--c-panel"),
        panel2: v("--c-panel2"),
        elevate: v("--c-elevate"),
        line: v("--c-line"),
        grid: v("--c-grid"),
        brand: { DEFAULT: v("--c-brand"), bright: v("--c-brand-bright"), deep: v("--c-brand-deep") },
        gold: v("--c-gold"),
        blue: v("--c-blue"),
        violet: v("--c-violet"),
        pink: v("--c-pink"),
        ink: { hi: v("--c-ink-hi"), mid: v("--c-ink-mid"), lo: v("--c-ink-lo") },
      },
      fontFamily: {
        display: ['"Bricolage Grotesque"', "ui-sans-serif", "system-ui"],
        sans: ['"Hanken Grotesk"', "ui-sans-serif", "system-ui"],
        mono: ['"JetBrains Mono"', "ui-monospace", "monospace"],
      },
      boxShadow: {
        card: "0 1px 0 rgb(255 255 255 / 0.03) inset, 0 18px 40px -24px rgb(0 0 0 / 0.8)",
        glow: "0 0 0 1px rgb(var(--c-brand) / 0.18), 0 0 36px -8px rgb(var(--c-brand) / 0.35)",
      },
    },
  },
  plugins: [],
};

import i18n from "i18next";
import { initReactI18next } from "react-i18next";
import LanguageDetector from "i18next-browser-languagedetector";
import pro from "./locales/pro";
import student from "./locales/student";
import admin from "./locales/admin";
import support from "./locales/support";

const en = {
  nav: { workspace: "Workspace", overview: "Overview", talent: "Talent", companies: "Companies", hiring: "Hiring", revenue: "Revenue", settings: "Settings" },
  topbar: { title: "Talent Insights", subtitle: "Your talent-market analytics workspace", demo: "DEMO DATA", signOut: "Sign out" },
  kpi: { totalTalent: "Total Talent", activeCompanies: "Active Companies", hiresPlaced: "Hires placed", fillRate: "Fill rate", arr: "ARR" },
  panel: { growth: "Talent pool growth", growthSub: "Cumulative registered talent", mix: "Talent mix", mixSub: "Professionals vs students", revenue: "Recurring revenue", funnel: "Hiring funnel", byCity: "Talent by city", byCitySub: "Top regions in the pool", hires: "Hires per year", live: "LIVE", liveFeed: "Live talent feed", recent: "Recent placements" },
  login: {
    portal: "Business portal", welcome: "Welcome back", subtitle: "Sign in securely to your Talent Insights workspace.",
    continue: "Continue with Keycloak", secured: "secured by Keycloak SSO", demo: "Explore the live demo →", oauth: "OAuth 2.1 / OIDC · tokens handled by your BFF",
    headline1: "The talent market,", headline2: "measured.",
    blurb: "Real-time analytics on Namibia's professional and student talent pool — growth, hiring funnels, benchmarks and revenue, in one console.",
    statTalent: "talent profiled", statCompanies: "companies", statHires: "hires placed", location: "Illumin Investments CC · Windhoek, Namibia",
  },
  common: { language: "Language", theme: "Theme" },
  filter: { tapCity: "Top regions · tap a city to filter the live feed", clear: "Clear filter", empty: "No live talent in {{city}} right now." },
};

const af: typeof en = {
  nav: { workspace: "Werkruimte", overview: "Oorsig", talent: "Talent", companies: "Maatskappye", hiring: "Werwing", revenue: "Inkomste", settings: "Instellings" },
  topbar: { title: "Talent-insigte", subtitle: "Jou talentmark-analise-werkruimte", demo: "DEMO-DATA", signOut: "Teken uit" },
  kpi: { totalTalent: "Totale talent", activeCompanies: "Aktiewe maatskappye", hiresPlaced: "Aanstellings", fillRate: "Vulkoers", arr: "JIR" },
  panel: { growth: "Groei van talentpoel", growthSub: "Kumulatiewe geregistreerde talent", mix: "Talentsamestelling", mixSub: "Professionele persone teenoor studente", revenue: "Herhalende inkomste", funnel: "Werwingstregter", byCity: "Talent per stad", byCitySub: "Top streke in die poel", hires: "Aanstellings per jaar", live: "LEWENDIG", liveFeed: "Lewendige talentvoer", recent: "Onlangse plasings" },
  login: {
    portal: "Besigheidsportaal", welcome: "Welkom terug", subtitle: "Teken veilig in by jou Talent-insigte werkruimte.",
    continue: "Gaan voort met Keycloak", secured: "beveilig deur Keycloak SSO", demo: "Verken die lewendige demo →", oauth: "OAuth 2.1 / OIDC · tokens deur jou BFF hanteer",
    headline1: "Die talentmark,", headline2: "gemeet.",
    blurb: "Intydse ontleding van Namibië se professionele en studente-talentpoel — groei, werwingstregters, maatstawwe en inkomste, in een konsole.",
    statTalent: "talent geprofileer", statCompanies: "maatskappye", statHires: "aanstellings gemaak", location: "Illumin Investments BK · Windhoek, Namibië",
  },
  common: { language: "Taal", theme: "Tema" },
  filter: { tapCity: "Top streke · tik 'n stad om die lewendige voer te filtreer", clear: "Verwyder filter", empty: "Tans geen lewendige talent in {{city}} nie." },
};

i18n
  .use(LanguageDetector)
  .use(initReactI18next)
  .init({
    resources: {
      en: { translation: { ...en, pro: pro.en, student: student.en, admin: admin.en, support: support.en } },
      af: { translation: { ...af, pro: pro.af, student: student.af, admin: admin.af, support: support.af } },
    },
    fallbackLng: "en",
    supportedLngs: ["en", "af"],
    interpolation: { escapeValue: false },
    detection: { order: ["localStorage", "navigator"], caches: ["localStorage"], lookupLocalStorage: "illumin-lang" },
  });

export default i18n;

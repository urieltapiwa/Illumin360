const en = {
  brand: { tag: "Support" },
  nav: { section: "Support desk", queue: "Queue", myTickets: "My Tickets", reports: "Reports", knowledge: "Knowledge", settings: "Settings" },
  sidebar: { slaLabel: "{{n}}% SLA", slaTip: "Great work — keep first responses under 15 minutes to stay on target." },
  topbar: { title: "Support Desk", subtitle: "Live queue, SLAs and customer satisfaction across Illumin360.", demo: "DEMO DATA", csat: "CSAT {{n}}%", signOut: "Sign out" },
  kpi: { openTickets: "Open tickets", inQueue: "in queue", assignedToMe: "Assigned to me", active: "active", resolvedToday: "Resolved today", resolvedDelta: "+12%", avgFirstResponse: "Avg 1st response", avgTarget: "target 15m", csat: "CSAT", csatPeriod: "last 30d", slaMet: "SLA met", slaPeriod: "this month" },
  queue: { title: "Ticket queue", subtitle: "Open & in-progress, newest first", openBadge: "{{n}} open", colId: "ID", colSubject: "Subject", colRequester: "Requester", colPriority: "Pri", colStatus: "Status", colAge: "Age" },
  sla: { title: "SLA & priority", subtitle: "Compliance this month", p1: "P1 · Critical", p2: "P2 · High", p3: "P3 · Normal" },
  volume: { title: "Ticket volume", subtitle: "Created vs resolved · last 14 days", seriesCreated: "Created", seriesResolved: "Resolved" },
  category: { title: "By category", subtitle: "Open tickets" },
  leaderboard: { title: "Agent leaderboard", resolved: "{{n}} resolved", csat: "CSAT {{n}}%" },
  activity: { title: "Activity" },
  loading: "Loading the desk…",
  footer: { brand: "Illumin360 · Support desk", disclaimer: "Synthetic demo data · for illustration only" },
};

const af: typeof en = {
  brand: { tag: "Ondersteuning" },
  nav: { section: "Ondersteuningstoonbank", queue: "Tou", myTickets: "My kaartjies", reports: "Verslae", knowledge: "Kennisbasis", settings: "Instellings" },
  sidebar: { slaLabel: "{{n}}% DVO", slaTip: "Goeie werk — hou eerste reaksies onder 15 minute om op teiken te bly." },
  topbar: { title: "Ondersteuningstoonbank", subtitle: "Lewendige tou, DVO's en kliëntetevredenheid regoor Illumin360.", demo: "DEMO-DATA", csat: "KTT {{n}}%", signOut: "Teken uit" },
  kpi: { openTickets: "Oop kaartjies", inQueue: "in tou", assignedToMe: "Aan my toegewys", active: "aktief", resolvedToday: "Vandag opgelos", resolvedDelta: "+12%", avgFirstResponse: "Gem. eerste reaksie", avgTarget: "teiken 15m", csat: "KTT", csatPeriod: "laaste 30 dae", slaMet: "DVO bereik", slaPeriod: "hierdie maand" },
  queue: { title: "Kaartjie-tou", subtitle: "Oop & in proses, nuutste eerste", openBadge: "{{n}} oop", colId: "ID", colSubject: "Onderwerp", colRequester: "Aanvraer", colPriority: "Pri", colStatus: "Status", colAge: "Ouderdom" },
  sla: { title: "DVO & prioriteit", subtitle: "Nakoming hierdie maand", p1: "P1 · Kritiek", p2: "P2 · Hoog", p3: "P3 · Normaal" },
  volume: { title: "Kaartjievolume", subtitle: "Geskep teenoor opgelos · laaste 14 dae", seriesCreated: "Geskep", seriesResolved: "Opgelos" },
  category: { title: "Per kategorie", subtitle: "Oop kaartjies" },
  leaderboard: { title: "Agent-ranglys", resolved: "{{n}} opgelos", csat: "KTT {{n}}%" },
  activity: { title: "Aktiwiteit" },
  loading: "Die toonbank laai…",
  footer: { brand: "Illumin360 · Ondersteuningstoonbank", disclaimer: "Sintetiese demo-data · slegs ter illustrasie" },
};

export default { en, af };

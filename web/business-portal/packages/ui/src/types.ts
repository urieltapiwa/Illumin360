// Chart-input data shapes shared across portals and the chart builders.
export interface Monthly {
  month: string; newTalent: number; cumTalent: number; newCompanies: number;
  cumCompanies: number; activeSubs: number; requests: number; applications: number;
  hires: number; mrr: number; revenue: number; cumRevenue: number;
}
export interface Annual {
  year: string; newTalent: number; professionals: number; students: number;
  hires: number; applications: number; revenue: number; eoyTalent: number; activeSubs: number;
}
export interface Named { name: string; value: number; }
export interface CityRow { city: string; value: number; }
export interface FunnelRow { stage: string; value: number; }

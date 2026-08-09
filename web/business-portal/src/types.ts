// App-specific data shapes. Chart-input types live in the shared @illumin360/ui package.
import type { Monthly, Annual, Named, CityRow, FunnelRow } from "@illumin360/ui";

export type { Monthly, Annual, Named, CityRow, FunnelRow };

export interface Hire {
  name: string; role: string; company: string; city: string; date: string; score: number; type: string;
}
export interface Kpis {
  totalTalent: number; professionals: number; students: number; companies: number;
  activeSubscribers: number; totalHires: number; applications: number; fillRate: number;
  mrr: number; arr: number; totalRevenue: number; yearsLive: number;
}
export interface Dashboard {
  meta: { product: string; scope: string; currency: string; generated: string };
  kpis: Kpis; monthly: Monthly[]; annual: Annual[]; byCity: CityRow[];
  byIndustry: Named[]; planMix: Named[]; funnel: FunnelRow[]; recentHires: Hire[];
}

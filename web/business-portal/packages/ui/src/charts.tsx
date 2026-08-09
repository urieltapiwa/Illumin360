import * as echarts from "echarts";
import ReactECharts from "echarts-for-react";
import type { Monthly, Annual, CityRow, FunnelRow, Named } from "./types";

/* read a palette channel from CSS and return an echarts-friendly color string */
function cssRGB(name: string): string {
  if (typeof document === "undefined") return "#888888";
  const raw = getComputedStyle(document.documentElement).getPropertyValue(name).trim();
  if (!raw) return "#888888";
  return raw.startsWith("#") ? raw : `rgb(${raw.split(/\s+/).join(",")})`;
}
function alpha(rgb: string, a: number): string {
  return rgb.startsWith("rgb(") ? rgb.replace("rgb(", "rgba(").replace(")", `,${a})`) : rgb;
}
function transparent(rgb: string): string {
  if (rgb.startsWith("rgb(")) return rgb.replace("rgb(", "rgba(").replace(")", ",0)");
  return rgb.length >= 7 ? rgb.slice(0, 7) + "00" : rgb;
}

/* live theme colors — getters so charts recolour when the palette changes */
export const C = {
  get brand() { return cssRGB("--c-brand"); },
  get brandBright() { return cssRGB("--c-brand-bright"); },
  get brandDeep() { return cssRGB("--c-brand-deep"); },
  get gold() { return cssRGB("--c-gold"); },
  get blue() { return cssRGB("--c-blue"); },
  get violet() { return cssRGB("--c-violet"); },
  get pink() { return cssRGB("--c-pink"); },
  get line() { return cssRGB("--c-line"); },
  get grid() { return cssRGB("--c-grid"); },
  get text() { return cssRGB("--c-ink-lo"); },
  get textHi() { return cssRGB("--c-ink-mid"); },
  get panel() { return cssRGB("--c-panel"); },
  get inkHi() { return cssRGB("--c-ink-hi"); },
};

export const nf = (n: number) => Math.round(n).toLocaleString("en-US");
export const compact = (n: number) =>
  n >= 1e6 ? (n / 1e6).toFixed(n >= 1e7 ? 0 : 1) + "M" : n >= 1e3 ? (n / 1e3).toFixed(0) + "k" : String(Math.round(n));
export const curC = (n: number) => "N$" + compact(n);

const grad = (top: string) => new echarts.graphic.LinearGradient(0, 0, 0, 1, [{ offset: 0, color: top }, { offset: 1, color: transparent(top) }]);
const ax = () => ({
  axisLine: { lineStyle: { color: C.line } },
  axisTick: { show: false },
  axisLabel: { color: C.text, fontFamily: "JetBrains Mono", fontSize: 10 },
  splitLine: { lineStyle: { color: C.grid } },
});
const tip = () => ({
  trigger: "axis" as const,
  backgroundColor: C.panel,
  borderColor: C.line,
  borderWidth: 1,
  textStyle: { color: C.inkHi, fontFamily: "Hanken Grotesk", fontSize: 12 },
  axisPointer: { lineStyle: { color: C.line } },
});

export function Chart({ option, height = 280, onClick }: { option: echarts.EChartsOption; height?: number; onClick?: (name: string) => void }) {
  const onEvents = onClick ? { click: (p: { name?: string }) => { if (p?.name) onClick(p.name); } } : undefined;
  return (
    <ReactECharts
      option={option}
      style={{ height, width: "100%", cursor: onClick ? "pointer" : "default" }}
      opts={{ renderer: "canvas" }}
      notMerge
      lazyUpdate
      onEvents={onEvents}
    />
  );
}

function monthLabel(idx: number, cats: string[]) {
  const m = cats[idx];
  return m && m.endsWith("-01") ? m.slice(0, 4) : "";
}

export function talentGrowthOption(m: Monthly[]): echarts.EChartsOption {
  const cats = m.map((d) => d.month);
  return {
    grid: { left: 46, right: 16, top: 18, bottom: 26 },
    tooltip: { ...tip(), valueFormatter: (v) => nf(v as number) },
    xAxis: { type: "category", data: cats, boundaryGap: false, ...ax(), axisLabel: { ...ax().axisLabel, formatter: (_: string, i: number) => monthLabel(i, cats) } },
    yAxis: { type: "value", ...ax(), axisLabel: { ...ax().axisLabel, formatter: (v: number) => compact(v) } },
    series: [{ name: "Talent pool", type: "line", data: m.map((d) => d.cumTalent), smooth: true, showSymbol: false, lineStyle: { color: C.brand, width: 2.5, shadowColor: alpha(C.brand, 0.5), shadowBlur: 12 }, areaStyle: { color: grad(C.brand), opacity: 0.85 } }],
  };
}

export function revenueOption(m: Monthly[]): echarts.EChartsOption {
  const cats = m.map((d) => d.month);
  return {
    grid: { left: 48, right: 48, top: 26, bottom: 26 },
    legend: { data: ["MRR", "Active subscribers"], textStyle: { color: C.textHi, fontFamily: "Hanken Grotesk" }, top: 0, right: 0, icon: "roundRect", itemWidth: 10, itemHeight: 10 },
    tooltip: { ...tip() },
    xAxis: { type: "category", data: cats, boundaryGap: false, ...ax(), axisLabel: { ...ax().axisLabel, formatter: (_: string, i: number) => monthLabel(i, cats) } },
    yAxis: [
      { type: "value", ...ax(), axisLabel: { ...ax().axisLabel, formatter: (v: number) => curC(v) } },
      { type: "value", ...ax(), splitLine: { show: false }, axisLabel: { ...ax().axisLabel, formatter: (v: number) => compact(v) } },
    ],
    series: [
      { name: "MRR", type: "line", data: m.map((d) => d.mrr), smooth: true, showSymbol: false, yAxisIndex: 0, lineStyle: { color: C.gold, width: 2.5, shadowColor: alpha(C.gold, 0.45), shadowBlur: 12 }, areaStyle: { color: grad(C.gold), opacity: 0.5 } },
      { name: "Active subscribers", type: "line", data: m.map((d) => d.activeSubs), smooth: true, showSymbol: false, yAxisIndex: 1, lineStyle: { color: C.blue, width: 2, type: "dashed" } },
    ],
  };
}

export function hiresOption(a: Annual[]): echarts.EChartsOption {
  return {
    grid: { left: 40, right: 14, top: 16, bottom: 24 },
    tooltip: { ...tip(), valueFormatter: (v) => nf(v as number) },
    xAxis: { type: "category", data: a.map((d) => d.year), ...ax(), splitLine: { show: false } },
    yAxis: { type: "value", ...ax(), axisLabel: { ...ax().axisLabel, formatter: (v: number) => compact(v) } },
    series: [{ name: "Hires", type: "bar", data: a.map((d) => d.hires), barWidth: "56%", itemStyle: { color: grad(C.brand), borderRadius: [5, 5, 0, 0] } }],
  };
}

export function cityOption(rows: CityRow[]): echarts.EChartsOption {
  const sorted = [...rows].sort((x, y) => x.value - y.value);
  return {
    grid: { left: 92, right: 28, top: 8, bottom: 8 },
    tooltip: { ...tip(), trigger: "item", valueFormatter: (v) => nf(v as number) },
    xAxis: { type: "value", ...ax(), axisLabel: { ...ax().axisLabel, formatter: (v: number) => compact(v) } },
    yAxis: { type: "category", data: sorted.map((d) => d.city), ...ax(), splitLine: { show: false }, axisLabel: { ...ax().axisLabel, fontFamily: "Hanken Grotesk", color: C.textHi } },
    series: [{
      type: "bar", data: sorted.map((d) => d.value), barWidth: "62%",
      itemStyle: { color: new echarts.graphic.LinearGradient(0, 0, 1, 0, [{ offset: 0, color: C.brandDeep }, { offset: 1, color: C.brand }]), borderRadius: [0, 5, 5, 0] },
      label: { show: true, position: "right", color: C.textHi, fontFamily: "JetBrains Mono", fontSize: 10, formatter: (p) => nf((p as { value: number }).value) },
    }],
  };
}

export function donutOption(rows: Named[], palette: string[]): echarts.EChartsOption {
  return {
    tooltip: { ...tip(), trigger: "item", valueFormatter: (v) => nf(v as number) },
    legend: { orient: "vertical", right: 6, top: "center", textStyle: { color: C.textHi, fontFamily: "Hanken Grotesk" }, icon: "circle", itemHeight: 9 },
    series: [{
      type: "pie", radius: ["58%", "82%"], center: ["34%", "50%"], avoidLabelOverlap: true,
      itemStyle: { borderColor: C.panel, borderWidth: 3 }, label: { show: false }, labelLine: { show: false },
      data: rows.map((r, i) => ({ name: r.name, value: r.value, itemStyle: { color: palette[i % palette.length] } })),
    }],
  };
}

export function funnelOption(rows: FunnelRow[]): echarts.EChartsOption {
  const colors = [C.brandDeep, C.brand, C.gold];
  return {
    tooltip: { ...tip(), trigger: "item", formatter: (p: unknown) => { const x = p as { name: string; value: number }; return `${x.name}<br/><b>${nf(x.value)}</b>`; } },
    series: [{
      type: "funnel", left: 10, right: 10, top: 10, bottom: 10, minSize: "32%", maxSize: "100%", gap: 4, sort: "descending",
      label: { color: cssRGB("--c-base"), fontFamily: "Hanken Grotesk", fontWeight: 700, formatter: (p: unknown) => { const x = p as { name: string; value: number }; return `${x.name}  ${nf(x.value)}`; } },
      data: rows.map((r, i) => ({ name: r.stage, value: r.value, itemStyle: { color: colors[i % colors.length] } })),
    }],
  };
}

export function sparkOption(values: number[], color: string): echarts.EChartsOption {
  return {
    grid: { left: 0, right: 0, top: 4, bottom: 0 },
    xAxis: { type: "category", show: false, boundaryGap: false, data: values.map((_, i) => i) },
    yAxis: { type: "value", show: false, min: "dataMin", max: "dataMax" },
    series: [{ type: "line", data: values, smooth: true, showSymbol: false, lineStyle: { color, width: 2 }, areaStyle: { color: grad(color), opacity: 0.35 } }],
  };
}

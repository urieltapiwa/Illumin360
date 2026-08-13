import { useEffect, useState } from "react";

// Shared sidebar helpers for the single-page portal dashboards (Admin/Student/Employer/Support).
// Each portal renders one long scroll of sections; the sidebar items are in-page anchors that
// smooth-scroll to a section and highlight the section currently in view (scrollspy).

// Vertical offset (px) to clear the sticky top header when scrolling / computing the active item.
const HEADER_OFFSET = 96;

/** Smooth-scroll the window so the element with `id` sits just below the sticky header. */
export function scrollToSection(id: string, offset = HEADER_OFFSET) {
  const el = document.getElementById(id);
  if (!el) return;
  // The anchor's position: recomputed on demand because async content (charts) can reflow the
  // page and shift it after the initial scroll request.
  const targetTop = () => Math.max(el.getBoundingClientRect().top + window.scrollY - offset, 0);
  window.scrollTo({ top: targetTop(), behavior: "smooth" });
  // Robustness net for contexts where smooth scrolling is a silent no-op or gets interrupted
  // (background/unfocused tabs, some embedded webviews, non-compositing pages) and for reflow
  // that leaves us short of the anchor: snap to the freshly-computed target if we're not there,
  // then nudge the scrollspy so the active item re-evaluates even where a programmatic scroll
  // emits no scroll event.
  window.setTimeout(() => {
    const t = targetTop();
    if (Math.abs(window.scrollY - t) > 4) window.scrollTo({ top: t });
    window.dispatchEvent(new Event("scroll"));
  }, 340);
}

/** Returns the id of the section currently in view, updated on scroll/resize/content-growth. */
export function useScrollSpy(ids: string[], offset = HEADER_OFFSET): string {
  const key = ids.join(",");
  const [active, setActive] = useState(ids[0] ?? "");
  useEffect(() => {
    const compute = () => {
      const se = document.scrollingElement || document.documentElement;
      // A page is only "scrollable" once its content is taller than the viewport. Async content
      // (charts) can grow the page after mount, so until then we must not treat it as scrolled.
      const scrollable = se.scrollHeight - se.clientHeight > 4;
      let current = ids[0] ?? "";
      for (const id of ids) {
        const el = document.getElementById(id);
        // The last section whose top has scrolled past the header line is the active one.
        if (el && el.getBoundingClientRect().top - offset <= 1) current = id;
      }
      // At the very bottom, force-select the last section so a short trailing section that can
      // never reach the header line still lights up. Only when the page can actually scroll.
      if (scrollable && ids.length && se.scrollHeight - se.clientHeight - se.scrollTop <= 2) {
        current = ids[ids.length - 1];
      }
      setActive((prev) => (prev === current ? prev : current));
    };
    compute();
    // Recompute a few times after mount to catch async layout growth (charts rendering in).
    const raf = requestAnimationFrame(compute);
    const timers = [120, 400, 900].map((ms) => setTimeout(compute, ms));
    window.addEventListener("scroll", compute, { passive: true });
    window.addEventListener("resize", compute);
    // React to content that changes the page height (lazy charts, expanding panels).
    const ro = typeof ResizeObserver !== "undefined" ? new ResizeObserver(compute) : null;
    ro?.observe(document.body);
    return () => {
      cancelAnimationFrame(raf);
      timers.forEach(clearTimeout);
      window.removeEventListener("scroll", compute);
      window.removeEventListener("resize", compute);
      ro?.disconnect();
    };
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [key, offset]);
  return active;
}

/** Anchor marker: an invisible, always-rendered scroll target placed before a section group. */
export function SectionAnchor({ id }: { id: string }) {
  return <div id={id} aria-hidden="true" className="scroll-mt-24" />;
}

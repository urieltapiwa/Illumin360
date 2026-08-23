// Brand theme toggle: authentic HopeUI blue (default) <-> Illumin360 green.
// Applied to <html data-brand> early (this file loads in <head>) to avoid a flash,
// then the navbar button is wired on DOMContentLoaded. Charts re-render on change.
(function () {
  var KEY = "illumin360-brand";
  var root = document.documentElement;

  function label(b) { return b === "green" ? "Illumin360 Green" : "HopeUI Blue"; }

  function apply(b) {
    root.setAttribute("data-brand", b);
    var l = document.getElementById("brandLabel");
    if (l) { l.textContent = label(b); }
    // Let charts recolour to the new primary.
    document.dispatchEvent(new CustomEvent("brandchange", { detail: { brand: b } }));
  }

  apply(localStorage.getItem(KEY) || "blue");

  document.addEventListener("DOMContentLoaded", function () {
    var btn = document.getElementById("brandToggle");
    if (!btn) { return; }
    btn.addEventListener("click", function () {
      var next = root.getAttribute("data-brand") === "green" ? "blue" : "green";
      localStorage.setItem(KEY, next);
      apply(next);
    });
  });

  // Expose the active primary colour for chart scripts.
  window.brandPrimary = function () {
    return getComputedStyle(root).getPropertyValue("--bs-primary").trim() || "#3a57e8";
  };
})();

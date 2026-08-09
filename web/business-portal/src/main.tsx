import { StrictMode } from "react";
import { createRoot } from "react-dom/client";
import "./index.css";
import "./i18n";
import { initTheme, initMode } from "@illumin360/ui";
import App from "./App.tsx";

initTheme();
initMode();

createRoot(document.getElementById("root")!).render(
  <StrictMode>
    <App />
  </StrictMode>,
);

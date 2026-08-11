// Browser push-notification helpers for the portals.
//
// Delivery model: the page polls the in-app notification feed and, when a new unread item appears,
// asks the service worker to raise an OS-level toast. This surfaces notifications outside the tab
// (backgrounded window, other tab) without a server-side Web Push (VAPID) stack. The service worker
// (public/sw.js) already carries a `push` handler, so a future server sender is a drop-in upgrade.

let swReg: ServiceWorkerRegistration | null = null;

/** Whether the browser supports the notification surface we use. */
export const pushSupported = (): boolean =>
  typeof window !== "undefined" && "serviceWorker" in navigator && "Notification" in window;

/** Current permission ("granted" | "denied" | "default"), or "unsupported". */
export const pushPermission = (): NotificationPermission | "unsupported" =>
  pushSupported() ? Notification.permission : "unsupported";

/** Registers the service worker (idempotent). Safe to call on every load. */
export async function registerServiceWorker(): Promise<void> {
  if (!pushSupported()) return;
  try {
    swReg = await navigator.serviceWorker.register(import.meta.env.BASE_URL + "sw.js");
  } catch {
    swReg = null;
  }
}

/** Prompts for notification permission. Returns true if granted. */
export async function enablePush(): Promise<boolean> {
  if (!pushSupported()) return false;
  if (Notification.permission === "granted") return true;
  if (Notification.permission === "denied") return false;
  const result = await Notification.requestPermission();
  return result === "granted";
}

/** Shows an OS notification via the service worker (falls back to a page Notification). */
export async function showPush(title: string, body: string, tag?: string): Promise<void> {
  if (pushPermission() !== "granted") return;
  const reg = swReg ?? (await navigator.serviceWorker.ready.catch(() => null));
  const options: NotificationOptions = { body, tag, icon: import.meta.env.BASE_URL + "favicon.svg" };
  if (reg) {
    await reg.showNotification(title, options);
  } else {
    // eslint-disable-next-line no-new
    new Notification(title, options);
  }
}

/* Illumin360 service worker — notification delivery.
 *
 * Today the portal drives notifications from the in-app feed (see push.ts): the page detects a new
 * unread notification while it polls `/me/notifications` and asks this worker to show an OS toast via
 * `registration.showNotification`. The `push` handler below is wired for a future server-side Web Push
 * (VAPID) upgrade — when the backend gains a push sender, it can post an encrypted payload here and the
 * same toast + click behaviour applies, with no page open required. */

self.addEventListener("install", () => self.skipWaiting());
self.addEventListener("activate", (event) => event.waitUntil(self.clients.claim()));

// Future server Web Push: render whatever payload the push service delivered.
self.addEventListener("push", (event) => {
  let data = { title: "Illumin360", body: "You have a new notification.", url: "/" };
  try {
    if (event.data) data = { ...data, ...event.data.json() };
  } catch {
    if (event.data) data.body = event.data.text();
  }
  event.waitUntil(
    self.registration.showNotification(data.title, {
      body: data.body,
      tag: data.tag,
      data: { url: data.url || "/" },
      icon: "/favicon.svg",
      badge: "/favicon.svg",
    }),
  );
});

// Clicking a toast focuses an existing portal tab (or opens one).
self.addEventListener("notificationclick", (event) => {
  event.notification.close();
  const target = (event.notification.data && event.notification.data.url) || "/";
  event.waitUntil(
    self.clients.matchAll({ type: "window", includeUncontrolled: true }).then((clients) => {
      for (const client of clients) {
        if ("focus" in client) return client.focus();
      }
      return self.clients.openWindow ? self.clients.openWindow(target) : undefined;
    }),
  );
});

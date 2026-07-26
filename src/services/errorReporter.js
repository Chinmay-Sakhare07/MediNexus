// Browser error capture -> POST /api/client-logs (our backend proxy; the
// browser never talks to LogBase directly). Buffers, flushes every 10s or at
// 20 events, uses sendBeacon on pagehide, and silently no-ops on any failure
// — this module must never break the app it watches.

const API_BASE_URL = import.meta.env.VITE_API_BASE_URL || 'http://localhost:5155/api';
const ENDPOINT = `${API_BASE_URL}/client-logs`;
const FLUSH_INTERVAL_MS = 10_000;
const FLUSH_AT = 20;
const BUFFER_CAP = 100;

let buffer = [];
let started = false;

const sessionId = (() => {
  try {
    let id = sessionStorage.getItem('mn_session_id');
    if (!id) {
      id = (crypto.randomUUID && crypto.randomUUID()) ||
        `${Date.now()}-${Math.random().toString(36).slice(2)}`;
      sessionStorage.setItem('mn_session_id', id);
    }
    return id;
  } catch {
    return 'no-session';
  }
})();

const push = (level, message, stack) => {
  // Loop guard: never report our own reporting.
  if (typeof message === 'string' && message.includes('client-logs')) return;
  if (buffer.length >= BUFFER_CAP) return;
  buffer.push({
    timestamp: new Date().toISOString(),
    level,
    message: String(message ?? 'Unknown error').slice(0, 4000),
    stack: stack ? String(stack).slice(0, 8000) : undefined,
    url: window.location.pathname,
    session_id: sessionId,
  });
  if (buffer.length >= FLUSH_AT) flush();
};

const flush = (useBeacon = false) => {
  if (buffer.length === 0) return;
  const events = buffer.splice(0, FLUSH_AT);
  const body = JSON.stringify({ events });
  try {
    if (useBeacon && navigator.sendBeacon) {
      navigator.sendBeacon(ENDPOINT, new Blob([body], { type: 'application/json' }));
      return;
    }
    fetch(ENDPOINT, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body,
      keepalive: true,
    }).catch(() => {});
  } catch {
    /* silently drop — never break the app */
  }
};

export const logError = (message, extra) => {
  const suffix = extra ? ` | ${JSON.stringify(extra).slice(0, 500)}` : '';
  push('ERROR', `${message}${suffix}`);
};

export const initErrorReporter = () => {
  if (started) return;
  started = true;

  window.addEventListener('error', (event) => {
    push('ERROR', event.message || 'Uncaught error', event.error?.stack);
  });

  window.addEventListener('unhandledrejection', (event) => {
    const reason = event.reason;
    push('ERROR',
      reason?.message || String(reason ?? 'Unhandled promise rejection'),
      reason?.stack);
  });

  setInterval(() => flush(), FLUSH_INTERVAL_MS);
  window.addEventListener('pagehide', () => flush(true));
};

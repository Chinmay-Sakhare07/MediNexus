import { useEffect, useState } from 'react';
import { Activity, GitBranch, Database, Server, Globe, Boxes, Radio } from 'lucide-react';

const API_BASE = import.meta.env.VITE_API_BASE_URL || 'http://localhost:5155/api';
const HEALTH_URL = API_BASE.replace(/\/api\/?$/, '') + '/health';

const Node = ({ title, sub, accent, ext }) => (
  <div className={`mn-arch-node${accent ? ' mn-arch-node--accent' : ''}${ext ? ' mn-arch-node--ext' : ''}`}>
    <div className="mn-arch-title">{title}</div>
    {sub && <div className="mn-arch-sub">{sub}</div>}
  </div>
);

const Flow = ({ passive }) => <div className={`mn-flow${passive ? ' mn-flow-passive' : ''}`} />;

const Chips = ({ items }) => (
  <div className="mn-chipline">
    {items.map((c, i) => (
      <span key={i} style={{ display: 'inline-flex', alignItems: 'center', gap: 6 }}>
        <span className={`mn-statechip${c.tone ? ` mn-statechip--${c.tone}` : ''}`}>{c.label}</span>
        {i < items.length - 1 && <span className="mn-chip-arrow">→</span>}
      </span>
    ))}
  </div>
);

const ServiceCard = ({ name, url, meta, pill, pillTone = 'ok' }) => (
  <div className="mn-card p-4">
    <div className="flex items-center justify-between mb-1">
      <span className="font-bold text-sm">{name}</span>
      <span className={`mn-pill mn-pill--${pillTone}`}>{pill}</span>
    </div>
    {url && <div className="text-xs" style={{ color: 'var(--mn-teal-deep)', wordBreak: 'break-all' }}>{url}</div>}
    <div className="text-xs mt-1" style={{ color: 'var(--mn-ink-soft)' }}>{meta}</div>
  </div>
);

export default function Architecture() {
  const [health, setHealth] = useState('checking');

  useEffect(() => {
    fetch(HEALTH_URL)
      .then((r) => r.json())
      .then((j) => setHealth(j?.status === 'ok' ? 'live' : 'degraded'))
      .catch(() => setHealth('waking'));
  }, []);

  const pill = health === 'live'
    ? <span className="mn-pill mn-pill--ok">● Live · db up</span>
    : health === 'checking'
      ? <span className="mn-pill mn-pill--warn">checking…</span>
      : <span className="mn-pill mn-pill--warn">{health}</span>;

  return (
    <div>
      <div className="flex items-center justify-between mb-1">
        <h1 className="text-2xl font-bold">System architecture</h1>
        {pill}
      </div>
      <p className="mn-kicker mb-6">MediNexus · full stack · $0/month across five providers</p>

      <div className="flex gap-6 items-start">
        {/* ================= Left: the diagram + detail cards ================= */}
        <div className="flex-1 min-w-0">
          <div className="mn-card p-6 mb-6">
            <p className="mn-kicker mb-5">Live system map</p>

            {/* Column headers */}
            <div className="flex gap-2 mb-3">
              {['CLIENT', 'DELIVERY', 'API', 'DATA'].map((c) => (
                <div key={c} className="flex-1 mn-kicker" style={{ minWidth: 130 }}>{c}</div>
              ))}
            </div>

            {/* Row 1: the request path */}
            <div className="flex gap-2 items-stretch mb-2">
              <Node title="React UI" sub="role-gated · files · slots · error capture" accent />
              <Flow />
              <Node title="Netlify CDN" sub="static build · env baked at build time" />
              <Flow />
              <Node title="ASP.NET Core 9" sub="Render · Docker · JWT · FluentValidation" accent />
              <Flow />
              <Node title="Aiven MySQL 8" sub="27 tables · constraints · stored UTC" accent />
            </div>

            {/* Branch: cache beside the database */}
            <div className="flex gap-2">
              <div style={{ flex: 3 }} />
              <div style={{ flex: 1.2, minWidth: 150 }}>
                <div className="mn-flow-v" style={{ minHeight: 22 }} />
                <Node title="Valkey" sub="cache-aside · 30-60s TTL · write invalidation" />
              </div>
              <div style={{ flex: 1 }} />
            </div>

            {/* Row 2: observability */}
            <p className="mn-kicker mt-6 mb-3">Observability — every event, one pipeline</p>
            <div className="flex gap-2 items-stretch">
              <Node title="ILogger + /api/client-logs" sub="human-tone events · browser errors" />
              <Flow />
              <Node title="LogBase Shipper" sub="batch ≤100/2s · backoff · wake-then-send" accent />
              <Flow />
              <Node title="LogBase Ingestion" sub="Fly.io LHR · validate · enrich" ext />
              <Flow />
              <Node title="Explorer" sub="Astra Cassandra + Neon PG · my own platform" ext />
            </div>

            {/* Row 3: automation (scheduled / passive) */}
            <p className="mn-kicker mt-6 mb-3">Automation — scheduled, passive</p>
            <div className="flex gap-2 items-stretch">
              <Node title="GitHub Actions" sub="keep-alive */10min · CI on every push" />
              <Flow passive />
              <Node title="CI gate" sub="real MySQL container · golden-arc test" />
              <Flow passive />
              <Node title="Render + Netlify" sub="deploy from the same commit" />
            </div>

            {/* Legend */}
            <div className="flex gap-6 mt-6 items-center text-xs" style={{ color: 'var(--mn-ink-soft)' }}>
              <span className="flex items-center gap-2">
                <span style={{ width: 34, display: 'inline-block' }} className="mn-flow" /> live data flow
              </span>
              <span className="flex items-center gap-2">
                <span style={{ width: 34, display: 'inline-block' }} className="mn-flow mn-flow-passive" /> scheduled / passive
              </span>
              <span>Animated dots show the direction data moves.</span>
            </div>
          </div>

          {/* ---- Detail cards ---- */}
          <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
            <div className="mn-card mn-accent p-5">
              <p className="mn-kicker mb-3"><Server className="w-3 h-3 inline mr-1" />Request lifecycle</p>
              <Chips items={[
                { label: 'Exception shield' }, { label: 'Forwarded headers' }, { label: 'CORS' },
                { label: 'Rate limiter' }, { label: 'JWT authN' }, { label: 'Role + row authZ' },
                { label: 'Validation (422)' }, { label: 'Controller' }, { label: 'Dapper SQL' },
              ]} />
              <p className="text-xs mt-3" style={{ color: 'var(--mn-ink-soft)' }}>
                Errors are a system: one middleware maps MySQL error codes to friendly
                409/422s and logs the truth; controllers never catch. All instants travel
                as UTC ISO-8601; business dates are IST, computed in code (D6).
              </p>
            </div>

            <div className="mn-card mn-accent p-5">
              <p className="mn-kicker mb-3"><GitBranch className="w-3 h-3 inline mr-1" />The Patient File — visit state machine</p>
              <Chips items={[
                { label: 'Requested' }, { label: 'Scheduled' }, { label: 'CheckedIn' },
                { label: 'InConsultation' }, { label: 'Completed' },
              ]} />
              <div className="mt-2">
                <Chips items={[
                  { label: 'SentToPharmacy', tone: 'dim' }, { label: 'Confirmed', tone: 'dim' },
                  { label: 'Ready', tone: 'dim' }, { label: 'Dispensed', tone: 'dim' },
                ]} />
              </div>
              <div className="mt-2">
                <Chips items={[{ label: 'Cancelled', tone: 'warn' }, { label: 'No-Show', tone: 'warn' }, { label: 'Rejected + reason', tone: 'warn' }]} />
              </div>
              <p className="text-xs mt-3" style={{ color: 'var(--mn-ink-soft)' }}>
                One File per visit — a projection assembled live, never a table (D7).
                Slots are schedule-driven and never offered twice (D13); dispensing is
                all-or-nothing with atomic stock decrements (D9); both bill types carry
                insurance claims, card adds a server-computed 2.5% (D10).
              </p>
            </div>

            <div className="mn-card mn-accent p-5">
              <p className="mn-kicker mb-3"><Radio className="w-3 h-3 inline mr-1" />A log event's journey</p>
              <Chips items={[
                { label: 'ILogger call' }, { label: 'Channel 10k · drop-oldest' },
                { label: 'batch ≤100 / 2s' }, { label: 'wake if cold' },
                { label: 'POST /ingest' }, { label: '202' },
              ]} />
              <p className="text-xs mt-3" style={{ color: 'var(--mn-ink-soft)' }}>
                At-most-once by design: retries with jitter on 429/5xx, instant drop on
                other 4xx, and every lost event is counted then confessed as a synthetic
                WARNING. Browser errors join the same channel via an anonymous,
                rate-limited proxy — the key never leaves the server. Logging can never
                block, slow, or fail a hospital request.
              </p>
            </div>

            <div className="mn-card mn-accent p-5">
              <p className="mn-kicker mb-3"><Database className="w-3 h-3 inline mr-1" />Data layer</p>
              <p className="text-xs" style={{ color: 'var(--mn-ink-soft)' }}>
                27 relational tables with real integrity: unique (doctor, slot) and
                (room, slot) indexes make double-booking impossible; CHECK constraints
                guard statuses; migrations 01→08 are one-shot by construction via a
                SCHEMA_MIGRATION marker table. Copay model: insurance pays
                (100 − copay)% through auto-filed claims; the remainder settles cash or
                card. Soft deletion keeps history — accounts deactivate, never vanish.
              </p>
            </div>
          </div>
        </div>

        {/* ================= Right: service cards ================= */}
        <div className="w-80 flex-shrink-0 space-y-4">
          <ServiceCard name="Frontend" pill="Netlify"
            url="medinexushealth.netlify.app"
            meta="React 18 · Vite · global CDN · free tier" />
          <ServiceCard name="API" pill="Render"
            url="medinexus-api-zw2m.onrender.com"
            meta="ASP.NET Core 9 · Docker · North America · free (kept warm by cron)" />
          <ServiceCard name="Database" pill="Aiven"
            meta="MySQL 8 · North America · Free-1-1gb · TLS required" />
          <ServiceCard name="Cache" pill="Valkey" pillTone="warn"
            meta="Aiven free tier · optional — app degrades gracefully without it" />
          <ServiceCard name="Observability" pill="LogBase" pillTone="ok"
            url="log-analytics-ingestion.fly.dev"
            meta="My own platform: FastAPI · Cassandra (Astra) · PostgreSQL (Neon) · Fly.io LHR" />
          <ServiceCard name="CI / automation" pill="GitHub"
            meta="Actions: integration tests vs real MySQL on every push · keep-alive every 10 min · one cron warms three clouds" />

          <div className="mn-card p-4">
            <p className="mn-kicker mb-2"><Boxes className="w-3 h-3 inline mr-1" />Stack</p>
            <div className="text-xs space-y-1" style={{ color: 'var(--mn-ink-soft)' }}>
              <div><strong style={{ color: 'var(--mn-ink)' }}>Frontend</strong> — React 18 · Tailwind + custom tokens</div>
              <div><strong style={{ color: 'var(--mn-ink)' }}>API</strong> — .NET 9 · Dapper · FluentValidation · JWT</div>
              <div><strong style={{ color: 'var(--mn-ink)' }}>Data</strong> — MySQL 8 · Valkey</div>
              <div><strong style={{ color: 'var(--mn-ink)' }}>Logs</strong> — custom ILoggerProvider → LogBase v1</div>
              <div><strong style={{ color: 'var(--mn-ink)' }}>Tests</strong> — xUnit · WebApplicationFactory · MySQL container</div>
            </div>
          </div>

          <div className="mn-card p-4" style={{ background: 'var(--mn-paper-deep)' }}>
            <p className="mn-kicker mb-2"><Globe className="w-3 h-3 inline mr-1" />Why it feels fast</p>
            <p className="text-xs" style={{ color: 'var(--mn-ink-soft)' }}>
              API and database share a region (every query is single-digit ms); the
              frontend ships from a CDN edge; hot reads come from cache; and one
              scheduled ping keeps every free tier from sleeping. Cold starts are
              survived, not suffered: reads auto-retry behind a banner, and the log
              shipper knocks before it delivers.
            </p>
          </div>
        </div>
      </div>
    </div>
  );
}

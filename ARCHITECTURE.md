# MediNexus v2 — High-Level Design

Target architecture for the free-tier rebuild. Supersedes the original
Oracle-VM design and the earlier idea of using a third-party log service:
observability is handled by **LogBase**, our own log analytics platform
(github.com/Chinmay-Sakhare07/Log_Analytics_System).

---

## 1. System overview

```
                    ┌─────────────────────────────────────┐
                    │        GitHub — single repo          │
                    │  Actions: CI gate · keep-alive cron  │
                    │        · nightly ops crons           │
                    └────────┬───────────────┬─────────────┘
              auto-deploys   │               │ every 10 min: GET /health
          ┌──────────────────┘               └──────────────────┐
          ▼                                                     ▼
┌───────────────────┐      HTTPS + JWT       ┌──────────────────────────────┐
│  Netlify           │ ─────────────────────▶ │  Render — .NET 9 API (Docker)│
│  React frontend    │                        │                              │
│  medinexushealth   │                        │  Controllers                 │
└───────────────────┘                        │   └ FluentValidation (P3)    │
                                             │  JWT auth + role policies(P2)│
                                             │  Repositories (Dapper)       │
                                             │  IDistributedCache→Valkey(P5)│
                                             │  Serilog ──▶ LogBase sink ───┼───┐
                                             └──────────────┬───────────────┘   │
                                                     TLS    │                   │ batched POST /ingest
                                                            ▼                   │ (JSON array + API key)
                                             ┌──────────────────────────┐       │
                                             │  Aiven MySQL — free tier │       │
                                             │  `medinexus` database    │       │
                                             └──────────────────────────┘       │
                                                                                 ▼
                              ══════════════ LogBase (separate product) ══════════════
                              Fly.io ingestion API ──▶ Astra DB (raw, by service+date)
                                                   └─▶ Neon PG (hourly aggregates)
                              Fly.io query API ──▶ React dashboard on Vercel
                                                   (loganalyticssystem.vercel.app)
```

Boundary rule: MediNexus knows **only** LogBase's public `/ingest` contract.
It has no knowledge of Astra, Neon, or LogBase internals. If LogBase's storage
layer changes tomorrow, MediNexus doesn't notice. This is the same discipline
a Datadog customer gets, applied to our own product.

## 2. Observability design — MediNexus × LogBase

### 2.1 Roles

MediNexus is a **log producer**. LogBase is the **observability backend**:
ingestion, storage, retention, search, and dashboards all live on the LogBase
side. Log *management* concerns (how long events are kept, how they age out)
are LogBase's job — its Cassandra TWCS + per-day partitioning already handle
aging — so MediNexus ships events and forgets them.

### 2.2 The shipper: in-process Serilog sink, not a file tailer

LogBase's existing agent (`shipper.py`) tails files on disk. MediNexus adapts
the *semantics* but not the mechanism, for one hard platform reason: Render's
free tier has an **ephemeral filesystem** — log files written to disk vanish
on every deploy/restart, and there is no persistent disk on the free plan. So
instead of file → tailer → HTTP, we hook the log pipeline directly:

```
ILogger calls ──▶ Serilog ──▶ LogBaseSink (in-process)
                                │  bounded queue (drop-oldest past ~10k)
                                │  batches ≤100 events or 2 s, whichever first
                                │  exponential backoff: 1s → 2s → 4s → 8s → give up
                                └─▶ POST {LogBase}/ingest   (API key header)
```

Delivery semantics, stated honestly: **at-most-once after bounded retries** —
deliberately weaker than shipper.py's disk-buffered at-least-once. Losing a
few log lines during a LogBase outage is acceptable; blocking or failing a
hospital request because a *logging* backend is asleep is not. The sink never
throws into request threads and degrades silently (console-only self-log).
This matters in practice because Fly.io free machines are known to sleep —
the sink's backoff window is sized to ride out a Fly cold start (~10–20 s).

### 2.3 Event taxonomy — what MediNexus ships

| Event class | Severity | Example message | Metadata keys |
|---|---|---|---|
| HTTP request completed | INFO (4xx→WARN, 5xx→ERROR) | `HTTP POST /api/appointments responded 201 in 84ms` | method, path, status, elapsed_ms, user* |
| Domain events | INFO | `AppointmentScheduled id=214 patient=87 doctor=3` | entity, entity_id, actor* |
| Auth events (Phase 2) | WARN on failure | `LOGIN_FAILED username=dr.sharma` | username, reason, ip |
| Unhandled exceptions | ERROR | `Unhandled exception on POST /api/billing/pay` | exception_type, stack (truncated), path |
| Heartbeats | DEBUG | `Health check ok` | db |

*user/actor populated once Phase 2 auth lands.

Heartbeats are shipped at DEBUG so the Explorer's default INFO view stays
clean, while LogBase's per-service volume charts still show a living pulse.

### 2.4 Contract mapping (MediNexus → LogBase `/ingest`)

| LogBase field | MediNexus value |
|---|---|
| `timestamp` | UTC ISO-8601 from the log event |
| `service` | `medinexus-api` (env-overridable) |
| `severity` | Trace/Debug→`DEBUG` · Information→`INFO` · Warning→`WARN` · Error/Critical→`ERROR` |
| `message` | rendered Serilog message |
| `host` | Render instance id (falls back to machine name) |
| `metadata` | Serilog properties, **every value stringified** — LogBase's Cassandra column is `MAP<TEXT,TEXT>` |

Payload shape: JSON **array** (batch), matching the documented `/ingest`
contract. Auth: API key header — name is configurable
(`LogBase__ApiKeyHeader`, default `X-API-Key`); confirm the exact header in
`ingestion_api/app.py` and set the env var accordingly.

### 2.5 Configuration (all Render env vars, nothing in code)

| Variable | Example |
|---|---|
| `LogBase__IngestUrl` | `https://log-analytics-ingestion.fly.dev/ingest` |
| `LogBase__ApiKey` | *(secret)* |
| `LogBase__ApiKeyHeader` | `X-API-Key` |
| `LogBase__ServiceName` | `medinexus-api` |
| `LogBase__MinimumLevel` | `Debug` (ship heartbeats) or `Information` |

Unset `LogBase__IngestUrl` ⇒ sink disabled, console logging only. Local dev
can point at `http://localhost:8000/ingest` against `make up`.

### 2.6 The keep-warm chain (one cron, four platforms)

The existing 10-minute GitHub Action ping does more work than it looks like:

```
GitHub cron ─▶ Render /health        (prevents free-instance sleep)
                  └─ SELECT 1        (counts as Aiven activity)
                  └─ log event ─▶ Fly ingestion  (keeps Fly machine warm)
                                      └─ write to Astra  (activity on Astra's
                                         free tier, which hibernates idle DBs)
```

### 2.7 Using the LogBase dashboard for MediNexus

Day-to-day flow: open the Explorer, filter `service = medinexus-api`. Error
trends surface 5xx spikes; hourly volume shows demo traffic; keyword search
(`appointment`, a patient id, `LOGIN_FAILED`) answers "what happened?"
questions. During grading/demos this doubles as live proof that both systems
are real and talking to each other.

### 2.8 Future hooks (LogBase roadmap alignment)

The LogBase README plans cloud adapters and multi-tenant RBAC. This
integration feeds both: the Serilog sink is effectively a ".NET adapter"
reference implementation, and the JWT + role-policy code built for MediNexus
in Phase 2 is the same pattern LogBase needs for org-scoped access — build
once, port across.

## 3. Data & platform decisions (locked)

| Concern | Decision | Why |
|---|---|---|
| Database | **MySQL on Aiven free** | Zero porting (already on MySQL), always-on 1 CPU/1 GB, no card |
| API hosting | **Render free (Docker)** | Dockerfile already existed; HTTPS out of the box kills the Worker/DuckDNS hack |
| Frontend | **Netlify** (unchanged) | Already auto-deploying |
| Logs | **LogBase** (own product) | See §2 |
| Cache (P5) | **Aiven Valkey free** | Redis-compatible; dashboard + doctors-list caching |
| Events (P7, opt.) | **Aiven Kafka free** | 5 topics / 250 kb/s / 3-day retention — enough for an `appointment-events` stream |
| IaC (P7, opt.) | **Terraform OSS** | Aiven/Neon/GitHub/Netlify providers; zero runtime cost |
| K8s | **Local kind/k3d only** | No genuinely free managed K8s off Oracle; manifests kept as a learning artifact |

## 4. Phase plan (rev. 2 — Full clinical build, see SCOPE.md)

| Phase | Scope | Status |
|---|---|---|
| 1 | Aiven MySQL + Render + Netlify env, keep-alive cron, kill Worker/DuckDNS/VM | **Built** — see `DEPLOYMENT.md` |
| 2 | Auth: BCrypt login, JWT, role policies + row-level access, rate limiting, React login/guards/role-aware UI | Next |
| 3 | Quality foundation: FluentValidation, exception middleware, double-booking constraint, IST dates, tests in CI | Queued |
| 4 | Clinical & Pharmacy modules: Medical Records, Prescriptions, Lab Tests, Dispensing — all 7 roles live | Queued |
| 5 | LogBase sink (§2) + Valkey caching | Queued |
| 6 | CI gate + scheduled ops: reseed, no-show, reminders-as-events, backups | Queued |
| 7 (opt.) | Kafka `appointment-events` + consumer, Terraform, local K8s manifests | Backlog |

Ordering note: the LogBase sink lands in Phase 5 but auth/domain events
(§2.3) are designed now, so Phases 2–4 emit them from day one — console-only
until the sink is wired.

## 5. Time handling convention (D6)

Store UTC, display local. Concretely:

**Instants** — anything that answers "when did/will this happen":
`APPOINTMENT.DateTime`, `MEDICINE_DISPENSE.DispensedAt`, log timestamps.
Stored as UTC `DATETIME`; the app supplies `DateTime.UtcNow` (never relies on
server-side `NOW()` semantics). The API emits ISO-8601 with `Z`; the browser
formats to the viewer's local zone (IST for our users). India has no DST, so
future appointments stored as UTC never shift meaning.

**Business dates** — calendar dates with hospital-day semantics:
`BillDate`, `DueDate`, `DateIssued`, `VisitDate`, insurance `ValidFrom/To`
checks. Computed by the app as the **IST calendar date**
(`TimeZoneInfo "Asia/Kolkata"`) and passed as parameters; every `CURDATE()`
is removed from repository SQL (a UTC-server `CURDATE()` flips to the wrong
day at 18:30 UTC / midnight IST).

**Timezone-agnostic dates** — `DOB`, `HireDate`, medicine `ExpiryDate`: plain
calendar facts, never converted.

**Day-window queries** — "today's/tomorrow's appointments", dashboard counts,
no-show and reminder crons: the backend computes the IST day, converts its
bounds to UTC, and queries
`WHERE `DateTime` >= @startUtc AND `DateTime` < @endUtc`.
Never `WHERE DATE(col) = ...` — wrapping the column defeats
`IX_Appointment_DateTime` and forces a scan.

**Platform notes** — Aiven's server runs UTC (we treat that as convention,
not dependency); all zone math happens in .NET, so MySQL's named-timezone
tables are never required. The aspnet Docker image ships tzdata, so
`Asia/Kolkata` resolves on Render. Seed appointment times are shifted once so
stored values are true UTC and demo data displays at sane hours.

Alignment: LogBase ingestion already expects UTC ISO-8601 (§2.4), and the
Phase 6 crons inherit correctness for free — "next day" is an IST window
converted to a UTC range, same as everything else.

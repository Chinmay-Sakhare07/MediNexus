# MediNexus

A full-stack, role-based **hospital management system** that runs a complete
clinical and administrative workflow — from a patient booking an appointment,
through the doctor's consultation, lab tests and prescription, to the
pharmacy dispensing medicine and the front desk settling the bill.

Built as a production-grade system on **entirely free cloud infrastructure**
(≈ $0/month), and wired to ship its own telemetry to **LogBase**, a separate
log-analytics platform.

```
React 18 (Netlify)  ──►  ASP.NET Core 9 API (Render, Docker)  ──►  MySQL 8 (Aiven)
                                    │
                                    ├──►  Valkey cache (Aiven)         — optional
                                    └──►  LogBase log analytics (Fly.io) — optional
```

---

## Table of contents

- [What it does](#what-it-does)
- [The seven roles](#the-seven-roles)
- [Architecture at a glance](#architecture-at-a-glance)
- [The Patient File — the heart of the system](#the-patient-file--the-heart-of-the-system)
- [Tech stack](#tech-stack)
- [Repository layout](#repository-layout)
- [Running it locally](#running-it-locally)
- [Deploying](#deploying)
- [Configuration reference](#configuration-reference)
- [Testing & CI](#testing--ci)
- [Deep-dive documentation](#deep-dive-documentation)

---

## What it does

MediNexus models a multi-speciality hospital. A visit flows through every
department as a single **"Patient File"**, and each role stamps its part:

```
Patient books ─► Reception approves ─► Reception checks in ─► Nurse takes vitals
   ─► Doctor consults (diagnosis, lab orders, prescription) ─► Consultation bill
   ─► Lab enters results ─► Pharmacy confirms → prepares → dispenses ─► Pharmacy bill
   ─► Both bills paid (cash / card / insurance) ─► File auto-closes
```

Alongside the clinical flow it handles patient records, doctor directories and
schedules, insurance policies and automatic claims, billing with insurance
copay and card surcharges, inventory management, user administration, and a
live self-describing architecture page.

---

## The seven roles

| Role | Can do |
|------|--------|
| **Admin** | Everything, plus user management and "view as" account switching |
| **Receptionist** | Register/edit patients, assign insurance, approve & schedule appointments, check patients in, take payments |
| **Doctor** | Own schedule & leave, consult own checked-in patients, order labs, prescribe (with allergy visibility), complete visits |
| **Nurse** | Record vitals on checked-in visits |
| **Lab Technician** | Personal work queue: start tests, enter results |
| **Pharmacist** | Prescription queue (confirm/reject/ready/dispense), inventory adjustment |
| **Patient** | Book appointments, view own file, results, bills and insurance (read-only) |

Access is enforced twice: by role on every endpoint, **and at the SQL row
level** — a patient's queries are filtered to their own data, a doctor's to
their own patients.

---

## Architecture at a glance

```
                          ┌─────────────────────────────┐
        Browser  ────────►│  React 18 + Vite (Netlify)  │  global CDN
                          │  role-gated UI · error capture│
                          └───────────────┬─────────────┘
                                          │ HTTPS  (VITE_API_BASE_URL)
                                          ▼
                          ┌─────────────────────────────┐
                          │  ASP.NET Core 9  (Render)    │  Docker container
                          │  JWT · validation · Dapper   │
                          │  global exception middleware │
                          └───┬───────────┬───────────┬──┘
                              │           │           │
                 ┌────────────▼──┐  ┌─────▼──────┐  ┌─▼───────────────┐
                 │ MySQL 8       │  │ Valkey     │  │ LogBase shipper │
                 │ (Aiven)       │  │ cache-aside│  │ → Fly.io ingest │
                 │ 28 tables     │  │ (optional) │  │ (optional)      │
                 └───────────────┘  └────────────┘  └─────────────────┘

        ┌───────────────────────────────────────────────────────────┐
        │ GitHub Actions:  CI (tests vs real MySQL)  ·  keep-alive    │
        │ one cron every 10 min keeps Render + Aiven (+ Fly) warm     │
        └───────────────────────────────────────────────────────────┘
```

Every free tier sleeps when idle; a single scheduled `/health` ping (which
runs a real `SELECT 1`) keeps the API and database awake, and the frontend
transparently retries reads during cold starts.

---

## The Patient File — the heart of the system

One visit is **one File**: not a database table, but a live projection
assembled from the appointment, medical record, lab tests, prescription and
bills. Every role sees the same story with role-appropriate actions.

The appointment moves through a state machine:

```
Requested ──► Scheduled ──► CheckedIn ──► InConsultation ──► Completed
    │             │                                              │
 (patient      (reception    (nurse + doctor work here)       (bills)
  booked)       approved)
    └─► Cancelled / No-Show                    Prescription: SentToPharmacy
                                               → Confirmed → Ready → Dispensed
                                               (or → Rejected, back to doctor)
```

Design highlights: booked slots are **never offered twice**; dispensing is
**all-or-nothing** with atomic stock decrements; both bills accept insurance,
cash, or card (card adds a server-computed 2.5% surcharge); doctor leave
**transactionally cancels** that day's appointments.

See **DATABASE.md** and **BACKEND.md** for the full walkthrough.

---

## Tech stack

| Layer | Technology |
|-------|------------|
| Frontend | React 18, Vite, Tailwind CSS + custom design tokens ("clinical modernism") |
| Backend | ASP.NET Core 9 (.NET 9), Dapper, FluentValidation, JWT (HS256), BCrypt |
| Database | MySQL 8 (Aiven managed, TLS) |
| Cache | Valkey / Redis-compatible (Aiven, optional) |
| Logging | Custom `ILoggerProvider` → LogBase v1 ingest (optional) |
| Tests | xUnit + `WebApplicationFactory` against a real MySQL service container |
| Hosting | Netlify (frontend), Render (API, Docker), Aiven (DB), Fly.io (LogBase) |
| CI/CD | GitHub Actions |

---

## Repository layout

```
MediNexus/
├── Backend/                     ASP.NET Core 9 API
│   ├── Controllers/             12 controllers (thin; no business logic)
│   ├── Repositories/            11 repositories (Dapper, raw SQL)
│   ├── Models/                  DTOs & request records
│   ├── Auth/                    roles, JWT claims, defaults
│   ├── Validation/              FluentValidation validators + filter
│   ├── Middleware/              global exception handler
│   ├── Logging/                 LogBase shipper (custom ILoggerProvider)
│   ├── Caching/                 Valkey cache-aside service
│   ├── Time/                    IST/UTC clock (the D6 convention)
│   └── Program.cs               composition root
├── Backend.Tests/               xUnit integration tests
├── src/                         React frontend
│   ├── pages/                   13 pages (Dashboard, File, Pharmacy, …)
│   ├── components/Layout/       sidebar, layout, banners
│   ├── context/                 auth context (+ impersonation)
│   ├── services/                axios client, error reporter
│   ├── auth/                    permissions map (mirrors backend matrix)
│   ├── utils/                   datetime (IST), pending-step logic
│   └── styles/                  design tokens
├── Database/MySQL/              9 ordered SQL scripts (schema → migrations)
└── .github/workflows/           CI + keep-alive
```

---

## Running it locally

**Prerequisites:** .NET 9 SDK, Node 20+, a MySQL 8 instance.

```bash
# 1. Database — run the scripts in order (01 → 09)
for f in Database/MySQL/*.sql; do mysql -u root -p < "$f"; done

# 2. Backend
cd Backend
export ConnectionStrings__HospitalDb="Server=localhost;Port=3306;Database=medinexus;User ID=root;Password=...;SslMode=None;"
dotnet run          # https://localhost:5155 (Swagger at /swagger in Development)

# 3. Frontend (new terminal)
npm install
echo "VITE_API_BASE_URL=http://localhost:5155/api" > .env.local
npm run dev         # http://localhost:5173
```

Sign in with any seeded account — password `MediNexus@2026`. The login page
has a demo-account picker (admin excluded on purpose).

---

## Deploying

The project deploys as three independent pieces from one Git push:

1. **Database (Aiven MySQL):** run scripts `01`–`09` once via MySQL Workbench.
2. **API (Render):** Docker web service; set the env vars below; health check
   path `/health`.
3. **Frontend (Netlify):** set `VITE_API_BASE_URL` to the Render URL **plus
   `/api`**, then trigger a deploy (Vite bakes env vars at build time).

Migrations are **one-shot by construction** — each script records itself in a
`SCHEMA_MIGRATION` table and aborts if re-run, so re-running the folder is
safe.

---

## Configuration reference

**Backend (Render):**

| Variable | Required | Purpose |
|----------|----------|---------|
| `ConnectionStrings__HospitalDb` | ✅ | MySQL connection string |
| `Jwt__Secret` | ✅ | 32+ char signing key (API refuses to start without it in prod) |
| `Cors__AllowedOrigins` | — | extra CORS origins (comma-separated) |
| `LOGBASE_ENABLED` / `LOGBASE_URL` / `LOGBASE_API_KEY` | — | enable log shipping |
| `Cache__RedisUrl` | — | Valkey URI; absent = no caching, app unaffected |

**Frontend (Netlify):** `VITE_API_BASE_URL` = `https://<api-host>/api`

**GitHub (repository variables):** `KEEPALIVE_URL` = `https://<api-host>/health`
(and optionally `LOGBASE_KEEPALIVE_URL`).

---

## Testing & CI

`Backend.Tests/` boots the real API in-process against a **real MySQL
container** (not an in-memory fake, because the SQL dialect and constraints
are part of what's being tested). It covers authentication, role denials,
row-level isolation, validation, slot exclusivity, the full "golden arc"
visit, the copay calculation, and the user lifecycle.

GitHub Actions runs it on every push: spin up MySQL 8 → load scripts 01–09 →
`dotnet test` → build the frontend.

```bash
dotnet test Backend.Tests/HospitalManagement.API.Tests.csproj
```

---

## Deep-dive documentation

| Document | What's inside |
|----------|---------------|
| **BACKEND.md** | How the API works in plain language, with every design decision explained |
| **FRONTEND.md** | How the React app is structured, page by page |
| **DATABASE.md** | The schema, the 9 scripts, and how data flows through a visit |
| **ISSUES_FACED.md** | The real bugs and obstacles hit during the build, and how each was solved |

---

*MediNexus is a portfolio project built on synthetic data. It is not a
medical device and stores no real patient information.*

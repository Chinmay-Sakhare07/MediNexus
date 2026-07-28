# MediNexus — Backend, Explained Simply

This document explains how the backend works in plain language, and — in the
boxed **Design decision** callouts — *why* each significant choice was made.
No prior knowledge of the codebase is assumed.

---

## 1. The big picture

The backend is an **ASP.NET Core 9** web API. Its whole job is to answer HTTP
requests from the React app: "log this person in", "book this appointment",
"dispense this prescription". It talks to a MySQL database, and optionally to
a cache and a logging platform.

It's organised in three simple layers:

```
   HTTP request
        │
        ▼
   ┌─────────────┐   Controllers — thin. They check permissions, then
   │ Controller  │   call a repository. No business logic lives here.
   └──────┬──────┘
          ▼
   ┌─────────────┐   Repositories — the real work. Each one owns the SQL
   │ Repository  │   for one area (patients, billing, pharmacy, …).
   └──────┬──────┘
          ▼
   ┌─────────────┐   MySQL — the source of truth.
   │  Database   │
   └─────────────┘
```

There are **12 controllers** and **11 repositories**. A controller never
writes SQL; a repository never checks a JWT. That separation keeps each file
small and easy to reason about.

> **Design decision — Dapper, not a big ORM (like Entity Framework).**
> Dapper is a thin layer: you write the SQL yourself and it maps the result
> to objects. We chose it because every query is visible and reviewable — you
> can see exactly what hits the database, which indexes are used, and where
> transactions begin and end. The cost (writing SQL by hand) is small at this
> size, and it made the timezone and double-booking work below far easier to
> get right.

---

## 2. What happens on every request

Before a request reaches its controller, it passes through a pipeline. Order
matters:

```
Request
  │
  ├─ 1. Exception shield   catches ANY error, returns a clean response
  ├─ 2. Forwarded headers  learn the real client IP (behind Render's proxy)
  ├─ 3. CORS               is this website allowed to call us?
  ├─ 4. Rate limiter       (login only) max 5 tries/minute/IP
  ├─ 5. Authentication     is the JWT valid? who is this?
  ├─ 6. Authorization      is this role allowed on this endpoint?
  │
  ▼
Controller  →  validation  →  repository  →  database
```

> **Design decision — errors are a system, not a habit.**
> One piece of middleware (the "exception shield") sits at the very top and
> catches everything. Controllers therefore **never** wrap their code in
> try/catch. The shield decides the HTTP response *and* logs the real cause,
> so behaviour is consistent everywhere and we never accidentally leak a
> stack trace to the user. Database errors are translated into friendly
> messages: a duplicate booking becomes **409 "That time slot is already
> booked"**, a missing reference becomes **422**, and anything truly
> unexpected becomes a generic **500 "Something went wrong"** — with the full
> detail going only to the logs.

---

## 3. Logging in — authentication & authorization

When someone logs in, the API checks their password (stored as a **BCrypt**
hash, never plain text) and hands back a **JWT** — a signed token that says
who they are and what role they hold.

```
POST /api/auth/login  {login, password}
        │
        ▼  BCrypt verify against USER_ACCOUNT
        ▼
   JWT token  { role: "Doctor", staffId: 1, patientId: null, ... }
        │
        ▼  the browser sends this token on every future request
```

Every later request carries that token. The API reads the role from it and
checks a **7-role × 10-module matrix**: can a Nurse open Billing? Can a
Patient list other patients? Each endpoint declares which roles it accepts.

> **Design decision — security enforced at the row level, not just the door.**
> Checking the role at the endpoint isn't enough. A logged-in patient could
> otherwise ask for *another* patient's data. So the token also carries the
> person's own `patientId` / `staffId`, and the repositories use it to filter
> the SQL: a patient's queries only ever return their own rows; a doctor sees
> their own patients. Even if someone bypassed the UI, the database layer
> still refuses to hand over data that isn't theirs.

> **Design decision — the login endpoint is rate-limited.**
> Five attempts per minute per IP address. This blunts password-guessing
> without inconveniencing real users. Because Render sits behind a proxy, the
> API is configured to read the *real* client IP from forwarded headers, so
> the limit applies per actual visitor rather than to the proxy.

> **Design decision — the API refuses to start with a weak secret in production.**
> The JWT signing key comes from an environment variable. In production, if
> it's missing, the app fails to boot rather than silently falling back to a
> guessable default. Failing loudly beats running insecurely.

---

## 4. The time problem (and how it's solved)

The servers run on UTC time. The hospital runs on India time (IST). Naïvely
mixing these caused a real bug: "today's appointments" would show the wrong
day after midnight IST.

> **Design decision — one time convention everywhere (called "D6").**
> The rule: **store and transport every moment as UTC**, but **treat business
> dates as IST calendar dates**, computed in one place (`Time/IstClock.cs`).
> "Today's appointments" becomes a UTC time-range query
> (`>= start AND < end`), which is both correct across midnight *and* fast
> (it uses the database index, unlike wrapping the column in a date function).
> The API always sends timestamps ending in `Z` (UTC), and the frontend
> renders them in India time. Result: appointment times look identical no
> matter where the viewer's browser is.

---

## 5. Validation

Before a controller trusts incoming data, it's validated by **FluentValidation**
rules that run automatically. A bad request comes back as **422** with a
per-field explanation:

```json
{ "success": false, "message": "Validation failed",
  "errors": { "BloodType": ["Blood type must be one of A+, A-, ..."] } }
```

> **Design decision — validation is a pipeline stage, not scattered `if`s.**
> Every request type has a validator, and a single filter runs the right one
> automatically before the controller code executes. The frontend gets a
> uniform, predictable error shape it can display next to the offending
> field. The database's own CHECK and foreign-key constraints remain the
> final safety net.

---

## 6. The Patient File workflow (the core feature)

A visit passes through many hands. Rather than a giant "encounter" table, the
File is **assembled on demand** from the pieces already stored.

> **Design decision — the File is a projection, not a table (D7).**
> One endpoint (`GET /api/files/{id}`) gathers the appointment, medical
> record, lab tests, prescription and bills into a single response. Nothing
> new is stored to represent "the File" — it's a view over existing data.
> This kept the schema change for the whole workflow down to a few status
> columns.

The appointment's `Status` column walks through the state machine, and each
transition is a guarded update (it only succeeds from the expected previous
state):

```
Requested → Scheduled → CheckedIn → InConsultation → Completed
```

> **Design decision — booked slots are never offered (D13).**
> Doctors have a fixed weekly schedule (working days, hours, slot length).
> When the UI asks for available slots, the API generates them from that
> schedule and then **subtracts** anything already booked, any leave day, and
> anything in the past. A taken slot simply never appears — and a unique
> database index on (doctor, time) is the final backstop that makes
> double-booking physically impossible.

> **Design decision — doctor leave cancels appointments transactionally.**
> When a doctor files leave for a date, that day's active appointments are
> cancelled **in the same database transaction** as the leave record, and the
> API reports how many. There's no window where the leave exists but the
> appointments are still standing.

> **Design decision — dispensing is all-or-nothing (D9).**
> A pharmacist can only dispense if *every* line is fillable (enough stock,
> not expired, prescription still valid). The whole dispense — stock
> decrements, the dispense records, the bill — happens in one transaction.
> Stock is decremented with a guarded update (`WHERE stock >= quantity`) so
> two simultaneous dispenses can't drive it negative.

> **Design decision — pharmacists manage inventory too (D9, amended).**
> Beyond dispensing, pharmacists can adjust stock (restock or correct), and
> dispensing automatically decrements it. A guard prevents stock ever going
> below zero.

---

## 7. Billing, insurance and payments

Completing a visit generates a **consultation bill**; dispensing generates a
**pharmacy bill**. Both can involve insurance.

> **Design decision — insurance on both bill types; card carries a surcharge (D10, amended).**
> If the patient has valid primary insurance, a claim is filed automatically
> for the covered portion (the policy's coverage percentage). The remaining
> balance is settled by **cash or card**, and **card payments add a 2.5%
> service charge, computed on the server** (never trusted from the client).
> The insurance math is null-safe and rounded to two decimals.

*(A subtle real-world bug lived here — see ISSUES_FACED.md, "The copay that
wasn't broken": the logic was fine, the demo insurance had simply expired.)*

---

## 8. User administration

Admins manage accounts through a full CRUD interface — with a safety-first
twist.

> **Design decision — soft deletion, never hard deletion.**
> "Deleting" a user sets `IsActive = 0`: they can no longer sign in and they
> vanish from the "view as" switcher, but all their history (appointments,
> bills, dispense records) stays intact. Deleting the row outright would
> orphan or destroy that history. Reactivation simply flips the flag back.

> **Design decision — you cannot lock everyone out.**
> The API refuses to deactivate or demote the **last active admin**, and
> refuses to let you deactivate **your own** account. Small guards, but they
> prevent an unrecoverable state.

> **Design decision — a shared default password, with self-service change.**
> New users start with a known default (`MediNexus@2026`); admins can reset
> any account back to it, and every user can change their own password from
> the sidebar. This is a deliberate convenience for a demo system — the
> guardrail that matters is that the *admin* password is changed away from the
> default.

> **Design decision — admin "view as" (impersonation).**
> An admin can adopt any non-admin account to see exactly what that role
> sees — real token, real permissions, real row-level scoping — with a
> banner and a one-click "return to admin". Every switch is audit-logged, and
> admins cannot impersonate other admins. This makes the whole multi-role
> demo runnable from a single login.

---

## 9. Caching (optional)

Some data is read constantly but changes rarely (the dashboard totals, the
doctor list, the medicine catalog).

> **Design decision — cache-aside with honest expiry, and graceful absence.**
> These few queries are cached in Valkey for short windows (30–60 seconds),
> and the cache is **explicitly cleared** on the writes that would make it
> stale (e.g. a stock adjustment clears the medicine cache). Crucially, if no
> cache is configured — or the cache server is down — every call quietly
> falls through to the database. Caching is an optimisation, never a
> dependency.

---

## 10. Logging (optional)

The API narrates what happens in readable sentences — and can ship those to
LogBase, a separate log-analytics platform.

> **Design decision — human-tone messages with structured data alongside.**
> Log messages read like sentences ("Pharmacy confirmed prescription #12;
> preparing items") while the machine-friendly values (ids, durations) ride
> along as structured fields. One line is pleasant for a human to read *and*
> filterable by a machine.

> **Design decision — logging can never block, slow, or break a request.**
> A log call just drops the event into an in-memory queue (a microsecond) and
> returns. A separate background task batches events and sends them. If the
> log platform is slow or completely down, the hospital API doesn't notice —
> the worst case is that some log lines are dropped, and the shipper later
> reports *how many* it dropped. This is deliberately "at-most-once":
> **losing a log line is acceptable; delaying a patient is not.**

> **Design decision — the shipper wakes a sleeping log server first.**
> The log platform runs on a free tier that sleeps when idle (a ~7-second
> cold start). If the shipper has been quiet for a while, it sends a cheap
> wake-up "knock" before the real batch, so the first events after a lull
> aren't lost to the cold start. All of this happens on the background task,
> never on a request.

Browser errors are handled too: a small React module reports uncaught errors
to a backend endpoint (`/api/client-logs`), which feeds them into the *same*
pipeline — so the browser never talks to the log platform directly and never
sees the API key.

---

## 11. Why it stays fast and cheap

> **Design decision — free-tier limits treated as design inputs.**
> The API and database live in the same region, so each query is a few
> milliseconds. Hot reads come from cache. One scheduled `/health` ping every
> 10 minutes (which runs a real `SELECT 1`) keeps both the API and the
> database from sleeping. And when a cold start does happen, the frontend
> retries automatically behind a friendly "server is waking up" banner. The
> whole system runs on free tiers at roughly $0/month without feeling like it.

---

## 12. Quick reference — the 12 controllers

| Controller | Looks after |
|------------|-------------|
| `AuthController` | login, "who am I", change password, impersonation |
| `PatientsController` | patient records (CRUD) |
| `DoctorsController` | doctor directory, schedules, leave |
| `AppointmentsController` | slots, booking, approve/check-in/start, status |
| `FilesController` | the Patient File + vitals, consultation, labs, prescription |
| `LabTestsController` | lab technician queue and results |
| `PharmacyController` | medicine catalog, inventory, prescription queue, dispense |
| `BillingController` | bill generation and payment |
| `InsuranceController` | policies and patient insurance |
| `DashboardController` | summary statistics |
| `UsersController` | admin user management (soft delete) |
| `ClientLogsController` | receives browser error reports |

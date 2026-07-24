# MediNexus v2 — Scope (rev. 2 — Full clinical build)

Companion to `ARCHITECTURE.md` (design) and `DEPLOYMENT.md` (Phase 1 runbook).
Decision **D1 = Full**: clinical + pharmacy modules are IN, so all seven roles
have real workflows. This revision renumbers the phases accordingly.

Sizes: **S** ≤ half a session · **M** one focused session · **L** multiple sessions.

The demo arc this unlocks:

```
Receptionist schedules ─▶ Doctor completes visit ─▶ Medical record (diagnosis, vitals)
                                                      ├─▶ Lab tests ─▶ LabTech enters results
                                                      └─▶ Prescription ─▶ Pharmacist dispenses
                                                                            (stock decremented)
                                                    ─▶ Billing + insurance claim ─▶ Payment
```

---

## 1. Work items by phase

### Phase 1 — Free hosting cutover — BUILT (pending your deploy)

Aiven MySQL + Render Docker + Netlify env switch + `/health` + env CORS +
keep-alive cron + old-stack teardown. See `DEPLOYMENT.md`.

### Phase 2 — Authentication & authorization (L overall)

| Item | Size |
|---|---|
| `POST /api/auth/login` — BCrypt verify against `USER_ACCOUNT`; JWT with role/staffId/patientId claims | M |
| JWT bearer middleware + role policies; **matrix below defined in code now**, applied to new controllers as they're born in P4 | M |
| Row-level access: patients → own data; doctors → own schedule; lab techs → own queue (P4) | M |
| Login rate limiting (built-in .NET rate limiter) | S |
| Prod credential rotation (`admin` at minimum); JWT secret env-only | S |
| React: login page, auth context, bearer interceptor, route guards, role-aware sidebar | M–L |
| Auth events (LOGIN_SUCCESS/FAILED, TOKEN_REJECTED) — console until P5 | S |
| Change-password endpoint + UI (stretch) | S |

Access matrix v2 (all seven roles):

| Module | Admin | Receptionist | Doctor | Nurse | LabTech | Pharmacist | Patient |
|---|---|---|---|---|---|---|---|
| Dashboard | ✔ | ✔ | ✔ | ✔ | ✔ | ✔ | own summary |
| Patients | ✔ CRUD | ✔ CRUD | view | view | view | view | own record |
| Doctors | ✔ | view | view | view | — | — | view |
| Appointments | ✔ | ✔ CRUD | own + complete | view | view | — | own only |
| Medical Records (P4) | ✔ | — | ✔ create/edit own patients' | view + vitals entry | — | — | own only |
| Lab Tests (P4) | ✔ | — | order | view | **own queue + results entry** | — | own results |
| Prescriptions (P4) | ✔ | — | ✔ create | view | — | view active | own only |
| Pharmacy (P4) | ✔ | — | — | — | — | **✔ dispense + inventory** | — |
| Billing | ✔ | ✔ create/pay | view own patients' | — | — | — | own bills |
| Insurance | ✔ | ✔ assign | view | — | — | — | own policies |

### Phase 3 — Quality foundation (M–L overall)

Built *before* the new modules so P4 code is born on it, not retrofitted.

| Item | Size |
|---|---|
| FluentValidation infra + validators for the existing 6 modules' requests | M |
| Global exception middleware → consistent `ApiResponse`; friendly DB-constraint mapping (409/422) | S–M |
| Double-booking guard: `05_constraints.sql` unique (DoctorID, DateTime) + (RoomID, DateTime) + app-level availability check | M |
| **UTC-everywhere convention** (D6): instants stored UTC; business dates computed in IST by the app (all `CURDATE()` in SQL removed — Appointment/Billing/Dashboard/Insurance repos); today/tomorrow as IST-day→UTC-range queries `>= @start AND < @end` (index-friendly, no `DATE()` wrap); frontend renders UTC → viewer-local | M |
| Shift seed appointment times so stored values are true UTC (one migration or regenerated seed) | S |
| `PUT /api/patients/{id}` + edit UI | S |
| Frontend validation mirroring backend rules | M |
| Cold-start-aware frontend (retry + "server waking" banner) | S |
| Swagger UI in Development | S |
| xUnit integration test harness (WebApplicationFactory + MySQL service container in Actions); tests for existing modules incl. authz denials | L |
| Repo hygiene: purge committed `bin/`/`obj/` | S |

### Phase 4 — Clinical & Pharmacy modules (NEW — L overall)

Every controller ships with `[Authorize]` policies (P2), validators + tests
(P3), and domain events (consumed in P5) from day one.

| Module | Backend | Frontend | Size |
|---|---|---|---|
| **Medical Records** | Create/list/detail by patient; doctor-owned edit; nurse vitals update. Note: schema links records to patient+doctor+date, not to an appointment — "create from appointment" pre-fills but doesn't FK | Records page: doctor/nurse/patient views; record detail hosts prescriptions + linked labs | M–L |
| **Prescriptions** | Create with `PRESCRIBED_MEDICINE` lines in one transaction; active-prescription list; patient allergies displayed at prescribe time (from `PATIENT_ALLERGY`) | Prescribe form inside record detail; patient "my prescriptions" | M |
| **Lab Tests** | Order per appointment; LabTech work queue (own, by status); result entry (result, units, normal range) → Completed | Lab page: tech queue + result form; doctor sees results on record/appointment; patient sees completed results | M |
| **Pharmacy** | Medicines catalog + stock; pending-dispense list (active prescriptions); dispense transaction: insert `MEDICINE_DISPENSE` (DispensedBy = user) + decrement stock atomically, guards: stock ≥ qty, medicine not expired, prescription within `ValidUntil`; inventory view + below-`ReorderLevel` alerts | Pharmacy page: dispense workflow + inventory tab with low-stock badges | L |

Design decision (v1): `MEDICINE.StockQuantity` is the single source of truth
for stock; `INVENTORY` batch rows are informational display. Batch-level
FEFO dispensing = future work.

### Phase 5 — Observability & caching (was P4)

LogBase Serilog sink per `ARCHITECTURE.md` §2 (M) · request_id correlation (S)
· confirm ingest API-key header (S) · Aiven Valkey + cache dashboard/doctors/
medicines catalog with write-through invalidation (M).

### Phase 6 — CI/CD & scheduled ops (was P5)

CI gate: dotnet build+test, npm build+lint (M) · nightly demo reseed (S) ·
no-show marker cron (S) · next-day reminders as LogBase events (S) · nightly
`mysqldump` → Actions artifact (S) · `X-Ops-Key` auth for ops endpoints (S).

### Phase 7 — Optional platform extras (was P6)

Kafka `appointment-events` + consumer (Aiven free) (M–L) · Terraform for
Aiven/GitHub/Netlify (M) · local K8s manifests + kind walkthrough (M).

## 2. Review gaps — status

All eight gaps from rev. 1 are now placed: role-purpose gap → **resolved by
D1 = Full** (Phase 4); row-level auth, rate limiting, credential rotation →
P2; double-booking, timezone, patient edit, cold-start UX, Swagger, hygiene →
P3; reseed + cron auth → P6.

## 3. Decisions

**D1: Full clinical build — DECIDED.** D2: single ~8 h JWT, no refresh in v1
— default accepted. D3: reminders as LogBase events, no email in v1 — default
accepted. D4: doctors/staff read-only — default accepted. New, small:
**D5 — stock model** = `MEDICINE.StockQuantity` authoritative (see P4 note) —
proceeding unless vetoed. **D6 — time handling** = UTC-everywhere convention
(user-proposed, adopted; spec in `ARCHITECTURE.md` §5).

## 4. Non-goals (unchanged)

No real patient data ever (synthetic only; not HIPAA-anything) · no
multi-tenancy · no staging env · no mobile app · no file uploads · no cloud
K8s · pagination deferred · audit columns deferred (LogBase events cover it).

## 5. Secrets & variables inventory

| Where | Name | Phase |
|---|---|---|
| Render | `ConnectionStrings__HospitalDb` | 1 |
| Render | `Cors__AllowedOrigins` (optional) | 1 |
| Render | `Jwt__Secret`, `Jwt__ExpiryHours` | 2 |
| Render | `LogBase__IngestUrl / ApiKey / ApiKeyHeader / ServiceName / MinimumLevel` | 5 |
| Render | `ConnectionStrings__Valkey` | 5 |
| Render | `Ops__ApiKey` | 6 |
| Netlify | `VITE_API_BASE_URL` | 1 |
| GitHub var | `KEEPALIVE_URL` | 1 |
| GitHub secret | `AIVEN_MYSQL_URI` (backup/reseed) | 6 |
| GitHub secret | `OPS_API_KEY` (matches Render) | 6 |

## 6. Definition of done

- **P1:** end-to-end over HTTPS on Render+Aiven; old stack terminated; keep-alive green.
- **P2:** every existing endpoint requires auth; matrix enforced incl. own-data; login rate-limited; prod admin rotated; role-aware UI; auth events emitted.
- **P3:** all requests validated; no raw exception leaks; double-booking impossible; IST-correct dates; tests green in CI.
- **P4:** the full demo arc (top of this doc) can be walked click-by-click as seven different logins, with correct row-level visibility at each step; dispense correctly decrements stock and blocks over-dispense/expired.
- **P5:** MediNexus events searchable in LogBase under `medinexus-api`; cached reads verified with invalidation on writes.
- **P6:** broken builds can't deploy; all crons green a full week; backup artifact present.

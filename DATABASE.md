# MediNexus — Database, Explained Simply

How the data is organised and how it flows through a hospital visit. The
database is **MySQL 8**, hosted on Aiven. It has **28 tables** and is set up
by **9 SQL scripts** that run in order.

---

## 1. The 9 scripts (run in this order)

Think of these as the database's build instructions. Scripts 01–04 create and
fill it; 05 onward are **migrations** — later changes layered on safely.

```
 01_schema.sql          creates all 28 tables (the empty skeleton)
 02_seed_data.sql       fills reference data: staff, doctors, rooms,
                        medicines, insurance policies, sample patients
 03_seed_users.sql      creates the 7 login accounts (BCrypt-hashed)
 04_procedures.sql      a few stored procedures
 ─────────────────────  ↑ initial build      ↓ migrations (added over time)
 05_seed_utc_shift.sql  converts stored appointment times to true UTC (the D6 fix)
 06_constraints.sql     adds unique indexes → double-booking becomes impossible
 07_visit_workflow.sql  the Patient File: new statuses, prescription pipeline,
                        pharmacy billing columns, doctor schedules & leave
 08_insurance_validity.sql   extends demo insurance dates so claims work
 09_medicine_expiry.sql      pushes medicine expiry dates into the future
```

> **Design decision — migrations are one-shot by construction.**
> Each migration first records its own number in a small `SCHEMA_MIGRATION`
> table. If you accidentally run it twice, the second run hits a duplicate
> key and **aborts before touching any data**. This makes "just run the whole
> folder again" completely safe — a property that saved real trouble during
> deployment.

---

## 2. What the tables cover

The 28 tables group into a few areas:

```
   PEOPLE                        CLINICAL
   ├─ PATIENT                    ├─ APPOINTMENT
   ├─ STAFF                      ├─ MEDICAL_RECORD  (diagnosis, vitals)
   ├─ DOCTOR                     ├─ LAB_TEST
   ├─ NURSE / LAB_TECHNICIAN     ├─ PRESCRIPTION
   ├─ PHARMACIST / …             ├─ PRESCRIBED_MEDICINE
   └─ USER_ACCOUNT  (logins)     └─ MEDICINE_DISPENSE

   SCHEDULING                    MONEY                    PHARMACY
   ├─ DOCTOR_SCHEDULE            ├─ BILLING               ├─ MEDICINE  (stock)
   ├─ DOCTOR_LEAVE              ├─ PAYMENT / CLAIM       └─ INVENTORY
   └─ ROOM                       ├─ INSURANCE_POLICY
                                 └─ PATIENT_INSURANCE
```

Plus small supporting tables (allergies, departments, etc.) and the
`SCHEMA_MIGRATION` bookkeeping table.

---

## 3. How the data model enforces the rules

The database doesn't just *store* data — it *guards* it.

> **Design decision — integrity lives in the schema, not just the code.**
> The rules that matter most are enforced as database constraints, so they
> hold even if application code has a bug:
> - **Unique index on (doctor, time)** and **(room, time)** → the same doctor
>   or room can't be double-booked, ever.
> - **CHECK constraints** → an appointment's status must be one of the known
>   values; a stock quantity can't go negative; a payment method must be Cash
>   or Card.
> - **Foreign keys** → you can't bill a patient who doesn't exist, or
>   prescribe a medicine that isn't in the catalog.

```
   Application checks  ──►  "friendly first line of defence"
                            (nice error messages, fast)
   Database constraints ─►  "unbreakable last line of defence"
                            (holds even under a race or a bug)
```

---

## 4. The timezone rule (D6) — why script 05 exists

> **Design decision — moments are UTC, business dates are India time.**
> Every timestamp is stored as **UTC**. But the hospital thinks in **IST
> calendar days**. So "today's appointments" is asked as a UTC *time range*
> (from IST-midnight to the next IST-midnight, converted to UTC). Script 05
> was a one-time correction: the original seed data had times written as if
> they were IST, so it shifted them to true UTC. From then on, the convention
> is consistent end to end — and because the queries use a range rather than
> wrapping the date in a function, they stay fast (the index is used).

---

## 5. How one visit flows through the tables

This is the most useful way to understand the database — follow a single
patient visit and watch which tables change.

```
 STEP                        TABLE(S) TOUCHED                 STATUS BECOMES
 ─────────────────────────────────────────────────────────────────────────
 Patient books           →   APPOINTMENT (new row)            Requested
 Reception approves      →   APPOINTMENT                      Scheduled
 Reception checks in     →   APPOINTMENT                      CheckedIn
 Nurse records vitals    →   MEDICAL_RECORD (created/updated)
 Doctor starts           →   APPOINTMENT                      InConsultation
 Doctor diagnoses        →   MEDICAL_RECORD (diagnosis, plan)
 Doctor orders labs      →   LAB_TEST (one row per test)      Pending
 Doctor prescribes       →   PRESCRIPTION + PRESCRIBED_MEDICINE   SentToPharmacy
 Visit completed         →   BILLING (consultation) + CLAIM   Completed
 Lab enters result       →   LAB_TEST                         Completed
 Pharmacy confirms       →   PRESCRIPTION                     Confirmed
 Pharmacy marks ready    →   PRESCRIPTION                     Ready
 Pharmacy dispenses      →   MEDICINE (stock ↓) + MEDICINE_DISPENSE
                             + BILLING (pharmacy) + CLAIM     Dispensed
 Bills paid              →   BILLING (+ PaymentMethod, surcharge)
```

Notice that **no single "visit" table** holds all this — the Patient File you
see in the app is *assembled* from these rows on demand.

> **Design decision — the File is a projection, not a table (D7).** Storing a
> separate "encounter" record would duplicate data and risk it drifting out
> of sync. Instead, one query joins these tables together when the File is
> opened. The only schema additions the whole workflow needed were a few
> **status columns** (on APPOINTMENT and PRESCRIPTION) and two small tables
> for doctor schedules and leave.

---

## 6. Medicines, stock and dispensing

The `MEDICINE` table holds the catalog and the **authoritative stock count**.

> **Design decision — one source of truth for stock, decremented safely.**
> When a prescription is dispensed, the stock is reduced with a guarded update
> — `SET stock = stock - qty WHERE stock >= qty` — inside a transaction along
> with the dispense records and the bill. If two pharmacists somehow dispensed
> at once, the guard ensures stock can never go negative; if any line can't be
> filled, the *whole* dispense is rolled back (all-or-nothing).

*(Scripts 08 and 09 exist because the original demo data had insurance and
medicine dates in the past — see ISSUES_FACED.md. They simply push those
dates into the future so the demo behaves.)*

---

## 7. Insurance and claims

```
   PATIENT ──has──► PATIENT_INSURANCE ──points to──► INSURANCE_POLICY
                    (valid-from / valid-to,           (coverage %)
                     primary yes/no)
```

When a bill is created and the patient has **valid, in-force, primary**
insurance, a `CLAIM` row is created automatically for the covered share
(coverage % of the bill). The patient then owes the remainder.

> **Design decision — coverage is checked against the billing date.** A policy
> only applies if the billing date falls between its valid-from and valid-to
> dates. This is correct behaviour — but it's exactly why the demo "copay
> bug" happened: the seeded policies had expired, so the check correctly found
> no coverage. The fix wasn't to the logic; it was to extend the demo dates
> (script 08).

---

## 8. Logins and soft deletion

`USER_ACCOUNT` links a username + BCrypt password to a role and to the
person's `STAFF` or `PATIENT` record.

> **Design decision — accounts deactivate, they don't delete.** A removed user
> has `IsActive = 0`: they can't log in, but their linked history survives.
> Hard-deleting the row would break every appointment or bill that referenced
> them.

---

## 9. In one sentence

The database is a 28-table relational model where the important rules are
enforced as constraints (so they can't be broken), every moment is stored in
UTC, and a hospital "visit" is not one table but a story told across several —
assembled into the Patient File only when someone opens it.

# MediNexus — Frontend, Explained Simply

How the React app is built, in plain language. The frontend's job is to be
the face of the system: show each role only what they need, collect input
safely, and talk to the backend API.

---

## 1. The big picture

It's a **single-page application** built with **React 18** and **Vite**. When
you visit the site, one small HTML file loads, and React draws everything
after that — swapping "pages" in and out without full reloads.

```
   Browser
      │  loads once
      ▼
   ┌──────────────────────────────────────────────┐
   │  React app                                     │
   │                                                │
   │   Sidebar        Main area (the current page)  │
   │   ┌────────┐     ┌───────────────────────────┐│
   │   │Dashboard│    │                           ││
   │   │Patients │    │   e.g. the Patient File   ││
   │   │Appts    │    │                           ││
   │   │Pharmacy │    │                           ││
   │   │ …       │    │                           ││
   │   └────────┘     └───────────────────────────┘│
   └──────────────────────────────────────────────┘
      │  every data need → HTTP call
      ▼
   Backend API
```

---

## 2. How data gets in and out — the API client

All communication with the backend goes through **one file**: `services/api.js`.
It's built on **axios** (a library for making HTTP calls) and configured once,
so every call automatically:

- points at the right backend URL (from `VITE_API_BASE_URL`),
- attaches the logged-in user's **token**,
- and handles common problems centrally.

```
   any page      api.js                     backend
      │   getPatients()  │                     │
      │ ───────────────► │  adds token header  │
      │                  │ ──────────────────► │
      │                  │ ◄────────────────── │
      │ ◄─────────────── │                     │
```

Two clever behaviours live here:

- **Auto-logout:** if the backend ever replies "your token is invalid" (401),
  the app clears the session and returns you to the login page.
- **Cold-start retry:** free hosting sleeps when idle. If a read fails because
  the server is waking up (502/503/504), the app **quietly retries twice** and
  shows a small amber "server is waking up" banner, instead of showing an
  error.

---

## 3. Who sees what — login and permissions

When you log in, the app stores your token and your profile (name, role) in
the browser and remembers you. From then on, **your role decides what you
see.**

```
   Login  ──►  token + role stored  ──►  Sidebar shows only your modules
                                    └──►  Route guards block typed-in URLs
```

- `context/AuthContext.jsx` holds "who is logged in" for the whole app.
- `auth/permissions.js` is a single map of which roles can see which module —
  it **mirrors the backend's rules exactly**, so the menu never shows
  something the API would reject.
- **Route guards** protect every page: if a nurse types the Billing URL
  directly, they're bounced back rather than shown a page they can't use.

> **Note:** the frontend hiding a button is a convenience, not the security.
> The real enforcement is on the backend — the UI just avoids showing people
> doors they can't open.

---

## 4. The look — "clinical modernism"

The visual style is deliberate and custom, not a stock template.

> **Design idea:** warm paper-white surfaces, ink-charcoal text, one deep
> teal as the working colour, and amber for warnings. Squared corners (2px),
> thin rules instead of heavy drop-shadows, no gradients, and **tabular
> numerals** for the data a hospital lives on (IDs, times, amounts, so digits
> line up in columns). Tabs are underlines, not pills; buttons shift weight on
> hover rather than glowing.

It's implemented as a small set of reusable CSS classes and design tokens in
`styles/theme.css` (`mn-btn`, `mn-card`, `mn-tab`, and so on), with a layer
that gently overrides old default styles so the whole app stays on-palette.

---

## 5. The pages

There are **13 pages**. Here's what each is for.

| Page | Who uses it | What it does |
|------|-------------|--------------|
| **Login** | everyone | sign in; demo-account picker (admin excluded) |
| **Dashboard** | all staff | summary tiles (patients, doctors, today's appointments, revenue) |
| **Patients** | front desk (edit), staff (view) | register/edit patients; role-gated buttons |
| **Doctors** | all | doctor directory |
| **Appointments** | varies by role | the schedule; book, approve, check-in, start, open File |
| **File** | all (scoped) | **the Patient File** — the visit timeline + role actions |
| **Lab** | lab techs | personal queue: start tests, enter results |
| **Pharmacy** | pharmacists | prescription queue + inventory tabs |
| **Schedule** | doctors | weekly working pattern + leave management |
| **Billing** | front desk | bills; pay by cash/card with live surcharge preview |
| **Insurance** | front desk | policies and patient coverage |
| **Users** | admin | user management (create, edit, soft-delete, reset password) |
| **Architecture** | admin | live self-describing system diagram |

### The Patient File page — the centrepiece

This is where the workflow comes alive. Opening a visit shows a **timeline**,
and — depending on your role — the actions you can take on it:

```
   ┌─────────────────────────────────────────────┐
   │  Amit Shah · Dr. Sharma · 10:30 · CheckedIn  │  ← header + live status
   ├─────────────────────────────────────────────┤
   │  PENDING ON THIS VISIT  (clickable chips)    │  ← what's left to do,
   │  [ Nurse: record vitals ] [ Pay bill #88 ]   │    derived live; click to
   ├─────────────────────────────────────────────┤    jump to that section
   │  Vitals        │ (nurse records here)         │
   │  Consultation  │ (doctor: diagnosis, plan)    │
   │  Lab tests     │ (ordered / results)          │
   │  Prescription  │ (doctor prescribes; allergy  │
   │                │  banner shown here)          │
   │  Bills         │ (consultation + pharmacy)    │
   └─────────────────────────────────────────────┘
```

> **Design idea — "what's pending" is computed, never stored.** The chips at
> the top read the File and work out the next real step for each role
> ("Pharmacy: dispense at pickup", "Pay pharmacy bill #88 — 430.00 due"), and
> each chip scrolls you to the right section. Nothing tracks this in the
> database; it's derived every time from the current state.

### Booking — the slot picker

Patients and the front desk don't type a date and time freely. They pick a
doctor, pick a day, and choose from **slots the backend offers** — which
already exclude taken slots, leave days, and the past. This makes
double-booking impossible from the UI, and the times shown are always in
India time.

### Billing — the payment modal

When taking payment, the modal lets you choose **cash or card**. If you pick
card, it shows the **2.5% service charge and the new total before you
confirm** — the surcharge itself is calculated by the server, so the UI only
previews it.

---

## 6. Handling errors gracefully

- **API errors** are caught per call and shown as a message; the Users page
  shows them **inline next to the offending field** (the pattern being rolled
  out to other forms).
- **Cold starts** trigger the auto-retry + banner described above.
- **Uncaught browser errors** are captured by a small reporter
  (`services/errorReporter.js`) that hooks into the browser's error events and
  sends them to the backend for logging — silently, never interrupting the
  user.

---

## 7. Frontend folder map

```
src/
├── pages/            the 13 pages above
├── components/
│   └── Layout/       Sidebar (role menu + "view as" + change password),
│                     Layout (page frame + banners)
├── context/
│   └── AuthContext   who's logged in; login/logout/impersonation
├── services/
│   ├── api.js        the single axios client (token, retries, all endpoints)
│   └── errorReporter.js   captures browser errors → backend
├── auth/
│   └── permissions.js     role → module map (mirrors the backend)
├── utils/
│   ├── datetime.js   render/parse times in India time
│   └── pending.js    work out "what's pending" on a File
└── styles/
    └── theme.css     the clinical-modernism design tokens
```

---

## 8. In one sentence

The frontend is a role-aware single-page app that talks to the backend
through one well-behaved API client, shows each person exactly the doors they
can open, and turns a multi-department hospital visit into a single, readable
File.

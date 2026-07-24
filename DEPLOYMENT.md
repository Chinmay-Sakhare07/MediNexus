# MediNexus — Free Hosting Cutover (Phase 1)

Goal: retire the Oracle VM + Cloudflare Worker + DuckDNS chain and run everything
on free, HTTPS-native services.

```
BEFORE:  Netlify ──HTTPS──> Cloudflare Worker ──HTTP──> Oracle VM (.NET + MariaDB)
AFTER:   Netlify ──HTTPS──> Render (.NET API, Docker) ──TLS──> Aiven MySQL (free)
                                     ▲
                    GitHub Action pings /health every 10 min
                    (keeps Render awake + counts as Aiven activity)
```

Everything below is $0. No credit card is required by Aiven, Render, Netlify,
or GitHub for any of these steps.

---

## Step 1 — Create the free Aiven MySQL and load the database

1. Sign up / log in at https://console.aiven.io (no card needed).
2. Create service → **MySQL** → Service tier: **Free** → pick the region group
   closest to the Render region you'll use in Step 2 (for India, Singapore-area
   if offered; otherwise the nearest available). Name it `medinexus-mysql`.
3. Wait for status **Running**, then open the service → **Connection information**.
   Note the **Host**, **Port**, and **Password** for user `avnadmin`.
4. From the repo root on your machine, load the scripts **in order**
   (the first one creates the `medinexus` database itself):

   ```bash
   HOST=<host>.aivencloud.com  PORT=<port>  PASS='<avnadmin password>'

   mysql --host=$HOST --port=$PORT --user=avnadmin --password=$PASS --ssl-mode=REQUIRED < Database/MySQL/01_schema.sql
   mysql --host=$HOST --port=$PORT --user=avnadmin --password=$PASS --ssl-mode=REQUIRED < Database/MySQL/02_seed_data.sql
   mysql --host=$HOST --port=$PORT --user=avnadmin --password=$PASS --ssl-mode=REQUIRED < Database/MySQL/03_seed_users.sql
   mysql --host=$HOST --port=$PORT --user=avnadmin --password=$PASS --ssl-mode=REQUIRED < Database/MySQL/04_procedures.sql
   ```

   Prefer a GUI? MySQL Workbench / DBeaver connect fine — use the same host,
   port, user `avnadmin`, and set SSL to "Required". Then run the four files
   in order.
5. Verify:

   ```bash
   mysql --host=$HOST --port=$PORT --user=avnadmin --password=$PASS --ssl-mode=REQUIRED \
     -e "USE medinexus; SELECT COUNT(*) AS patients FROM PATIENT; SELECT COUNT(*) AS users FROM USER_ACCOUNT;"
   ```

6. Build the connection string the API will use (keep it handy for Step 2):

   ```
   Server=<host>.aivencloud.com;Port=<port>;Database=medinexus;User ID=avnadmin;Password=<password>;SslMode=Required;
   ```

Note: the free service is always-on, but Aiven powers off services that sit
idle for an extended period (they email first, and you can power it back on
from the console). The keep-alive in Step 4 makes this a non-issue.

## Step 2 — Deploy the API to Render (free, Docker)

1. Push the current branch (including the changed `Program.cs` and the new
   workflow file) to GitHub.
2. https://dashboard.render.com → **New → Web Service** → connect the
   `MediNexus` GitHub repo.
3. Render detects the `Dockerfile` at the repo root — keep runtime **Docker**.
   - Region: closest to your Aiven region (Singapore for India-based traffic).
   - Instance type: **Free**.
4. Environment variables (Environment tab):

   | Key | Value |
   |---|---|
   | `ConnectionStrings__HospitalDb` | the connection string from Step 1.6 |

   (Double underscore is how .NET reads nested config from env vars. The
   optional `Cors__AllowedOrigins` var exists too if you ever serve the
   frontend from a new domain — comma-separated list, no code change needed.)
5. Settings → **Health Check Path**: `/health`.
6. Deploy. First build takes a few minutes. Your API URL will be
   `https://<service-name>.onrender.com`.
7. Smoke test in a browser:
   - `https://<service-name>.onrender.com/` → the running banner
   - `https://<service-name>.onrender.com/health` → `{"status":"ok","db":"up",...}`
   - `https://<service-name>.onrender.com/api/dashboard` → JSON stats

Free-tier behavior to expect: with no traffic for ~15 minutes the service
sleeps and the next request takes ~30–60 s. Step 4 prevents that during
normal hours.

## Step 3 — Point Netlify at the new API

`src/services/api.js` now reads the URL from a build-time variable.

1. Netlify → your site → **Site configuration → Environment variables** →
   add `VITE_API_BASE_URL` = `https://<service-name>.onrender.com/api`.
2. **Deploys → Trigger deploy → Deploy site.** (Required: Vite inlines env
   vars at build time, so a rebuild must happen for the change to apply.)
3. Open https://medinexushealth.netlify.app — the dashboard should load over
   pure HTTPS with no Worker in the path.

## Step 4 — Turn on the keep-alive cron

1. GitHub repo → **Settings → Secrets and variables → Actions → Variables** →
   **New repository variable**:
   - `KEEPALIVE_URL` = `https://<service-name>.onrender.com/health`
2. The workflow `.github/workflows/keep-alive.yml` (already in the repo after
   Step 2's push) pings it every 10 minutes. Run it once manually from the
   **Actions** tab (`Keep services awake → Run workflow`) to confirm a green run.

Heads-up: GitHub automatically disables scheduled workflows in repos with no
commit activity for ~60 days — any push re-enables them, or click "Enable"
in the Actions tab if you see the notice.

## Step 5 — Verify, then tear down the old stack

Checklist first (all against the Netlify site):

- Dashboard loads with counts
- Register a test patient → appears in Patients
- Schedule an appointment for the test patient → appears in Appointments
- Complete the appointment with billing → bill appears in Billing
- `/health` returns `ok` twice in a row

Only after everything above passes:

1. Cloudflare dashboard → Workers & Pages → delete the `medinexus-api` worker.
2. DuckDNS → remove the `medinexus` subdomain.
3. Oracle Cloud console → terminate the VM (this deletes the old MariaDB —
   the data now lives in Aiven; export anything sentimental first).
4. Optional cosmetics: update README's tech-stack table (API hosting → Render,
   DB → Aiven for MySQL free tier; remove Worker/DuckDNS rows).

## Troubleshooting

- Browser console shows a CORS error → the Netlify domain must be in the
  allowed origins. `medinexushealth.netlify.app` is built in; a different
  domain goes into Render env var `Cors__AllowedOrigins`.
- `/health` returns 503 `db: unreachable` → re-check the connection string
  (host, port, `SslMode=Required`), and confirm the Aiven service shows
  **Running** — if it was powered off for inactivity, power it on in the
  Aiven console.
- First request after a quiet period takes ~a minute → the keep-alive isn't
  running: check the Actions tab for red runs and that `KEEPALIVE_URL` is set.
- Render build fails → confirm the Dockerfile is at the repo root and the
  service's root directory setting is empty (repo root is the build context).

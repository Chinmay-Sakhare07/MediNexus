# 🏥 MediNexus | Hospital Management System

A full-stack hospital management platform built as an academic project for the Data Management and Database Design (DMDD) course at Northeastern University. Handles everything a multispecialty hospital needs: patient registration, doctor scheduling, appointment management, billing with insurance claims, lab test tracking, prescription management, and pharmacy inventory.

🔗 **Live:** [medinexushealth.netlify.app](https://medinexushealth.netlify.app)

> **Note:** The database runs on Azure SQL Serverless free tier and may take 20–30 seconds to wake up on first load after a period of inactivity. Subsequent requests will be fast.

---

## What It Does

**Clinical Workflow**
- Register patients with full demographics, blood type, emergency contacts, and allergy tracking
- Assign primary physicians and manage doctor availability
- Schedule, confirm, complete, and cancel appointments with room assignment
- Create medical records with diagnoses, vital signs, treatment plans, and follow-up flags
- Order lab tests tied to appointments, track results and status
- Generate prescriptions linked to medical records with dosage, frequency, and renewal tracking

**Financial Workflow**
- Generate bills on appointment completion with tax calculation and discount support
- Automatic insurance claim creation based on patient's primary policy
- Track claim status: submitted, under review, approved, partially approved, denied
- Calculate patient responsibility after insurance coverage
- Process payments and update billing status

**Pharmacy & Inventory**
- Track 20+ medicines with stock quantities, pricing, expiry dates, and categories
- Monitor inventory levels with reorder alerts and supplier tracking
- Map storage requirements (temperature, humidity, special handling) to medicines
- Link prescribed medicines to prescriptions with dosage instructions

**Administrative**
- 8 departments with operating hours and department heads
- Staff management with roles: doctors, lab technicians, nurses, admin, HR
- Room management across floors with equipment tracking
- Doctor language proficiency tracking for patient matching

---

## Tech Stack

| Layer | Technology |
|---|---|
| **Frontend** | React 18, Tailwind CSS, Axios, React Router |
| **Backend** | .NET Core 9, ASP.NET Web API, Dapper ORM |
| **Database** | SQL Server (Azure SQL Database, free tier serverless) |
| **API Hosting** | Oracle Cloud Always Free VM (VM.Standard.E2.1.Micro, Ubuntu 22.04) |
| **HTTPS Proxy** | Cloudflare Worker (`medinexus-api.sakhare-c.workers.dev`) |
| **DNS** | DuckDNS (`medinexus.duckdns.org` → Oracle VM IP) |
| **Frontend Hosting** | Netlify (auto-deploy from GitHub) |
| **Architecture** | REST API, Repository Pattern, CORS enabled |

---

## Architecture

```
┌──────────────┐     ┌──────────────────────┐     ┌────────────────────┐     ┌─────────────────┐
│   React UI   │────▶│  Cloudflare Worker   │────▶│   .NET Core API    │────▶│  Azure SQL DB   │
│  (Netlify)   │HTTPS│ medinexus-api        │HTTP │  Oracle Cloud VM   │ SQL │  medinexus-db   │
│              │     │ .sakhare-c.workers   │     │  port 5000         │     │  (serverless)   │
└──────────────┘     │ .dev                 │     └────────────────────┘     └─────────────────┘
                     └──────────────────────┘
                               ▲
                               │ routes via
                     medinexus.duckdns.org
```

**Why this architecture?**
- Netlify forces HTTPS. The Oracle VM serves HTTP. Browsers block mixed HTTP/HTTPS requests.
- A Cloudflare Worker acts as a permanent HTTPS proxy, solving the mixed content issue for free.
- DuckDNS provides a stable hostname so the Worker doesn't need updating if the VM IP changes.

---

## Database Design

The database has **26 normalized tables** organized into 4 clusters:

**Administrative Cluster:** Department, Staff, Doctor, Lab Technician, Room, Equipment, Room Equipment, Language, Doctor Language

**Clinical Cluster:** Patient, Appointment, Medical Record, Lab Test, Prescription, Prescribed Medicine, Allergy, Patient Allergy

**Financial Cluster:** Billing, Claim, Insurance Provider, Insurance Policy, Patient Insurance

**Pharmacy Cluster:** Medicine, Inventory, Storage Requirement, Medicine Storage

### Database Objects

- **3 User-Defined Functions:** Patient age calculator, patient financial responsibility calculator, doctor available time slots
- **6 Views:** Patient medical history, doctor schedule, revenue analysis, inventory status, insurance claim summary, decrypted patient data
- **1 Trigger:** Appointment audit trail logging all changes with timestamps and user info
- **3 Stored Procedures:** Register patient, schedule appointment (with room auto-assignment), update appointment (with conflict detection)
- **3 Helper Procedures:** Encrypt patient data, decrypt patient data, decrypt policy number
- **Audit table** for tracking all appointment modifications
- **54 Indexes** for query optimization across all tables
- **CHECK constraints** for data validation on emails, dates, statuses, percentages, and amounts

### Security

Column-level AES-256 encryption is implemented for sensitive patient and insurance data:
- `PATIENT.Email`, `Phone`, `Address`, `EmergencyContact`
- `INSURANCE_POLICY.PolicyNumber`

> Note: Encryption is implemented in the database layer as an academic feature. The current API deployment skips the encryption script to maintain compatibility with the .NET data layer.

---

## API Endpoints

**Patients**
- `GET /api/patients` — List all patients
- `GET /api/patients/{id}` — Get patient by ID
- `POST /api/patients` — Register new patient
- `DELETE /api/patients/{id}` — Remove patient

**Doctors**
- `GET /api/doctors` — List all doctors with specializations
- `GET /api/doctors/{id}` — Get doctor details
- `GET /api/doctors/{id}/slots?date=` — Get available time slots

**Appointments**
- `GET /api/appointments` — List all appointments
- `POST /api/appointments` — Schedule new appointment
- `PUT /api/appointments/{id}` — Update appointment
- `PUT /api/appointments/{id}/complete` — Complete with billing

**Billing**
- `GET /api/billing` — List all bills
- `GET /api/billing/patient/{id}` — Bills by patient
- `POST /api/billing/payment` — Process payment

**Insurance**
- `GET /api/insurance/providers` — List providers
- `GET /api/insurance/policies` — List all policies
- `GET /api/insurance/patient/{id}` — Patient's insurance
- `POST /api/insurance/assign` — Assign policy to patient

**Dashboard**
- `GET /api/dashboard` — Aggregated stats for the dashboard view

---

## Project Structure

```
MediNexus/
├── src/                              # React Frontend
│   ├── components/
│   │   └── Layout/                   # Sidebar, Layout wrapper
│   ├── pages/                        # Dashboard, Patients, Doctors,
│   │                                 # Appointments, Billing, Insurance
│   ├── services/
│   │   └── api.js                    # Axios base URL configuration
│   ├── App.jsx
│   └── main.jsx
│
├── Backend/                          # .NET Core 9 Backend
│   ├── Controllers/                  # REST API controllers (6 modules)
│   ├── Models/
│   │   ├── DTOs/                     # Data transfer objects
│   │   └── Requests/                 # Request models
│   ├── Repositories/
│   │   ├── Interfaces/               # Repository interfaces (DI)
│   │   └── *.cs                      # Dapper repository implementations
│   ├── Program.cs                    # App startup, CORS, DI registration
│   └── appsettings.json
│
├── public/
│   └── favicon.svg                   # Hospital cross favicon
│
└── SQL Scripts/
    ├── MediNexus_Complete_Azure.sql  # Single script for fresh DB setup
    ├── 1_DDL_Scripts.sql             # Table creation (original)
    ├── 2_DML_Scripts.sql             # Sample data (original)
    ├── 3_PSM_Scripts.sql             # Functions, views, triggers, SPs
    ├── 4_Encryption_Script.sql       # Column encryption (skip for deployment)
    └── 5_Indexes_Script.sql          # Performance indexes
```

---

## Deployment

### Infrastructure (all free, permanent)

| Service | Provider | Cost |
|---|---|---|
| Frontend hosting | Netlify | Free |
| API hosting | Oracle Cloud VM.Standard.E2.1.Micro | Always Free |
| Database | Azure SQL Serverless free offer | Free forever |
| HTTPS proxy | Cloudflare Worker | Free tier |
| DNS | DuckDNS | Free |

### API Server (Oracle Cloud VM)

The API runs as a systemd service (`medinexus.service`) that auto-starts on boot and restarts on crash. The application was deployed as a **self-contained publish** because the 1GB RAM on E2.1.Micro prevented installing the .NET runtime via package manager.

```bash
# Publish locally
cd Backend
dotnet publish -c Release -r linux-x64 --self-contained true -o ./deploy

# Upload to VM
scp -i ~/ssh-key.key -r ./deploy opc@129.153.7.145:~/medinexus-api

# Restart service on VM
sudo systemctl restart medinexus
```

### HTTPS Flow

```
Browser → https://medinexus-api.sakhare-c.workers.dev
       → Cloudflare Worker fetches http://medinexus.duckdns.org:5000
       → DuckDNS resolves to 129.153.7.145 (Oracle VM)
       → .NET API responds
       → Worker adds CORS headers and returns HTTPS response
```

### Database (Azure SQL)

The database uses Azure SQL Database's permanent free offer (100,000 vCore seconds/month, 32 GB storage). The connection string is stored as an environment variable in the systemd service file, never in source code. Auto-pauses when idle — first request after inactivity may be slow.

### Setting up a fresh database

1. Create a new Azure SQL Database with the free offer applied
2. Open SSMS and connect to your server
3. Run `MediNexus_Complete_Azure.sql` against your new database — this single script creates all 26 tables, inserts sample data, and creates all stored procedures, views, triggers, and indexes

---

## Run Locally

### Prerequisites
- Node.js 18+
- .NET 9 SDK
- SQL Server (local instance or Azure SQL)

### Database Setup
```sql
-- In SSMS connected to your SQL Server, run:
-- SQL Scripts/MediNexus_Complete_Azure.sql
```

### Backend
```bash
cd Backend
# Set connection string
export ConnectionStrings__HospitalDb="Server=...;Database=...;User ID=...;Password=...;TrustServerCertificate=True;"
dotnet run
# API runs at http://localhost:5000
```

### Frontend
```bash
npm install
# Update API_BASE_URL in src/services/api.js to http://localhost:5000/api
npm run dev
# Frontend runs at http://localhost:5173
```

---

## Sample Data

The database comes pre-loaded with:
- 8 departments
- 20 staff members (5 doctors, 5 lab technicians, 10 admin/operational)
- 12 rooms across 6 departments
- 15 patients with diverse demographics
- 20 allergies with patient mappings
- 49 appointments spanning Nov 2025 to Jan 2026
- 15 medical records with diagnoses and treatment plans
- 18 lab tests with results
- 11 prescriptions with 21 prescribed medicines
- 5 insurance providers with 12 policies
- 20 billing records with 15 insurance claims
- 20 medicines with inventory, storage requirements, and supplier info

---

Built at [Northeastern University](https://www.northeastern.edu/) for the DMDD course.
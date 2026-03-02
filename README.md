# MediNexus | Hospital Management System

A full-stack hospital management platform built as an academic project for the Data Management and Database Design course at Northeastern University. Handles everything a multispecialty hospital needs: patient registration, doctor scheduling, appointment management, billing with insurance claims, lab test tracking, prescription management, and pharmacy inventory.

🔗 **Live:** [medinexushealth.netlify.app](https://medinexushealth.netlify.app)
> Note: The backend runs on Azure's free student tier and takes about 30-45 seconds to wake up on first load.

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
| **Database** | SQL Server (Azure SQL Database, free tier) |
| **Cloud** | Azure App Service (API), Netlify (frontend) |
| **Architecture** | REST API, Repository Pattern, CORS enabled |

---

## Database Design

The database has **26 normalized tables** organized into 4 clusters:

**Administrative Cluster:** Department, Staff, Doctor, Lab Technician, Room, Equipment, Room Equipment, Language, Doctor Language

**Clinical Cluster:** Patient, Appointment, Medical Record, Lab Test, Prescription, Prescribed Medicine, Allergy, Patient Allergy

**Financial Cluster:** Billing, Claim, Insurance Provider, Insurance Policy, Patient Insurance

**Pharmacy Cluster:** Medicine, Inventory, Storage Requirement, Medicine Storage

### Database Objects

- **3 User-Defined Functions:** Patient age calculator, patient financial responsibility calculator, doctor available time slots
- **5 Views:** Patient medical history, doctor schedule, revenue analysis, inventory status, insurance claim summary
- **1 Trigger:** Appointment audit trail logging all changes with timestamps and user info
- **3 Stored Procedures:** Register patient, schedule appointment (with room auto-assignment), update appointment (with conflict detection)
- **Audit table** for tracking all appointment modifications
- **55+ indexes** for query optimization across all tables
- **CHECK constraints** for data validation on emails, dates, statuses, percentages, and amounts

---

## Architecture

```
┌──────────────┐       ┌──────────────────┐       ┌─────────────────┐
│   React UI   │──────▶│  .NET Core API   │──────▶│  Azure SQL DB   │
│  (Netlify)   │ HTTPS │ (Azure App Svc)  │  SQL  │  (medinexus_db) │
└──────────────┘       └──────────────────┘       └─────────────────┘
```

**Frontend** makes API calls to the backend using Axios with a configurable base URL. **Backend** uses the Repository pattern with Dapper for lightweight data access. Each entity has its own repository with an interface, registered via dependency injection. **Database** runs on Azure SQL Database's free tier with auto-pause when idle.

---

## API Endpoints

**Patients**
- `GET /api/patients` - List all patients
- `GET /api/patients/{id}` - Get patient by ID
- `POST /api/patients` - Register new patient
- `DELETE /api/patients/{id}` - Remove patient

**Doctors**
- `GET /api/doctors` - List all doctors with specializations
- `GET /api/doctors/{id}` - Get doctor details
- `GET /api/doctors/{id}/slots?date=` - Get available time slots

**Appointments**
- `GET /api/appointments` - List all appointments
- `POST /api/appointments` - Schedule new appointment
- `PUT /api/appointments/{id}` - Update appointment
- `PUT /api/appointments/{id}/complete` - Complete with billing

**Billing**
- `GET /api/billing` - List all bills
- `GET /api/billing/patient/{id}` - Bills by patient
- `POST /api/billing/payment` - Process payment

**Insurance**
- `GET /api/insurance/providers` - List providers
- `GET /api/insurance/policies` - List all policies
- `GET /api/insurance/patient/{id}` - Patient's insurance
- `POST /api/insurance/assign` - Assign policy to patient

**Dashboard**
- `GET /api/dashboard` - Aggregated stats for the dashboard view

---

## Project Structure

```
Hospital_Management_System/
├── hospital-client/                 # React Frontend
│   ├── src/
│   │   ├── components/              # Reusable UI components
│   │   ├── pages/                   # Page-level components
│   │   ├── services/
│   │   │   └── api.js               # Axios configuration
│   │   ├── App.jsx
│   │   └── main.jsx
│   ├── package.json
│   └── vite.config.js
│
├── HospitalManagement.API/          # .NET Core Backend
│   ├── Controllers/                 # API controllers
│   ├── Models/
│   │   ├── DTOs/                    # Data transfer objects
│   │   └── Requests/                # Request models
│   ├── Repositories/
│   │   ├── Interfaces/              # Repository interfaces
│   │   ├── PatientRepository.cs
│   │   ├── DoctorRepository.cs
│   │   ├── AppointmentRepository.cs
│   │   ├── BillingRepository.cs
│   │   ├── InsuranceRepository.cs
│   │   └── DashboardRepository.cs
│   ├── Program.cs
│   └── appsettings.json
│
└── SQL Scripts/
    ├── 1_DDL_Scripts.sql            # Table creation
    ├── 2_DML_Scripts.sql            # Sample data (15 patients, 49 appointments, etc.)
    ├── 3_PSM_Scripts.sql            # Functions, views, triggers, procedures
    └── 5_Indexes_Script.sql         # Additional performance indexes
```

---

## Run Locally

### Prerequisites
- Node.js 18+
- .NET 9 SDK
- SQL Server (local instance or Azure)

### Database Setup
1. Open SSMS and connect to your SQL Server
2. Run the SQL scripts in order: DDL, DML, PSM, Indexes
3. Note your connection string

### Backend
```bash
cd HospitalManagement.API
# Update connection string in appsettings.json
dotnet run
```
API runs at `https://localhost:5001`

### Frontend
```bash
cd hospital-client
npm install
# Update API_BASE_URL in src/services/api.js to https://localhost:5001
npm run dev
```
Frontend runs at `http://localhost:5173`

---

## Deployment

**Frontend** is deployed on Netlify with automatic builds from GitHub.

**Backend** runs on Azure App Service (free tier, .NET 9, Linux).

**Database** runs on Azure SQL Database (free tier, General Purpose Serverless) with auto-pause enabled to stay within free limits.

Connection string is stored in Azure App Service environment variables, not in source code.

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

Built at [Northeastern University](https://www.northeastern.edu/) | Deployed on [Azure](https://azure.microsoft.com/) + [Netlify](https://www.netlify.com/)
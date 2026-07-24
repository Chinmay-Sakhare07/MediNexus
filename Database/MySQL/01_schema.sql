-- =============================================
-- MediNexus Hospital Management System
-- MySQL 8.0 Schema (ported from SQL Server 2019/2021)
-- 26 core tables + USER_ACCOUNT (auth) + MEDICINE_DISPENSE (pharmacy dispensing)
--
-- Porting notes vs the original T-SQL:
--   * IDENTITY(1,1)          -> AUTO_INCREMENT
--   * BIT                    -> TINYINT(1)
--   * GETDATE() defaults     -> CURDATE() / CURRENT_TIMESTAMP
--   * CHECK (col <= GETDATE()) dropped: MySQL forbids non-deterministic
--     functions in CHECK constraints (enforced in the app layer instead).
--   * CHECK (... LIKE '[0-9]%') -> REGEXP '^[0-9]' (MySQL LIKE has no ranges)
--   * `DateTime` and `LANGUAGE` are backticked (keyword / type-name clashes)
--   * FK columns are auto-indexed by InnoDB; only extra secondary indexes listed
-- =============================================

-- utf8mb4 with the server's default collation (portable across MySQL 8 and
-- MariaDB 10.x; both pick a sensible utf8mb4 collation).
CREATE DATABASE IF NOT EXISTS medinexus CHARACTER SET utf8mb4;
USE medinexus;

-- ---- Clean slate ---------------------------------------------------------
SET FOREIGN_KEY_CHECKS = 0;
DROP TABLE IF EXISTS
    MEDICINE_DISPENSE, USER_ACCOUNT,
    MEDICINE_STORAGE, PRESCRIBED_MEDICINE, INVENTORY, STORAGE_REQUIREMENT, MEDICINE,
    CLAIM, BILLING, PATIENT_INSURANCE, INSURANCE_POLICY, INSURANCE_PROVIDER,
    PATIENT_ALLERGY, ALLERGY, LAB_TEST, PRESCRIPTION, MEDICAL_RECORD, APPOINTMENT, PATIENT,
    DOCTOR_LANGUAGE, `LANGUAGE`, ROOM_EQUIPMENT, EQUIPMENT, ROOM, LAB_TECHNICIAN, DOCTOR,
    STAFF, DEPARTMENT;
SET FOREIGN_KEY_CHECKS = 1;

-- =============================================
-- ADMINISTRATIVE CLUSTER
-- =============================================

CREATE TABLE DEPARTMENT (
    DepartmentID        INT             NOT NULL AUTO_INCREMENT,
    Name                VARCHAR(100)    NOT NULL,
    Location            VARCHAR(150)    NULL,
    ContactNumber       VARCHAR(15)     NULL,
    HeadOfDepartment    VARCHAR(100)    NULL,
    OperatingHours      VARCHAR(50)     NULL,
    PRIMARY KEY (DepartmentID),
    CONSTRAINT UQ_Department_Name UNIQUE (Name),
    CONSTRAINT CHK_Department_ContactNumber CHECK (ContactNumber REGEXP '^[0-9]')
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE STAFF (
    StaffID             INT             NOT NULL AUTO_INCREMENT,
    DepartmentID        INT             NOT NULL,
    FirstName           VARCHAR(50)     NOT NULL,
    LastName            VARCHAR(50)     NOT NULL,
    Role                VARCHAR(50)     NOT NULL,
    Phone               VARCHAR(15)     NULL,
    Email               VARCHAR(100)    NULL,
    HireDate            DATE            NOT NULL DEFAULT (CURDATE()),
    EmploymentType      VARCHAR(20)     NULL DEFAULT 'Full-Time',
    SalaryBand          VARCHAR(20)     NULL,
    EmergencyContact    VARCHAR(100)    NULL,
    PRIMARY KEY (StaffID),
    CONSTRAINT FK_Staff_Department FOREIGN KEY (DepartmentID)
        REFERENCES DEPARTMENT(DepartmentID) ON DELETE NO ACTION ON UPDATE CASCADE,
    CONSTRAINT CHK_Staff_EmploymentType CHECK (EmploymentType IN ('Full-Time','Part-Time','Contract','Intern')),
    CONSTRAINT CHK_Staff_Email CHECK (Email LIKE '%_@__%.__%')
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
CREATE INDEX IX_Staff_Email ON STAFF(Email);
CREATE INDEX IX_Staff_Role  ON STAFF(Role);

CREATE TABLE DOCTOR (
    DoctorID            INT             NOT NULL,
    Specialization      VARCHAR(100)    NULL,
    LicenseNumber       VARCHAR(50)     NULL,
    AvailabilityStatus  VARCHAR(20)     NULL DEFAULT 'Available',
    YearsOfExperience   INT             NULL,
    ConsultationFee     DECIMAL(8,2)    NULL,
    LanguagesSpoken     VARCHAR(200)    NULL,
    PRIMARY KEY (DoctorID),
    CONSTRAINT FK_Doctor_Staff FOREIGN KEY (DoctorID)
        REFERENCES STAFF(StaffID) ON DELETE CASCADE ON UPDATE CASCADE,
    CONSTRAINT UQ_Doctor_LicenseNumber UNIQUE (LicenseNumber),
    CONSTRAINT CHK_Doctor_AvailabilityStatus CHECK (AvailabilityStatus IN ('Available','On Leave','Busy','Retired')),
    CONSTRAINT CHK_Doctor_YearsOfExperience CHECK (YearsOfExperience >= 0),
    CONSTRAINT CHK_Doctor_ConsultationFee CHECK (ConsultationFee >= 0)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
CREATE INDEX IX_Doctor_Specialization     ON DOCTOR(Specialization);
CREATE INDEX IX_Doctor_AvailabilityStatus ON DOCTOR(AvailabilityStatus);

CREATE TABLE LAB_TECHNICIAN (
    LabTechnicianID     INT             NOT NULL,
    Certification       VARCHAR(100)    NULL,
    ShiftHours          VARCHAR(50)     NULL,
    SkillLevel          VARCHAR(20)     NULL,
    AssignedLab         VARCHAR(50)     NULL,
    PRIMARY KEY (LabTechnicianID),
    CONSTRAINT FK_LabTechnician_Staff FOREIGN KEY (LabTechnicianID)
        REFERENCES STAFF(StaffID) ON DELETE CASCADE ON UPDATE CASCADE,
    CONSTRAINT CHK_LabTechnician_SkillLevel CHECK (SkillLevel IN ('Junior','Mid-Level','Senior','Expert'))
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
CREATE INDEX IX_LabTechnician_AssignedLab ON LAB_TECHNICIAN(AssignedLab);

CREATE TABLE ROOM (
    RoomID              INT             NOT NULL AUTO_INCREMENT,
    DepartmentID        INT             NOT NULL,
    RoomType            VARCHAR(50)     NULL,
    Capacity            INT             NULL,
    AvailabilityStatus  VARCHAR(20)     NULL DEFAULT 'Available',
    FloorNumber         INT             NULL,
    EquipmentAvailable  VARCHAR(200)    NULL,
    PRIMARY KEY (RoomID),
    CONSTRAINT FK_Room_Department FOREIGN KEY (DepartmentID)
        REFERENCES DEPARTMENT(DepartmentID) ON DELETE NO ACTION ON UPDATE CASCADE,
    CONSTRAINT CHK_Room_Capacity CHECK (Capacity > 0),
    CONSTRAINT CHK_Room_AvailabilityStatus CHECK (AvailabilityStatus IN ('Available','Occupied','Under Maintenance','Reserved')),
    CONSTRAINT CHK_Room_FloorNumber CHECK (FloorNumber >= 0)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
CREATE INDEX IX_Room_AvailabilityStatus ON ROOM(AvailabilityStatus);
CREATE INDEX IX_Room_RoomType           ON ROOM(RoomType);

CREATE TABLE EQUIPMENT (
    EquipmentID         INT             NOT NULL AUTO_INCREMENT,
    Name                VARCHAR(30)     NOT NULL,
    PRIMARY KEY (EquipmentID),
    CONSTRAINT UQ_Equipment_Name UNIQUE (Name)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE ROOM_EQUIPMENT (
    RoomID              INT             NOT NULL,
    EquipmentID         INT             NOT NULL,
    PRIMARY KEY (RoomID, EquipmentID),
    CONSTRAINT FK_RoomEquipment_Room FOREIGN KEY (RoomID)
        REFERENCES ROOM(RoomID) ON DELETE CASCADE ON UPDATE CASCADE,
    CONSTRAINT FK_RoomEquipment_Equipment FOREIGN KEY (EquipmentID)
        REFERENCES EQUIPMENT(EquipmentID) ON DELETE CASCADE ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE `LANGUAGE` (
    LanguageID          INT             NOT NULL AUTO_INCREMENT,
    Name                VARCHAR(50)     NOT NULL,
    PRIMARY KEY (LanguageID),
    CONSTRAINT UQ_Language_Name UNIQUE (Name)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE DOCTOR_LANGUAGE (
    DoctorID            INT             NOT NULL,
    LanguageID          INT             NOT NULL,
    PRIMARY KEY (DoctorID, LanguageID),
    CONSTRAINT FK_DoctorLanguage_Doctor FOREIGN KEY (DoctorID)
        REFERENCES DOCTOR(DoctorID) ON DELETE CASCADE ON UPDATE CASCADE,
    CONSTRAINT FK_DoctorLanguage_Language FOREIGN KEY (LanguageID)
        REFERENCES `LANGUAGE`(LanguageID) ON DELETE CASCADE ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- =============================================
-- CLINICAL CLUSTER
-- =============================================

CREATE TABLE PATIENT (
    PatientID           INT             NOT NULL AUTO_INCREMENT,
    PrimaryPhysicianID  INT             NULL,
    FirstName           VARCHAR(50)     NOT NULL,
    LastName            VARCHAR(50)     NOT NULL,
    DOB                 DATE            NOT NULL,
    Gender              CHAR(1)         NULL,
    Phone               VARCHAR(15)     NULL,
    Email               VARCHAR(100)    NULL,
    Address             VARCHAR(200)    NULL,
    BloodType           VARCHAR(3)      NULL,
    EmergencyContact    VARCHAR(100)    NULL,
    MaritalStatus       VARCHAR(20)     NULL,
    PRIMARY KEY (PatientID),
    CONSTRAINT FK_Patient_PrimaryPhysician FOREIGN KEY (PrimaryPhysicianID)
        REFERENCES DOCTOR(DoctorID) ON DELETE SET NULL ON UPDATE CASCADE,
    CONSTRAINT CHK_Patient_Gender CHECK (Gender IN ('M','F','O')),
    CONSTRAINT CHK_Patient_BloodType CHECK (BloodType IN ('A+','A-','B+','B-','AB+','AB-','O+','O-')),
    CONSTRAINT CHK_Patient_Email CHECK (Email LIKE '%_@__%.__%'),
    CONSTRAINT CHK_Patient_MaritalStatus CHECK (MaritalStatus IN ('Single','Married','Divorced','Widowed','Other'))
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
CREATE INDEX IX_Patient_LastName ON PATIENT(LastName);
CREATE INDEX IX_Patient_Email    ON PATIENT(Email);
CREATE INDEX IX_Patient_DOB      ON PATIENT(DOB);

CREATE TABLE APPOINTMENT (
    AppointmentID       INT             NOT NULL AUTO_INCREMENT,
    PatientID           INT             NOT NULL,
    DoctorID            INT             NOT NULL,
    RoomID              INT             NOT NULL,
    `DateTime`          DATETIME        NOT NULL,
    Reason              VARCHAR(200)    NULL,
    Status              VARCHAR(20)     NULL DEFAULT 'Scheduled',
    AppointmentType     VARCHAR(50)     NULL,
    Duration            INT             NULL DEFAULT 30,
    Notes               VARCHAR(200)    NULL,
    PRIMARY KEY (AppointmentID),
    CONSTRAINT FK_Appointment_Patient FOREIGN KEY (PatientID)
        REFERENCES PATIENT(PatientID) ON DELETE CASCADE ON UPDATE CASCADE,
    CONSTRAINT FK_Appointment_Doctor FOREIGN KEY (DoctorID)
        REFERENCES DOCTOR(DoctorID) ON DELETE NO ACTION ON UPDATE NO ACTION,
    CONSTRAINT FK_Appointment_Room FOREIGN KEY (RoomID)
        REFERENCES ROOM(RoomID) ON DELETE NO ACTION ON UPDATE NO ACTION,
    CONSTRAINT CHK_Appointment_Status CHECK (Status IN ('Scheduled','Confirmed','Completed','Cancelled','No-Show')),
    CONSTRAINT CHK_Appointment_Duration CHECK (Duration > 0)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
CREATE INDEX IX_Appointment_DateTime ON APPOINTMENT(`DateTime`);
CREATE INDEX IX_Appointment_Status   ON APPOINTMENT(Status);

CREATE TABLE MEDICAL_RECORD (
    RecordID            INT             NOT NULL AUTO_INCREMENT,
    PatientID           INT             NOT NULL,
    DoctorID            INT             NOT NULL,
    VisitDate           DATE            NOT NULL DEFAULT (CURDATE()),
    Diagnosis           VARCHAR(200)    NULL,
    Notes               VARCHAR(200)    NULL,
    VitalSigns          VARCHAR(200)    NULL,
    TreatmentPlan       VARCHAR(200)    NULL,
    FollowUpRequired    TINYINT(1)      NULL DEFAULT 0,
    RecordType          VARCHAR(50)     NULL,
    PRIMARY KEY (RecordID),
    CONSTRAINT FK_MedicalRecord_Patient FOREIGN KEY (PatientID)
        REFERENCES PATIENT(PatientID) ON DELETE CASCADE ON UPDATE CASCADE,
    CONSTRAINT FK_MedicalRecord_Doctor FOREIGN KEY (DoctorID)
        REFERENCES DOCTOR(DoctorID) ON DELETE NO ACTION ON UPDATE NO ACTION
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
CREATE INDEX IX_MedicalRecord_VisitDate ON MEDICAL_RECORD(VisitDate);

CREATE TABLE LAB_TEST (
    LabTestID           INT             NOT NULL AUTO_INCREMENT,
    AppointmentID       INT             NOT NULL,
    LabTechnicianID     INT             NOT NULL,
    TestType            VARCHAR(100)    NULL,
    Result              VARCHAR(200)    NULL,
    ResultDate          DATE            NULL,
    Status              VARCHAR(20)     NULL DEFAULT 'Pending',
    NormalRange         VARCHAR(50)     NULL,
    Units               VARCHAR(20)     NULL,
    Comments            VARCHAR(200)    NULL,
    PRIMARY KEY (LabTestID),
    CONSTRAINT FK_LabTest_Appointment FOREIGN KEY (AppointmentID)
        REFERENCES APPOINTMENT(AppointmentID) ON DELETE CASCADE ON UPDATE CASCADE,
    CONSTRAINT FK_LabTest_LabTechnician FOREIGN KEY (LabTechnicianID)
        REFERENCES LAB_TECHNICIAN(LabTechnicianID) ON DELETE NO ACTION ON UPDATE NO ACTION,
    CONSTRAINT CHK_LabTest_Status CHECK (Status IN ('Pending','In Progress','Completed','Cancelled'))
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
CREATE INDEX IX_LabTest_Status ON LAB_TEST(Status);

CREATE TABLE PRESCRIPTION (
    PrescriptionID      INT             NOT NULL AUTO_INCREMENT,
    RecordID            INT             NOT NULL,
    DoctorID            INT             NOT NULL,
    DateIssued          DATE            NOT NULL DEFAULT (CURDATE()),
    Notes               VARCHAR(200)    NULL,
    ValidUntil          DATE            NULL,
    PrescriptionType    VARCHAR(50)     NULL,
    RenewalAllowed      TINYINT(1)      NULL DEFAULT 0,
    PRIMARY KEY (PrescriptionID),
    CONSTRAINT FK_Prescription_MedicalRecord FOREIGN KEY (RecordID)
        REFERENCES MEDICAL_RECORD(RecordID) ON DELETE CASCADE ON UPDATE CASCADE,
    CONSTRAINT FK_Prescription_Doctor FOREIGN KEY (DoctorID)
        REFERENCES DOCTOR(DoctorID) ON DELETE NO ACTION ON UPDATE NO ACTION,
    CONSTRAINT CHK_Prescription_ValidUntil CHECK (ValidUntil IS NULL OR ValidUntil >= DateIssued)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
CREATE INDEX IX_Prescription_DateIssued ON PRESCRIPTION(DateIssued);

CREATE TABLE ALLERGY (
    AllergyID           INT             NOT NULL AUTO_INCREMENT,
    Name                VARCHAR(30)     NOT NULL,
    Reaction            VARCHAR(100)    NULL,
    Notes               VARCHAR(200)    NULL,
    FirstIdentified     DATE            NULL,
    LastUpdated         DATE            NULL DEFAULT (CURDATE()),
    PRIMARY KEY (AllergyID),
    CONSTRAINT UQ_Allergy_Name UNIQUE (Name)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE PATIENT_ALLERGY (
    PatientID           INT             NOT NULL,
    AllergyID           INT             NOT NULL,
    PRIMARY KEY (PatientID, AllergyID),
    CONSTRAINT FK_PatientAllergy_Patient FOREIGN KEY (PatientID)
        REFERENCES PATIENT(PatientID) ON DELETE CASCADE ON UPDATE CASCADE,
    CONSTRAINT FK_PatientAllergy_Allergy FOREIGN KEY (AllergyID)
        REFERENCES ALLERGY(AllergyID) ON DELETE CASCADE ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- =============================================
-- FINANCIAL CLUSTER
-- =============================================

CREATE TABLE INSURANCE_PROVIDER (
    ProviderID          INT             NOT NULL AUTO_INCREMENT,
    ProviderName        VARCHAR(50)     NOT NULL,
    ContactNumber       VARCHAR(15)     NULL,
    PRIMARY KEY (ProviderID),
    CONSTRAINT UQ_InsuranceProvider_Name UNIQUE (ProviderName)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE INSURANCE_POLICY (
    PolicyID            INT             NOT NULL AUTO_INCREMENT,
    ProviderID          INT             NOT NULL,
    PolicyNumber        VARCHAR(30)     NOT NULL,
    CoverageDetails     VARCHAR(200)    NULL,
    PlanType            VARCHAR(20)     NULL,
    ValidFrom           DATE            NOT NULL,
    ValidTo             DATE            NULL,
    CopayPercentage     DECIMAL(5,2)    NULL,
    MaxCoverageLimit    DECIMAL(12,2)   NULL,
    PRIMARY KEY (PolicyID),
    CONSTRAINT FK_InsurancePolicy_Provider FOREIGN KEY (ProviderID)
        REFERENCES INSURANCE_PROVIDER(ProviderID) ON DELETE CASCADE ON UPDATE CASCADE,
    CONSTRAINT UQ_InsurancePolicy_PolicyNumber UNIQUE (PolicyNumber),
    CONSTRAINT CHK_InsurancePolicy_ValidDates CHECK (ValidTo IS NULL OR ValidTo >= ValidFrom),
    CONSTRAINT CHK_InsurancePolicy_CopayPercentage CHECK (CopayPercentage >= 0 AND CopayPercentage <= 100),
    CONSTRAINT CHK_InsurancePolicy_MaxCoverageLimit CHECK (MaxCoverageLimit >= 0)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE PATIENT_INSURANCE (
    PatientID           INT             NOT NULL,
    PolicyID            INT             NOT NULL,
    ValidFrom           DATE            NOT NULL,
    ValidTo             DATE            NULL,
    IsPrimary           TINYINT(1)      NULL DEFAULT 0,
    PRIMARY KEY (PatientID, PolicyID),
    CONSTRAINT FK_PatientInsurance_Patient FOREIGN KEY (PatientID)
        REFERENCES PATIENT(PatientID) ON DELETE CASCADE ON UPDATE CASCADE,
    CONSTRAINT FK_PatientInsurance_Policy FOREIGN KEY (PolicyID)
        REFERENCES INSURANCE_POLICY(PolicyID) ON DELETE CASCADE ON UPDATE CASCADE,
    CONSTRAINT CHK_PatientInsurance_ValidDates CHECK (ValidTo IS NULL OR ValidTo >= ValidFrom)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE BILLING (
    BillID              INT             NOT NULL AUTO_INCREMENT,
    PatientID           INT             NOT NULL,
    AppointmentID       INT             NOT NULL,
    Amount              DECIMAL(10,2)   NOT NULL,
    DateIssued          DATE            NOT NULL DEFAULT (CURDATE()),
    Status              VARCHAR(20)     NULL DEFAULT 'Pending',
    DueDate             DATE            NULL,
    DiscountApplied     DECIMAL(5,2)    NULL DEFAULT 0.00,
    TaxAmount           DECIMAL(6,2)    NULL DEFAULT 0.00,
    PaymentTerms        VARCHAR(50)     NULL,
    PRIMARY KEY (BillID),
    CONSTRAINT FK_Billing_Patient FOREIGN KEY (PatientID)
        REFERENCES PATIENT(PatientID) ON DELETE CASCADE ON UPDATE CASCADE,
    CONSTRAINT FK_Billing_Appointment FOREIGN KEY (AppointmentID)
        REFERENCES APPOINTMENT(AppointmentID) ON DELETE NO ACTION ON UPDATE NO ACTION,
    CONSTRAINT CHK_Billing_Amount CHECK (Amount >= 0),
    CONSTRAINT CHK_Billing_Status CHECK (Status IN ('Pending','Paid','Partially Paid','Overdue','Cancelled')),
    CONSTRAINT CHK_Billing_DiscountApplied CHECK (DiscountApplied >= 0 AND DiscountApplied <= 100),
    CONSTRAINT CHK_Billing_TaxAmount CHECK (TaxAmount >= 0)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
CREATE INDEX IX_Billing_Status     ON BILLING(Status);
CREATE INDEX IX_Billing_DateIssued ON BILLING(DateIssued);

CREATE TABLE CLAIM (
    ClaimID             INT             NOT NULL AUTO_INCREMENT,
    BillID              INT             NOT NULL,
    ClaimDate           DATE            NOT NULL DEFAULT (CURDATE()),
    ClaimStatus         VARCHAR(20)     NULL DEFAULT 'Submitted',
    AmountCovered       DECIMAL(10,2)   NULL,
    AmountDenied        DECIMAL(10,2)   NULL,
    ProcessedDate       DATE            NULL,
    AdjusterNotes       VARCHAR(200)    NULL,
    PRIMARY KEY (ClaimID),
    CONSTRAINT FK_Claim_Billing FOREIGN KEY (BillID)
        REFERENCES BILLING(BillID) ON DELETE CASCADE ON UPDATE CASCADE,
    CONSTRAINT CHK_Claim_Status CHECK (ClaimStatus IN ('Submitted','Under Review','Approved','Denied','Partially Approved')),
    CONSTRAINT CHK_Claim_AmountCovered CHECK (AmountCovered >= 0),
    CONSTRAINT CHK_Claim_AmountDenied CHECK (AmountDenied >= 0)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
CREATE INDEX IX_Claim_ClaimStatus ON CLAIM(ClaimStatus);
CREATE INDEX IX_Claim_ClaimDate   ON CLAIM(ClaimDate);

-- =============================================
-- PHARMACY CLUSTER
-- =============================================

CREATE TABLE MEDICINE (
    MedicineID          INT             NOT NULL AUTO_INCREMENT,
    Name                VARCHAR(100)    NOT NULL,
    Description         VARCHAR(200)    NULL,
    Manufacturer        VARCHAR(100)    NULL,
    UnitPrice           DECIMAL(8,2)    NOT NULL,
    StockQuantity       INT             NOT NULL DEFAULT 0,
    ExpiryDate          DATE            NULL,
    Category            VARCHAR(50)     NULL,
    PRIMARY KEY (MedicineID),
    CONSTRAINT UQ_Medicine_Name UNIQUE (Name),
    CONSTRAINT CHK_Medicine_UnitPrice CHECK (UnitPrice >= 0),
    CONSTRAINT CHK_Medicine_StockQuantity CHECK (StockQuantity >= 0)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
CREATE INDEX IX_Medicine_Category   ON MEDICINE(Category);
CREATE INDEX IX_Medicine_ExpiryDate ON MEDICINE(ExpiryDate);

CREATE TABLE STORAGE_REQUIREMENT (
    RequirementID       INT             NOT NULL AUTO_INCREMENT,
    RequirementName     VARCHAR(50)     NOT NULL,
    TemperatureRange    VARCHAR(20)     NULL,
    HumidityRange       VARCHAR(20)     NULL,
    SpecialHandling     VARCHAR(100)    NULL,
    PRIMARY KEY (RequirementID),
    CONSTRAINT UQ_StorageRequirement_Name UNIQUE (RequirementName)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE MEDICINE_STORAGE (
    MedicineID          INT             NOT NULL,
    RequirementID       INT             NOT NULL,
    PRIMARY KEY (MedicineID, RequirementID),
    CONSTRAINT FK_MedicineStorage_Medicine FOREIGN KEY (MedicineID)
        REFERENCES MEDICINE(MedicineID) ON DELETE CASCADE ON UPDATE CASCADE,
    CONSTRAINT FK_MedicineStorage_Requirement FOREIGN KEY (RequirementID)
        REFERENCES STORAGE_REQUIREMENT(RequirementID) ON DELETE CASCADE ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE INVENTORY (
    InventoryID         INT             NOT NULL AUTO_INCREMENT,
    MedicineID          INT             NOT NULL,
    QuantityAvailable   INT             NOT NULL DEFAULT 0,
    ReorderLevel        INT             NULL DEFAULT 10,
    SupplierName        VARCHAR(50)     NULL,
    BatchNumber         VARCHAR(30)     NULL,
    ExpiryDate          DATE            NULL,
    StorageLocation     VARCHAR(50)     NULL,
    PRIMARY KEY (InventoryID),
    CONSTRAINT FK_Inventory_Medicine FOREIGN KEY (MedicineID)
        REFERENCES MEDICINE(MedicineID) ON DELETE CASCADE ON UPDATE CASCADE,
    CONSTRAINT CHK_Inventory_QuantityAvailable CHECK (QuantityAvailable >= 0),
    CONSTRAINT CHK_Inventory_ReorderLevel CHECK (ReorderLevel >= 0)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
CREATE INDEX IX_Inventory_StorageLocation ON INVENTORY(StorageLocation);
CREATE INDEX IX_Inventory_ExpiryDate      ON INVENTORY(ExpiryDate);

CREATE TABLE PRESCRIBED_MEDICINE (
    PrescriptionID      INT             NOT NULL,
    MedicineID          INT             NOT NULL,
    Dosage              VARCHAR(50)     NULL,
    Frequency           VARCHAR(50)     NULL,
    Duration            VARCHAR(50)     NULL,
    Instructions        VARCHAR(200)    NULL,
    StartDate           DATE            NULL,
    EndDate             DATE            NULL,
    PRIMARY KEY (PrescriptionID, MedicineID),
    CONSTRAINT FK_PrescribedMedicine_Prescription FOREIGN KEY (PrescriptionID)
        REFERENCES PRESCRIPTION(PrescriptionID) ON DELETE CASCADE ON UPDATE CASCADE,
    CONSTRAINT FK_PrescribedMedicine_Medicine FOREIGN KEY (MedicineID)
        REFERENCES MEDICINE(MedicineID) ON DELETE NO ACTION ON UPDATE NO ACTION,
    CONSTRAINT CHK_PrescribedMedicine_Dates CHECK (EndDate IS NULL OR EndDate >= StartDate)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- =============================================
-- AUTH: USER_ACCOUNT  (multi-user role-based login)
--   PasswordHash holds a BCrypt hash (~60 chars); never plaintext.
--   A user maps to a STAFF member (staff roles) or a PATIENT (patient portal).
-- =============================================

CREATE TABLE USER_ACCOUNT (
    UserID              INT             NOT NULL AUTO_INCREMENT,
    Username            VARCHAR(50)     NOT NULL,
    Email               VARCHAR(100)    NOT NULL,
    PasswordHash        VARCHAR(255)    NOT NULL,
    Role                VARCHAR(20)     NOT NULL,
    StaffID             INT             NULL,
    PatientID           INT             NULL,
    IsActive            TINYINT(1)      NOT NULL DEFAULT 1,
    LastLogin           DATETIME        NULL,
    CreatedAt           DATETIME        NOT NULL DEFAULT CURRENT_TIMESTAMP,
    PRIMARY KEY (UserID),
    CONSTRAINT UQ_User_Username UNIQUE (Username),
    CONSTRAINT UQ_User_Email    UNIQUE (Email),
    CONSTRAINT FK_User_Staff FOREIGN KEY (StaffID)
        REFERENCES STAFF(StaffID) ON DELETE SET NULL ON UPDATE CASCADE,
    CONSTRAINT FK_User_Patient FOREIGN KEY (PatientID)
        REFERENCES PATIENT(PatientID) ON DELETE SET NULL ON UPDATE CASCADE,
    CONSTRAINT CHK_User_Role CHECK (Role IN ('Admin','Doctor','Nurse','LabTech','Pharmacist','Receptionist','Patient'))
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
CREATE INDEX IX_User_Role ON USER_ACCOUNT(Role);

-- =============================================
-- PHARMACY DISPENSING: MEDICINE_DISPENSE
--   One row per fill event. Written by usp_DispenseMedicine (see 04_procedures.sql),
--   which also decrements INVENTORY / MEDICINE stock inside a transaction.
-- =============================================

CREATE TABLE MEDICINE_DISPENSE (
    DispenseID          INT             NOT NULL AUTO_INCREMENT,
    PrescriptionID      INT             NOT NULL,
    MedicineID          INT             NOT NULL,
    QuantityDispensed   INT             NOT NULL,
    DispensedBy         INT             NULL,
    DispensedAt         DATETIME        NOT NULL DEFAULT CURRENT_TIMESTAMP,
    Notes               VARCHAR(200)    NULL,
    PRIMARY KEY (DispenseID),
    CONSTRAINT FK_Dispense_Prescription FOREIGN KEY (PrescriptionID)
        REFERENCES PRESCRIPTION(PrescriptionID) ON DELETE CASCADE ON UPDATE CASCADE,
    CONSTRAINT FK_Dispense_Medicine FOREIGN KEY (MedicineID)
        REFERENCES MEDICINE(MedicineID) ON DELETE NO ACTION ON UPDATE NO ACTION,
    CONSTRAINT FK_Dispense_User FOREIGN KEY (DispensedBy)
        REFERENCES USER_ACCOUNT(UserID) ON DELETE SET NULL ON UPDATE CASCADE,
    CONSTRAINT CHK_Dispense_Qty CHECK (QuantityDispensed > 0)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
CREATE INDEX IX_Dispense_Prescription ON MEDICINE_DISPENSE(PrescriptionID);
CREATE INDEX IX_Dispense_Medicine     ON MEDICINE_DISPENSE(MedicineID);

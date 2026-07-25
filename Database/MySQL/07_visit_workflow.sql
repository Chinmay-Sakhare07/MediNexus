-- =============================================
-- MediNexus migration 07: the Patient File visit workflow.
-- Adds: richer appointment lifecycle, prescription pipeline statuses,
-- pharmacy billing + payment fields (card surcharge), a medical-record link
-- to appointments, per-line prescription quantities, and doctor schedules +
-- leave. Guarded by SCHEMA_MIGRATION (aborts here if already applied).
-- Requires migrations 05 and 06.
-- =============================================
USE medinexus;

INSERT INTO SCHEMA_MIGRATION (Id) VALUES ('07');

-- 1) Appointment lifecycle: Requested (patient-booked, awaiting approval),
--    CheckedIn (arrived), InConsultation (doctor started).
ALTER TABLE APPOINTMENT DROP CHECK CHK_Appointment_Status;
ALTER TABLE APPOINTMENT ADD CONSTRAINT CHK_Appointment_Status CHECK
    (Status IN ('Requested','Scheduled','Confirmed','CheckedIn',
                'InConsultation','Completed','Cancelled','No-Show'));

-- 2) The File's clinical spine: one medical record per appointment.
ALTER TABLE MEDICAL_RECORD
    ADD COLUMN AppointmentID INT NULL AFTER DoctorID,
    ADD CONSTRAINT FK_MedicalRecord_Appointment FOREIGN KEY (AppointmentID)
        REFERENCES APPOINTMENT(AppointmentID) ON DELETE CASCADE ON UPDATE CASCADE,
    ADD CONSTRAINT UQ_MedicalRecord_Appointment UNIQUE (AppointmentID);

-- 3) Prescription pipeline (doctor -> pharmacy pool -> pickup).
ALTER TABLE PRESCRIPTION
    ADD COLUMN AppointmentID INT NULL AFTER RecordID,
    ADD COLUMN Status VARCHAR(20) NOT NULL DEFAULT 'SentToPharmacy',
    ADD COLUMN RejectReason VARCHAR(200) NULL,
    ADD CONSTRAINT FK_Prescription_Appointment FOREIGN KEY (AppointmentID)
        REFERENCES APPOINTMENT(AppointmentID) ON DELETE CASCADE ON UPDATE CASCADE,
    ADD CONSTRAINT UQ_Prescription_Appointment UNIQUE (AppointmentID),
    ADD CONSTRAINT CHK_Prescription_Status CHECK
        (Status IN ('SentToPharmacy','Confirmed','Ready','Dispensed','Rejected'));

-- Per-line quantity: what the pharmacist dispenses and bills.
ALTER TABLE PRESCRIBED_MEDICINE
    ADD COLUMN Quantity INT NOT NULL DEFAULT 1,
    ADD CONSTRAINT CHK_PrescribedMedicine_Quantity CHECK (Quantity > 0);

-- 4) Billing: two bill types, one payment record on the bill itself.
--    Card payments carry a 2.5% service charge, computed server-side.
ALTER TABLE BILLING
    ADD COLUMN BillType VARCHAR(20) NOT NULL DEFAULT 'Consultation',
    ADD COLUMN PrescriptionID INT NULL,
    ADD COLUMN PaymentMethod VARCHAR(20) NULL,
    ADD COLUMN CardSurcharge DECIMAL(10,2) NOT NULL DEFAULT 0,
    ADD COLUMN PaidAt DATETIME NULL,
    ADD CONSTRAINT FK_Billing_Prescription FOREIGN KEY (PrescriptionID)
        REFERENCES PRESCRIPTION(PrescriptionID) ON DELETE SET NULL ON UPDATE CASCADE,
    ADD CONSTRAINT CHK_Billing_BillType CHECK (BillType IN ('Consultation','Pharmacy')),
    ADD CONSTRAINT CHK_Billing_PaymentMethod CHECK
        (PaymentMethod IS NULL OR PaymentMethod IN ('Cash','Card'));

-- 5) Doctor working pattern: fixed weekly days + hours + slot size.
CREATE TABLE DOCTOR_SCHEDULE (
    DoctorID    INT         NOT NULL,
    WorkDays    VARCHAR(30) NOT NULL DEFAULT 'Mon,Tue,Wed,Thu,Fri,Sat',
    StartTime   TIME        NOT NULL DEFAULT '09:00:00',
    EndTime     TIME        NOT NULL DEFAULT '17:00:00',
    SlotMinutes INT         NOT NULL DEFAULT 30,
    PRIMARY KEY (DoctorID),
    CONSTRAINT FK_DoctorSchedule_Doctor FOREIGN KEY (DoctorID)
        REFERENCES DOCTOR(DoctorID) ON DELETE CASCADE ON UPDATE CASCADE,
    CONSTRAINT CHK_DoctorSchedule_Slot CHECK (SlotMinutes BETWEEN 5 AND 120),
    CONSTRAINT CHK_DoctorSchedule_Hours CHECK (EndTime > StartTime)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- Every existing doctor gets the default pattern; doctors edit their own.
INSERT INTO DOCTOR_SCHEDULE (DoctorID)
SELECT DoctorID FROM DOCTOR;

-- 6) Doctor leave: a leave day cancels that day's active appointments
--    (the API performs the cancellation transactionally when leave is filed).
CREATE TABLE DOCTOR_LEAVE (
    LeaveID     INT          NOT NULL AUTO_INCREMENT,
    DoctorID    INT          NOT NULL,
    LeaveDate   DATE         NOT NULL,
    Reason      VARCHAR(200) NULL,
    CreatedAt   DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP,
    PRIMARY KEY (LeaveID),
    CONSTRAINT FK_DoctorLeave_Doctor FOREIGN KEY (DoctorID)
        REFERENCES DOCTOR(DoctorID) ON DELETE CASCADE ON UPDATE CASCADE,
    CONSTRAINT UQ_DoctorLeave UNIQUE (DoctorID, LeaveDate)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

SELECT 'Migration 07 applied: Patient File workflow enabled' AS Result;

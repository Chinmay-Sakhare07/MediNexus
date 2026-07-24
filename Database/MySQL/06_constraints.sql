-- =============================================
-- MediNexus migration 06: double-booking guards (SCOPE P3).
-- Dedupes any existing collisions (keeps the lowest AppointmentID), then adds
-- unique indexes on (DoctorID, DateTime) and (RoomID, DateTime).
-- Requires migration 05 first. Guarded by SCHEMA_MIGRATION.
-- =============================================
USE medinexus;

INSERT INTO SCHEMA_MIGRATION (Id) VALUES ('06');

-- Remove doctor-slot duplicates (seed data may contain accidental collisions).
DELETE a FROM APPOINTMENT a
INNER JOIN APPOINTMENT b
    ON a.DoctorID = b.DoctorID
   AND a.`DateTime` = b.`DateTime`
   AND a.AppointmentID > b.AppointmentID;

-- Remove room-slot duplicates.
DELETE a FROM APPOINTMENT a
INNER JOIN APPOINTMENT b
    ON a.RoomID = b.RoomID
   AND a.`DateTime` = b.`DateTime`
   AND a.AppointmentID > b.AppointmentID;

CREATE UNIQUE INDEX UX_Appointment_DoctorSlot ON APPOINTMENT (DoctorID, `DateTime`);
CREATE UNIQUE INDEX UX_Appointment_RoomSlot   ON APPOINTMENT (RoomID, `DateTime`);

SELECT 'Migration 06 applied: double-booking is now impossible' AS Result;

-- =============================================
-- MediNexus migration 05: reinterpret stored appointment instants as UTC (D6).
-- Seed DateTimes were authored as IST wall-clock; true UTC = IST - 5:30.
-- Guarded by SCHEMA_MIGRATION so it can NEVER run twice (a second run would
-- shift the data again). Run with the mysql CLI; it aborts on the duplicate
-- key if already applied.
-- =============================================
USE medinexus;

CREATE TABLE IF NOT EXISTS SCHEMA_MIGRATION (
    Id        VARCHAR(10) NOT NULL,
    AppliedAt DATETIME    NOT NULL DEFAULT CURRENT_TIMESTAMP,
    PRIMARY KEY (Id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- Aborts the script here if '05' was already applied (duplicate key).
INSERT INTO SCHEMA_MIGRATION (Id) VALUES ('05');

UPDATE APPOINTMENT
SET `DateTime` = `DateTime` - INTERVAL 330 MINUTE;

-- USER_ACCOUNT.LastLogin is app-written UTC already; CreatedAt defaults to
-- the server's CURRENT_TIMESTAMP which is UTC on Aiven. Nothing else stores
-- instants yet (MEDICINE_DISPENSE is empty until Phase 4).

SELECT 'Migration 05 applied: appointment instants are now UTC' AS Result;

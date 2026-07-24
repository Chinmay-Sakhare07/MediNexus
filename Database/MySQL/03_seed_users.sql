-- =============================================
-- MediNexus - Login accounts (multi-user RBAC)
-- One account per role, linked to a STAFF member or PATIENT.
--
-- PasswordHash is BCrypt (cost 11). All demo accounts share the password:
--        MediNexus@2026
-- >>> CHANGE THESE before any real/public deployment. <<<
--
-- The hash below was generated with bcrypt and verifies against BCrypt.Net
-- in the .NET API (both use the standard $2b$ format).
-- Assumes 01_schema.sql + 02_seed_data.sql have been run.
-- =============================================

USE medinexus;

DELETE FROM USER_ACCOUNT;
ALTER TABLE USER_ACCOUNT AUTO_INCREMENT = 1;

-- All hashes here = BCrypt('MediNexus@2026', cost 11)
SET @pw = '$2b$11$AavagVyDF9ZYjzl0vKNpz.Y.eHSeeDy18qQN2l0nqZuqcdRpgjrjS';

INSERT INTO USER_ACCOUNT (Username, Email, PasswordHash, Role, StaffID, PatientID) VALUES
    ('admin',          'jennifer.obrien@hospital.com',   @pw, 'Admin',        11,   NULL),
    ('dr.sharma',      'rajesh.sharma@hospital.com',      @pw, 'Doctor',       1,    NULL),
    ('nurse.anderson', 'james.anderson@hospital.com',     @pw, 'Nurse',        15,   NULL),
    ('lab.kumar',      'anil.kumar@hospital.com',         @pw, 'LabTech',      6,    NULL),
    ('pharmacist',     'olivia.bennett@hospital.com',     @pw, 'Pharmacist',   21,   NULL),
    ('reception',      'christopher.garcia@hospital.com', @pw, 'Receptionist', 17,   NULL),
    ('patient.shah',   'amit.shah@email.com',             @pw, 'Patient',      NULL, 1);

-- =============================================
-- MediNexus migration 08: revive expired demo insurance coverage.
-- Every seeded policy/assignment ended 2025-12-31, so from Jan 2026 the
-- claim engine correctly found no in-force primary insurance and issued
-- bills with zero coverage ("copay not working"). Extend the demo world.
-- Guarded by SCHEMA_MIGRATION. Requires 05-07.
-- =============================================
USE medinexus;

INSERT INTO SCHEMA_MIGRATION (Id) VALUES ('08');

UPDATE PATIENT_INSURANCE
SET ValidTo = '2027-12-31'
WHERE ValidTo IS NOT NULL AND ValidTo < '2026-07-01';

UPDATE INSURANCE_POLICY
SET ValidTo = '2027-12-31'
WHERE ValidTo IS NOT NULL AND ValidTo < '2026-07-01';

SELECT 'Migration 08 applied: demo insurance coverage extended to 2027-12-31' AS Result;

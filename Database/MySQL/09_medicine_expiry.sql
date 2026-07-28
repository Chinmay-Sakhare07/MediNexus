-- =============================================
-- MediNexus migration 09: push all medicine/batch expirations to 2030-12-31
-- or later, so the demo pharmacy never refuses stock as expired. Guarded by
-- SCHEMA_MIGRATION. Requires 05-08.
-- =============================================
USE medinexus;

INSERT INTO SCHEMA_MIGRATION (Id) VALUES ('09');

UPDATE MEDICINE
SET ExpiryDate = '2030-12-31'
WHERE ExpiryDate IS NOT NULL AND ExpiryDate < '2030-12-31';

UPDATE INVENTORY
SET ExpiryDate = '2030-12-31'
WHERE ExpiryDate IS NOT NULL AND ExpiryDate < '2030-12-31';

SELECT 'Migration 09 applied: all expirations now 2030-12-31 or later' AS Result;

-- Belt and suspenders: if the medicine catalog is unexpectedly EMPTY on this
-- database (partial seed load), this SELECT makes it obvious right here.
SELECT COUNT(*) AS MedicineCount FROM MEDICINE;

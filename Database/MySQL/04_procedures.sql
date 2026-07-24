-- =============================================
-- MediNexus - Pharmacy dispensing logic + helper views (MySQL 8.0)
--   * usp_DispenseMedicine : fills a prescription line, FIFO-decrements
--     INVENTORY (nearest expiry first) and MEDICINE.StockQuantity, and logs
--     the event to MEDICINE_DISPENSE -- all inside one transaction.
--   * vw_InventoryStatus         : stock monitoring for the pharmacy screen
--   * vw_PharmacyPrescriptionQueue: prescribed vs already-dispensed per line
-- Assumes 01/02/03 have been run.
-- =============================================

USE medinexus;

DROP VIEW IF EXISTS vw_InventoryStatus;
DROP VIEW IF EXISTS vw_PharmacyPrescriptionQueue;
DROP PROCEDURE IF EXISTS usp_DispenseMedicine;

-- ---- Stored procedure: dispense a medicine against a prescription --------
DELIMITER $$

CREATE PROCEDURE usp_DispenseMedicine(
    IN  p_PrescriptionID INT,
    IN  p_MedicineID     INT,
    IN  p_Quantity       INT,
    IN  p_UserID         INT,          -- USER_ACCOUNT.UserID of the pharmacist
    IN  p_Notes          VARCHAR(200),
    OUT p_DispenseID     INT,
    OUT p_Message        VARCHAR(255)
)
proc: BEGIN
    DECLARE v_stock     INT DEFAULT 0;
    DECLARE v_linked    INT DEFAULT 0;
    DECLARE v_remaining INT DEFAULT 0;
    DECLARE v_invId     INT;
    DECLARE v_qty       INT;
    DECLARE v_take      INT;
    DECLARE v_done      INT DEFAULT 0;
    DECLARE cur CURSOR FOR
        SELECT InventoryID, QuantityAvailable
        FROM INVENTORY
        WHERE MedicineID = p_MedicineID AND QuantityAvailable > 0
        ORDER BY ExpiryDate ASC, InventoryID ASC;
    DECLARE CONTINUE HANDLER FOR NOT FOUND SET v_done = 1;
    DECLARE EXIT HANDLER FOR SQLEXCEPTION
    BEGIN
        ROLLBACK;
        SET p_DispenseID = NULL;
        SET p_Message = 'ERROR: dispensing failed and was rolled back';
    END;

    SET p_DispenseID = NULL;

    IF p_Quantity IS NULL OR p_Quantity <= 0 THEN
        SET p_Message = 'ERROR: Quantity must be a positive number';
        LEAVE proc;
    END IF;

    -- The medicine must actually be on this prescription
    SELECT COUNT(*) INTO v_linked
    FROM PRESCRIBED_MEDICINE
    WHERE PrescriptionID = p_PrescriptionID AND MedicineID = p_MedicineID;
    IF v_linked = 0 THEN
        SET p_Message = 'ERROR: That medicine is not part of this prescription';
        LEAVE proc;
    END IF;

    START TRANSACTION;

    -- Lock this medicine's inventory rows and total the available stock
    SELECT COALESCE(SUM(QuantityAvailable), 0) INTO v_stock
    FROM INVENTORY WHERE MedicineID = p_MedicineID FOR UPDATE;

    IF v_stock < p_Quantity THEN
        ROLLBACK;
        SET p_Message = CONCAT('ERROR: Insufficient stock (available ', v_stock, ', requested ', p_Quantity, ')');
        LEAVE proc;
    END IF;

    -- FIFO decrement across batches (soonest to expire first)
    SET v_remaining = p_Quantity;
    OPEN cur;
    fifo: LOOP
        FETCH cur INTO v_invId, v_qty;
        IF v_done = 1 OR v_remaining <= 0 THEN
            LEAVE fifo;
        END IF;
        IF v_qty >= v_remaining THEN
            SET v_take = v_remaining;
        ELSE
            SET v_take = v_qty;
        END IF;
        UPDATE INVENTORY SET QuantityAvailable = QuantityAvailable - v_take
        WHERE InventoryID = v_invId;
        SET v_remaining = v_remaining - v_take;
    END LOOP fifo;
    CLOSE cur;

    -- Keep the denormalised MEDICINE.StockQuantity in step
    UPDATE MEDICINE SET StockQuantity = GREATEST(StockQuantity - p_Quantity, 0)
    WHERE MedicineID = p_MedicineID;

    -- Log the dispense event
    INSERT INTO MEDICINE_DISPENSE (PrescriptionID, MedicineID, QuantityDispensed, DispensedBy, Notes)
    VALUES (p_PrescriptionID, p_MedicineID, p_Quantity, p_UserID, p_Notes);
    SET p_DispenseID = LAST_INSERT_ID();

    COMMIT;
    SET p_Message = CONCAT('SUCCESS: Dispensed ', p_Quantity, ' unit(s); dispense #', p_DispenseID);
END proc$$

DELIMITER ;

-- ---- View: pharmacy inventory monitoring ---------------------------------
CREATE VIEW vw_InventoryStatus AS
SELECT
    m.MedicineID,
    m.Name              AS MedicineName,
    m.Category          AS MedicineCategory,
    m.Manufacturer,
    i.InventoryID,
    i.QuantityAvailable AS CurrentStock,
    i.ReorderLevel      AS MinimumStock,
    (i.QuantityAvailable - i.ReorderLevel) AS StockBuffer,
    CASE
        WHEN i.QuantityAvailable = 0                        THEN 'OUT OF STOCK'
        WHEN i.QuantityAvailable <= i.ReorderLevel * 0.5    THEN 'CRITICALLY LOW'
        WHEN i.QuantityAvailable <= i.ReorderLevel          THEN 'LOW STOCK'
        WHEN i.QuantityAvailable <= i.ReorderLevel * 2      THEN 'Adequate Stock'
        ELSE 'Well Stocked'
    END AS StockStatus,
    (i.QuantityAvailable <= i.ReorderLevel) AS ReorderRequired,
    m.UnitPrice,
    CAST(i.QuantityAvailable * m.UnitPrice AS DECIMAL(12,2)) AS InventoryValue,
    i.ExpiryDate,
    DATEDIFF(i.ExpiryDate, CURDATE()) AS DaysUntilExpiry,
    i.StorageLocation,
    i.BatchNumber,
    i.SupplierName
FROM MEDICINE m
JOIN INVENTORY i ON m.MedicineID = i.MedicineID;

-- ---- View: pharmacist prescription queue (prescribed vs dispensed) -------
CREATE VIEW vw_PharmacyPrescriptionQueue AS
SELECT
    p.PrescriptionID,
    p.DateIssued,
    p.PrescriptionType,
    CONCAT(pat.FirstName, ' ', pat.LastName) AS PatientName,
    CONCAT(s.FirstName, ' ', s.LastName)     AS PrescribingDoctor,
    pm.MedicineID,
    m.Name        AS MedicineName,
    pm.Dosage,
    pm.Frequency,
    pm.Duration,
    COALESCE(d.TotalDispensed, 0) AS TotalDispensed
FROM PRESCRIPTION p
JOIN MEDICAL_RECORD mr    ON p.RecordID = mr.RecordID
JOIN PATIENT pat          ON mr.PatientID = pat.PatientID
JOIN DOCTOR doc           ON p.DoctorID = doc.DoctorID
JOIN STAFF s              ON doc.DoctorID = s.StaffID
JOIN PRESCRIBED_MEDICINE pm ON p.PrescriptionID = pm.PrescriptionID
JOIN MEDICINE m           ON pm.MedicineID = m.MedicineID
LEFT JOIN (
    SELECT PrescriptionID, MedicineID, SUM(QuantityDispensed) AS TotalDispensed
    FROM MEDICINE_DISPENSE
    GROUP BY PrescriptionID, MedicineID
) d ON d.PrescriptionID = pm.PrescriptionID AND d.MedicineID = pm.MedicineID;

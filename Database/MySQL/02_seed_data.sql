-- =============================================
-- MediNexus - Sample Data (MySQL 8.0)
-- Ported from the SQL Server DML.
-- Fixes applied during port:
--   * GETDATE() -> CURDATE()
--   * mojibake degree signs (Â°) -> °
--   * INSURANCE_POLICY 'BCBS-PPO-2025-001' had ValidFrom > ValidTo in the
--     original (2026-01-01 .. 2025-12-31) which violates CHK_..._ValidDates;
--     corrected to 2025-01-01 .. 2025-12-31.
-- Assumes 01_schema.sql has been run. Safe to re-run (clears + reseeds).
-- =============================================

USE medinexus;

-- ---- Reset (clear data + restart identities) -----------------------------
SET FOREIGN_KEY_CHECKS = 0;
DELETE FROM MEDICINE_DISPENSE;
DELETE FROM PRESCRIBED_MEDICINE;
DELETE FROM INVENTORY;
DELETE FROM MEDICINE_STORAGE;
DELETE FROM STORAGE_REQUIREMENT;
DELETE FROM MEDICINE;
DELETE FROM CLAIM;
DELETE FROM BILLING;
DELETE FROM PATIENT_INSURANCE;
DELETE FROM INSURANCE_POLICY;
DELETE FROM INSURANCE_PROVIDER;
DELETE FROM PRESCRIPTION;
DELETE FROM PATIENT_ALLERGY;
DELETE FROM ALLERGY;
DELETE FROM LAB_TEST;
DELETE FROM MEDICAL_RECORD;
DELETE FROM APPOINTMENT;
DELETE FROM PATIENT;
DELETE FROM DOCTOR_LANGUAGE;
DELETE FROM `LANGUAGE`;
DELETE FROM ROOM_EQUIPMENT;
DELETE FROM EQUIPMENT;
DELETE FROM ROOM;
DELETE FROM LAB_TECHNICIAN;
DELETE FROM DOCTOR;
DELETE FROM STAFF;
DELETE FROM DEPARTMENT;
ALTER TABLE DEPARTMENT AUTO_INCREMENT = 1;
ALTER TABLE STAFF AUTO_INCREMENT = 1;
ALTER TABLE ROOM AUTO_INCREMENT = 1;
ALTER TABLE EQUIPMENT AUTO_INCREMENT = 1;
ALTER TABLE `LANGUAGE` AUTO_INCREMENT = 1;
ALTER TABLE PATIENT AUTO_INCREMENT = 1;
ALTER TABLE APPOINTMENT AUTO_INCREMENT = 1;
ALTER TABLE MEDICAL_RECORD AUTO_INCREMENT = 1;
ALTER TABLE LAB_TEST AUTO_INCREMENT = 1;
ALTER TABLE PRESCRIPTION AUTO_INCREMENT = 1;
ALTER TABLE ALLERGY AUTO_INCREMENT = 1;
ALTER TABLE INSURANCE_PROVIDER AUTO_INCREMENT = 1;
ALTER TABLE INSURANCE_POLICY AUTO_INCREMENT = 1;
ALTER TABLE BILLING AUTO_INCREMENT = 1;
ALTER TABLE CLAIM AUTO_INCREMENT = 1;
ALTER TABLE MEDICINE AUTO_INCREMENT = 1;
ALTER TABLE STORAGE_REQUIREMENT AUTO_INCREMENT = 1;
ALTER TABLE INVENTORY AUTO_INCREMENT = 1;
SET FOREIGN_KEY_CHECKS = 1;

-- =============================================
-- ADMINISTRATIVE CLUSTER
-- =============================================

INSERT INTO DEPARTMENT (Name, Location, ContactNumber, HeadOfDepartment, OperatingHours) VALUES
    ('Emergency Medicine', 'Building A - Ground Floor', '6175551001', 'Dr. Sarah Johnson', '24/7'),
    ('Cardiology', 'Building B - 3rd Floor', '6175551002', 'Dr. Rajesh Sharma', '8 AM - 8 PM'),
    ('Orthopedics', 'Building C - 2nd Floor', '6175551003', 'Dr. Michael Chen', '7 AM - 7 PM'),
    ('Pediatrics', 'Building A - 1st Floor', '6175551004', 'Dr. Priya Patel', '8 AM - 6 PM'),
    ('Radiology', 'Building D - Ground Floor', '6175551005', 'Dr. James Williams', '24/7'),
    ('Laboratory Services', 'Building D - Basement Level', '6175551006', 'Maria Rodriguez', '6 AM - 10 PM'),
    ('Administration', 'Building E - 4th Floor', '6175551007', 'Jennifer O''Brien', '9 AM - 5 PM'),
    ('Human Resources', 'Building E - 3rd Floor', '6175551008', 'Anjali Deshmukh', '9 AM - 5 PM'),
    ('Pharmacy', 'Building A - Ground Floor', '6175551009', 'Olivia Bennett', '24/7');

INSERT INTO STAFF (DepartmentID, FirstName, LastName, Role, Phone, Email, HireDate, EmploymentType, SalaryBand, EmergencyContact) VALUES
    (1, 'Rajesh', 'Sharma', 'Doctor', '6175552001', 'rajesh.sharma@hospital.com', '2020-03-15', 'Full-Time', 'Senior', 'Meera Sharma - 6175559001'),
    (2, 'Priya', 'Patel', 'Doctor', '6175552002', 'priya.patel@hospital.com', '2019-07-22', 'Full-Time', 'Senior', 'Amit Patel - 6175559002'),
    (1, 'Sarah', 'Johnson', 'Doctor', '6175552003', 'sarah.johnson@hospital.com', '2018-01-10', 'Full-Time', 'Senior', 'Mark Johnson - 6175559003'),
    (3, 'Michael', 'Chen', 'Doctor', '6175552004', 'michael.chen@hospital.com', '2021-05-18', 'Full-Time', 'Mid-Level', 'Lisa Chen - 6175559004'),
    (5, 'Maria', 'Rodriguez', 'Doctor', '6175552005', 'maria.rodriguez@hospital.com', '2019-11-30', 'Full-Time', 'Senior', 'Carlos Rodriguez - 6175559005'),
    (6, 'Anil', 'Kumar', 'Lab Technician', '6175552006', 'anil.kumar@hospital.com', '2020-02-14', 'Full-Time', 'Mid-Level', 'Sunita Kumar - 6175559006'),
    (6, 'Jessica', 'Williams', 'Lab Technician', '6175552007', 'jessica.williams@hospital.com', '2021-06-10', 'Full-Time', 'Mid-Level', 'David Williams - 6175559007'),
    (6, 'Deepak', 'Singh', 'Lab Technician', '6175552008', 'deepak.singh@hospital.com', '2019-09-05', 'Full-Time', 'Senior', 'Kavita Singh - 6175559008'),
    (6, 'Emily', 'Martinez', 'Lab Technician', '6175552009', 'emily.martinez@hospital.com', '2022-01-20', 'Full-Time', 'Junior', 'Jose Martinez - 6175559009'),
    (6, 'Vikram', 'Reddy', 'Lab Technician', '6175552010', 'vikram.reddy@hospital.com', '2020-08-12', 'Full-Time', 'Mid-Level', 'Lakshmi Reddy - 6175559010'),
    (7, 'Jennifer', 'O''Brien', 'Administrator', '6175552011', 'jennifer.obrien@hospital.com', '2017-04-01', 'Full-Time', 'Senior', 'Patrick O''Brien - 6175559011'),
    (8, 'Anjali', 'Deshmukh', 'HR Manager', '6175552012', 'anjali.deshmukh@hospital.com', '2018-08-15', 'Full-Time', 'Senior', 'Rahul Deshmukh - 6175559012'),
    (7, 'Robert', 'Davis', 'Operations Manager', '6175552013', 'robert.davis@hospital.com', '2019-03-20', 'Full-Time', 'Mid-Level', 'Amanda Davis - 6175559013'),
    (8, 'Neha', 'Gupta', 'HR Coordinator', '6175552014', 'neha.gupta@hospital.com', '2021-10-05', 'Full-Time', 'Junior', 'Arjun Gupta - 6175559014'),
    (1, 'James', 'Anderson', 'Nurse', '6175552015', 'james.anderson@hospital.com', '2020-12-01', 'Full-Time', 'Mid-Level', 'Sarah Anderson - 6175559015'),
    (4, 'Sneha', 'Iyer', 'Nurse', '6175552016', 'sneha.iyer@hospital.com', '2021-07-18', 'Full-Time', 'Mid-Level', 'Karthik Iyer - 6175559016'),
    (7, 'Christopher', 'Garcia', 'Receptionist', '6175552017', 'christopher.garcia@hospital.com', '2022-02-10', 'Full-Time', 'Junior', 'Isabella Garcia - 6175559017'),
    (3, 'Pooja', 'Mehta', 'Physiotherapist', '6175552018', 'pooja.mehta@hospital.com', '2020-05-25', 'Full-Time', 'Mid-Level', 'Nikhil Mehta - 6175559018'),
    (7, 'Thomas', 'Murphy', 'Billing Coordinator', '6175552019', 'thomas.murphy@hospital.com', '2019-11-12', 'Full-Time', 'Mid-Level', 'Mary Murphy - 6175559019'),
    (5, 'Ravi', 'Nair', 'Radiologist Technician', '6175552020', 'ravi.nair@hospital.com', '2021-09-01', 'Full-Time', 'Mid-Level', 'Divya Nair - 6175559020'),
    (9, 'Olivia', 'Bennett', 'Pharmacist', '6175552021', 'olivia.bennett@hospital.com', '2021-04-12', 'Full-Time', 'Senior', 'Daniel Bennett - 6175559021');

INSERT INTO DOCTOR (DoctorID, Specialization, LicenseNumber, AvailabilityStatus, YearsOfExperience, ConsultationFee, LanguagesSpoken) VALUES
    (1, 'Emergency Medicine', 'MA-EM-45678', 'Available', 12, 250.00, 'English, Hindi, Marathi'),
    (2, 'Cardiology', 'MA-CARD-56789', 'Available', 15, 350.00, 'English, Gujarati, Hindi'),
    (3, 'Emergency Medicine', 'MA-EM-67890', 'Available', 18, 300.00, 'English, Spanish'),
    (4, 'Orthopedics', 'MA-ORTH-78901', 'Available', 8, 280.00, 'English, Mandarin'),
    (5, 'Radiology', 'MA-RAD-89012', 'On Leave', 10, 320.00, 'English, Spanish');

INSERT INTO LAB_TECHNICIAN (LabTechnicianID, Certification, ShiftHours, SkillLevel, AssignedLab) VALUES
    (6, 'Medical Laboratory Scientist (MLS)', '7 AM - 3 PM', 'Mid-Level', 'Hematology Lab'),
    (7, 'Clinical Laboratory Technician (CLT)', '3 PM - 11 PM', 'Mid-Level', 'Chemistry Lab'),
    (8, 'Medical Laboratory Scientist (MLS)', '7 AM - 3 PM', 'Senior', 'Microbiology Lab'),
    (9, 'Phlebotomy Technician', '11 PM - 7 AM', 'Junior', 'Blood Collection'),
    (10, 'Medical Laboratory Scientist (MLS)', '3 PM - 11 PM', 'Mid-Level', 'Immunology Lab');

INSERT INTO ROOM (DepartmentID, RoomType, Capacity, AvailabilityStatus, FloorNumber, EquipmentAvailable) VALUES
    (1, 'Emergency Room', 2, 'Available', 0, 'Defibrillator, Oxygen Tank, IV Stand'),
    (1, 'Emergency Room', 2, 'Occupied', 0, 'Defibrillator, Oxygen Tank, IV Stand'),
    (2, 'Consultation Room', 1, 'Available', 3, 'ECG Machine, Blood Pressure Monitor'),
    (2, 'Cardiac Care Unit', 4, 'Available', 3, 'ECG Monitor, Ventilator, Infusion Pump'),
    (3, 'Examination Room', 1, 'Available', 2, 'X-Ray Viewer, Examination Table'),
    (3, 'Operating Theater', 1, 'Under Maintenance', 2, 'Surgical Instruments, Anesthesia Machine'),
    (4, 'Pediatric Ward', 6, 'Available', 1, 'Pediatric Beds, Oxygen, Toys'),
    (4, 'Consultation Room', 1, 'Available', 1, 'Weighing Scale, Stethoscope'),
    (5, 'Radiology Suite', 1, 'Available', 0, 'CT Scanner, X-Ray Machine'),
    (5, 'MRI Room', 1, 'Available', 0, 'MRI Machine, Lead Shielding'),
    (6, 'Laboratory', 4, 'Available', 0, 'Microscope, Centrifuge, Incubator'),
    (6, 'Sample Collection', 2, 'Available', 0, 'Phlebotomy Chairs, Centrifuge');

INSERT INTO EQUIPMENT (Name) VALUES
    ('Defibrillator'), ('ECG Machine'), ('X-Ray Machine'), ('CT Scanner'), ('MRI Machine'),
    ('Ultrasound Machine'), ('Ventilator'), ('Infusion Pump'), ('Blood Pressure Monitor'),
    ('Pulse Oximeter'), ('Surgical Table'), ('Anesthesia Machine'), ('Microscope'),
    ('Centrifuge'), ('Autoclave'), ('Incubator'), ('Oxygen Concentrator'), ('Wheelchair'),
    ('Patient Monitor'), ('Glucometer');

INSERT INTO ROOM_EQUIPMENT (RoomID, EquipmentID) VALUES
    (1, 1), (1, 9), (1, 8), (1, 17),
    (2, 1), (2, 9), (2, 8), (2, 17),
    (3, 2), (3, 9), (3, 10),
    (5, 9), (5, 10),
    (8, 9), (8, 10), (8, 20),
    (4, 2), (4, 19), (4, 7), (4, 8),
    (6, 11), (6, 12),
    (9, 3), (9, 4),
    (10, 5),
    (11, 13), (11, 14), (11, 15), (11, 16),
    (12, 14), (12, 10);

INSERT INTO `LANGUAGE` (Name) VALUES
    ('English'), ('Hindi'), ('Marathi'), ('Spanish'), ('Mandarin');

INSERT INTO DOCTOR_LANGUAGE (DoctorID, LanguageID) VALUES
    (1, 1), (1, 2), (1, 3),
    (2, 1), (2, 2),
    (3, 1), (3, 4),
    (4, 1), (4, 5),
    (5, 1), (5, 4);

-- =============================================
-- CLINICAL CLUSTER
-- =============================================

INSERT INTO PATIENT (PrimaryPhysicianID, FirstName, LastName, DOB, Gender, Phone, Email, Address, BloodType, EmergencyContact, MaritalStatus) VALUES
    (1, 'Amit', 'Shah', '1985-06-15', 'M', '6175553001', 'amit.shah@email.com', '123 Main St, Boston MA 02108', 'O+', 'Nisha Shah - 6175558001', 'Married'),
    (2, 'Kavya', 'Menon', '1992-09-20', 'F', '6175553002', 'kavya.menon@email.com', '456 Oak Ave, Cambridge MA 02139', 'A+', 'Rahul Menon - 6175558002', 'Single'),
    (1, 'Rohan', 'Kapoor', '1978-03-10', 'M', '6175553003', 'rohan.kapoor@email.com', '789 Elm St, Somerville MA 02143', 'B+', 'Anjali Kapoor - 6175558003', 'Married'),
    (3, 'Aditi', 'Rao', '1988-11-25', 'F', '6175553004', 'aditi.rao@email.com', '321 Pine Rd, Brookline MA 02445', 'AB+', 'Sanjay Rao - 6175558004', 'Married'),
    (2, 'Arjun', 'Verma', '1995-07-08', 'M', '6175553005', 'arjun.verma@email.com', '654 Maple Dr, Newton MA 02458', 'O-', 'Priya Verma - 6175558005', 'Single'),
    (4, 'Ishaan', 'Joshi', '2010-02-14', 'M', '6175553006', 'ishaan.joshi@email.com', '987 Cedar Ln, Waltham MA 02451', 'A+', 'Meera Joshi - 6175558006', 'Single'),
    (1, 'Ananya', 'Kulkarni', '2015-12-05', 'F', '6175553007', 'ananya.kulkarni@email.com', '147 Birch St, Quincy MA 02169', 'B-', 'Vivek Kulkarni - 6175558007', 'Single'),
    (3, 'Siddharth', 'Bose', '1980-08-30', 'M', '6175553008', 'siddharth.bose@email.com', '258 Willow Ave, Medford MA 02155', 'AB-', 'Rina Bose - 6175558008', 'Divorced'),
    (2, 'William', 'Thompson', '1975-04-18', 'M', '6175553009', 'william.thompson@email.com', '369 Spruce Ct, Arlington MA 02474', 'O+', 'Linda Thompson - 6175558009', 'Married'),
    (4, 'Elizabeth', 'Harris', '1990-10-22', 'F', '6175553010', 'elizabeth.harris@email.com', '741 Ashmore Rd, Belmont MA 02478', 'A-', 'John Harris - 6175558010', 'Single'),
    (3, 'Benjamin', 'Walker', '1968-01-12', 'M', '6175553011', 'benjamin.walker@email.com', '852 Highland Ave, Watertown MA 02472', 'B+', 'Susan Walker - 6175558011', 'Widowed'),
    (1, 'Emma', 'Lewis', '2008-05-28', 'F', '6175553012', 'emma.lewis@email.com', '963 Park St, Lexington MA 02420', 'O+', 'Robert Lewis - 6175558012', 'Single'),
    (2, 'Miguel', 'Fernandez', '1982-07-14', 'M', '6175553013', 'miguel.fernandez@email.com', '159 Valley Rd, Malden MA 02148', 'A+', 'Sofia Fernandez - 6175558013', 'Married'),
    (4, 'Isabella', 'D''Angelo', '1993-03-09', 'F', '6175553014', 'isabella.dangelo@email.com', '357 River St, Revere MA 02151', 'B+', 'Marco D''Angelo - 6175558014', 'Single'),
    (1, 'Patrick', 'O''Connor', '1987-11-17', 'M', '6175553015', 'patrick.oconnor@email.com', '753 Beach Blvd, Chelsea MA 02150', 'AB+', 'Bridget O''Connor - 6175558015', 'Married');

INSERT INTO ALLERGY (Name, Reaction, Notes, FirstIdentified, LastUpdated) VALUES
    ('Penicillin', 'Anaphylaxis, Rash', 'Common antibiotic allergy', '2020-01-15', CURDATE()),
    ('Peanuts', 'Anaphylaxis, Swelling', 'Severe food allergy', '2020-02-20', CURDATE()),
    ('Shellfish', 'Hives, Vomiting', 'Seafood allergy', '2020-03-10', CURDATE()),
    ('Latex', 'Contact Dermatitis', 'Common in healthcare settings', '2020-04-05', CURDATE()),
    ('Sulfa Drugs', 'Rash, Fever', 'Antibiotic class allergy', '2020-05-12', CURDATE()),
    ('Aspirin', 'Stomach Pain, Bleeding', 'NSAID sensitivity', '2020-06-18', CURDATE()),
    ('Dust Mites', 'Sneezing, Congestion', 'Environmental allergy', '2020-07-22', CURDATE()),
    ('Pollen', 'Hay Fever, Watery Eyes', 'Seasonal allergy', '2020-08-30', CURDATE()),
    ('Bee Stings', 'Anaphylaxis, Swelling', 'Insect venom allergy', '2020-09-14', CURDATE()),
    ('Eggs', 'Hives, Digestive Issues', 'Food allergy', '2020-10-08', CURDATE()),
    ('Milk', 'Diarrhea, Bloating', 'Lactose intolerance', '2020-11-25', CURDATE()),
    ('Soy', 'Rash, Itching', 'Legume allergy', '2021-01-10', CURDATE()),
    ('Wheat', 'Digestive Issues', 'Gluten sensitivity', '2021-02-16', CURDATE()),
    ('Iodine', 'Rash, Anaphylaxis', 'Contrast dye allergy', '2021-03-20', CURDATE()),
    ('Codeine', 'Nausea, Dizziness', 'Opioid sensitivity', '2021-04-14', CURDATE()),
    ('Adhesive Tape', 'Contact Dermatitis', 'Medical supply allergy', '2021-05-28', CURDATE()),
    ('Cat Dander', 'Sneezing, Asthma', 'Pet allergy', '2021-06-30', CURDATE()),
    ('Mold', 'Respiratory Issues', 'Environmental allergy', '2021-07-19', CURDATE()),
    ('Nickel', 'Skin Irritation', 'Metal allergy', '2021-08-25', CURDATE()),
    ('Chlorhexidine', 'Anaphylaxis', 'Antiseptic allergy', '2021-09-12', CURDATE());

INSERT INTO PATIENT_ALLERGY (PatientID, AllergyID) VALUES
    (1, 1), (1, 7), (1, 8),
    (2, 2), (2, 11),
    (3, 4), (3, 6),
    (4, 3), (4, 14),
    (5, 5), (5, 15),
    (6, 10), (6, 13),
    (7, 2), (7, 12),
    (8, 1), (8, 9),
    (9, 6), (9, 17),
    (10, 3), (10, 18),
    (11, 7), (11, 8), (11, 19),
    (12, 2), (12, 10),
    (13, 11), (13, 13),
    (14, 1), (14, 16),
    (15, 4), (15, 20);

INSERT INTO APPOINTMENT (PatientID, DoctorID, RoomID, `DateTime`, Reason, Status, AppointmentType, Duration, Notes) VALUES
    (1, 1, 1, '2025-11-01 09:00:00', 'Chest Pain', 'Completed', 'Emergency', 45, 'ECG performed, stable condition'),
    (2, 2, 3, '2025-11-02 10:30:00', 'Annual Cardiac Checkup', 'Completed', 'Follow-up', 30, 'Heart healthy, continue medication'),
    (3, 1, 2, '2025-11-03 14:00:00', 'Severe Headache', 'Completed', 'Emergency', 60, 'CT scan ordered'),
    (4, 3, 5, '2025-11-05 11:00:00', 'Knee Pain', 'Completed', 'Consultation', 30, 'X-ray shows mild arthritis'),
    (5, 2, 3, '2025-11-06 15:30:00', 'High Blood Pressure', 'Completed', 'Consultation', 30, 'Medication adjusted'),
    (6, 4, 8, '2025-11-07 09:30:00', 'Vaccination', 'Completed', 'Vaccination', 15, 'MMR vaccine administered'),
    (7, 4, 8, '2025-11-08 10:00:00', 'Fever and Cough', 'Completed', 'Sick Visit', 20, 'Prescribed antibiotics'),
    (8, 1, 1, '2025-11-10 16:00:00', 'Difficulty Breathing', 'Completed', 'Emergency', 90, 'Asthma attack managed'),
    (9, 2, 3, '2025-11-11 08:30:00', 'Cholesterol Screening', 'Completed', 'Preventive', 30, 'Lab tests ordered'),
    (10, 3, 5, '2025-11-12 13:00:00', 'Back Pain', 'Completed', 'Consultation', 30, 'Physical therapy recommended'),
    (11, 4, 5, '2025-11-13 14:30:00', 'Sports Injury', 'Completed', 'Consultation', 45, 'Sprained ankle, ice and rest'),
    (12, 4, 8, '2025-11-14 11:30:00', 'Well Child Visit', 'Completed', 'Preventive', 30, 'Growth and development normal'),
    (13, 2, 3, '2025-11-15 09:00:00', 'Chest Tightness', 'Completed', 'Consultation', 30, 'Stress-related, no cardiac issues'),
    (14, 4, 8, '2025-11-16 10:30:00', 'Skin Rash', 'Completed', 'Consultation', 20, 'Allergic reaction, prescribed cream'),
    (15, 1, 1, '2025-11-17 17:00:00', 'Accident Injury', 'Completed', 'Emergency', 120, 'Laceration sutured'),
    (1, 2, 3, '2025-11-18 14:00:00', 'Follow-up ECG', 'Completed', 'Follow-up', 20, 'ECG normal'),
    (3, 3, 5, '2025-11-19 11:00:00', 'Persistent Headache Follow-up', 'Completed', 'Follow-up', 30, 'CT scan normal, tension headache'),
    (5, 2, 3, '2025-11-20 08:30:00', 'Blood Pressure Check', 'Completed', 'Follow-up', 15, 'BP improving with medication'),
    (7, 4, 8, '2025-11-21 09:00:00', 'Cough Follow-up', 'Completed', 'Follow-up', 15, 'Recovering well'),
    (9, 2, 3, '2025-11-22 10:00:00', 'Cholesterol Results Review', 'Completed', 'Follow-up', 20, 'Cholesterol slightly elevated'),
    (2, 2, 3, '2025-11-23 09:30:00', 'Heart Palpitations', 'Completed', 'Consultation', 30, 'Holter monitor ordered'),
    (4, 3, 5, '2025-11-24 14:00:00', 'Joint Pain', 'Completed', 'Consultation', 30, 'Rheumatoid factor test ordered'),
    (6, 4, 8, '2025-11-25 10:30:00', 'Ear Infection', 'Scheduled', 'Sick Visit', 20, 'Appointment today'),
    (8, 1, 1, '2025-11-25 16:30:00', 'Abdominal Pain', 'Confirmed', 'Emergency', 45, 'Coming in this evening'),
    (10, 3, 5, '2025-11-26 11:00:00', 'Physical Therapy Session', 'Scheduled', 'Therapy', 60, 'Second session'),
    (11, 4, 5, '2025-11-27 13:30:00', 'Ankle Follow-up', 'Scheduled', 'Follow-up', 20, 'Check healing progress'),
    (12, 4, 8, '2025-11-28 09:00:00', 'Annual Checkup', 'Scheduled', 'Preventive', 30, 'Yearly wellness visit'),
    (13, 2, 3, '2025-11-29 15:00:00', 'Stress Test', 'Scheduled', 'Diagnostic', 90, 'Cardiac stress test'),
    (14, 3, 5, '2025-11-30 10:00:00', 'Skin Follow-up', 'Scheduled', 'Follow-up', 15, 'Check rash improvement'),
    (15, 1, 2, '2025-12-02 08:30:00', 'Wound Check', 'Scheduled', 'Follow-up', 20, 'Suture removal'),
    (1, 2, 3, '2025-12-05 14:00:00', '3-Month Cardiac Follow-up', 'Scheduled', 'Follow-up', 30, 'Routine monitoring'),
    (3, 1, 1, '2025-12-07 09:00:00', 'Headache Management', 'Scheduled', 'Consultation', 30, 'Discuss treatment options'),
    (5, 2, 3, '2025-12-10 11:30:00', 'Blood Pressure Monitoring', 'Scheduled', 'Follow-up', 20, 'Monthly check'),
    (7, 4, 8, '2025-12-12 10:00:00', 'Flu Vaccination', 'Scheduled', 'Vaccination', 15, 'Annual flu shot'),
    (9, 2, 3, '2025-12-15 13:00:00', 'Lipid Panel Review', 'Scheduled', 'Follow-up', 20, 'Discuss diet changes'),
    (2, 2, 3, '2025-12-18 09:30:00', 'Holter Monitor Results', 'Scheduled', 'Follow-up', 30, 'Review 24-hr monitoring'),
    (4, 3, 5, '2025-12-20 14:30:00', 'Lab Results Discussion', 'Scheduled', 'Follow-up', 30, 'Rheumatoid panel results'),
    (6, 4, 8, '2025-12-22 11:00:00', 'Ear Infection Follow-up', 'Scheduled', 'Follow-up', 15, 'Check ear healing'),
    (8, 1, 1, '2025-12-26 16:00:00', 'Breathing Assessment', 'Scheduled', 'Follow-up', 30, 'Post-asthma attack check'),
    (10, 3, 5, '2025-12-28 10:30:00', 'Back Pain Review', 'Scheduled', 'Follow-up', 30, 'Evaluate PT progress'),
    (1, 2, 3, '2026-01-05 09:00:00', 'New Year Health Assessment', 'Scheduled', 'Preventive', 45, 'Comprehensive check'),
    (11, 4, 5, '2026-01-08 13:00:00', 'Sports Clearance', 'Scheduled', 'Consultation', 30, 'Return to sports evaluation'),
    (12, 4, 8, '2026-01-10 10:30:00', 'Growth Check', 'Scheduled', 'Preventive', 20, '6-month growth assessment'),
    (13, 2, 3, '2026-01-12 14:00:00', 'Cardiac Risk Assessment', 'Scheduled', 'Preventive', 45, 'Comprehensive cardiac eval'),
    (14, 4, 8, '2026-01-15 09:30:00', 'Allergy Testing', 'Scheduled', 'Diagnostic', 60, 'Skin prick test'),
    (15, 1, 2, '2026-01-18 11:00:00', 'Accident Injury Final Check', 'Scheduled', 'Follow-up', 20, 'Confirm healing complete'),
    (3, 3, 5, '2026-01-20 15:00:00', 'Headache Resolution Check', 'Scheduled', 'Follow-up', 30, 'Final follow-up'),
    (5, 2, 3, '2026-01-25 08:30:00', 'Hypertension Review', 'Scheduled', 'Follow-up', 30, 'Quarterly monitoring'),
    (7, 4, 8, '2026-01-28 10:00:00', 'School Physical', 'Scheduled', 'Preventive', 30, 'Required for school');

INSERT INTO MEDICAL_RECORD (PatientID, DoctorID, VisitDate, Diagnosis, Notes, VitalSigns, TreatmentPlan, FollowUpRequired, RecordType) VALUES
    (1, 1, '2025-11-01', 'Acute Chest Pain - Non-Cardiac', 'Patient presented with chest pain. ECG normal. Likely musculoskeletal.', 'BP: 128/82, HR: 78, Temp: 98.6°F, SpO2: 98%', 'Rest, NSAIDs, follow-up in 2 weeks', 1, 'Emergency Visit'),
    (2, 2, '2025-11-02', 'Stable Coronary Artery Disease', 'Annual checkup. No new symptoms. Medication compliance good.', 'BP: 118/76, HR: 72, Temp: 98.4°F', 'Continue current medications', 1, 'Routine Follow-up'),
    (3, 1, '2025-11-03', 'Tension Headache', 'Severe headache for 3 days. CT scan ordered to rule out serious causes.', 'BP: 135/88, HR: 82, Temp: 98.7°F', 'Pain management, CT scan, stress reduction', 1, 'Emergency Visit'),
    (4, 3, '2025-11-05', 'Osteoarthritis - Knee', 'Chronic knee pain. X-ray shows mild degenerative changes.', 'BP: 122/80, HR: 76, Temp: 98.5°F', 'Physical therapy, NSAIDs, weight management', 1, 'Consultation'),
    (5, 2, '2025-11-06', 'Essential Hypertension', 'Blood pressure elevated. Medication adjustment needed.', 'BP: 152/94, HR: 84, Temp: 98.6°F', 'Increase Lisinopril dose, dietary counseling', 1, 'Consultation'),
    (6, 4, '2025-11-07', 'Routine Vaccination', 'MMR vaccine administered. No adverse reactions.', 'Temp: 98.4°F, Weight: 35 kg, Height: 120 cm', 'No follow-up needed unless reactions occur', 0, 'Vaccination'),
    (7, 4, '2025-11-08', 'Acute Bronchitis', 'Productive cough, fever. Lung auscultation shows mild wheezing.', 'BP: 105/68, HR: 92, Temp: 101.2°F, SpO2: 96%', 'Antibiotics, increase fluids, rest', 1, 'Sick Visit'),
    (8, 1, '2025-11-10', 'Acute Asthma Exacerbation', 'Difficulty breathing, wheezing. Nebulizer treatment given.', 'BP: 142/90, HR: 110, Temp: 98.8°F, SpO2: 92%', 'Albuterol inhaler, oral steroids, pulmonology referral', 1, 'Emergency Visit'),
    (9, 2, '2025-11-11', 'Hyperlipidemia', 'Cholesterol screening ordered. Patient asymptomatic.', 'BP: 124/78, HR: 68, Weight: 82 kg', 'Await lab results, dietary counseling', 1, 'Preventive Care'),
    (10, 3, '2025-11-12', 'Chronic Lower Back Pain', 'Back pain for 6 months. Physical exam shows muscle spasm.', 'BP: 130/84, HR: 74, Temp: 98.5°F', 'Physical therapy, muscle relaxants, ergonomic counseling', 1, 'Consultation'),
    (11, 4, '2025-11-13', 'Ankle Sprain - Grade II', 'Twisted ankle playing basketball. Moderate swelling and pain.', 'BP: 118/72, HR: 80, Temp: 98.6°F', 'RICE protocol, aircast, follow-up in 2 weeks', 1, 'Consultation'),
    (12, 4, '2025-11-14', 'Well Child Visit', 'Annual checkup. Growth and development appropriate for age.', 'BP: 95/60, HR: 85, Temp: 98.4°F, Weight: 28 kg, Height: 135 cm', 'Continue routine care, next visit in 1 year', 0, 'Preventive Care'),
    (13, 2, '2025-11-15', 'Anxiety-Related Chest Tightness', 'Chest tightness with stress. Cardiac workup negative.', 'BP: 136/86, HR: 88, Temp: 98.5°F, SpO2: 99%', 'Stress management, consider counseling', 0, 'Consultation'),
    (14, 4, '2025-11-16', 'Contact Dermatitis', 'Rash after using new soap. Allergic reaction suspected.', 'BP: 112/70, HR: 72, Temp: 98.6°F', 'Topical steroid cream, avoid allergen', 1, 'Consultation'),
    (15, 1, '2025-11-17', 'Laceration - Left Forearm', 'Fall resulted in 5cm laceration. Wound cleaned and sutured.', 'BP: 128/80, HR: 86, Temp: 98.7°F', 'Wound care, antibiotics, suture removal in 10 days', 1, 'Emergency Visit');

INSERT INTO LAB_TEST (AppointmentID, LabTechnicianID, TestType, Result, ResultDate, Status, NormalRange, Units, Comments) VALUES
    (1, 6, 'Troponin-I', '0.02', '2025-11-01', 'Completed', '0-0.04', 'ng/mL', 'Normal, cardiac event ruled out'),
    (2, 8, 'Lipid Panel', 'Total: 185, LDL: 110, HDL: 55, Trig: 120', '2025-11-02', 'Completed', 'Total <200, LDL <100', 'mg/dL', 'LDL slightly elevated'),
    (3, 6, 'CT Head without contrast', 'No acute intracranial abnormality', '2025-11-03', 'Completed', 'N/A', 'N/A', 'Normal brain CT'),
    (4, 7, 'X-Ray Knee', 'Mild joint space narrowing', '2025-11-05', 'Completed', 'N/A', 'N/A', 'Consistent with early OA'),
    (5, 8, 'Basic Metabolic Panel', 'All values within normal limits', '2025-11-06', 'Completed', 'See reference ranges', 'Various', 'Normal kidney function'),
    (7, 9, 'Rapid Strep Test', 'Negative', '2025-11-08', 'Completed', 'Negative', 'N/A', 'Viral infection likely'),
    (8, 6, 'Arterial Blood Gas', 'pH: 7.38, pO2: 85, pCO2: 42', '2025-11-10', 'Completed', 'pH: 7.35-7.45', 'mmHg', 'Mild hypoxia during exacerbation'),
    (9, 8, 'Complete Lipid Panel', 'Total: 245, LDL: 165, HDL: 42, Trig: 190', '2025-11-12', 'Completed', 'Total <200, LDL <100', 'mg/dL', 'Significantly elevated cholesterol'),
    (10, 7, 'X-Ray Lumbar Spine', 'Mild degenerative changes L4-L5', '2025-11-12', 'Completed', 'N/A', 'N/A', 'Age-appropriate changes'),
    (11, 9, 'X-Ray Ankle', 'No fracture, soft tissue swelling', '2025-11-13', 'Completed', 'N/A', 'N/A', 'Sprain confirmed, no bony injury'),
    (13, 6, 'ECG 12-Lead', 'Normal sinus rhythm', '2025-11-15', 'Completed', 'N/A', 'N/A', 'No cardiac abnormalities'),
    (14, 10, 'Allergy Skin Test Panel', 'Positive for nickel, latex', '2025-11-17', 'Completed', 'Negative', 'N/A', 'Contact allergens identified'),
    (15, 9, 'Complete Blood Count', 'WBC: 8.5, RBC: 4.8, Hgb: 14.2, Plt: 220', '2025-11-17', 'Completed', 'WBC: 4-11', 'K/uL', 'Normal CBC'),
    (16, 6, 'ECG Follow-up', 'Normal sinus rhythm, no changes', '2025-11-18', 'Completed', 'N/A', 'N/A', 'Stable cardiac status'),
    (17, 8, 'CT Head Follow-up', 'No new findings, unchanged', '2025-11-19', 'Completed', 'N/A', 'N/A', 'Confirms benign headache'),
    (20, 8, 'Fasting Lipid Panel', 'Total: 238, LDL: 158, HDL: 45, Trig: 175', '2025-11-23', 'Completed', 'Total <200', 'mg/dL', 'Remains elevated'),
    (21, 6, 'Holter Monitor 24hr', 'Occasional PVCs, no significant arrhythmia', '2025-11-24', 'Completed', 'N/A', 'N/A', 'Benign palpitations'),
    (22, 10, 'Rheumatoid Factor', 'RF: 42', '2025-11-25', 'Completed', '<20', 'IU/mL', 'Elevated, suggests RA');

INSERT INTO PRESCRIPTION (RecordID, DoctorID, DateIssued, Notes, ValidUntil, PrescriptionType, RenewalAllowed) VALUES
    (1, 1, '2025-11-01', 'For chest pain management', '2025-12-01', 'Acute', 0),
    (2, 2, '2025-11-02', 'Continuation of cardiac medications', '2026-02-02', 'Chronic', 1),
    (3, 1, '2025-11-03', 'Pain management for headache', '2025-11-17', 'Acute', 0),
    (4, 3, '2025-11-05', 'Osteoarthritis management', '2026-01-05', 'Chronic', 1),
    (5, 2, '2025-11-06', 'Hypertension - increased dose', '2026-02-06', 'Chronic', 1),
    (7, 4, '2025-11-08', 'Bronchitis treatment', '2025-11-22', 'Acute', 0),
    (8, 1, '2025-11-10', 'Asthma exacerbation management', '2025-12-10', 'Acute', 0),
    (10, 3, '2025-11-12', 'Back pain management', '2026-01-12', 'Chronic', 1),
    (11, 4, '2025-11-13', 'Ankle sprain treatment', '2025-11-27', 'Acute', 0),
    (14, 4, '2025-11-16', 'Dermatitis treatment', '2025-12-16', 'Acute', 0),
    (15, 1, '2025-11-17', 'Wound infection prophylaxis', '2025-11-27', 'Acute', 0);

-- =============================================
-- FINANCIAL CLUSTER
-- =============================================

INSERT INTO INSURANCE_PROVIDER (ProviderName, ContactNumber) VALUES
    ('Blue Cross Blue Shield', '8005551001'),
    ('UnitedHealthcare', '8005551002'),
    ('Aetna', '8005551003'),
    ('Cigna', '8005551004'),
    ('Humana', '8005551005');

INSERT INTO INSURANCE_POLICY (ProviderID, PolicyNumber, CoverageDetails, PlanType, ValidFrom, ValidTo, CopayPercentage, MaxCoverageLimit) VALUES
    (1, 'BCBS-PPO-2025-001', 'Comprehensive PPO plan with nationwide coverage', 'PPO', '2025-01-01', '2025-12-31', 20.00, 5000000.00),
    (1, 'BCBS-HMO-2025-002', 'HMO plan with primary care focus', 'HMO', '2025-01-01', '2025-12-31', 10.00, 2000000.00),
    (1, 'BCBS-PPO-2025-003', 'Premium PPO with dental and vision', 'PPO', '2025-01-01', '2026-12-31', 15.00, 10000000.00),
    (2, 'UHC-GOLD-2025-001', 'Gold tier marketplace plan', 'Gold', '2025-01-01', '2025-12-31', 20.00, 3000000.00),
    (2, 'UHC-SILVER-2025-002', 'Silver tier affordable plan', 'Silver', '2025-01-01', NULL, 30.00, 1500000.00),
    (2, 'UHC-PLATINUM-2025-003', 'Platinum tier comprehensive coverage', 'Platinum', '2025-01-01', '2025-12-31', 10.00, 15000000.00),
    (3, 'AETNA-STD-2025-001', 'Standard employee plan', 'Standard', '2025-01-01', '2026-12-31', 25.00, 2500000.00),
    (3, 'AETNA-PREM-2025-002', 'Premium individual plan', 'Premium', '2025-01-01', '2025-12-31', 15.00, 8000000.00),
    (4, 'CIGNA-CHOICE-2025-001', 'Choice Fund HSA plan', 'HSA', '2025-01-01', NULL, 20.00, 4000000.00),
    (4, 'CIGNA-GLOBAL-2025-002', 'Global expat coverage', 'Global', '2025-01-01', '2025-12-31', 10.00, 20000000.00),
    (5, 'HUM-MEDICARE-2025-001', 'Medicare Advantage plan', 'Medicare', '2025-01-01', NULL, 20.00, 3000000.00),
    (5, 'HUM-EMPLOYER-2025-002', 'Employer group plan', 'Group', '2025-01-01', '2025-12-31', 15.00, 5000000.00);

INSERT INTO PATIENT_INSURANCE (PatientID, PolicyID, ValidFrom, ValidTo, IsPrimary) VALUES
    (1, 3, '2025-01-01', '2025-12-31', 1),
    (2, 6, '2025-01-01', '2025-12-31', 1),
    (3, 8, '2025-01-01', '2025-12-31', 1),
    (4, 1, '2025-01-01', '2025-12-31', 1),
    (4, 8, '2025-01-01', '2025-12-31', 0),
    (5, 4, '2025-01-01', '2025-12-31', 1),
    (6, 12, '2025-01-01', '2025-12-31', 1),
    (7, 12, '2025-01-01', '2025-12-31', 1),
    (8, 2, '2025-01-01', '2025-12-31', 1),
    (9, 11, '2025-01-01', NULL, 1),
    (10, 5, '2025-01-01', '2025-12-31', 1),
    (11, 9, '2025-01-01', '2025-12-31', 1),
    (12, 3, '2025-01-01', '2025-12-31', 1),
    (13, 7, '2025-01-01', '2025-12-31', 1),
    (14, 6, '2025-01-01', '2025-12-31', 1),
    (15, 10, '2025-01-01', '2025-12-31', 1);

INSERT INTO BILLING (PatientID, AppointmentID, Amount, DateIssued, Status, DueDate, DiscountApplied, TaxAmount, PaymentTerms) VALUES
    (1, 1, 850.00, '2025-11-01', 'Paid', '2025-11-15', 0.00, 42.50, 'Net 15'),
    (2, 2, 350.00, '2025-11-02', 'Paid', '2025-11-16', 0.00, 17.50, 'Net 15'),
    (3, 3, 1250.00, '2025-11-03', 'Paid', '2025-11-17', 5.00, 59.38, 'Net 15'),
    (4, 4, 420.00, '2025-11-05', 'Paid', '2025-11-19', 0.00, 21.00, 'Net 15'),
    (5, 5, 280.00, '2025-11-06', 'Paid', '2025-11-20', 0.00, 14.00, 'Net 15'),
    (6, 6, 125.00, '2025-11-07', 'Paid', '2025-11-21', 0.00, 6.25, 'Net 15'),
    (7, 7, 180.00, '2025-11-08', 'Paid', '2025-11-22', 0.00, 9.00, 'Net 15'),
    (8, 8, 1450.00, '2025-11-10', 'Partially Paid', '2025-11-24', 0.00, 72.50, 'Net 15'),
    (9, 9, 420.00, '2025-11-11', 'Paid', '2025-11-25', 0.00, 21.00, 'Net 15'),
    (10, 10, 350.00, '2025-11-12', 'Paid', '2025-11-26', 0.00, 17.50, 'Net 15'),
    (11, 11, 380.00, '2025-11-13', 'Pending', '2025-11-27', 10.00, 17.10, 'Net 15'),
    (12, 12, 250.00, '2025-11-14', 'Paid', '2025-11-28', 0.00, 12.50, 'Net 15'),
    (13, 13, 350.00, '2025-11-15', 'Paid', '2025-11-29', 0.00, 17.50, 'Net 15'),
    (14, 14, 220.00, '2025-11-16', 'Pending', '2025-11-30', 0.00, 11.00, 'Net 15'),
    (15, 15, 1650.00, '2025-11-17', 'Partially Paid', '2025-12-01', 0.00, 82.50, 'Net 15'),
    (1, 16, 180.00, '2025-11-18', 'Paid', '2025-12-02', 0.00, 9.00, 'Net 15'),
    (3, 17, 280.00, '2025-11-19', 'Pending', '2025-12-03', 0.00, 14.00, 'Net 15'),
    (5, 18, 150.00, '2025-11-20', 'Paid', '2025-12-04', 0.00, 7.50, 'Net 15'),
    (7, 19, 150.00, '2025-11-21', 'Paid', '2025-12-05', 0.00, 7.50, 'Net 15'),
    (9, 20, 320.00, '2025-11-22', 'Pending', '2025-12-06', 0.00, 16.00, 'Net 15');

INSERT INTO CLAIM (BillID, ClaimDate, ClaimStatus, AmountCovered, AmountDenied, ProcessedDate, AdjusterNotes) VALUES
    (1, '2025-11-02', 'Approved', 680.00, 0.00, '2025-11-10', 'Emergency visit approved, patient copay $170'),
    (2, '2025-11-03', 'Approved', 280.00, 0.00, '2025-11-11', 'Routine follow-up, patient copay $70'),
    (3, '2025-11-04', 'Partially Approved', 1000.00, 250.00, '2025-11-12', 'CT scan partially covered, patient responsible for balance'),
    (4, '2025-11-06', 'Approved', 336.00, 0.00, '2025-11-14', 'Specialist visit approved'),
    (5, '2025-11-07', 'Approved', 224.00, 0.00, '2025-11-15', 'Standard consultation covered'),
    (6, '2025-11-08', 'Approved', 125.00, 0.00, '2025-11-16', 'Vaccination fully covered - preventive care'),
    (7, '2025-11-09', 'Approved', 144.00, 0.00, '2025-11-17', 'Sick visit covered'),
    (8, '2025-11-11', 'Approved', 1160.00, 0.00, '2025-11-19', 'Emergency asthma treatment approved'),
    (9, '2025-11-12', 'Approved', 336.00, 0.00, '2025-11-20', 'Preventive screening covered'),
    (10, '2025-11-13', 'Approved', 280.00, 0.00, '2025-11-21', 'Consultation approved'),
    (11, '2025-11-14', 'Under Review', NULL, NULL, NULL, 'Awaiting medical records'),
    (12, '2025-11-15', 'Approved', 200.00, 0.00, '2025-11-23', 'Well child visit fully covered'),
    (13, '2025-11-16', 'Approved', 280.00, 0.00, '2025-11-24', 'Consultation covered'),
    (14, '2025-11-17', 'Submitted', NULL, NULL, NULL, 'Claim submitted, pending review'),
    (15, '2025-11-18', 'Approved', 1320.00, 0.00, '2025-11-25', 'Emergency suturing approved, high deductible applied');

-- =============================================
-- PHARMACY CLUSTER
-- =============================================

INSERT INTO MEDICINE (Name, Description, Manufacturer, UnitPrice, StockQuantity, ExpiryDate, Category) VALUES
    ('Ibuprofen 400mg', 'Nonsteroidal anti-inflammatory drug', 'Pfizer', 0.25, 5000, '2026-12-31', 'Analgesic'),
    ('Lisinopril 10mg', 'ACE inhibitor for hypertension', 'Merck', 0.35, 3000, '2026-10-31', 'Cardiovascular'),
    ('Amoxicillin 500mg', 'Broad-spectrum antibiotic', 'GSK', 0.50, 2500, '2026-08-31', 'Antibiotic'),
    ('Albuterol Inhaler', 'Bronchodilator for asthma', 'Teva', 45.00, 200, '2026-06-30', 'Respiratory'),
    ('Atorvastatin 20mg', 'Statin for cholesterol management', 'Pfizer', 0.60, 4000, '2026-11-30', 'Cardiovascular'),
    ('Metformin 500mg', 'Antidiabetic medication', 'Bristol Myers', 0.20, 6000, '2027-01-31', 'Diabetes'),
    ('Omeprazole 20mg', 'Proton pump inhibitor', 'AstraZeneca', 0.40, 3500, '2026-09-30', 'Gastrointestinal'),
    ('Prednisone 10mg', 'Corticosteroid', 'Pfizer', 0.30, 2000, '2026-07-31', 'Steroid'),
    ('Ciprofloxacin 500mg', 'Fluoroquinolone antibiotic', 'Bayer', 0.80, 1500, '2026-05-31', 'Antibiotic'),
    ('Acetaminophen 500mg', 'Pain reliever and fever reducer', 'Johnson & Johnson', 0.15, 8000, '2027-03-31', 'Analgesic'),
    ('Amlodipine 5mg', 'Calcium channel blocker', 'Novartis', 0.25, 4500, '2026-12-31', 'Cardiovascular'),
    ('Azithromycin 250mg', 'Macrolide antibiotic', 'Pfizer', 1.20, 1200, '2026-04-30', 'Antibiotic'),
    ('Hydrochlorothiazide 25mg', 'Diuretic', 'Merck', 0.18, 5000, '2027-02-28', 'Cardiovascular'),
    ('Cyclobenzaprine 10mg', 'Muscle relaxant', 'Teva', 0.45, 2500, '2026-08-31', 'Muscle Relaxant'),
    ('Gabapentin 300mg', 'Anticonvulsant/nerve pain', 'Pfizer', 0.55, 3000, '2026-10-31', 'Neurological'),
    ('Montelukast 10mg', 'Leukotriene receptor antagonist', 'Merck', 0.70, 2000, '2026-09-30', 'Respiratory'),
    ('Cephalexin 500mg', 'Cephalosporin antibiotic', 'GSK', 0.60, 2200, '2026-07-31', 'Antibiotic'),
    ('Hydrocortisone Cream 1%', 'Topical corticosteroid', 'Johnson & Johnson', 8.50, 500, '2026-06-30', 'Dermatological'),
    ('Loratadine 10mg', 'Antihistamine', 'Bayer', 0.35, 4000, '2027-01-31', 'Allergy'),
    ('Doxycycline 100mg', 'Tetracycline antibiotic', 'Pfizer', 0.65, 1800, '2026-05-31', 'Antibiotic');

INSERT INTO PRESCRIBED_MEDICINE (PrescriptionID, MedicineID, Dosage, Frequency, Duration, Instructions, StartDate, EndDate) VALUES
    (1, 1, '400mg', 'Three times daily', '10 days', 'Take with food', '2025-11-01', '2025-11-11'),
    (2, 2, '10mg', 'Once daily', 'Ongoing', 'Take in morning', '2025-11-02', NULL),
    (2, 5, '20mg', 'Once daily at bedtime', 'Ongoing', 'Take at night', '2025-11-02', NULL),
    (2, 11, '5mg', 'Once daily', 'Ongoing', 'Take in morning', '2025-11-02', NULL),
    (3, 10, '500mg', 'Every 6 hours as needed', '14 days', 'Do not exceed 4g/day', '2025-11-03', '2025-11-17'),
    (4, 1, '400mg', 'Three times daily', 'Ongoing', 'Take with food', '2025-11-05', NULL),
    (4, 14, '10mg', 'Once at bedtime', '2 weeks', 'For muscle spasms', '2025-11-05', '2025-11-19'),
    (5, 2, '20mg', 'Once daily', 'Ongoing', 'Increased dose, take in morning', '2025-11-06', NULL),
    (5, 13, '25mg', 'Once daily', 'Ongoing', 'Take in morning', '2025-11-06', NULL),
    (6, 3, '500mg', 'Three times daily', '10 days', 'Complete full course', '2025-11-08', '2025-11-18'),
    (6, 10, '500mg', 'Every 6 hours as needed', '7 days', 'For fever/pain', '2025-11-08', '2025-11-15'),
    (7, 4, '2 puffs', 'Every 4-6 hours as needed', '30 days', 'Use spacer device', '2025-11-10', '2025-12-10'),
    (7, 8, '40mg', 'Once daily', '5 days', 'Take in morning with food', '2025-11-10', '2025-11-15'),
    (7, 16, '10mg', 'Once daily at bedtime', 'Ongoing', 'Long-term asthma control', '2025-11-10', NULL),
    (8, 1, '400mg', 'Three times daily', 'Ongoing', 'Take with food', '2025-11-12', NULL),
    (8, 14, '10mg', 'Once at bedtime', '3 weeks', 'Muscle relaxation', '2025-11-12', '2025-12-03'),
    (8, 15, '300mg', 'Three times daily', 'Ongoing', 'For nerve pain', '2025-11-12', NULL),
    (9, 1, '400mg', 'Three times daily', '14 days', 'Take with food, for pain/inflammation', '2025-11-13', '2025-11-27'),
    (10, 18, 'Apply thin layer', 'Twice daily', '14 days', 'Apply to affected area only', '2025-11-16', '2025-11-30'),
    (10, 19, '10mg', 'Once daily', '7 days', 'For itching', '2025-11-16', '2025-11-23'),
    (11, 17, '500mg', 'Four times daily', '7 days', 'Complete full course', '2025-11-17', '2025-11-24');

INSERT INTO INVENTORY (MedicineID, QuantityAvailable, ReorderLevel, SupplierName, BatchNumber, ExpiryDate, StorageLocation) VALUES
    (1, 5000, 1000, 'McKesson Pharma', 'IBU-2025-A1234', '2026-12-31', 'Shelf A1'),
    (2, 3000, 500, 'Cardinal Health', 'LIS-2025-B5678', '2026-10-31', 'Shelf A2'),
    (3, 2500, 500, 'AmerisourceBergen', 'AMX-2025-C9012', '2026-08-31', 'Refrigerator R1'),
    (4, 200, 50, 'McKesson Pharma', 'ALB-2025-D3456', '2026-06-30', 'Shelf B1'),
    (5, 4000, 800, 'Cardinal Health', 'ATO-2025-E7890', '2026-11-30', 'Shelf A3'),
    (6, 6000, 1200, 'AmerisourceBergen', 'MET-2025-F1234', '2027-01-31', 'Shelf C1'),
    (7, 3500, 700, 'McKesson Pharma', 'OME-2025-G5678', '2026-09-30', 'Shelf B2'),
    (8, 2000, 400, 'Cardinal Health', 'PRE-2025-H9012', '2026-07-31', 'Shelf A4'),
    (9, 1500, 300, 'AmerisourceBergen', 'CIP-2025-I3456', '2026-05-31', 'Shelf D1'),
    (10, 8000, 1500, 'McKesson Pharma', 'ACE-2025-J7890', '2027-03-31', 'Shelf C2'),
    (11, 4500, 900, 'Cardinal Health', 'AML-2025-K1234', '2026-12-31', 'Shelf A5'),
    (12, 1200, 250, 'AmerisourceBergen', 'AZI-2025-L5678', '2026-04-30', 'Refrigerator R2'),
    (13, 5000, 1000, 'McKesson Pharma', 'HYD-2025-M9012', '2027-02-28', 'Shelf B3'),
    (14, 2500, 500, 'Cardinal Health', 'CYC-2025-N3456', '2026-08-31', 'Shelf D2'),
    (15, 3000, 600, 'AmerisourceBergen', 'GAB-2025-O7890', '2026-10-31', 'Shelf C3'),
    (16, 2000, 400, 'McKesson Pharma', 'MON-2025-P1234', '2026-09-30', 'Shelf B4'),
    (17, 2200, 450, 'Cardinal Health', 'CEP-2025-Q5678', '2026-07-31', 'Refrigerator R3'),
    (18, 500, 100, 'AmerisourceBergen', 'HYC-2025-R9012', '2026-06-30', 'Shelf E1'),
    (19, 4000, 800, 'McKesson Pharma', 'LOR-2025-S3456', '2027-01-31', 'Shelf C4'),
    (20, 1800, 350, 'Cardinal Health', 'DOX-2025-T7890', '2026-05-31', 'Refrigerator R4');

INSERT INTO STORAGE_REQUIREMENT (RequirementName, TemperatureRange, HumidityRange, SpecialHandling) VALUES
    ('Room Temperature', '20-25°C (68-77°F)', '30-60%', 'Standard pharmaceutical storage'),
    ('Refrigerated', '2-8°C (36-46°F)', '30-60%', 'Keep refrigerated, do not freeze'),
    ('Cool Dry Place', '15-25°C (59-77°F)', 'Below 60%', 'Protect from moisture'),
    ('Controlled Room Temp', '20-22°C (68-72°F)', '40-50%', 'Climate-controlled storage required'),
    ('Light Protected', '20-25°C (68-77°F)', '30-60%', 'Store in original container, protect from light');

INSERT INTO MEDICINE_STORAGE (MedicineID, RequirementID) VALUES
    (1, 1), (2, 1), (5, 1), (6, 1), (7, 1), (10, 1), (11, 1), (13, 1), (14, 1), (15, 1), (16, 1), (19, 1),
    (3, 2), (12, 2), (17, 2), (20, 2),
    (4, 3), (8, 3),
    (9, 4),
    (18, 5);

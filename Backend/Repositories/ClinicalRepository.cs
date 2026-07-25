using MySqlConnector;
using Dapper;
using HospitalManagement.API.Exceptions;
using HospitalManagement.API.Models.DTOs;
using HospitalManagement.API.Models.Requests;
using HospitalManagement.API.Repositories.Interfaces;
using HospitalManagement.API.Time;

namespace HospitalManagement.API.Repositories;

public class ClinicalRepository : IClinicalRepository
{
    private readonly string _connectionString;

    public ClinicalRepository(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("HospitalDb")!;
    }

    public async Task<FileDto?> GetFileAsync(int appointmentId)
    {
        using var connection = new MySqlConnection(_connectionString);

        var sql = @"
            SELECT a.AppointmentID as AppointmentId, a.PatientID as PatientId,
                   CONCAT(p.FirstName,' ',p.LastName) as PatientName,
                   a.DoctorID as DoctorId, CONCAT(s.FirstName,' ',s.LastName) as DoctorName,
                   a.`DateTime`, a.Reason, a.Status, a.AppointmentType
            FROM APPOINTMENT a
            INNER JOIN PATIENT p ON a.PatientID = p.PatientID
            INNER JOIN DOCTOR d ON a.DoctorID = d.DoctorID
            INNER JOIN STAFF s ON d.DoctorID = s.StaffID
            WHERE a.AppointmentID = @Id;

            SELECT al.Name
            FROM PATIENT_ALLERGY pa
            INNER JOIN ALLERGY al ON pa.AllergyID = al.AllergyID
            INNER JOIN APPOINTMENT a ON a.PatientID = pa.PatientID
            WHERE a.AppointmentID = @Id;

            SELECT RecordID as RecordId, VitalSigns, Diagnosis, Notes, TreatmentPlan,
                   FollowUpRequired
            FROM MEDICAL_RECORD WHERE AppointmentID = @Id;

            SELECT t.LabTestID as LabTestId, t.TestType, t.Status, t.Result, t.Units,
                   t.NormalRange, t.Comments, t.ResultDate,
                   CONCAT(ts.FirstName,' ',ts.LastName) as TechnicianName
            FROM LAB_TEST t
            INNER JOIN LAB_TECHNICIAN lt ON t.LabTechnicianID = lt.LabTechnicianID
            INNER JOIN STAFF ts ON lt.LabTechnicianID = ts.StaffID
            WHERE t.AppointmentID = @Id
            ORDER BY t.LabTestID;

            SELECT PrescriptionID as PrescriptionId, Status, RejectReason, DateIssued, ValidUntil
            FROM PRESCRIPTION WHERE AppointmentID = @Id;

            SELECT pm.MedicineID as MedicineId, m.Name as MedicineName, pm.Quantity,
                   pm.Dosage, pm.Frequency, pm.Duration, pm.Instructions, m.UnitPrice
            FROM PRESCRIBED_MEDICINE pm
            INNER JOIN MEDICINE m ON pm.MedicineID = m.MedicineID
            INNER JOIN PRESCRIPTION pr ON pm.PrescriptionID = pr.PrescriptionID
            WHERE pr.AppointmentID = @Id;

            SELECT b.BillID as BillId, b.BillType, b.Amount,
                   IFNULL(c.AmountCovered, 0) as InsuranceCovered,
                   (b.Amount - IFNULL(c.AmountCovered, 0)) as PatientResponsibility,
                   b.Status, b.PaymentMethod, b.CardSurcharge
            FROM BILLING b
            LEFT JOIN CLAIM c ON b.BillID = c.BillID
            WHERE b.AppointmentID = @Id
            ORDER BY b.BillID;";

        using var multi = await connection.QueryMultipleAsync(sql, new { Id = appointmentId });

        var file = await multi.ReadSingleOrDefaultAsync<FileDto>();
        if (file == null) return null;

        file.Allergies = (await multi.ReadAsync<string>()).ToList();
        file.Record = await multi.ReadSingleOrDefaultAsync<FileRecordDto>();
        file.LabTests = (await multi.ReadAsync<FileLabTestDto>()).ToList();
        file.Prescription = await multi.ReadSingleOrDefaultAsync<FilePrescriptionDto>();
        var lines = (await multi.ReadAsync<FilePrescriptionLineDto>()).ToList();
        if (file.Prescription != null) file.Prescription.Lines = lines;
        file.Bills = (await multi.ReadAsync<FileBillDto>()).ToList();
        return file;
    }

    // The nurse (or doctor) opens the clinical spine of the File at check-in.
    public async Task<int> UpsertVitalsAsync(int appointmentId, string vitalSigns)
    {
        using var connection = new MySqlConnection(_connectionString);
        var recordId = await EnsureRecordAsync(connection, appointmentId);
        await connection.ExecuteAsync(
            "UPDATE MEDICAL_RECORD SET VitalSigns = @Vitals WHERE RecordID = @RecordId",
            new { Vitals = vitalSigns, RecordId = recordId });
        return recordId;
    }

    public async Task<int> SaveConsultationAsync(int appointmentId, SaveConsultationRequest request)
    {
        using var connection = new MySqlConnection(_connectionString);
        var recordId = await EnsureRecordAsync(connection, appointmentId);
        await connection.ExecuteAsync(@"
            UPDATE MEDICAL_RECORD
            SET Diagnosis = @Diagnosis, Notes = @Notes,
                TreatmentPlan = @TreatmentPlan, FollowUpRequired = @FollowUpRequired
            WHERE RecordID = @RecordId",
            new { request.Diagnosis, request.Notes, request.TreatmentPlan,
                  request.FollowUpRequired, RecordId = recordId });
        return recordId;
    }

    public async Task<int> OrderLabTestsAsync(int appointmentId, OrderLabTestsRequest request)
    {
        using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync();

        // Pool with balance: the least-loaded technician takes the new order.
        var technicianId = await connection.ExecuteScalarAsync<int?>(@"
            SELECT lt.LabTechnicianID
            FROM LAB_TECHNICIAN lt
            LEFT JOIN LAB_TEST t ON t.LabTechnicianID = lt.LabTechnicianID
                 AND t.Status IN ('Pending','In Progress')
            GROUP BY lt.LabTechnicianID
            ORDER BY COUNT(t.LabTestID), lt.LabTechnicianID
            LIMIT 1");
        if (!technicianId.HasValue)
            throw new ConflictException("No lab technician is available to receive test orders.");

        foreach (var t in request.Tests)
        {
            await connection.ExecuteAsync(@"
                INSERT INTO LAB_TEST (AppointmentID, LabTechnicianID, TestType, Status, NormalRange, Units)
                VALUES (@AppointmentId, @TechnicianId, @TestType, 'Pending', @NormalRange, @Units)",
                new { AppointmentId = appointmentId, TechnicianId = technicianId,
                      t.TestType, t.NormalRange, t.Units });
        }
        return request.Tests.Count;
    }

    public async Task<int> CreatePrescriptionAsync(int appointmentId, int doctorId, CreatePrescriptionRequest request)
    {
        using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync();

        var existing = await connection.ExecuteScalarAsync<int?>(
            "SELECT PrescriptionID FROM PRESCRIPTION WHERE AppointmentID = @Id",
            new { Id = appointmentId });
        if (existing.HasValue)
            throw new ConflictException("This visit already has a prescription. Reject it at the pharmacy first if it needs changes.");

        using var transaction = await connection.BeginTransactionAsync();

        var recordId = await EnsureRecordAsync(connection, appointmentId, transaction);
        var todayIst = IstClock.TodayIstDate();

        var prescriptionId = await connection.ExecuteScalarAsync<int>(@"
            INSERT INTO PRESCRIPTION (RecordID, AppointmentID, DoctorID, DateIssued, ValidUntil,
                                      RenewalAllowed, PrescriptionType, Status)
            VALUES (@RecordId, @AppointmentId, @DoctorId, @TodayIst, @ValidUntil,
                    0, 'Outpatient', 'SentToPharmacy');
            SELECT LAST_INSERT_ID();",
            new { RecordId = recordId, AppointmentId = appointmentId, DoctorId = doctorId,
                  TodayIst = todayIst, ValidUntil = todayIst.AddDays(request.ValidDays) },
            transaction);

        foreach (var line in request.Lines)
        {
            await connection.ExecuteAsync(@"
                INSERT INTO PRESCRIBED_MEDICINE
                    (PrescriptionID, MedicineID, Quantity, Dosage, Frequency, Duration, Instructions, StartDate)
                VALUES (@PrescriptionId, @MedicineId, @Quantity, @Dosage, @Frequency, @Duration, @Instructions, @TodayIst)",
                new { PrescriptionId = prescriptionId, line.MedicineId, line.Quantity, line.Dosage,
                      line.Frequency, line.Duration, line.Instructions, TodayIst = todayIst },
                transaction);
        }

        await transaction.CommitAsync();
        return prescriptionId;
    }

    private static async Task<int> EnsureRecordAsync(
        MySqlConnection connection, int appointmentId, MySqlTransaction? transaction = null)
    {
        var recordId = await connection.ExecuteScalarAsync<int?>(
            "SELECT RecordID FROM MEDICAL_RECORD WHERE AppointmentID = @Id",
            new { Id = appointmentId }, transaction);
        if (recordId.HasValue) return recordId.Value;

        return await connection.ExecuteScalarAsync<int>(@"
            INSERT INTO MEDICAL_RECORD (PatientID, DoctorID, AppointmentID, VisitDate, RecordType)
            SELECT PatientID, DoctorID, AppointmentID, @VisitDate, 'Visit'
            FROM APPOINTMENT WHERE AppointmentID = @Id;
            SELECT LAST_INSERT_ID();",
            new { Id = appointmentId, VisitDate = IstClock.TodayIstDate() }, transaction);
    }
}

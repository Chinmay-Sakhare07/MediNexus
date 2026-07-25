using MySqlConnector;
using Dapper;
using HospitalManagement.API.Exceptions;
using HospitalManagement.API.Models.DTOs;
using HospitalManagement.API.Repositories.Interfaces;
using HospitalManagement.API.Time;

namespace HospitalManagement.API.Repositories;

public class PharmacyRepository : IPharmacyRepository
{
    private readonly string _connectionString;

    public PharmacyRepository(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("HospitalDb")!;
    }

    public async Task<IEnumerable<MedicineDto>> GetMedicinesAsync()
    {
        using var connection = new MySqlConnection(_connectionString);
        return await connection.QueryAsync<MedicineDto>(@"
            SELECT MedicineID as MedicineId, Name, Description, Category,
                   UnitPrice, StockQuantity, ExpiryDate
            FROM MEDICINE ORDER BY Name");
    }

    public async Task<int> AdjustStockAsync(int medicineId, int adjustment)
    {
        using var connection = new MySqlConnection(_connectionString);

        // Guarded update: never below zero (CHK constraint is the backstop).
        var affected = await connection.ExecuteAsync(@"
            UPDATE MEDICINE SET StockQuantity = StockQuantity + @Adj
            WHERE MedicineID = @Id AND StockQuantity + @Adj >= 0",
            new { Id = medicineId, Adj = adjustment });

        if (affected == 0)
        {
            var exists = await connection.ExecuteScalarAsync<int>(
                "SELECT COUNT(*) FROM MEDICINE WHERE MedicineID = @Id", new { Id = medicineId });
            throw exists == 0
                ? new ConflictException("Medicine not found.")
                : new ConflictException("That adjustment would take stock below zero.");
        }

        return await connection.ExecuteScalarAsync<int>(
            "SELECT StockQuantity FROM MEDICINE WHERE MedicineID = @Id", new { Id = medicineId });
    }

    public async Task<IEnumerable<PharmacyQueueItemDto>> GetQueueAsync()
    {
        using var connection = new MySqlConnection(_connectionString);
        return await connection.QueryAsync<PharmacyQueueItemDto>(@"
            SELECT pr.PrescriptionID as PrescriptionId, pr.AppointmentID as AppointmentId,
                   CONCAT(p.FirstName,' ',p.LastName) as PatientName,
                   CONCAT(s.FirstName,' ',s.LastName) as DoctorName,
                   pr.Status, pr.RejectReason, pr.DateIssued, pr.ValidUntil,
                   COUNT(pm.MedicineID) as ItemCount,
                   IFNULL(SUM(pm.Quantity * m.UnitPrice), 0) as EstimatedTotal
            FROM PRESCRIPTION pr
            INNER JOIN APPOINTMENT a ON pr.AppointmentID = a.AppointmentID
            INNER JOIN PATIENT p ON a.PatientID = p.PatientID
            INNER JOIN STAFF s ON pr.DoctorID = s.StaffID
            LEFT JOIN PRESCRIBED_MEDICINE pm ON pr.PrescriptionID = pm.PrescriptionID
            LEFT JOIN MEDICINE m ON pm.MedicineID = m.MedicineID
            WHERE pr.Status IN ('SentToPharmacy','Confirmed','Ready')
            GROUP BY pr.PrescriptionID, pr.AppointmentID, PatientName, DoctorName,
                     pr.Status, pr.RejectReason, pr.DateIssued, pr.ValidUntil
            ORDER BY pr.DateIssued, pr.PrescriptionID");
    }

    public async Task<bool> ConfirmAsync(int prescriptionId)
    {
        using var connection = new MySqlConnection(_connectionString);
        await ValidateFillableAsync(connection, prescriptionId, null);
        var affected = await connection.ExecuteAsync(@"
            UPDATE PRESCRIPTION SET Status = 'Confirmed'
            WHERE PrescriptionID = @Id AND Status = 'SentToPharmacy'",
            new { Id = prescriptionId });
        return affected > 0;
    }

    public async Task<bool> RejectAsync(int prescriptionId, string reason)
    {
        using var connection = new MySqlConnection(_connectionString);
        var affected = await connection.ExecuteAsync(@"
            UPDATE PRESCRIPTION SET Status = 'Rejected', RejectReason = @Reason
            WHERE PrescriptionID = @Id AND Status IN ('SentToPharmacy','Confirmed')",
            new { Id = prescriptionId, Reason = reason });
        return affected > 0;
    }

    public async Task<bool> MarkReadyAsync(int prescriptionId)
    {
        using var connection = new MySqlConnection(_connectionString);
        var affected = await connection.ExecuteAsync(@"
            UPDATE PRESCRIPTION SET Status = 'Ready'
            WHERE PrescriptionID = @Id AND Status = 'Confirmed'",
            new { Id = prescriptionId });
        return affected > 0;
    }

    /// <summary>
    /// The pickup moment: dispense records + atomic stock decrements + the
    /// pharmacy bill (with insurance claim when valid primary coverage exists),
    /// all in one transaction. All-or-nothing per prescription (D9).
    /// </summary>
    public async Task<DispenseResultDto> DispenseAsync(int prescriptionId, int dispensedByUserId)
    {
        using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync();
        using var transaction = await connection.BeginTransactionAsync();

        var status = await connection.ExecuteScalarAsync<string?>(
            "SELECT Status FROM PRESCRIPTION WHERE PrescriptionID = @Id",
            new { Id = prescriptionId }, transaction);
        if (status is null) throw new ConflictException("Prescription not found.");
        if (status != "Ready" && status != "Confirmed")
            throw new ConflictException($"Prescription cannot be dispensed from status '{status}'.");

        var lines = (await ValidateFillableAsync(connection, prescriptionId, transaction)).ToList();

        var utcNow = DateTime.UtcNow;
        foreach (var line in lines)
        {
            var updated = await connection.ExecuteAsync(@"
                UPDATE MEDICINE SET StockQuantity = StockQuantity - @Qty
                WHERE MedicineID = @MedicineId AND StockQuantity >= @Qty",
                new { Qty = line.Quantity, line.MedicineId }, transaction);
            if (updated == 0)
                throw new ConflictException($"Stock ran out for {line.MedicineName} while dispensing.");

            await connection.ExecuteAsync(@"
                INSERT INTO MEDICINE_DISPENSE (PrescriptionID, MedicineID, QuantityDispensed, DispensedBy, DispensedAt)
                VALUES (@PrescriptionId, @MedicineId, @Qty, @By, @At)",
                new { PrescriptionId = prescriptionId, line.MedicineId, Qty = line.Quantity,
                      By = dispensedByUserId, At = utcNow }, transaction);
        }

        await connection.ExecuteAsync(
            "UPDATE PRESCRIPTION SET Status = 'Dispensed' WHERE PrescriptionID = @Id",
            new { Id = prescriptionId }, transaction);

        // ---- Bill #2: pharmacy, from actual dispensed lines ----
        var subtotal = lines.Sum(l => l.Quantity * l.UnitPrice);
        var taxAmount = Math.Round(subtotal * 0.07m, 2);
        var totalAmount = subtotal + taxAmount;
        var todayIst = IstClock.TodayIstDate();

        var (patientId, appointmentId) = await connection.QuerySingleAsync<(int, int)>(@"
            SELECT a.PatientID, a.AppointmentID
            FROM PRESCRIPTION pr INNER JOIN APPOINTMENT a ON pr.AppointmentID = a.AppointmentID
            WHERE pr.PrescriptionID = @Id",
            new { Id = prescriptionId }, transaction);

        var billId = await connection.ExecuteScalarAsync<int>(@"
            INSERT INTO BILLING (PatientID, AppointmentID, Amount, DateIssued, Status, DueDate,
                                 DiscountApplied, TaxAmount, PaymentTerms, BillType, PrescriptionID)
            VALUES (@PatientId, @AppointmentId, @Amount, @TodayIst, 'Pending', @DueDate,
                    0, @TaxAmount, 'On pickup', 'Pharmacy', @PrescriptionId);
            SELECT LAST_INSERT_ID();",
            new { PatientId = patientId, AppointmentId = appointmentId, Amount = totalAmount,
                  TodayIst = todayIst, DueDate = todayIst.AddDays(7), TaxAmount = taxAmount,
                  PrescriptionId = prescriptionId }, transaction);

        // Insurance participates in pharmacy bills too (D10 as amended):
        // valid primary coverage auto-files a claim for its coverage share.
        var copay = await connection.QuerySingleOrDefaultAsync<decimal?>(@"
            SELECT pol.CopayPercentage
            FROM PATIENT_INSURANCE pi
            INNER JOIN INSURANCE_POLICY pol ON pi.PolicyID = pol.PolicyID
            WHERE pi.PatientID = @PatientId AND pi.IsPrimary = 1
              AND @TodayIst BETWEEN pi.ValidFrom AND IFNULL(pi.ValidTo, '9999-12-31')
            LIMIT 1",
            new { PatientId = patientId, TodayIst = todayIst }, transaction);

        decimal covered = 0;
        if (copay.HasValue)
        {
            // Same convention as consultation billing: insurance pays (100 - copay)%.
            covered = Math.Round(totalAmount * ((100 - copay.Value) / 100m), 2);
            await connection.ExecuteAsync(@"
                INSERT INTO CLAIM (BillID, ClaimDate, ClaimStatus, AmountCovered, AmountDenied, ProcessedDate)
                VALUES (@BillId, @TodayIst, 'Approved', @Covered, 0, @TodayIst)",
                new { BillId = billId, TodayIst = todayIst, Covered = covered }, transaction);
        }

        await transaction.CommitAsync();

        return new DispenseResultDto
        {
            BillId = billId,
            Amount = totalAmount,
            InsuranceCovered = covered,
            PatientResponsibility = totalAmount - covered
        };
    }

    private record FillableLine(int MedicineId, string MedicineName, int Quantity, int StockQuantity,
                                DateTime? ExpiryDate, decimal UnitPrice);

    private static async Task<IEnumerable<FillableLine>> ValidateFillableAsync(
        MySqlConnection connection, int prescriptionId, MySqlTransaction? transaction)
    {
        var header = await connection.QuerySingleOrDefaultAsync<(DateTime? ValidUntil, string Status)?>(
            "SELECT ValidUntil, Status FROM PRESCRIPTION WHERE PrescriptionID = @Id",
            new { Id = prescriptionId }, transaction);
        if (header is null) throw new ConflictException("Prescription not found.");

        var todayIst = IstClock.TodayIstDate();
        if (header.Value.ValidUntil.HasValue && header.Value.ValidUntil.Value.Date < todayIst)
            throw new ConflictException("This prescription has expired and cannot be filled.");

        var lines = (await connection.QueryAsync<FillableLine>(@"
            SELECT pm.MedicineID as MedicineId, m.Name as MedicineName, pm.Quantity,
                   m.StockQuantity, m.ExpiryDate, m.UnitPrice
            FROM PRESCRIBED_MEDICINE pm
            INNER JOIN MEDICINE m ON pm.MedicineID = m.MedicineID
            WHERE pm.PrescriptionID = @Id",
            new { Id = prescriptionId }, transaction)).ToList();

        if (lines.Count == 0) throw new ConflictException("Prescription has no medicine lines.");

        var problems = new List<string>();
        foreach (var l in lines)
        {
            if (l.ExpiryDate.HasValue && l.ExpiryDate.Value.Date < todayIst)
                problems.Add($"{l.MedicineName} is expired");
            else if (l.StockQuantity < l.Quantity)
                problems.Add($"{l.MedicineName}: need {l.Quantity}, have {l.StockQuantity}");
        }
        if (problems.Count > 0)
            throw new ConflictException("Cannot fill prescription — " + string.Join("; ", problems) + ".");

        return lines;
    }
}

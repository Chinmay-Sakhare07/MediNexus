using System.Data;
using MySqlConnector;
using Dapper;
using HospitalManagement.API.Models.DTOs;
using HospitalManagement.API.Models.Requests;
using HospitalManagement.API.Repositories.Interfaces;
using HospitalManagement.API.Time;

namespace HospitalManagement.API.Repositories;

public class BillingRepository : IBillingRepository
{
    private readonly string _connectionString;

    public BillingRepository(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("HospitalDb")!;
    }

    public async Task<IEnumerable<BillingDto>> GetAllAsync()
    {
        using var connection = new MySqlConnection(_connectionString);

        var sql = @"
            SELECT
                b.BillID as BillId,
                b.PatientID as PatientId,
                CONCAT(p.FirstName, ' ', p.LastName) as PatientName,
                b.AppointmentID as AppointmentId,
                a.`DateTime` as AppointmentDate,
                a.Reason as AppointmentReason,
                b.Amount,
                b.DiscountApplied,
                b.TaxAmount,
                b.DateIssued,
                b.DueDate,
                b.Status,
                b.PaymentTerms,
                ip.ProviderName as InsuranceProvider,
                pol.PolicyNumber,
                IFNULL(c.AmountCovered, 0) as InsuranceCovered,
                (b.Amount - IFNULL(c.AmountCovered, 0)) as PatientResponsibility
            FROM BILLING b
            INNER JOIN PATIENT p ON b.PatientID = p.PatientID
            LEFT JOIN APPOINTMENT a ON b.AppointmentID = a.AppointmentID
            LEFT JOIN CLAIM c ON b.BillID = c.BillID
            LEFT JOIN PATIENT_INSURANCE pi ON p.PatientID = pi.PatientID AND pi.IsPrimary = 1
            LEFT JOIN INSURANCE_POLICY pol ON pi.PolicyID = pol.PolicyID
            LEFT JOIN INSURANCE_PROVIDER ip ON pol.ProviderID = ip.ProviderID
            ORDER BY b.DateIssued DESC";

        return await connection.QueryAsync<BillingDto>(sql);
    }

    public async Task<IEnumerable<BillingDto>> GetByPatientIdAsync(int patientId)
    {
        using var connection = new MySqlConnection(_connectionString);

        var sql = @"
            SELECT
                b.BillID as BillId,
                b.PatientID as PatientId,
                CONCAT(p.FirstName, ' ', p.LastName) as PatientName,
                b.AppointmentID as AppointmentId,
                a.`DateTime` as AppointmentDate,
                a.Reason as AppointmentReason,
                b.Amount,
                b.DiscountApplied,
                b.TaxAmount,
                b.DateIssued,
                b.DueDate,
                b.Status,
                b.PaymentTerms,
                ip.ProviderName as InsuranceProvider,
                pol.PolicyNumber,
                IFNULL(c.AmountCovered, 0) as InsuranceCovered,
                (b.Amount - IFNULL(c.AmountCovered, 0)) as PatientResponsibility
            FROM BILLING b
            INNER JOIN PATIENT p ON b.PatientID = p.PatientID
            LEFT JOIN APPOINTMENT a ON b.AppointmentID = a.AppointmentID
            LEFT JOIN CLAIM c ON b.BillID = c.BillID
            LEFT JOIN PATIENT_INSURANCE pi ON p.PatientID = pi.PatientID AND pi.IsPrimary = 1
            LEFT JOIN INSURANCE_POLICY pol ON pi.PolicyID = pol.PolicyID
            LEFT JOIN INSURANCE_PROVIDER ip ON pol.ProviderID = ip.ProviderID
            WHERE b.PatientID = @PatientId
            ORDER BY b.DateIssued DESC";

        return await connection.QueryAsync<BillingDto>(sql, new { PatientId = patientId });
    }

    public async Task<BillingDto?> GetByIdAsync(int id)
    {
        using var connection = new MySqlConnection(_connectionString);

        var sql = @"
            SELECT
                b.BillID as BillId,
                b.PatientID as PatientId,
                CONCAT(p.FirstName, ' ', p.LastName) as PatientName,
                b.AppointmentID as AppointmentId,
                a.`DateTime` as AppointmentDate,
                a.Reason as AppointmentReason,
                b.Amount,
                b.DiscountApplied,
                b.TaxAmount,
                b.DateIssued,
                b.DueDate,
                b.Status,
                b.PaymentTerms,
                ip.ProviderName as InsuranceProvider,
                pol.PolicyNumber,
                IFNULL(c.AmountCovered, 0) as InsuranceCovered,
                (b.Amount - IFNULL(c.AmountCovered, 0)) as PatientResponsibility
            FROM BILLING b
            INNER JOIN PATIENT p ON b.PatientID = p.PatientID
            LEFT JOIN APPOINTMENT a ON b.AppointmentID = a.AppointmentID
            LEFT JOIN CLAIM c ON b.BillID = c.BillID
            LEFT JOIN PATIENT_INSURANCE pi ON p.PatientID = pi.PatientID AND pi.IsPrimary = 1
            LEFT JOIN INSURANCE_POLICY pol ON pi.PolicyID = pol.PolicyID
            LEFT JOIN INSURANCE_PROVIDER ip ON pol.ProviderID = ip.ProviderID
            WHERE b.BillID = @Id";

        return await connection.QuerySingleOrDefaultAsync<BillingDto>(sql, new { Id = id });
    }

    public async Task<int> CompleteAppointmentWithBillingAsync(CompleteAppointmentRequest request)
    {
        using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync();
        using var transaction = connection.BeginTransaction();

        try
        {
            await connection.ExecuteAsync(
                "UPDATE APPOINTMENT SET Status = 'Completed' WHERE AppointmentID = @AppointmentId",
                new { request.AppointmentId },
                transaction);

            var patientId = await connection.ExecuteScalarAsync<int>(
                "SELECT PatientID FROM APPOINTMENT WHERE AppointmentID = @AppointmentId",
                new { request.AppointmentId },
                transaction);

            var subtotal = request.ConsultationFee + request.AdditionalFees;
            var discountAmount = subtotal * (request.DiscountPercentage / 100);
            var afterDiscount = subtotal - discountAmount;
            var taxAmount = afterDiscount * 0.07m;
            var totalAmount = afterDiscount + taxAmount;

            var billSql = @"
                INSERT INTO BILLING (
                    PatientID, AppointmentID, Amount, DateIssued, Status,
                    DueDate, DiscountApplied, TaxAmount, PaymentTerms
                )
                VALUES (
                    @PatientId, @AppointmentId, @Amount, @TodayIst, 'Pending',
                    @DueDateIst, @DiscountApplied, @TaxAmount, '30 days'
                );
                SELECT LAST_INSERT_ID();";

            var todayIst = IstClock.TodayIstDate();
            var billId = await connection.ExecuteScalarAsync<int>(billSql, new {
                PatientId = patientId,
                request.AppointmentId,
                Amount = totalAmount,
                TodayIst = todayIst,
                DueDateIst = todayIst.AddDays(30),
                DiscountApplied = request.DiscountPercentage,
                TaxAmount = taxAmount
            }, transaction);

            var insuranceSql = @"
                SELECT pi.PolicyID, pol.CopayPercentage
                FROM PATIENT_INSURANCE pi
                INNER JOIN INSURANCE_POLICY pol ON pi.PolicyID = pol.PolicyID
                WHERE pi.PatientID = @PatientId AND pi.IsPrimary = 1
                AND @TodayIst BETWEEN pi.ValidFrom AND IFNULL(pi.ValidTo, '9999-12-31')
                LIMIT 1";

            var insurance = await connection.QuerySingleOrDefaultAsync<dynamic>(
                insuranceSql,
                new { PatientId = patientId, TodayIst = todayIst },
                transaction);

            if (insurance != null)
            {
                var copayPercentage = (decimal)insurance.CopayPercentage;
                var insurancePercentage = (100 - copayPercentage) / 100;
                var amountCovered = totalAmount * insurancePercentage;

                var claimSql = @"
                    INSERT INTO CLAIM (
                        BillID, ClaimDate, ClaimStatus, AmountCovered, AmountDenied, ProcessedDate
                    )
                    VALUES (
                        @BillId, @TodayIst, 'Approved', @AmountCovered, 0, @TodayIst
                    )";

                await connection.ExecuteAsync(claimSql, new {
                    BillId = billId,
                    AmountCovered = amountCovered,
                    TodayIst = todayIst
                }, transaction);
            }

            transaction.Commit();
            return billId;
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    public async Task<bool> ProcessPaymentAsync(ProcessPaymentRequest request)
    {
        using var connection = new MySqlConnection(_connectionString);

        var billInfoSql = @"
            SELECT
                b.Amount,
                IFNULL(c.AmountCovered, 0) as AmountCovered,
                (b.Amount - IFNULL(c.AmountCovered, 0)) as PatientResponsibility
            FROM BILLING b
            LEFT JOIN CLAIM c ON b.BillID = c.BillID
            WHERE b.BillID = @BillId";

        var billInfo = await connection.QuerySingleOrDefaultAsync<dynamic>(
            billInfoSql,
            new { request.BillId });

        if (billInfo == null) return false;

        var patientResponsibility = (decimal)billInfo.PatientResponsibility;
        var newStatus = request.AmountPaid >= patientResponsibility ? "Paid" : "Partially Paid";

        var sql = @"
            UPDATE BILLING
            SET Status = @Status
            WHERE BillID = @BillId";

        var affected = await connection.ExecuteAsync(sql, new {
            request.BillId,
            Status = newStatus
        });

        return affected > 0;
    }

    public async Task<IEnumerable<BillingDto>> GetByDoctorIdAsync(int doctorId)
    {
        using var connection = new MySqlConnection(_connectionString);

        var sql = @"
            SELECT
                b.BillID as BillId,
                b.PatientID as PatientId,
                CONCAT(p.FirstName, ' ', p.LastName) as PatientName,
                b.AppointmentID as AppointmentId,
                a.`DateTime` as AppointmentDate,
                a.Reason as AppointmentReason,
                b.Amount,
                b.DiscountApplied,
                b.TaxAmount,
                b.DateIssued,
                b.DueDate,
                b.Status,
                b.PaymentTerms,
                ip.ProviderName as InsuranceProvider,
                pol.PolicyNumber,
                IFNULL(c.AmountCovered, 0) as InsuranceCovered,
                (b.Amount - IFNULL(c.AmountCovered, 0)) as PatientResponsibility
            FROM BILLING b
            INNER JOIN PATIENT p ON b.PatientID = p.PatientID
            INNER JOIN APPOINTMENT a ON b.AppointmentID = a.AppointmentID
            LEFT JOIN CLAIM c ON b.BillID = c.BillID
            LEFT JOIN PATIENT_INSURANCE pi ON p.PatientID = pi.PatientID AND pi.IsPrimary = 1
            LEFT JOIN INSURANCE_POLICY pol ON pi.PolicyID = pol.PolicyID
            LEFT JOIN INSURANCE_PROVIDER ip ON pol.ProviderID = ip.ProviderID
            WHERE a.DoctorID = @DoctorId
            ORDER BY b.DateIssued DESC";

        return await connection.QueryAsync<BillingDto>(sql, new { DoctorId = doctorId });
    }
}

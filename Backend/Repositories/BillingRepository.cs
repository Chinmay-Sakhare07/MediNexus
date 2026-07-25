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
    private readonly ILogger<BillingRepository> _logger;

    public BillingRepository(IConfiguration configuration, ILogger<BillingRepository> logger)
    {
        _connectionString = configuration.GetConnectionString("HospitalDb")!;
        _logger = logger;
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
                b.BillType,
                b.PaymentMethod,
                b.CardSurcharge,
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
                b.BillType,
                b.PaymentMethod,
                b.CardSurcharge,
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
                b.BillType,
                b.PaymentMethod,
                b.CardSurcharge,
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

            if (insurance == null)
            {
                // The telltale for "why didn't insurance apply": logged, human-tone.
                _logger.LogWarning(
                    "No in-force primary insurance for patient #{PatientId} on the billing date — bill issued without a claim",
                    patientId);
            }

            decimal? copayNullable = insurance?.CopayPercentage;
            if (insurance != null && copayNullable.HasValue)
            {
                var copayPercentage = copayNullable.Value;
                var insurancePercentage = (100 - copayPercentage) / 100;
                var amountCovered = Math.Round(totalAmount * insurancePercentage, 2);

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

    public async Task<PaymentReceiptDto> ProcessPaymentAsync(PayBillRequest request)
    {
        using var connection = new MySqlConnection(_connectionString);

        var bill = await connection.QuerySingleOrDefaultAsync<(decimal Amount, decimal Covered, string Status)?>(@"
            SELECT b.Amount, IFNULL(c.AmountCovered, 0) as Covered, b.Status
            FROM BILLING b
            LEFT JOIN CLAIM c ON b.BillID = c.BillID
            WHERE b.BillID = @BillId",
            new { request.BillId });

        if (bill is null) throw new Exceptions.ConflictException("Bill not found.");
        if (bill.Value.Status == "Paid") throw new Exceptions.ConflictException("This bill is already paid.");

        // Insurance is applied via the claim; the remainder is settled Cash or Card.
        var due = Math.Max(0, bill.Value.Amount - bill.Value.Covered);
        var surcharge = request.Method == "Card" ? Math.Round(due * 0.025m, 2) : 0m;

        await connection.ExecuteAsync(@"
            UPDATE BILLING
            SET Status = 'Paid', PaymentMethod = @Method, CardSurcharge = @Surcharge, PaidAt = @PaidAt
            WHERE BillID = @BillId",
            new { request.BillId, request.Method, Surcharge = surcharge, PaidAt = DateTime.UtcNow });

        return new PaymentReceiptDto
        {
            BillId = request.BillId,
            AmountDue = due,
            CardSurcharge = surcharge,
            TotalCharged = due + surcharge,
            Method = request.Method
        };
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
                b.BillType,
                b.PaymentMethod,
                b.CardSurcharge,
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

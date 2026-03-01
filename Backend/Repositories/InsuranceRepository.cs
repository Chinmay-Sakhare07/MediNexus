
using Microsoft.Data.SqlClient;
using Dapper;
using HospitalManagement.API.Models.DTOs;
using HospitalManagement.API.Models.Requests;
using HospitalManagement.API.Repositories.Interfaces;

namespace HospitalManagement.API.Repositories;

public class InsuranceRepository : IInsuranceRepository
{
    private readonly string _connectionString;

    public InsuranceRepository(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("HospitalDb")!;
    }

    public async Task<IEnumerable<InsuranceProviderDto>> GetProvidersAsync()
    {
        using var connection = new SqlConnection(_connectionString);
        
        var sql = @"
            SELECT 
                ProviderID as ProviderId,
                ProviderName,
                ContactNumber
            FROM INSURANCE_PROVIDER
            ORDER BY ProviderName";

        return await connection.QueryAsync<InsuranceProviderDto>(sql);
    }

    public async Task<IEnumerable<InsurancePolicyDto>> GetPoliciesAsync()
    {
        using var connection = new SqlConnection(_connectionString);
        
        var sql = @"
            SELECT 
                p.PolicyID as PolicyId,
                p.ProviderID as ProviderId,
                ip.ProviderName,
                p.PolicyNumber,
                p.CoverageDetails,
                p.PlanType,
                p.ValidFrom,
                p.ValidTo,
                p.CopayPercentage,
                p.MaxCoverageLimit
            FROM INSURANCE_POLICY p
            INNER JOIN INSURANCE_PROVIDER ip ON p.ProviderID = ip.ProviderID
            ORDER BY ip.ProviderName, p.PlanType";

        return await connection.QueryAsync<InsurancePolicyDto>(sql);
    }

    public async Task<IEnumerable<InsurancePolicyDto>> GetPoliciesByProviderAsync(int providerId)
    {
        using var connection = new SqlConnection(_connectionString);
        
        var sql = @"
            SELECT 
                p.PolicyID as PolicyId,
                p.ProviderID as ProviderId,
                ip.ProviderName,
                p.PolicyNumber,
                p.CoverageDetails,
                p.PlanType,
                p.ValidFrom,
                p.ValidTo,
                p.CopayPercentage,
                p.MaxCoverageLimit
            FROM INSURANCE_POLICY p
            INNER JOIN INSURANCE_PROVIDER ip ON p.ProviderID = ip.ProviderID
            WHERE p.ProviderID = @ProviderId
            ORDER BY p.PlanType";

        return await connection.QueryAsync<InsurancePolicyDto>(sql, new { ProviderId = providerId });
    }

    public async Task<IEnumerable<PatientInsuranceDto>> GetPatientInsuranceAsync(int patientId)
    {
        using var connection = new SqlConnection(_connectionString);
        
        var sql = @"
            SELECT 
                pi.PatientID as PatientId,
                pi.PolicyID as PolicyId,
                pol.PolicyNumber,
                ip.ProviderName,
                pol.PlanType,
                pol.CopayPercentage,
                pi.IsPrimary
            FROM PATIENT_INSURANCE pi
            INNER JOIN INSURANCE_POLICY pol ON pi.PolicyID = pol.PolicyID
            INNER JOIN INSURANCE_PROVIDER ip ON pol.ProviderID = ip.ProviderID
            WHERE pi.PatientID = @PatientId
            ORDER BY pi.IsPrimary DESC";

        return await connection.QueryAsync<PatientInsuranceDto>(sql, new { PatientId = patientId });
    }

    public async Task<bool> AssignInsuranceAsync(AssignInsuranceRequest request)
    {
        using var connection = new SqlConnection(_connectionString);
        
        if (request.IsPrimary)
        {
            await connection.ExecuteAsync(
                "UPDATE PATIENT_INSURANCE SET IsPrimary = 0 WHERE PatientID = @PatientId",
                new { PatientId = request.PatientId });
        }

        var sql = @"
            INSERT INTO PATIENT_INSURANCE (PatientID, PolicyID, ValidFrom, ValidTo, IsPrimary)
            VALUES (@PatientId, @PolicyId, GETDATE(), NULL, @IsPrimary)";

        var affected = await connection.ExecuteAsync(sql, request);
        return affected > 0;
    }

    public async Task<bool> RemoveInsuranceAsync(int patientId, int policyId)
    {
        using var connection = new SqlConnection(_connectionString);
        
        var sql = @"
            DELETE FROM PATIENT_INSURANCE 
            WHERE PatientID = @PatientId AND PolicyID = @PolicyId";

        var affected = await connection.ExecuteAsync(sql, new { PatientId = patientId, PolicyId = policyId });
        return affected > 0;
    }
}
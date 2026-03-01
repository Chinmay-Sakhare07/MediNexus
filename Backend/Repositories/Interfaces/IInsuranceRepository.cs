using HospitalManagement.API.Models.DTOs;
using HospitalManagement.API.Models.Requests;

namespace HospitalManagement.API.Repositories.Interfaces;

public interface IInsuranceRepository
{
    Task<IEnumerable<InsuranceProviderDto>> GetProvidersAsync();
    Task<IEnumerable<InsurancePolicyDto>> GetPoliciesAsync();
    Task<IEnumerable<InsurancePolicyDto>> GetPoliciesByProviderAsync(int providerId);
    Task<IEnumerable<PatientInsuranceDto>> GetPatientInsuranceAsync(int patientId);
    Task<bool> AssignInsuranceAsync(AssignInsuranceRequest request);
    Task<bool> RemoveInsuranceAsync(int patientId, int policyId);
}
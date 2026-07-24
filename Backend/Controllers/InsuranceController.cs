using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using HospitalManagement.API.Auth;
using HospitalManagement.API.Models;
using HospitalManagement.API.Models.DTOs;
using HospitalManagement.API.Models.Requests;
using HospitalManagement.API.Repositories.Interfaces;

namespace HospitalManagement.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = Roles.InsuranceRead)]
public class InsuranceController : ControllerBase
{
    private readonly IInsuranceRepository _insuranceRepository;

    public InsuranceController(IInsuranceRepository insuranceRepository)
    {
        _insuranceRepository = insuranceRepository;
    }

    [HttpGet("providers")]
    public async Task<ActionResult<ApiResponse<IEnumerable<InsuranceProviderDto>>>> GetProviders()
    {
        var providers = await _insuranceRepository.GetProvidersAsync();
        return Ok(ApiResponse<IEnumerable<InsuranceProviderDto>>.SuccessResponse(providers));
    }

    [HttpGet("policies")]
    public async Task<ActionResult<ApiResponse<IEnumerable<InsurancePolicyDto>>>> GetPolicies()
    {
        var policies = await _insuranceRepository.GetPoliciesAsync();
        return Ok(ApiResponse<IEnumerable<InsurancePolicyDto>>.SuccessResponse(policies));
    }

    [HttpGet("policies/provider/{providerId}")]
    public async Task<ActionResult<ApiResponse<IEnumerable<InsurancePolicyDto>>>> GetPoliciesByProvider(int providerId)
    {
        var policies = await _insuranceRepository.GetPoliciesByProviderAsync(providerId);
        return Ok(ApiResponse<IEnumerable<InsurancePolicyDto>>.SuccessResponse(policies));
    }

    [HttpGet("patient/{patientId}")]
    public async Task<ActionResult<ApiResponse<IEnumerable<PatientInsuranceDto>>>> GetPatientInsurance(int patientId)
    {
        // Row-level: a Patient account may only read its own coverage.
        if (User.IsInRoleName(Roles.Patient) && User.GetPatientId() != patientId)
            return Forbid();

        var insurance = await _insuranceRepository.GetPatientInsuranceAsync(patientId);
        return Ok(ApiResponse<IEnumerable<PatientInsuranceDto>>.SuccessResponse(insurance));
    }

    [HttpPost("assign")]
    [Authorize(Roles = Roles.FrontDesk)]
    public async Task<ActionResult<ApiResponse<bool>>> AssignInsurance([FromBody] AssignInsuranceRequest request)
    {
        var success = await _insuranceRepository.AssignInsuranceAsync(request);
        return Ok(ApiResponse<bool>.SuccessResponse(true, "Insurance assigned successfully"));
    }

    [HttpDelete("patient/{patientId}/policy/{policyId}")]
    [Authorize(Roles = Roles.FrontDesk)]
    public async Task<ActionResult<ApiResponse<bool>>> RemoveInsurance(int patientId, int policyId)
    {
        var success = await _insuranceRepository.RemoveInsuranceAsync(patientId, policyId);
        if (!success)
            return NotFound(ApiResponse<bool>.ErrorResponse("Insurance assignment not found"));

        return Ok(ApiResponse<bool>.SuccessResponse(true, "Insurance removed successfully"));
    }
}

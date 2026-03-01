using Microsoft.AspNetCore.Mvc;
using HospitalManagement.API.Models;
using HospitalManagement.API.Models.DTOs;
using HospitalManagement.API.Models.Requests;
using HospitalManagement.API.Repositories.Interfaces;

namespace HospitalManagement.API.Controllers;

[ApiController]
[Route("api/[controller]")]
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
        try
        {
            var providers = await _insuranceRepository.GetProvidersAsync();
            return Ok(ApiResponse<IEnumerable<InsuranceProviderDto>>.SuccessResponse(providers));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse<IEnumerable<InsuranceProviderDto>>.ErrorResponse(ex.Message));
        }
    }

    [HttpGet("policies")]
    public async Task<ActionResult<ApiResponse<IEnumerable<InsurancePolicyDto>>>> GetPolicies()
    {
        try
        {
            var policies = await _insuranceRepository.GetPoliciesAsync();
            return Ok(ApiResponse<IEnumerable<InsurancePolicyDto>>.SuccessResponse(policies));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse<IEnumerable<InsurancePolicyDto>>.ErrorResponse(ex.Message));
        }
    }

    [HttpGet("policies/provider/{providerId}")]
    public async Task<ActionResult<ApiResponse<IEnumerable<InsurancePolicyDto>>>> GetPoliciesByProvider(int providerId)
    {
        try
        {
            var policies = await _insuranceRepository.GetPoliciesByProviderAsync(providerId);
            return Ok(ApiResponse<IEnumerable<InsurancePolicyDto>>.SuccessResponse(policies));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse<IEnumerable<InsurancePolicyDto>>.ErrorResponse(ex.Message));
        }
    }

    [HttpGet("patient/{patientId}")]
    public async Task<ActionResult<ApiResponse<IEnumerable<PatientInsuranceDto>>>> GetPatientInsurance(int patientId)
    {
        try
        {
            var insurance = await _insuranceRepository.GetPatientInsuranceAsync(patientId);
            return Ok(ApiResponse<IEnumerable<PatientInsuranceDto>>.SuccessResponse(insurance));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse<IEnumerable<PatientInsuranceDto>>.ErrorResponse(ex.Message));
        }
    }

    [HttpPost("assign")]
    public async Task<ActionResult<ApiResponse<bool>>> AssignInsurance([FromBody] AssignInsuranceRequest request)
    {
        try
        {
            var success = await _insuranceRepository.AssignInsuranceAsync(request);
            return Ok(ApiResponse<bool>.SuccessResponse(true, "Insurance assigned successfully"));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse<bool>.ErrorResponse(ex.Message));
        }
    }

    [HttpDelete("patient/{patientId}/policy/{policyId}")]
    public async Task<ActionResult<ApiResponse<bool>>> RemoveInsurance(int patientId, int policyId)
    {
        try
        {
            var success = await _insuranceRepository.RemoveInsuranceAsync(patientId, policyId);
            if (!success)
                return NotFound(ApiResponse<bool>.ErrorResponse("Insurance assignment not found"));

            return Ok(ApiResponse<bool>.SuccessResponse(true, "Insurance removed successfully"));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse<bool>.ErrorResponse(ex.Message));
        }
    }
}
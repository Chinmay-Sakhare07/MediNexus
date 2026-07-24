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
[Authorize]
public class PatientsController : ControllerBase
{
    private readonly IPatientRepository _patientRepository;

    public PatientsController(IPatientRepository patientRepository)
    {
        _patientRepository = patientRepository;
    }

    [HttpGet]
    [Authorize(Roles = Roles.AllStaff)] // patients never list other patients
    public async Task<ActionResult<ApiResponse<IEnumerable<PatientDto>>>> GetAll()
    {
        var patients = await _patientRepository.GetAllAsync();
        return Ok(ApiResponse<IEnumerable<PatientDto>>.SuccessResponse(patients));
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<PatientDto>>> GetById(int id)
    {
        // Row-level: a Patient account may only read its own record.
        if (User.IsInRoleName(Roles.Patient) && User.GetPatientId() != id)
            return Forbid();

        var patient = await _patientRepository.GetByIdAsync(id);
        if (patient == null)
            return NotFound(ApiResponse<PatientDto>.ErrorResponse("Patient not found"));

        return Ok(ApiResponse<PatientDto>.SuccessResponse(patient));
    }

    [HttpPost]
    [Authorize(Roles = Roles.FrontDesk)]
    public async Task<ActionResult<ApiResponse<int>>> Register([FromBody] RegisterPatientRequest request)
    {
        var patientId = await _patientRepository.RegisterAsync(request);
        return CreatedAtAction(nameof(GetById), new { id = patientId },
            ApiResponse<int>.SuccessResponse(patientId, "Patient registered successfully"));
    }

    [HttpPut("{id}")]
    [Authorize(Roles = Roles.FrontDesk)]
    public async Task<ActionResult<ApiResponse<bool>>> Update(int id, [FromBody] UpdatePatientRequest request)
    {
        var success = await _patientRepository.UpdateAsync(id, request);
        if (!success)
            return NotFound(ApiResponse<bool>.ErrorResponse("Patient not found"));

        return Ok(ApiResponse<bool>.SuccessResponse(true, "Patient updated successfully"));
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = Roles.FrontDesk)]
    public async Task<ActionResult<ApiResponse<bool>>> Delete(int id)
    {
        var success = await _patientRepository.DeleteAsync(id);
        if (!success)
            return NotFound(ApiResponse<bool>.ErrorResponse("Patient not found"));

        return Ok(ApiResponse<bool>.SuccessResponse(true, "Patient deleted successfully"));
    }
}

using Microsoft.AspNetCore.Mvc;
using HospitalManagement.API.Models;
using HospitalManagement.API.Models.DTOs;
using HospitalManagement.API.Models.Requests;
using HospitalManagement.API.Repositories.Interfaces;

namespace HospitalManagement.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PatientsController : ControllerBase
{
    private readonly IPatientRepository _patientRepository;

    public PatientsController(IPatientRepository patientRepository)
    {
        _patientRepository = patientRepository;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<IEnumerable<PatientDto>>>> GetAll()
    {
        try
        {
            var patients = await _patientRepository.GetAllAsync();
            return Ok(ApiResponse<IEnumerable<PatientDto>>.SuccessResponse(patients));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse<IEnumerable<PatientDto>>.ErrorResponse(ex.Message));
        }
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<PatientDto>>> GetById(int id)
    {
        try
        {
            var patient = await _patientRepository.GetByIdAsync(id);
            if (patient == null)
                return NotFound(ApiResponse<PatientDto>.ErrorResponse("Patient not found"));

            return Ok(ApiResponse<PatientDto>.SuccessResponse(patient));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse<PatientDto>.ErrorResponse(ex.Message));
        }
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<int>>> Register([FromBody] RegisterPatientRequest request)
    {
        try
        {
            var patientId = await _patientRepository.RegisterAsync(request);
            return CreatedAtAction(nameof(GetById), new { id = patientId },
                ApiResponse<int>.SuccessResponse(patientId, "Patient registered successfully"));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse<int>.ErrorResponse(ex.Message));
        }
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult<ApiResponse<bool>>> Delete(int id)
    {
        try
        {
            var success = await _patientRepository.DeleteAsync(id);
            if (!success)
                return NotFound(ApiResponse<bool>.ErrorResponse("Patient not found"));

            return Ok(ApiResponse<bool>.SuccessResponse(true, "Patient deleted successfully"));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse<bool>.ErrorResponse(ex.Message));
        }
    }
}
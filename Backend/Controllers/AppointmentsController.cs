using Microsoft.AspNetCore.Mvc;
using HospitalManagement.API.Models;
using HospitalManagement.API.Models.DTOs;
using HospitalManagement.API.Models.Requests;
using HospitalManagement.API.Repositories.Interfaces;

namespace HospitalManagement.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AppointmentsController : ControllerBase
{
    private readonly IAppointmentRepository _appointmentRepository;
    private readonly IInsuranceRepository _insuranceRepository;

    public AppointmentsController(
        IAppointmentRepository appointmentRepository,
        IInsuranceRepository insuranceRepository)
    {
        _appointmentRepository = appointmentRepository;
        _insuranceRepository = insuranceRepository;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<IEnumerable<AppointmentDto>>>> GetAll()
    {
        try
        {
            var appointments = await _appointmentRepository.GetAllAsync();
            return Ok(ApiResponse<IEnumerable<AppointmentDto>>.SuccessResponse(appointments));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse<IEnumerable<AppointmentDto>>.ErrorResponse(ex.Message));
        }
    }

    [HttpGet("today")]
    public async Task<ActionResult<ApiResponse<IEnumerable<AppointmentDto>>>> GetToday()
    {
        try
        {
            var appointments = await _appointmentRepository.GetTodayAsync();
            return Ok(ApiResponse<IEnumerable<AppointmentDto>>.SuccessResponse(appointments));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse<IEnumerable<AppointmentDto>>.ErrorResponse(ex.Message));
        }
    }

    [HttpGet("tomorrow")]
    public async Task<ActionResult<ApiResponse<IEnumerable<AppointmentDto>>>> GetTomorrow()
    {
        try
        {
            var appointments = await _appointmentRepository.GetTomorrowAsync();
            return Ok(ApiResponse<IEnumerable<AppointmentDto>>.SuccessResponse(appointments));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse<IEnumerable<AppointmentDto>>.ErrorResponse(ex.Message));
        }
    }

    [HttpGet("date/{date}")]
    public async Task<ActionResult<ApiResponse<IEnumerable<AppointmentDto>>>> GetByDate(DateTime date)
    {
        try
        {
            var appointments = await _appointmentRepository.GetByDateAsync(date);
            return Ok(ApiResponse<IEnumerable<AppointmentDto>>.SuccessResponse(appointments));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse<IEnumerable<AppointmentDto>>.ErrorResponse(ex.Message));
        }
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<int>>> Schedule([FromBody] ScheduleAppointmentRequest request)
    {
        try
        {
            // Check if patient has insurance
            var patientInsurance = await _insuranceRepository.GetPatientInsuranceAsync(request.PatientId);
            if (!patientInsurance.Any())
            {
                return BadRequest(ApiResponse<int>.ErrorResponse(
                    "Patient must have at least one insurance policy before scheduling an appointment"));
            }

            var appointmentId = await _appointmentRepository.ScheduleAsync(request);
            return CreatedAtAction(nameof(GetAll), new { id = appointmentId },
                ApiResponse<int>.SuccessResponse(appointmentId, "Appointment scheduled successfully"));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse<int>.ErrorResponse(ex.Message));
        }
    }

    [HttpPut("{id}/status")]
    public async Task<ActionResult<ApiResponse<bool>>> UpdateStatus(int id, [FromBody] string status)
    {
        try
        {
            var success = await _appointmentRepository.UpdateStatusAsync(id, status);
            if (!success)
                return NotFound(ApiResponse<bool>.ErrorResponse("Appointment not found"));

            return Ok(ApiResponse<bool>.SuccessResponse(true, "Appointment status updated"));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse<bool>.ErrorResponse(ex.Message));
        }
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult<ApiResponse<bool>>> Delete(int id)
    {
        try
        {
            var success = await _appointmentRepository.DeleteAsync(id);
            if (!success)
                return NotFound(ApiResponse<bool>.ErrorResponse("Appointment not found"));

            return Ok(ApiResponse<bool>.SuccessResponse(true, "Appointment deleted successfully"));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse<bool>.ErrorResponse(ex.Message));
        }
    }
}
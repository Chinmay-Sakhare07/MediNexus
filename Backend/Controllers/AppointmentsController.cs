using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using HospitalManagement.API.Auth;
using HospitalManagement.API.Models;
using HospitalManagement.API.Models.DTOs;
using HospitalManagement.API.Models.Requests;
using HospitalManagement.API.Repositories.Interfaces;

namespace HospitalManagement.API.Controllers;

// Unhandled errors are shaped by ExceptionHandlingMiddleware; controllers stay thin.
[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = Roles.AppointmentsRead)] // Pharmacist has no appointment access (matrix)
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

    private (int? DoctorId, int? PatientId) ScopeFilters() => User.GetRole() switch
    {
        Roles.Doctor  => (User.GetStaffId(), null),
        Roles.Patient => (null, User.GetPatientId()),
        _             => (null, null)
    };

    [HttpGet]
    public async Task<ActionResult<ApiResponse<IEnumerable<AppointmentDto>>>> GetAll()
    {
        var (doctorId, patientId) = ScopeFilters();
        var appointments = await _appointmentRepository.GetAllAsync(doctorId, patientId);
        return Ok(ApiResponse<IEnumerable<AppointmentDto>>.SuccessResponse(appointments));
    }

    [HttpGet("today")]
    public async Task<ActionResult<ApiResponse<IEnumerable<AppointmentDto>>>> GetToday()
    {
        var (doctorId, patientId) = ScopeFilters();
        var appointments = await _appointmentRepository.GetTodayAsync(doctorId, patientId);
        return Ok(ApiResponse<IEnumerable<AppointmentDto>>.SuccessResponse(appointments));
    }

    [HttpGet("tomorrow")]
    public async Task<ActionResult<ApiResponse<IEnumerable<AppointmentDto>>>> GetTomorrow()
    {
        var (doctorId, patientId) = ScopeFilters();
        var appointments = await _appointmentRepository.GetTomorrowAsync(doctorId, patientId);
        return Ok(ApiResponse<IEnumerable<AppointmentDto>>.SuccessResponse(appointments));
    }

    // D6: the date segment is an IST calendar day.
    [HttpGet("date/{date}")]
    public async Task<ActionResult<ApiResponse<IEnumerable<AppointmentDto>>>> GetByDate(DateTime date)
    {
        var (doctorId, patientId) = ScopeFilters();
        var appointments = await _appointmentRepository.GetByDateAsync(date.Date, doctorId, patientId);
        return Ok(ApiResponse<IEnumerable<AppointmentDto>>.SuccessResponse(appointments));
    }

    [HttpPost]
    [Authorize(Roles = Roles.FrontDesk)]
    public async Task<ActionResult<ApiResponse<int>>> Schedule([FromBody] ScheduleAppointmentRequest request)
    {
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

    // Receptionists confirm/cancel; doctors complete — but only their own visits.
    [HttpPut("{id}/status")]
    [Authorize(Roles = Roles.BillingCreate)]
    public async Task<ActionResult<ApiResponse<bool>>> UpdateStatus(int id, [FromBody] string status)
    {
        if (User.IsInRoleName(Roles.Doctor))
        {
            var appointment = await _appointmentRepository.GetByIdAsync(id);
            if (appointment == null)
                return NotFound(ApiResponse<bool>.ErrorResponse("Appointment not found"));
            if (appointment.DoctorId != User.GetStaffId())
                return Forbid();
        }

        var success = await _appointmentRepository.UpdateStatusAsync(id, status);
        if (!success)
            return NotFound(ApiResponse<bool>.ErrorResponse("Appointment not found"));

        return Ok(ApiResponse<bool>.SuccessResponse(true, "Appointment status updated"));
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = Roles.FrontDesk)]
    public async Task<ActionResult<ApiResponse<bool>>> Delete(int id)
    {
        var success = await _appointmentRepository.DeleteAsync(id);
        if (!success)
            return NotFound(ApiResponse<bool>.ErrorResponse("Appointment not found"));

        return Ok(ApiResponse<bool>.SuccessResponse(true, "Appointment deleted successfully"));
    }
}

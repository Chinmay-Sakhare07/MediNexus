using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using HospitalManagement.API.Auth;
using HospitalManagement.API.Models;
using HospitalManagement.API.Models.DTOs;
using HospitalManagement.API.Models.Requests;
using HospitalManagement.API.Repositories.Interfaces;
using HospitalManagement.API.Time;

namespace HospitalManagement.API.Controllers;

// Unhandled errors are shaped by ExceptionHandlingMiddleware; controllers stay thin.
[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = Roles.AppointmentsRead)] // Pharmacist has no appointment access (matrix)
public class AppointmentsController : ControllerBase
{
    private readonly IAppointmentRepository _appointmentRepository;
    private readonly IInsuranceRepository _insuranceRepository;
    private readonly IScheduleRepository _scheduleRepository;
    private readonly ILogger<AppointmentsController> _logger;

    public AppointmentsController(
        IAppointmentRepository appointmentRepository,
        IInsuranceRepository insuranceRepository,
        IScheduleRepository scheduleRepository,
        ILogger<AppointmentsController> logger)
    {
        _appointmentRepository = appointmentRepository;
        _insuranceRepository = insuranceRepository;
        _scheduleRepository = scheduleRepository;
        _logger = logger;
    }

    // D13: both booking paths only accept a slot the schedule actually offers
    // (fixed weekly pattern, minus leave, minus already-booked, minus the past).
    private async Task<bool> SlotIsOfferedAsync(int doctorId, DateTime slotUtc)
    {
        var istDay = IstClock.UtcToIst(slotUtc).Date;
        var slots = await _scheduleRepository.GetSlotsAsync(doctorId, istDay);
        return slots.Contains(slotUtc);
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

    /// <summary>Bookable slots for a doctor on one IST calendar day.</summary>
    [HttpGet("slots")]
    public async Task<ActionResult<ApiResponse<IEnumerable<DateTime>>>> GetSlots(
        [FromQuery] int doctorId, [FromQuery] DateTime date)
    {
        var slots = await _scheduleRepository.GetSlotsAsync(doctorId, date.Date);
        return Ok(ApiResponse<IEnumerable<DateTime>>.SuccessResponse(slots));
    }

    /// <summary>Patient self-booking: lands as Requested, awaiting front-desk approval.</summary>
    [HttpPost("book")]
    [Authorize(Roles = Roles.Patient)]
    public async Task<ActionResult<ApiResponse<int>>> Book([FromBody] BookAppointmentRequest request)
    {
        var patientId = User.GetPatientId() ?? 0;

        var patientInsurance = await _insuranceRepository.GetPatientInsuranceAsync(patientId);
        if (!patientInsurance.Any())
            return BadRequest(ApiResponse<int>.ErrorResponse(
                "You need an active insurance policy on record before booking. Please contact the front desk."));

        if (!await SlotIsOfferedAsync(request.DoctorId, request.DateTime))
            return BadRequest(ApiResponse<int>.ErrorResponse(
                "That slot is not available. Please pick one of the offered times."));

        var id = await _appointmentRepository.BookAsRequestedAsync(patientId, request);
        _logger.LogInformation("{Patient} requested an appointment #{AppointmentId} with doctor #{DoctorId} for {SlotUtc:u}",
            User.FindFirst("displayName")?.Value ?? User.GetUsername(), id, request.DoctorId, request.DateTime);
        return CreatedAtAction(nameof(GetAll), new { id },
            ApiResponse<int>.SuccessResponse(id, "Appointment requested — the front desk will confirm it"));
    }

    [HttpPut("{id}/approve")]
    [Authorize(Roles = Roles.FrontDesk)]
    public async Task<ActionResult<ApiResponse<bool>>> Approve(int id)
    {
        var ok = await _appointmentRepository.TransitionAsync(id, new[] { "Requested" }, "Scheduled");
        if (!ok) return NotFound(ApiResponse<bool>.ErrorResponse("Appointment not found or not awaiting approval"));
        _logger.LogInformation("{Who} approved appointment request #{AppointmentId}",
            User.FindFirst("displayName")?.Value ?? User.GetUsername(), id);
        return Ok(ApiResponse<bool>.SuccessResponse(true, "Appointment approved"));
    }

    [HttpPut("{id}/checkin")]
    [Authorize(Roles = Roles.FrontDesk)]
    public async Task<ActionResult<ApiResponse<bool>>> CheckIn(int id)
    {
        var ok = await _appointmentRepository.TransitionAsync(id, new[] { "Scheduled", "Confirmed" }, "CheckedIn");
        if (!ok) return NotFound(ApiResponse<bool>.ErrorResponse("Appointment not found or not in a check-in-able state"));
        _logger.LogInformation("{Who} checked in the patient for appointment #{AppointmentId}",
            User.FindFirst("displayName")?.Value ?? User.GetUsername(), id);
        return Ok(ApiResponse<bool>.SuccessResponse(true, "Patient checked in"));
    }

    [HttpPut("{id}/start")]
    [Authorize(Roles = Roles.Doctor + "," + Roles.Admin)]
    public async Task<ActionResult<ApiResponse<bool>>> StartConsultation(int id)
    {
        if (User.IsInRoleName(Roles.Doctor))
        {
            var appointment = await _appointmentRepository.GetByIdAsync(id);
            if (appointment == null) return NotFound(ApiResponse<bool>.ErrorResponse("Appointment not found"));
            if (appointment.DoctorId != User.GetStaffId()) return Forbid();
        }
        var ok = await _appointmentRepository.TransitionAsync(id, new[] { "CheckedIn" }, "InConsultation");
        if (!ok) return NotFound(ApiResponse<bool>.ErrorResponse("Patient must be checked in first"));
        _logger.LogInformation("Dr. {Doctor} started the consultation for appointment #{AppointmentId}",
            User.FindFirst("displayName")?.Value ?? User.GetUsername(), id);
        return Ok(ApiResponse<bool>.SuccessResponse(true, "Consultation started"));
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

        if (!await SlotIsOfferedAsync(request.DoctorId, request.DateTime))
            return BadRequest(ApiResponse<int>.ErrorResponse(
                "That slot is not available for this doctor. Please pick one of the offered times."));

        var appointmentId = await _appointmentRepository.ScheduleAsync(request);
        _logger.LogInformation("{Who} scheduled appointment #{AppointmentId} with doctor #{DoctorId} for {SlotUtc:u}",
            User.FindFirst("displayName")?.Value ?? User.GetUsername(), appointmentId, request.DoctorId, request.DateTime);
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

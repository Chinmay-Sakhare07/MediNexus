using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using HospitalManagement.API.Auth;
using HospitalManagement.API.Models;
using HospitalManagement.API.Models.DTOs;
using HospitalManagement.API.Models.Requests;
using HospitalManagement.API.Repositories.Interfaces;

namespace HospitalManagement.API.Controllers;

/// <summary>The Patient File: one visit's timeline plus the clinical actions on it.</summary>
[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = Roles.FileRead)]
public class FilesController : ControllerBase
{
    private readonly IClinicalRepository _clinical;
    private readonly IAppointmentRepository _appointments;
    private readonly ILogger<FilesController> _logger;

    public FilesController(IClinicalRepository clinical, IAppointmentRepository appointments,
        ILogger<FilesController> logger)
    {
        _clinical = clinical;
        _appointments = appointments;
        _logger = logger;
    }

    [HttpGet("{appointmentId}")]
    public async Task<ActionResult<ApiResponse<FileDto>>> Get(int appointmentId)
    {
        var file = await _clinical.GetFileAsync(appointmentId);
        if (file == null)
            return NotFound(ApiResponse<FileDto>.ErrorResponse("File not found"));

        // Row-level: patients open only their own file; doctors only their own visits.
        if (User.IsInRoleName(Roles.Patient) && User.GetPatientId() != file.PatientId) return Forbid();
        if (User.IsInRoleName(Roles.Doctor) && User.GetStaffId() != file.DoctorId) return Forbid();

        return Ok(ApiResponse<FileDto>.SuccessResponse(file));
    }

    [HttpPost("{appointmentId}/vitals")]
    [Authorize(Roles = Roles.VitalsWriters)]
    public async Task<ActionResult<ApiResponse<int>>> RecordVitals(int appointmentId, [FromBody] RecordVitalsRequest request)
    {
        if (await AppointmentMissingOrNotDoctorOwn(appointmentId, doctorMustOwn: User.IsInRoleName(Roles.Doctor)) is { } fail)
            return fail;

        var parts = new List<string>();
        if (!string.IsNullOrWhiteSpace(request.BloodPressure)) parts.Add($"BP {request.BloodPressure}");
        if (!string.IsNullOrWhiteSpace(request.Pulse)) parts.Add($"Pulse {request.Pulse}");
        if (!string.IsNullOrWhiteSpace(request.Temperature)) parts.Add($"Temp {request.Temperature}");
        if (!string.IsNullOrWhiteSpace(request.Spo2)) parts.Add($"SpO2 {request.Spo2}");
        if (!string.IsNullOrWhiteSpace(request.Notes)) parts.Add(request.Notes!);
        var vitals = string.Join(", ", parts);

        var recordId = await _clinical.UpsertVitalsAsync(appointmentId, vitals);
        _logger.LogInformation("{Who} recorded vitals on visit #{AppointmentId} — {Vitals}",
            User.FindFirst("displayName")?.Value ?? User.GetUsername(), appointmentId, vitals);
        return Ok(ApiResponse<int>.SuccessResponse(recordId, "Vitals recorded"));
    }

    [HttpPost("{appointmentId}/consultation")]
    [Authorize(Roles = Roles.Doctor + "," + Roles.Admin)]
    public async Task<ActionResult<ApiResponse<int>>> SaveConsultation(int appointmentId, [FromBody] SaveConsultationRequest request)
    {
        if (await AppointmentMissingOrNotDoctorOwn(appointmentId, doctorMustOwn: true) is { } fail)
            return fail;

        var recordId = await _clinical.SaveConsultationAsync(appointmentId, request);
        _logger.LogInformation("Dr. {Doctor} saved the consultation for visit #{AppointmentId} — diagnosis: {Diagnosis}",
            User.FindFirst("displayName")?.Value ?? User.GetUsername(), appointmentId, request.Diagnosis);
        return Ok(ApiResponse<int>.SuccessResponse(recordId, "Consultation saved"));
    }

    [HttpPost("{appointmentId}/lab-tests")]
    [Authorize(Roles = Roles.Doctor + "," + Roles.Admin)]
    public async Task<ActionResult<ApiResponse<int>>> OrderLabTests(int appointmentId, [FromBody] OrderLabTestsRequest request)
    {
        if (await AppointmentMissingOrNotDoctorOwn(appointmentId, doctorMustOwn: true) is { } fail)
            return fail;

        var count = await _clinical.OrderLabTestsAsync(appointmentId, request);
        _logger.LogInformation("Dr. {Doctor} ordered {Count} lab test(s) for visit #{AppointmentId}",
            User.FindFirst("displayName")?.Value ?? User.GetUsername(), count, appointmentId);
        return Ok(ApiResponse<int>.SuccessResponse(count, $"{count} lab test(s) ordered"));
    }

    [HttpPost("{appointmentId}/prescription")]
    [Authorize(Roles = Roles.Doctor + "," + Roles.Admin)]
    public async Task<ActionResult<ApiResponse<int>>> CreatePrescription(int appointmentId, [FromBody] CreatePrescriptionRequest request)
    {
        if (await AppointmentMissingOrNotDoctorOwn(appointmentId, doctorMustOwn: true) is { } fail)
            return fail;

        var prescriptionId = await _clinical.CreatePrescriptionAsync(appointmentId, User.GetStaffId() ?? 0, request);
        _logger.LogInformation("Dr. {Doctor} sent prescription #{PrescriptionId} ({Lines} item(s)) to the pharmacy for visit #{AppointmentId}",
            User.FindFirst("displayName")?.Value ?? User.GetUsername(), prescriptionId, request.Lines.Count, appointmentId);
        return Ok(ApiResponse<int>.SuccessResponse(prescriptionId, "Prescription sent to pharmacy"));
    }

    private async Task<ActionResult<ApiResponse<int>>?> AppointmentMissingOrNotDoctorOwn(int appointmentId, bool doctorMustOwn)
    {
        var appointment = await _appointments.GetByIdAsync(appointmentId);
        if (appointment == null)
            return NotFound(ApiResponse<int>.ErrorResponse("Appointment not found"));
        if (doctorMustOwn && User.IsInRoleName(Roles.Doctor) && appointment.DoctorId != User.GetStaffId())
            return Forbid();
        return null;
    }
}

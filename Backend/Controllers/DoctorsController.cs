using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using HospitalManagement.API.Models.DTOs;
using HospitalManagement.API.Models.Requests;
using HospitalManagement.API.Auth;
using HospitalManagement.API.Models;
using HospitalManagement.API.Models.DTOs;
using HospitalManagement.API.Repositories.Interfaces;

namespace HospitalManagement.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = Roles.DoctorsRead)]
public class DoctorsController : ControllerBase
{
    private readonly IDoctorRepository _doctorRepository;

    public DoctorsController(IDoctorRepository doctorRepository)
    {
        _doctorRepository = doctorRepository;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<IEnumerable<DoctorDto>>>> GetAll()
    {
        var doctors = await _doctorRepository.GetAllAsync();
        return Ok(ApiResponse<IEnumerable<DoctorDto>>.SuccessResponse(doctors));
    }

    [HttpGet("available")]
    public async Task<ActionResult<ApiResponse<IEnumerable<DoctorDto>>>> GetAvailable()
    {
        var doctors = await _doctorRepository.GetAvailableAsync();
        return Ok(ApiResponse<IEnumerable<DoctorDto>>.SuccessResponse(doctors));
    }

    // ---- Schedule & leave (doctor self, or Admin) ----

    private bool CanManage(int doctorId) =>
        User.IsInRoleName(Roles.Admin) ||
        (User.IsInRoleName(Roles.Doctor) && User.GetStaffId() == doctorId);

    [HttpGet("{id}/schedule")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<DoctorScheduleDto>>> GetSchedule(int id,
        [FromServices] IScheduleRepository schedules)
    {
        var schedule = await schedules.GetScheduleAsync(id);
        if (schedule == null) return NotFound(ApiResponse<DoctorScheduleDto>.ErrorResponse("No schedule found"));
        return Ok(ApiResponse<DoctorScheduleDto>.SuccessResponse(schedule));
    }

    [HttpPut("{id}/schedule")]
    [Authorize(Roles = Roles.Admin + "," + Roles.Doctor)]
    public async Task<ActionResult<ApiResponse<bool>>> UpdateSchedule(int id,
        [FromBody] UpdateScheduleRequest request, [FromServices] IScheduleRepository schedules,
        [FromServices] ILogger<DoctorsController> logger)
    {
        if (!CanManage(id)) return Forbid();
        await schedules.UpdateScheduleAsync(id, request);
        logger.LogInformation("Doctor #{DoctorId} schedule updated: {Days} {Start}-{End}, {Slot}-min slots",
            id, string.Join(",", request.WorkDays), request.StartTime, request.EndTime, request.SlotMinutes);
        return Ok(ApiResponse<bool>.SuccessResponse(true, "Schedule updated"));
    }

    [HttpGet("{id}/leaves")]
    [Authorize(Roles = Roles.Admin + "," + Roles.Doctor)]
    public async Task<ActionResult<ApiResponse<IEnumerable<DoctorLeaveDto>>>> GetLeaves(int id,
        [FromServices] IScheduleRepository schedules)
    {
        if (!CanManage(id)) return Forbid();
        var leaves = await schedules.GetLeavesAsync(id);
        return Ok(ApiResponse<IEnumerable<DoctorLeaveDto>>.SuccessResponse(leaves));
    }

    [HttpPost("{id}/leaves")]
    [Authorize(Roles = Roles.Admin + "," + Roles.Doctor)]
    public async Task<ActionResult<ApiResponse<int>>> AddLeave(int id,
        [FromBody] AddLeaveRequest request, [FromServices] IScheduleRepository schedules,
        [FromServices] ILogger<DoctorsController> logger)
    {
        if (!CanManage(id)) return Forbid();
        var (leaveId, cancelled) = await schedules.AddLeaveAsync(id, request.LeaveDate, request.Reason);
        logger.LogWarning("Doctor #{DoctorId} filed leave for {Date:yyyy-MM-dd} — {Cancelled} appointment(s) cancelled",
            id, request.LeaveDate, cancelled);
        return Ok(ApiResponse<int>.SuccessResponse(leaveId,
            cancelled > 0
                ? $"Leave filed — {cancelled} appointment(s) on that day were cancelled"
                : "Leave filed"));
    }

    [HttpDelete("{id}/leaves/{leaveId}")]
    [Authorize(Roles = Roles.Admin + "," + Roles.Doctor)]
    public async Task<ActionResult<ApiResponse<bool>>> RemoveLeave(int id, int leaveId,
        [FromServices] IScheduleRepository schedules)
    {
        if (!CanManage(id)) return Forbid();
        var ok = await schedules.RemoveLeaveAsync(id, leaveId);
        if (!ok) return NotFound(ApiResponse<bool>.ErrorResponse("Leave entry not found"));
        return Ok(ApiResponse<bool>.SuccessResponse(true, "Leave removed"));
    }
}

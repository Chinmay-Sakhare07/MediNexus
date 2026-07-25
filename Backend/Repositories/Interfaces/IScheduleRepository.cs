using HospitalManagement.API.Models.DTOs;
using HospitalManagement.API.Models.Requests;

namespace HospitalManagement.API.Repositories.Interfaces;

public interface IScheduleRepository
{
    Task<DoctorScheduleDto?> GetScheduleAsync(int doctorId);
    Task UpdateScheduleAsync(int doctorId, UpdateScheduleRequest request);
    Task<IEnumerable<DoctorLeaveDto>> GetLeavesAsync(int doctorId);
    Task<(int LeaveId, int CancelledAppointments)> AddLeaveAsync(int doctorId, DateTime istDate, string? reason);
    Task<bool> RemoveLeaveAsync(int doctorId, int leaveId);
    Task<IEnumerable<DateTime>> GetSlotsAsync(int doctorId, DateTime istDate);
}

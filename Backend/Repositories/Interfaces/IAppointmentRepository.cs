using HospitalManagement.API.Models.DTOs;
using HospitalManagement.API.Models.Requests;

namespace HospitalManagement.API.Repositories.Interfaces;

public interface IAppointmentRepository
{
    // Optional filters implement row-level access (SCOPE §1):
    // Doctor role passes doctorId (= StaffID claim), Patient role passes patientId.
    Task<IEnumerable<AppointmentDto>> GetAllAsync(int? doctorId = null, int? patientId = null);
    Task<IEnumerable<AppointmentDto>> GetTodayAsync(int? doctorId = null, int? patientId = null);
    Task<IEnumerable<AppointmentDto>> GetTomorrowAsync(int? doctorId = null, int? patientId = null);
    Task<IEnumerable<AppointmentDto>> GetByDateAsync(DateTime istDate, int? doctorId = null, int? patientId = null);
    Task<AppointmentDto?> GetByIdAsync(int id);
    Task<int> ScheduleAsync(ScheduleAppointmentRequest request);
    Task<int> BookAsRequestedAsync(int patientId, BookAppointmentRequest request);
    /// <summary>Guarded transition: only fires when the current status is in `from`.</summary>
    Task<bool> TransitionAsync(int id, string[] from, string to);
    Task<bool> UpdateStatusAsync(int id, string status);
    Task<bool> DeleteAsync(int id);
}

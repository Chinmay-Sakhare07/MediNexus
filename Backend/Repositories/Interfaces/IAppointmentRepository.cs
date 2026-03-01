using HospitalManagement.API.Models.DTOs;
using HospitalManagement.API.Models.Requests;

namespace HospitalManagement.API.Repositories.Interfaces;

public interface IAppointmentRepository
{
    Task<IEnumerable<AppointmentDto>> GetAllAsync();
    Task<IEnumerable<AppointmentDto>> GetTodayAsync();
    Task<IEnumerable<AppointmentDto>> GetTomorrowAsync();
    Task<IEnumerable<AppointmentDto>> GetByDateAsync(DateTime date);
    Task<int> ScheduleAsync(ScheduleAppointmentRequest request);
    Task<bool> UpdateStatusAsync(int id, string status);
    Task<bool> DeleteAsync(int id);
}
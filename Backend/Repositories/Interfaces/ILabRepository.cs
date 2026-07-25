using HospitalManagement.API.Models.DTOs;

namespace HospitalManagement.API.Repositories.Interfaces;

public interface ILabRepository
{
    Task<IEnumerable<LabQueueItemDto>> GetQueueAsync(int? technicianId);
    Task<bool> StartAsync(int labTestId, int? technicianId);
    Task<bool> EnterResultAsync(int labTestId, int? technicianId, string result, string? comments);
}

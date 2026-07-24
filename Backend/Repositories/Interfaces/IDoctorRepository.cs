using HospitalManagement.API.Models.DTOs;

namespace HospitalManagement.API.Repositories.Interfaces;

public interface IDoctorRepository
{
    Task<IEnumerable<DoctorDto>> GetAllAsync();
    Task<IEnumerable<DoctorDto>> GetAvailableAsync();
}
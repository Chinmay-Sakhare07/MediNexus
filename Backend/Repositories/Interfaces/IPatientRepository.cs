using HospitalManagement.API.Models.DTOs;
using HospitalManagement.API.Models.Requests;

namespace HospitalManagement.API.Repositories.Interfaces;

public interface IPatientRepository
{
    Task<IEnumerable<PatientDto>> GetAllAsync();
    Task<PatientDto?> GetByIdAsync(int id);
    Task<int> RegisterAsync(RegisterPatientRequest request);
    Task<bool> DeleteAsync(int id);
}
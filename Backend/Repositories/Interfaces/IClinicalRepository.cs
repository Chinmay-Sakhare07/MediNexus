using HospitalManagement.API.Models.DTOs;
using HospitalManagement.API.Models.Requests;

namespace HospitalManagement.API.Repositories.Interfaces;

public interface IClinicalRepository
{
    Task<FileDto?> GetFileAsync(int appointmentId);
    Task<int> UpsertVitalsAsync(int appointmentId, string vitalSigns);
    Task<int> SaveConsultationAsync(int appointmentId, SaveConsultationRequest request);
    Task<int> OrderLabTestsAsync(int appointmentId, OrderLabTestsRequest request);
    Task<int> CreatePrescriptionAsync(int appointmentId, int doctorId, CreatePrescriptionRequest request);
}

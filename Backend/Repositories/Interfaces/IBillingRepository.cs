using HospitalManagement.API.Models.DTOs;
using HospitalManagement.API.Models.Requests;

namespace HospitalManagement.API.Repositories.Interfaces;

public interface IBillingRepository
{
    Task<IEnumerable<BillingDto>> GetAllAsync();
    Task<IEnumerable<BillingDto>> GetByPatientIdAsync(int patientId);
    Task<BillingDto?> GetByIdAsync(int id);
    Task<int> CompleteAppointmentWithBillingAsync(CompleteAppointmentRequest request);
    Task<bool> ProcessPaymentAsync(ProcessPaymentRequest request);
}
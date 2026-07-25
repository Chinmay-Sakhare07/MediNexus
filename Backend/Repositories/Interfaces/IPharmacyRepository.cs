using HospitalManagement.API.Models.DTOs;

namespace HospitalManagement.API.Repositories.Interfaces;

public interface IPharmacyRepository
{
    Task<IEnumerable<MedicineDto>> GetMedicinesAsync();
    Task<int> AdjustStockAsync(int medicineId, int adjustment);
    Task<IEnumerable<PharmacyQueueItemDto>> GetQueueAsync();
    Task<bool> ConfirmAsync(int prescriptionId);
    Task<bool> RejectAsync(int prescriptionId, string reason);
    Task<bool> MarkReadyAsync(int prescriptionId);
    Task<DispenseResultDto> DispenseAsync(int prescriptionId, int dispensedByUserId);
}

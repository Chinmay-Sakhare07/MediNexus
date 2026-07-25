using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using HospitalManagement.API.Auth;
using HospitalManagement.API.Models;
using HospitalManagement.API.Models.DTOs;
using HospitalManagement.API.Models.Requests;
using HospitalManagement.API.Repositories.Interfaces;

namespace HospitalManagement.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = Roles.Pharmacy)]
public class PharmacyController : ControllerBase
{
    private readonly IPharmacyRepository _pharmacy;
    private readonly ILogger<PharmacyController> _logger;

    public PharmacyController(IPharmacyRepository pharmacy, ILogger<PharmacyController> logger)
    {
        _pharmacy = pharmacy;
        _logger = logger;
    }

    // Doctors also read the catalog (they pick medicines while prescribing).
    [HttpGet("medicines")]
    [Authorize(Roles = Roles.MedicineRead)]
    public async Task<ActionResult<ApiResponse<IEnumerable<MedicineDto>>>> GetMedicines()
    {
        var medicines = await _pharmacy.GetMedicinesAsync();
        return Ok(ApiResponse<IEnumerable<MedicineDto>>.SuccessResponse(medicines));
    }

    [HttpPut("medicines/{id}/stock")]
    public async Task<ActionResult<ApiResponse<int>>> AdjustStock(int id, [FromBody] AdjustStockRequest request)
    {
        var newQuantity = await _pharmacy.AdjustStockAsync(id, request.Adjustment);
        var verb = request.Adjustment > 0 ? "restocked" : "corrected";
        _logger.LogInformation("{Who} {Verb} medicine #{MedicineId} by {Adjustment} — now {NewQuantity} in stock. {Note}",
            User.FindFirst("displayName")?.Value ?? User.GetUsername(), verb, id,
            request.Adjustment, newQuantity, request.Note ?? string.Empty);
        return Ok(ApiResponse<int>.SuccessResponse(newQuantity, $"Stock updated — now {newQuantity}"));
    }

    [HttpGet("queue")]
    public async Task<ActionResult<ApiResponse<IEnumerable<PharmacyQueueItemDto>>>> GetQueue()
    {
        var queue = await _pharmacy.GetQueueAsync();
        return Ok(ApiResponse<IEnumerable<PharmacyQueueItemDto>>.SuccessResponse(queue));
    }

    [HttpPut("prescriptions/{id}/confirm")]
    public async Task<ActionResult<ApiResponse<bool>>> Confirm(int id)
    {
        var ok = await _pharmacy.ConfirmAsync(id);
        if (!ok) return NotFound(ApiResponse<bool>.ErrorResponse("Prescription not found or not awaiting confirmation"));
        _logger.LogInformation("Pharmacy confirmed prescription #{PrescriptionId}; preparing items", id);
        return Ok(ApiResponse<bool>.SuccessResponse(true, "Prescription confirmed"));
    }

    [HttpPut("prescriptions/{id}/reject")]
    public async Task<ActionResult<ApiResponse<bool>>> Reject(int id, [FromBody] RejectPrescriptionRequest request)
    {
        var ok = await _pharmacy.RejectAsync(id, request.Reason);
        if (!ok) return NotFound(ApiResponse<bool>.ErrorResponse("Prescription not found or already past rejection"));
        _logger.LogWarning("Pharmacy rejected prescription #{PrescriptionId} — {Reason}", id, request.Reason);
        return Ok(ApiResponse<bool>.SuccessResponse(true, "Prescription rejected"));
    }

    [HttpPut("prescriptions/{id}/ready")]
    public async Task<ActionResult<ApiResponse<bool>>> Ready(int id)
    {
        var ok = await _pharmacy.MarkReadyAsync(id);
        if (!ok) return NotFound(ApiResponse<bool>.ErrorResponse("Prescription not found or not confirmed yet"));
        _logger.LogInformation("Prescription #{PrescriptionId} is ready for pickup", id);
        return Ok(ApiResponse<bool>.SuccessResponse(true, "Marked ready for pickup"));
    }

    [HttpPost("prescriptions/{id}/dispense")]
    public async Task<ActionResult<ApiResponse<DispenseResultDto>>> Dispense(int id)
    {
        var result = await _pharmacy.DispenseAsync(id, User.GetUserId() ?? 0);
        _logger.LogInformation(
            "{Who} dispensed prescription #{PrescriptionId} — pharmacy bill #{BillId} for {Amount:0.00} (insurance covered {Covered:0.00})",
            User.FindFirst("displayName")?.Value ?? User.GetUsername(), id,
            result.BillId, result.Amount, result.InsuranceCovered);
        return Ok(ApiResponse<DispenseResultDto>.SuccessResponse(result, "Dispensed — pharmacy bill created"));
    }
}

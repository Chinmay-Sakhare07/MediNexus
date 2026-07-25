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
[Authorize(Roles = Roles.BillingRead)]
public class BillingController : ControllerBase
{
    private readonly IBillingRepository _billingRepository;

    public BillingController(IBillingRepository billingRepository)
    {
        _billingRepository = billingRepository;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<IEnumerable<BillingDto>>>> GetAll()
    {
        // Row-level scope on the list endpoint.
        var bills = User.GetRole() switch
        {
            Roles.Patient => await _billingRepository.GetByPatientIdAsync(User.GetPatientId() ?? 0),
            Roles.Doctor  => await _billingRepository.GetByDoctorIdAsync(User.GetStaffId() ?? 0),
            _             => await _billingRepository.GetAllAsync()
        };
        return Ok(ApiResponse<IEnumerable<BillingDto>>.SuccessResponse(bills));
    }

    [HttpGet("patient/{patientId}")]
    public async Task<ActionResult<ApiResponse<IEnumerable<BillingDto>>>> GetByPatientId(int patientId)
    {
        // Row-level: a Patient account may only read its own bills.
        if (User.IsInRoleName(Roles.Patient) && User.GetPatientId() != patientId)
            return Forbid();

        var bills = await _billingRepository.GetByPatientIdAsync(patientId);
        return Ok(ApiResponse<IEnumerable<BillingDto>>.SuccessResponse(bills));
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<BillingDto>>> GetById(int id)
    {
        var bill = await _billingRepository.GetByIdAsync(id);
        if (bill == null)
            return NotFound(ApiResponse<BillingDto>.ErrorResponse("Bill not found"));

        // Row-level: patients can only open their own bill.
        if (User.IsInRoleName(Roles.Patient) && User.GetPatientId() != bill.PatientId)
            return Forbid();

        return Ok(ApiResponse<BillingDto>.SuccessResponse(bill));
    }

    [HttpPost("complete-appointment")]
    [Authorize(Roles = Roles.BillingCreate)] // doctors complete visits; front desk can too
    public async Task<ActionResult<ApiResponse<int>>> CompleteAppointment([FromBody] CompleteAppointmentRequest request)
    {
        var billId = await _billingRepository.CompleteAppointmentWithBillingAsync(request);
        return Ok(ApiResponse<int>.SuccessResponse(billId, "Appointment completed and bill generated successfully"));
    }

    [HttpPost("pay")]
    [Authorize(Roles = Roles.FrontDesk)]
    public async Task<ActionResult<ApiResponse<PaymentReceiptDto>>> ProcessPayment(
        [FromBody] PayBillRequest request, [FromServices] ILogger<BillingController> logger)
    {
        var receipt = await _billingRepository.ProcessPaymentAsync(request);
        logger.LogInformation(
            "Bill #{BillId} paid by {Method}: due {Due:0.00}" +
            (receipt.CardSurcharge > 0 ? " + {Surcharge:0.00} card service charge" : "") +
            " = {Total:0.00} charged",
            receipt.BillId, receipt.Method, receipt.AmountDue, receipt.CardSurcharge, receipt.TotalCharged);
        return Ok(ApiResponse<PaymentReceiptDto>.SuccessResponse(receipt,
            receipt.CardSurcharge > 0
                ? $"Paid by card — {receipt.TotalCharged:0.00} charged (incl. {receipt.CardSurcharge:0.00} service charge)"
                : $"Paid by {receipt.Method.ToLower()} — {receipt.TotalCharged:0.00} received"));
    }
}

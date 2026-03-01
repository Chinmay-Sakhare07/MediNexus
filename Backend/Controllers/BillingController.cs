using Microsoft.AspNetCore.Mvc;
using HospitalManagement.API.Models;
using HospitalManagement.API.Models.DTOs;
using HospitalManagement.API.Models.Requests;
using HospitalManagement.API.Repositories.Interfaces;

namespace HospitalManagement.API.Controllers;

[ApiController]
[Route("api/[controller]")]
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
        try
        {
            var bills = await _billingRepository.GetAllAsync();
            return Ok(ApiResponse<IEnumerable<BillingDto>>.SuccessResponse(bills));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse<IEnumerable<BillingDto>>.ErrorResponse(ex.Message));
        }
    }

    [HttpGet("patient/{patientId}")]
    public async Task<ActionResult<ApiResponse<IEnumerable<BillingDto>>>> GetByPatientId(int patientId)
    {
        try
        {
            var bills = await _billingRepository.GetByPatientIdAsync(patientId);
            return Ok(ApiResponse<IEnumerable<BillingDto>>.SuccessResponse(bills));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse<IEnumerable<BillingDto>>.ErrorResponse(ex.Message));
        }
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<BillingDto>>> GetById(int id)
    {
        try
        {
            var bill = await _billingRepository.GetByIdAsync(id);
            if (bill == null)
                return NotFound(ApiResponse<BillingDto>.ErrorResponse("Bill not found"));

            return Ok(ApiResponse<BillingDto>.SuccessResponse(bill));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse<BillingDto>.ErrorResponse(ex.Message));
        }
    }

    [HttpPost("complete-appointment")]
    public async Task<ActionResult<ApiResponse<int>>> CompleteAppointment([FromBody] CompleteAppointmentRequest request)
    {
        try
        {
            var billId = await _billingRepository.CompleteAppointmentWithBillingAsync(request);
            return Ok(ApiResponse<int>.SuccessResponse(billId, "Appointment completed and bill generated successfully"));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse<int>.ErrorResponse(ex.Message));
        }
    }

    [HttpPost("pay")]
    public async Task<ActionResult<ApiResponse<bool>>> ProcessPayment([FromBody] ProcessPaymentRequest request)
    {
        try
        {
            var success = await _billingRepository.ProcessPaymentAsync(request);
            return Ok(ApiResponse<bool>.SuccessResponse(true, "Payment processed successfully"));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse<bool>.ErrorResponse(ex.Message));
        }
    }
}
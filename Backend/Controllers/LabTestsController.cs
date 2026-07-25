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
[Authorize(Roles = Roles.Lab)]
public class LabTestsController : ControllerBase
{
    private readonly ILabRepository _lab;
    private readonly ILogger<LabTestsController> _logger;

    public LabTestsController(ILabRepository lab, ILogger<LabTestsController> logger)
    {
        _lab = lab;
        _logger = logger;
    }

    // A technician works their own queue; Admin sees the whole lab.
    private int? ScopedTechnicianId() =>
        User.IsInRoleName(Roles.LabTech) ? User.GetStaffId() : null;

    [HttpGet("queue")]
    public async Task<ActionResult<ApiResponse<IEnumerable<LabQueueItemDto>>>> GetQueue()
    {
        var queue = await _lab.GetQueueAsync(ScopedTechnicianId());
        return Ok(ApiResponse<IEnumerable<LabQueueItemDto>>.SuccessResponse(queue));
    }

    [HttpPut("{id}/start")]
    public async Task<ActionResult<ApiResponse<bool>>> Start(int id)
    {
        var ok = await _lab.StartAsync(id, ScopedTechnicianId());
        if (!ok) return NotFound(ApiResponse<bool>.ErrorResponse("Test not found in your pending queue"));
        _logger.LogInformation("{Who} started lab test #{LabTestId}",
            User.FindFirst("displayName")?.Value ?? User.GetUsername(), id);
        return Ok(ApiResponse<bool>.SuccessResponse(true, "Test started"));
    }

    [HttpPut("{id}/result")]
    public async Task<ActionResult<ApiResponse<bool>>> EnterResult(int id, [FromBody] EnterLabResultRequest request)
    {
        var ok = await _lab.EnterResultAsync(id, ScopedTechnicianId(), request.Result, request.Comments);
        if (!ok) return NotFound(ApiResponse<bool>.ErrorResponse("Test not found in your queue"));
        _logger.LogInformation("{Who} completed lab test #{LabTestId} — result: {Result}",
            User.FindFirst("displayName")?.Value ?? User.GetUsername(), id, request.Result);
        return Ok(ApiResponse<bool>.SuccessResponse(true, "Result recorded"));
    }
}

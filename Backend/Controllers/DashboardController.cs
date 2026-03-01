using Microsoft.AspNetCore.Mvc;
using HospitalManagement.API.Models;
using HospitalManagement.API.Models.DTOs;
using HospitalManagement.API.Repositories.Interfaces;

namespace HospitalManagement.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DashboardController : ControllerBase
{
    private readonly IDashboardRepository _dashboardRepository;

    public DashboardController(IDashboardRepository dashboardRepository)
    {
        _dashboardRepository = dashboardRepository;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<DashboardDto>>> GetStats()
    {
        try
        {
            var stats = await _dashboardRepository.GetStatsAsync();
            return Ok(ApiResponse<DashboardDto>.SuccessResponse(stats));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse<DashboardDto>.ErrorResponse(ex.Message));
        }
    }
}
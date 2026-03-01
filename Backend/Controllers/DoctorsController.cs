using Microsoft.AspNetCore.Mvc;
using HospitalManagement.API.Models;
using HospitalManagement.API.Models.DTOs;
using HospitalManagement.API.Repositories.Interfaces;

namespace HospitalManagement.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DoctorsController : ControllerBase
{
    private readonly IDoctorRepository _doctorRepository;

    public DoctorsController(IDoctorRepository doctorRepository)
    {
        _doctorRepository = doctorRepository;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<IEnumerable<DoctorDto>>>> GetAll()
    {
        try
        {
            var doctors = await _doctorRepository.GetAllAsync();
            return Ok(ApiResponse<IEnumerable<DoctorDto>>.SuccessResponse(doctors));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse<IEnumerable<DoctorDto>>.ErrorResponse(ex.Message));
        }
    }

    [HttpGet("available")]
    public async Task<ActionResult<ApiResponse<IEnumerable<DoctorDto>>>> GetAvailable()
    {
        try
        {
            var doctors = await _doctorRepository.GetAvailableAsync();
            return Ok(ApiResponse<IEnumerable<DoctorDto>>.SuccessResponse(doctors));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse<IEnumerable<DoctorDto>>.ErrorResponse(ex.Message));
        }
    }
}
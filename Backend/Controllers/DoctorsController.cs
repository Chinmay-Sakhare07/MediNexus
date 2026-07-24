using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using HospitalManagement.API.Auth;
using HospitalManagement.API.Models;
using HospitalManagement.API.Models.DTOs;
using HospitalManagement.API.Repositories.Interfaces;

namespace HospitalManagement.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = Roles.DoctorsRead)]
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
        var doctors = await _doctorRepository.GetAllAsync();
        return Ok(ApiResponse<IEnumerable<DoctorDto>>.SuccessResponse(doctors));
    }

    [HttpGet("available")]
    public async Task<ActionResult<ApiResponse<IEnumerable<DoctorDto>>>> GetAvailable()
    {
        var doctors = await _doctorRepository.GetAvailableAsync();
        return Ok(ApiResponse<IEnumerable<DoctorDto>>.SuccessResponse(doctors));
    }
}
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using HospitalManagement.API.Auth;
using HospitalManagement.API.Models;
using HospitalManagement.API.Models.DTOs;
using HospitalManagement.API.Models.Requests;
using HospitalManagement.API.Repositories.Interfaces;

namespace HospitalManagement.API.Controllers;

/// <summary>Admin user management. Deletion is soft: IsActive=0 disables
/// sign-in and removes the account from View-as, but keeps all history.</summary>
[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = Roles.Admin)]
public class UsersController : ControllerBase
{
    private readonly IUserRepository _users;
    private readonly ILogger<UsersController> _logger;

    public UsersController(IUserRepository users, ILogger<UsersController> logger)
    {
        _users = users;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<IEnumerable<UserAdminDto>>>> GetAll(
        [FromQuery] bool includeInactive = false)
    {
        var users = await _users.GetAllUsersAsync(includeInactive);
        return Ok(ApiResponse<IEnumerable<UserAdminDto>>.SuccessResponse(users));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<int>>> Create([FromBody] CreateUserRequest request)
    {
        var usedDefault = string.IsNullOrEmpty(request.Password);
        var hash = BCrypt.Net.BCrypt.HashPassword(usedDefault ? Defaults.UserPassword : request.Password);
        var userId = await _users.CreateUserAsync(request, hash);
        _logger.LogInformation("{Admin} created user {Username} ({Role}){DefaultNote}",
            User.GetUsername(), request.Username, request.Role,
            usedDefault ? " with the default password" : "");
        return CreatedAtAction(nameof(GetAll), new { id = userId },
            ApiResponse<int>.SuccessResponse(userId,
                usedDefault
                    ? $"User {request.Username} created with the default password — ask them to change it after first sign-in"
                    : $"User {request.Username} created"));
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<ApiResponse<bool>>> Update(int id, [FromBody] UpdateUserRequest request)
    {
        var target = await _users.GetByIdAsync(id);
        if (target == null) return NotFound(ApiResponse<bool>.ErrorResponse("User not found"));

        // Demoting the last active admin locks everyone out. Refuse.
        if (target.Role == Roles.Admin && request.Role != Roles.Admin
            && await _users.CountActiveAdminsAsync() <= 1)
            return BadRequest(ApiResponse<bool>.ErrorResponse(
                "This is the last active admin account; assign another admin first."));

        var ok = await _users.UpdateUserAsync(id, request);
        if (!ok) return NotFound(ApiResponse<bool>.ErrorResponse("User not found"));

        _logger.LogInformation("{Admin} updated user {Username}", User.GetUsername(), target.Username);
        return Ok(ApiResponse<bool>.SuccessResponse(true, "User updated"));
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult<ApiResponse<bool>>> Deactivate(int id)
    {
        var target = await _users.GetByIdAsync(id);
        if (target == null) return NotFound(ApiResponse<bool>.ErrorResponse("User not found"));

        if (target.UserID == (User.GetUserId() ?? -1))
            return BadRequest(ApiResponse<bool>.ErrorResponse("You cannot deactivate your own account."));

        if (target.Role == Roles.Admin && await _users.CountActiveAdminsAsync() <= 1)
            return BadRequest(ApiResponse<bool>.ErrorResponse(
                "This is the last active admin account; it cannot be deactivated."));

        await _users.SetActiveAsync(id, false);
        _logger.LogWarning("{Admin} deactivated user {Username} ({Role}) — sign-in disabled, history kept",
            User.GetUsername(), target.Username, target.Role);
        return Ok(ApiResponse<bool>.SuccessResponse(true, $"{target.Username} deactivated"));
    }

    [HttpPut("{id}/activate")]
    public async Task<ActionResult<ApiResponse<bool>>> Reactivate(int id)
    {
        var target = await _users.GetByIdAsync(id);
        if (target == null) return NotFound(ApiResponse<bool>.ErrorResponse("User not found"));

        await _users.SetActiveAsync(id, true);
        _logger.LogInformation("{Admin} reactivated user {Username} — sign-in restored",
            User.GetUsername(), target.Username);
        return Ok(ApiResponse<bool>.SuccessResponse(true, $"{target.Username} reactivated"));
    }

    /// <summary>Overwrites the user's password with the default (MediNexus@2026).</summary>
    [HttpPost("{id}/reset-password")]
    public async Task<ActionResult<ApiResponse<bool>>> ResetPassword(int id)
    {
        var target = await _users.GetByIdAsync(id);
        if (target == null) return NotFound(ApiResponse<bool>.ErrorResponse("User not found"));

        var hash = BCrypt.Net.BCrypt.HashPassword(Defaults.UserPassword);
        await _users.UpdatePasswordHashAsync(id, hash);
        _logger.LogWarning("{Admin} reset {Username}'s password to the default",
            User.GetUsername(), target.Username);
        return Ok(ApiResponse<bool>.SuccessResponse(true,
            $"{target.Username}'s password was reset to the default — ask them to change it after signing in"));
    }
}

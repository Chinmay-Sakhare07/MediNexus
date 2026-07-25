using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using HospitalManagement.API.Models.Requests;
using Microsoft.AspNetCore.RateLimiting;
using HospitalManagement.API.Auth;
using HospitalManagement.API.Models;
using HospitalManagement.API.Models.DTOs;
using HospitalManagement.API.Repositories.Interfaces;
using HospitalManagement.API.Services;

namespace HospitalManagement.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IUserRepository _users;
    private readonly ITokenService _tokens;
    private readonly ILogger<AuthController> _logger;

    public AuthController(IUserRepository users, ITokenService tokens, ILogger<AuthController> logger)
    {
        _users = users;
        _tokens = tokens;
        _logger = logger;
    }

    /// <summary>Login with username OR email + password. Rate limited (see Program.cs "auth" policy).</summary>
    [HttpPost("login")]
    [AllowAnonymous]
    [EnableRateLimiting("auth")]
    public async Task<ActionResult<ApiResponse<LoginResponse>>> Login([FromBody] LoginRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Login) || string.IsNullOrWhiteSpace(request.Password))
            return BadRequest(ApiResponse<LoginResponse>.ErrorResponse("Username and password are required"));

        try
        {
            var user = await _users.GetByLoginAsync(request.Login.Trim());

            // One generic client message for all failure modes (don't leak which part failed);
            // the specific reason goes to structured logs -> LogBase in Phase 5 (auth events, SCOPE §2.3).
            if (user is null)
                return LoginFailed(request.Login, "unknown_user");

            if (!user.IsActive)
                return LoginFailed(user.Username, "inactive_account");

            if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
                return LoginFailed(user.Username, "bad_password");

            var (token, expiresAtUtc) = _tokens.CreateToken(user);
            await _users.UpdateLastLoginAsync(user.UserID, DateTime.UtcNow);

            _logger.LogInformation("LOGIN_SUCCESS username={Username} role={Role}", user.Username, user.Role);

            return Ok(ApiResponse<LoginResponse>.SuccessResponse(new LoginResponse
            {
                Token = token,
                ExpiresAtUtc = expiresAtUtc,
                User = new AuthUserDto
                {
                    UserId = user.UserID,
                    Username = user.Username,
                    DisplayName = user.DisplayName,
                    Role = user.Role,
                    StaffId = user.StaffID,
                    PatientId = user.PatientID
                }
            }, "Login successful"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Login failed unexpectedly for {Login}", request.Login);
            return StatusCode(500, ApiResponse<LoginResponse>.ErrorResponse("Something went wrong. Please try again."));
        }
    }

    /// <summary>Who am I — lets the frontend re-validate a stored token on refresh.</summary>
    [HttpGet("me")]
    [Authorize]
    public ActionResult<ApiResponse<AuthUserDto>> Me()
    {
        return Ok(ApiResponse<AuthUserDto>.SuccessResponse(new AuthUserDto
        {
            UserId = User.GetUserId() ?? 0,
            Username = User.GetUsername(),
            DisplayName = User.FindFirst("displayName")?.Value ?? User.GetUsername(),
            Role = User.GetRole(),
            StaffId = User.GetStaffId(),
            PatientId = User.GetPatientId()
        }));
    }

    /// <summary>Change own password. Also the sanctioned way to rotate the demo `admin` password after cutover.</summary>
    [HttpPost("change-password")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<bool>>> ChangePassword([FromBody] ChangePasswordRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.NewPassword) || request.NewPassword.Length < 8)
            return BadRequest(ApiResponse<bool>.ErrorResponse("New password must be at least 8 characters"));

        try
        {
            var user = await _users.GetByLoginAsync(User.GetUsername());
            if (user is null || !user.IsActive)
                return Unauthorized(ApiResponse<bool>.ErrorResponse("Account not found or inactive"));

            if (!BCrypt.Net.BCrypt.Verify(request.CurrentPassword, user.PasswordHash))
            {
                _logger.LogWarning("PASSWORD_CHANGE_FAILED username={Username} reason=bad_current_password", user.Username);
                return BadRequest(ApiResponse<bool>.ErrorResponse("Current password is incorrect"));
            }

            var newHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword, workFactor: 11);
            await _users.UpdatePasswordHashAsync(user.UserID, newHash);

            _logger.LogInformation("PASSWORD_CHANGED username={Username}", user.Username);
            return Ok(ApiResponse<bool>.SuccessResponse(true, "Password changed successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Change password failed for {Username}", User.GetUsername());
            return StatusCode(500, ApiResponse<bool>.ErrorResponse("Something went wrong. Please try again."));
        }
    }

    private ActionResult<ApiResponse<LoginResponse>> LoginFailed(string login, string reason)
    {
        _logger.LogWarning("LOGIN_FAILED username={Username} reason={Reason} ip={Ip}",
            login, reason, HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown");
        return Unauthorized(ApiResponse<LoginResponse>.ErrorResponse("Invalid username or password"));
    }

    /// <summary>Accounts an admin can switch into (active, non-admin).</summary>
    [HttpGet("users")]
    [Authorize(Roles = Roles.Admin)]
    public async Task<ActionResult<ApiResponse<IEnumerable<AuthUserDto>>>> GetSwitchTargets()
    {
        var users = await _users.GetSwitchTargetsAsync();
        return Ok(ApiResponse<IEnumerable<AuthUserDto>>.SuccessResponse(users));
    }

    /// <summary>
    /// Admin account switching: issues a token for the target user carrying an
    /// impersonatedBy claim. Admins cannot impersonate other admins. Audited.
    /// </summary>
    [HttpPost("impersonate")]
    [Authorize(Roles = Roles.Admin)]
    public async Task<ActionResult<ApiResponse<LoginResponse>>> Impersonate([FromBody] ImpersonateRequest request)
    {
        var target = await _users.GetByIdAsync(request.UserId);
        if (target == null || !target.IsActive)
            return NotFound(ApiResponse<LoginResponse>.ErrorResponse("User not found or inactive"));
        if (target.Role == Roles.Admin)
            return BadRequest(ApiResponse<LoginResponse>.ErrorResponse("Admins cannot view as other admins"));

        var adminName = User.GetUsername() ?? "admin";
        var (token, expiresAtUtc) = _tokens.CreateToken(target, impersonatedBy: adminName);

        _logger.LogWarning("Admin {Admin} started viewing as {Target} ({Role})",
            adminName, target.Username, target.Role);

        return Ok(ApiResponse<LoginResponse>.SuccessResponse(new LoginResponse
        {
            Token = token,
            ExpiresAtUtc = expiresAtUtc,
            User = new AuthUserDto
            {
                UserId = target.UserID,
                Username = target.Username,
                DisplayName = target.DisplayName,
                Role = target.Role,
                StaffId = target.StaffID,
                PatientId = target.PatientID
            }
        }, $"Now viewing as {target.DisplayName}"));
    }
}

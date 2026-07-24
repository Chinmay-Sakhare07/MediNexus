namespace HospitalManagement.API.Models.DTOs;

public class LoginRequest
{
    /// <summary>Username or email — both are unique in USER_ACCOUNT.</summary>
    public string Login { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class ChangePasswordRequest
{
    public string CurrentPassword { get; set; } = string.Empty;
    public string NewPassword { get; set; } = string.Empty;
}

public class AuthUserDto
{
    public int UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public int? StaffId { get; set; }
    public int? PatientId { get; set; }
}

public class LoginResponse
{
    public string Token { get; set; } = string.Empty;
    public DateTime ExpiresAtUtc { get; set; }
    public AuthUserDto User { get; set; } = new();
}

/// <summary>Internal row shape for USER_ACCOUNT (never leaves the API).</summary>
public class UserAccountRecord
{
    public int UserID { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public int? StaffID { get; set; }
    public int? PatientID { get; set; }
    public bool IsActive { get; set; }
    public string DisplayName { get; set; } = string.Empty;
}

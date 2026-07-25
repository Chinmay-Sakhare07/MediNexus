namespace HospitalManagement.API.Models.DTOs;

public class UserAdminDto
{
    public int UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public int? StaffId { get; set; }
    public int? PatientId { get; set; }
    public bool IsActive { get; set; }
    public DateTime? LastLogin { get; set; }
    public DateTime CreatedAt { get; set; }
}

namespace HospitalManagement.API.Models.DTOs;

public class PatientDto
{
    public int PatientId { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public DateTime DateOfBirth { get; set; }
    public string Gender { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
    public string? BloodType { get; set; }
    public string? EmergencyContact { get; set; }
    public string? MaritalStatus { get; set; }
    public int? PrimaryPhysicianId { get; set; }
    public string? PrimaryPhysicianName { get; set; }
    public DateTime RegistrationDate { get; set; }
}
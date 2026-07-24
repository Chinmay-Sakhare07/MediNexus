namespace HospitalManagement.API.Models.DTOs;

public class DoctorDto
{
    public int DoctorId { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Specialization { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public string? Email { get; set; }
    public string? Department { get; set; }
    public decimal ConsultationFee { get; set; }
    public string? Availability { get; set; }
    public int YearsOfExperience { get; set; }
}
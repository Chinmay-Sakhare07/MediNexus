namespace HospitalManagement.API.Models.DTOs;

public class AppointmentDto
{
    public int AppointmentId { get; set; }
    public int PatientId { get; set; }
    public string PatientName { get; set; } = string.Empty;
    public int DoctorId { get; set; }
    public string DoctorName { get; set; } = string.Empty;
    public DateTime DateTime { get; set; }
    public string? Reason { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? AppointmentType { get; set; }
    public int Duration { get; set; }
    public DateTime? CompletedAt { get; set; }
}
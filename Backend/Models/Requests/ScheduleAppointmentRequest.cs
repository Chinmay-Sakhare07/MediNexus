namespace HospitalManagement.API.Models.Requests;

public class ScheduleAppointmentRequest
{
    public int PatientId { get; set; }
    public int DoctorId { get; set; }
    public DateTime DateTime { get; set; }
    public string? Reason { get; set; }
    public string? AppointmentType { get; set; }
    public int Duration { get; set; } = 30;
}
namespace HospitalManagement.API.Models.Requests;

public class UpdateAppointmentRequest
{
    public DateTime? NewDateTime { get; set; }
    public string? NewStatus { get; set; }
    public int? NewDoctorId { get; set; }
    public int? NewRoomId { get; set; }
    public string? NewReason { get; set; }
    public string? NewNotes { get; set; }
}
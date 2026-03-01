namespace HospitalManagement.API.Models.Requests;

public class CompleteAppointmentRequest
{
    public int AppointmentId { get; set; }
    public decimal ConsultationFee { get; set; }
    public decimal AdditionalFees { get; set; }
    public string? AdditionalFeesDescription { get; set; }
    public decimal DiscountPercentage { get; set; }
}
namespace HospitalManagement.API.Models.Requests;

public class ProcessPaymentRequest
{
    public int BillId { get; set; }
    public decimal AmountPaid { get; set; }
    public string PaymentMethod { get; set; } = string.Empty;
}
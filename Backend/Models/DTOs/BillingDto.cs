namespace HospitalManagement.API.Models.DTOs;

public class BillingDto
{
    public int BillId { get; set; }
    public int PatientId { get; set; }
    public string PatientName { get; set; } = string.Empty;
    public int? AppointmentId { get; set; }
    public DateTime? AppointmentDate { get; set; }
    public string? AppointmentReason { get; set; }
    public decimal Amount { get; set; }
    public decimal DiscountApplied { get; set; }
    public decimal TaxAmount { get; set; }
    public DateTime DateIssued { get; set; }
    public DateTime? DueDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? PaymentTerms { get; set; }
    public string? InsuranceProvider { get; set; }
    public string? PolicyNumber { get; set; }
    public decimal InsuranceCovered { get; set; }
    public decimal PatientResponsibility { get; set; }
}
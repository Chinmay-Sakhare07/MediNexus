namespace HospitalManagement.API.Models.DTOs;

public class PatientInsuranceDto
{
    public int PatientInsuranceId { get; set; }
    public int PatientId { get; set; }
    public int PolicyId { get; set; }
    public string PolicyNumber { get; set; } = string.Empty;
    public string ProviderName { get; set; } = string.Empty;
    public string? PlanType { get; set; }
    public decimal CopayPercentage { get; set; }
    public bool IsPrimary { get; set; }
}
namespace HospitalManagement.API.Models.DTOs;

public class InsurancePolicyDto
{
    public int PolicyId { get; set; }
    public int ProviderId { get; set; }
    public string ProviderName { get; set; } = string.Empty;
    public string PolicyNumber { get; set; } = string.Empty;
    public string? CoverageDetails { get; set; }
    public string? PlanType { get; set; }
    public DateTime ValidFrom { get; set; }
    public DateTime? ValidTo { get; set; }
    public decimal CopayPercentage { get; set; }
    public decimal MaxCoverageLimit { get; set; }
}
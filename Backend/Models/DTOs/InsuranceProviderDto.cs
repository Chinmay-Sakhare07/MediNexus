namespace HospitalManagement.API.Models.DTOs;

public class InsuranceProviderDto
{
    public int ProviderId { get; set; }
    public string ProviderName { get; set; } = string.Empty;
    public string? ContactNumber { get; set; }
}
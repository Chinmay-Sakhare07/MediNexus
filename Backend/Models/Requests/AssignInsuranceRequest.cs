namespace HospitalManagement.API.Models.Requests;

public class AssignInsuranceRequest
{
    public int PatientId { get; set; }
    public int PolicyId { get; set; }
    public bool IsPrimary { get; set; }
}
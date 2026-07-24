namespace HospitalManagement.API.Models.DTOs;

public class DashboardDto
{
    public int TotalPatients { get; set; }
    public int TotalDoctors { get; set; }
    public int TodayAppointments { get; set; }
    public int PendingBills { get; set; }
    public decimal TotalRevenue { get; set; }
}
using HospitalManagement.API.Models.DTOs;

namespace HospitalManagement.API.Repositories.Interfaces;

public interface IDashboardRepository
{
    Task<DashboardDto> GetStatsAsync();
}
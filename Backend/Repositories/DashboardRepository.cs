using Microsoft.Data.SqlClient;
using Dapper;
using HospitalManagement.API.Models.DTOs;
using HospitalManagement.API.Repositories.Interfaces;

namespace HospitalManagement.API.Repositories;

public class DashboardRepository : IDashboardRepository
{
    private readonly string _connectionString;

    public DashboardRepository(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("HospitalDb")!;
    }

    public async Task<DashboardDto> GetStatsAsync()
    {
        using var connection = new SqlConnection(_connectionString);
        
        var sql = @"
            SELECT 
                (SELECT COUNT(*) FROM PATIENT) as TotalPatients,
                (SELECT COUNT(*) FROM DOCTOR) as TotalDoctors,
                (SELECT COUNT(*) FROM APPOINTMENT WHERE CAST(DateTime AS DATE) = CAST(GETDATE() AS DATE)) as TodayAppointments,
                (SELECT COUNT(*) FROM BILLING WHERE Status = 'Pending') as PendingBills,
                (SELECT ISNULL(SUM(Amount), 0) FROM BILLING WHERE Status = 'Paid') as TotalRevenue";

        return await connection.QuerySingleAsync<DashboardDto>(sql);
    }
}
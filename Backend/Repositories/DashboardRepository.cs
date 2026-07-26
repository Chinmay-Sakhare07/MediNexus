using MySqlConnector;
using Dapper;
using HospitalManagement.API.Models.DTOs;
using HospitalManagement.API.Caching;
using HospitalManagement.API.Repositories.Interfaces;
using HospitalManagement.API.Time;

namespace HospitalManagement.API.Repositories;

public class DashboardRepository : IDashboardRepository
{
    private readonly string _connectionString;
    private readonly ICacheService _cache;

    public DashboardRepository(IConfiguration configuration, ICacheService cache)
    {
        _connectionString = configuration.GetConnectionString("HospitalDb")!;
        _cache = cache;
    }

    public async Task<DashboardDto> GetStatsAsync()
    {
        var cached = await _cache.GetAsync<DashboardDto>("mn:dashboard");
        if (cached != null) return cached;

        using var connection = new MySqlConnection(_connectionString);

        // D6: "today" is the IST day expressed as a UTC range (index-friendly).
        var (startUtc, endUtc) = IstClock.TodayRangeUtc();

        var sql = @"
            SELECT
                (SELECT COUNT(*) FROM PATIENT) as TotalPatients,
                (SELECT COUNT(*) FROM DOCTOR) as TotalDoctors,
                (SELECT COUNT(*) FROM APPOINTMENT
                    WHERE `DateTime` >= @StartUtc AND `DateTime` < @EndUtc) as TodayAppointments,
                (SELECT COUNT(*) FROM BILLING WHERE Status = 'Pending') as PendingBills,
                (SELECT IFNULL(SUM(Amount), 0) FROM BILLING WHERE Status = 'Paid') as TotalRevenue";

        var stats = await connection.QuerySingleAsync<DashboardDto>(sql, new { StartUtc = startUtc, EndUtc = endUtc });
        await _cache.SetAsync("mn:dashboard", stats, TimeSpan.FromSeconds(30));
        return stats;
    }
}

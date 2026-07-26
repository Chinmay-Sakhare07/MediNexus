using MySqlConnector;
using Dapper;
using HospitalManagement.API.Models.DTOs;
using HospitalManagement.API.Caching;
using HospitalManagement.API.Repositories.Interfaces;

namespace HospitalManagement.API.Repositories;

public class DoctorRepository : IDoctorRepository
{
    private readonly string _connectionString;
    private readonly ICacheService _cache;

    public DoctorRepository(IConfiguration configuration, ICacheService cache)
    {
        _connectionString = configuration.GetConnectionString("HospitalDb")!;
        _cache = cache;
    }

    public async Task<IEnumerable<DoctorDto>> GetAllAsync()
    {
        var cached = await _cache.GetAsync<List<DoctorDto>>("mn:doctors");
        if (cached != null) return cached;

        using var connection = new MySqlConnection(_connectionString);

        var sql = @"
            SELECT
                d.DoctorID as DoctorId,
                s.FirstName,
                s.LastName,
                d.Specialization,
                s.Phone as PhoneNumber,
                s.Email,
                dept.Name as Department,
                d.ConsultationFee,
                d.AvailabilityStatus as Availability,
                d.YearsOfExperience
            FROM DOCTOR d
            INNER JOIN STAFF s ON d.DoctorID = s.StaffID
            INNER JOIN DEPARTMENT dept ON s.DepartmentID = dept.DepartmentID
            ORDER BY s.LastName, s.FirstName";

        var doctors = (await connection.QueryAsync<DoctorDto>(sql)).ToList();
        await _cache.SetAsync("mn:doctors", doctors, TimeSpan.FromSeconds(60));
        return doctors;
    }

    public async Task<IEnumerable<DoctorDto>> GetAvailableAsync()
    {
        using var connection = new MySqlConnection(_connectionString);

        var sql = @"
            SELECT
                d.DoctorID as DoctorId,
                s.FirstName,
                s.LastName,
                d.Specialization,
                s.Phone as PhoneNumber,
                s.Email,
                dept.Name as Department,
                d.ConsultationFee,
                d.AvailabilityStatus as Availability,
                d.YearsOfExperience
            FROM DOCTOR d
            INNER JOIN STAFF s ON d.DoctorID = s.StaffID
            INNER JOIN DEPARTMENT dept ON s.DepartmentID = dept.DepartmentID
            WHERE d.AvailabilityStatus = 'Available'
            ORDER BY s.LastName, s.FirstName";

        return await connection.QueryAsync<DoctorDto>(sql);
    }
}

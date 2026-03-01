using Microsoft.Data.SqlClient;
using Dapper;
using HospitalManagement.API.Models.DTOs;
using HospitalManagement.API.Repositories.Interfaces;

namespace HospitalManagement.API.Repositories;

public class DoctorRepository : IDoctorRepository
{
    private readonly string _connectionString;

    public DoctorRepository(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("HospitalDb")!;
    }

    public async Task<IEnumerable<DoctorDto>> GetAllAsync()
    {
        using var connection = new SqlConnection(_connectionString);
        
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

        return await connection.QueryAsync<DoctorDto>(sql);
    }

    public async Task<IEnumerable<DoctorDto>> GetAvailableAsync()
    {
        using var connection = new SqlConnection(_connectionString);
        
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
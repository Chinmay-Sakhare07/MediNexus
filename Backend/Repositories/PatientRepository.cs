using System.Data;
using MySqlConnector;
using Dapper;
using HospitalManagement.API.Models.DTOs;
using HospitalManagement.API.Models.Requests;
using HospitalManagement.API.Repositories.Interfaces;

namespace HospitalManagement.API.Repositories;

public class PatientRepository : IPatientRepository
{
    private readonly string _connectionString;

    public PatientRepository(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("HospitalDb")!;
    }

    public async Task<IEnumerable<PatientDto>> GetAllAsync()
    {
        using var connection = new MySqlConnection(_connectionString);

        var sql = @"
            SELECT
                p.PatientID as PatientId,
                p.FirstName,
                p.LastName,
                p.DOB as DateOfBirth,
                p.Gender,
                p.Phone as PhoneNumber,
                p.Email,
                p.Address,
                p.BloodType,
                p.EmergencyContact,
                p.MaritalStatus,
                p.PrimaryPhysicianID as PrimaryPhysicianId,
                CONCAT(s.FirstName, ' ', s.LastName) as PrimaryPhysicianName
            FROM PATIENT p
            LEFT JOIN DOCTOR d ON p.PrimaryPhysicianID = d.DoctorID
            LEFT JOIN STAFF s ON d.DoctorID = s.StaffID
            ORDER BY p.PatientID DESC";

        return await connection.QueryAsync<PatientDto>(sql);
    }

    public async Task<PatientDto?> GetByIdAsync(int id)
    {
        using var connection = new MySqlConnection(_connectionString);

        var sql = @"
            SELECT
                p.PatientID as PatientId,
                p.FirstName,
                p.LastName,
                p.DOB as DateOfBirth,
                p.Gender,
                p.Phone as PhoneNumber,
                p.Email,
                p.Address,
                p.BloodType,
                p.EmergencyContact,
                p.MaritalStatus,
                p.PrimaryPhysicianID as PrimaryPhysicianId,
                CONCAT(s.FirstName, ' ', s.LastName) as PrimaryPhysicianName
            FROM PATIENT p
            LEFT JOIN DOCTOR d ON p.PrimaryPhysicianID = d.DoctorID
            LEFT JOIN STAFF s ON d.DoctorID = s.StaffID
            WHERE p.PatientID = @Id";

        return await connection.QuerySingleOrDefaultAsync<PatientDto>(sql, new { Id = id });
    }

    public async Task<int> RegisterAsync(RegisterPatientRequest request)
    {
        using var connection = new MySqlConnection(_connectionString);

        var sql = @"
            INSERT INTO PATIENT (
                FirstName, LastName, DOB, Gender, Phone, Email, Address,
                BloodType, EmergencyContact, MaritalStatus, PrimaryPhysicianID
            )
            VALUES (
                @FirstName, @LastName, @DateOfBirth, @Gender,
                @PhoneNumber, @Email, @Address,
                @BloodType, @EmergencyContact, @MaritalStatus, @PrimaryPhysicianId
            );
            SELECT LAST_INSERT_ID();";

        return await connection.ExecuteScalarAsync<int>(sql, request);
    }

    public async Task<bool> DeleteAsync(int id)
    {
        using var connection = new MySqlConnection(_connectionString);
        var sql = "DELETE FROM PATIENT WHERE PatientID = @Id";
        var affected = await connection.ExecuteAsync(sql, new { Id = id });
        return affected > 0;
    }

    public async Task<bool> UpdateAsync(int id, UpdatePatientRequest request)
    {
        using var connection = new MySqlConnection(_connectionString);

        var sql = @"
            UPDATE PATIENT SET
                FirstName = @FirstName,
                LastName = @LastName,
                DOB = @DateOfBirth,
                Gender = @Gender,
                Phone = @PhoneNumber,
                Email = @Email,
                Address = @Address,
                BloodType = @BloodType,
                EmergencyContact = @EmergencyContact,
                MaritalStatus = @MaritalStatus,
                PrimaryPhysicianID = @PrimaryPhysicianId
            WHERE PatientID = @Id";

        var affected = await connection.ExecuteAsync(sql, new {
            Id = id,
            request.FirstName,
            request.LastName,
            request.DateOfBirth,
            request.Gender,
            request.PhoneNumber,
            request.Email,
            request.Address,
            request.BloodType,
            request.EmergencyContact,
            request.MaritalStatus,
            request.PrimaryPhysicianId
        });
        return affected > 0;
    }
}

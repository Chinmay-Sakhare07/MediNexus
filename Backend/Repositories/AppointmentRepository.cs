using System.Data;
using Microsoft.Data.SqlClient;
using Dapper;
using HospitalManagement.API.Models.DTOs;
using HospitalManagement.API.Models.Requests;
using HospitalManagement.API.Repositories.Interfaces;

namespace HospitalManagement.API.Repositories;

public class AppointmentRepository : IAppointmentRepository
{
    private readonly string _connectionString;

    public AppointmentRepository(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("HospitalDb")!;
    }

    public async Task<IEnumerable<AppointmentDto>> GetAllAsync()
    {
        using var connection = new SqlConnection(_connectionString);
        
        var sql = @"
            SELECT 
                a.AppointmentID as AppointmentId,
                a.PatientID as PatientId,
                CONCAT(p.FirstName, ' ', p.LastName) as PatientName,
                a.DoctorID as DoctorId,
                CONCAT(s.FirstName, ' ', s.LastName) as DoctorName,
                a.DateTime,
                a.Reason,
                a.Status,
                a.AppointmentType,
                a.Duration,
                NULL as CompletedAt
            FROM APPOINTMENT a
            INNER JOIN PATIENT p ON a.PatientID = p.PatientID
            INNER JOIN DOCTOR d ON a.DoctorID = d.DoctorID
            INNER JOIN STAFF s ON d.DoctorID = s.StaffID
            ORDER BY a.DateTime DESC";

        return await connection.QueryAsync<AppointmentDto>(sql);
    }

    public async Task<IEnumerable<AppointmentDto>> GetTodayAsync()
    {
        using var connection = new SqlConnection(_connectionString);
        
        var sql = @"
            SELECT 
                a.AppointmentID as AppointmentId,
                a.PatientID as PatientId,
                CONCAT(p.FirstName, ' ', p.LastName) as PatientName,
                a.DoctorID as DoctorId,
                CONCAT(s.FirstName, ' ', s.LastName) as DoctorName,
                a.DateTime,
                a.Reason,
                a.Status,
                a.AppointmentType,
                a.Duration,
                NULL as CompletedAt
            FROM APPOINTMENT a
            INNER JOIN PATIENT p ON a.PatientID = p.PatientID
            INNER JOIN DOCTOR d ON a.DoctorID = d.DoctorID
            INNER JOIN STAFF s ON d.DoctorID = s.StaffID
            WHERE CAST(a.DateTime AS DATE) = CAST(GETDATE() AS DATE)
            ORDER BY a.DateTime";

        return await connection.QueryAsync<AppointmentDto>(sql);
    }

    public async Task<IEnumerable<AppointmentDto>> GetTomorrowAsync()
    {
        using var connection = new SqlConnection(_connectionString);
        
        var sql = @"
            SELECT 
                a.AppointmentID as AppointmentId,
                a.PatientID as PatientId,
                CONCAT(p.FirstName, ' ', p.LastName) as PatientName,
                a.DoctorID as DoctorId,
                CONCAT(s.FirstName, ' ', s.LastName) as DoctorName,
                a.DateTime,
                a.Reason,
                a.Status,
                a.AppointmentType,
                a.Duration,
                NULL as CompletedAt
            FROM APPOINTMENT a
            INNER JOIN PATIENT p ON a.PatientID = p.PatientID
            INNER JOIN DOCTOR d ON a.DoctorID = d.DoctorID
            INNER JOIN STAFF s ON d.DoctorID = s.StaffID
            WHERE CAST(a.DateTime AS DATE) = CAST(DATEADD(DAY, 1, GETDATE()) AS DATE)
            ORDER BY a.DateTime";

        return await connection.QueryAsync<AppointmentDto>(sql);
    }

    public async Task<IEnumerable<AppointmentDto>> GetByDateAsync(DateTime date)
    {
        using var connection = new SqlConnection(_connectionString);
        
        var sql = @"
            SELECT 
                a.AppointmentID as AppointmentId,
                a.PatientID as PatientId,
                CONCAT(p.FirstName, ' ', p.LastName) as PatientName,
                a.DoctorID as DoctorId,
                CONCAT(s.FirstName, ' ', s.LastName) as DoctorName,
                a.DateTime,
                a.Reason,
                a.Status,
                a.AppointmentType,
                a.Duration,
                NULL as CompletedAt
            FROM APPOINTMENT a
            INNER JOIN PATIENT p ON a.PatientID = p.PatientID
            INNER JOIN DOCTOR d ON a.DoctorID = d.DoctorID
            INNER JOIN STAFF s ON d.DoctorID = s.StaffID
            WHERE CAST(a.DateTime AS DATE) = CAST(@Date AS DATE)
            ORDER BY a.DateTime";

        return await connection.QueryAsync<AppointmentDto>(sql, new { Date = date });
    }

    public async Task<int> ScheduleAsync(ScheduleAppointmentRequest request)
    {
        using var connection = new SqlConnection(_connectionString);
        
        // Get a default room (first available room)
        var roomSql = "SELECT TOP 1 RoomID FROM ROOM WHERE AvailabilityStatus = 'Available'";
        var roomId = await connection.ExecuteScalarAsync<int?>(roomSql);
        
        if (!roomId.HasValue)
        {
            // If no available room, just use the first room
            roomId = await connection.ExecuteScalarAsync<int>("SELECT TOP 1 RoomID FROM ROOM");
        }

        var sql = @"
            INSERT INTO APPOINTMENT (
                PatientID, DoctorID, RoomID, DateTime, Reason, 
                Status, AppointmentType, Duration
            )
            VALUES (
                @PatientId, @DoctorId, @RoomId, @DateTime, @Reason,
                'Scheduled', @AppointmentType, @Duration
            );
            SELECT CAST(SCOPE_IDENTITY() as int);";

        return await connection.ExecuteScalarAsync<int>(sql, new {
            request.PatientId,
            request.DoctorId,
            RoomId = roomId,
            request.DateTime,
            request.Reason,
            request.AppointmentType,
            request.Duration
        });
    }

    public async Task<bool> UpdateStatusAsync(int id, string status)
    {
        using var connection = new SqlConnection(_connectionString);
        var sql = "UPDATE APPOINTMENT SET Status = @Status WHERE AppointmentID = @Id";
        var affected = await connection.ExecuteAsync(sql, new { Id = id, Status = status });
        return affected > 0;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        using var connection = new SqlConnection(_connectionString);
        var sql = "DELETE FROM APPOINTMENT WHERE AppointmentID = @Id";
        var affected = await connection.ExecuteAsync(sql, new { Id = id });
        return affected > 0;
    }
}
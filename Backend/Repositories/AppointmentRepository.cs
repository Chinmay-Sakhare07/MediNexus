using MySqlConnector;
using Dapper;
using HospitalManagement.API.Exceptions;
using HospitalManagement.API.Models.DTOs;
using HospitalManagement.API.Models.Requests;
using HospitalManagement.API.Repositories.Interfaces;
using HospitalManagement.API.Time;

namespace HospitalManagement.API.Repositories;

public class AppointmentRepository : IAppointmentRepository
{
    private readonly string _connectionString;

    public AppointmentRepository(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("HospitalDb")!;
    }

    private const string SelectBlock = @"
            SELECT
                a.AppointmentID as AppointmentId,
                a.PatientID as PatientId,
                CONCAT(p.FirstName, ' ', p.LastName) as PatientName,
                a.DoctorID as DoctorId,
                CONCAT(s.FirstName, ' ', s.LastName) as DoctorName,
                a.`DateTime`,
                a.Reason,
                a.Status,
                a.AppointmentType,
                a.Duration,
                NULL as CompletedAt
            FROM APPOINTMENT a
            INNER JOIN PATIENT p ON a.PatientID = p.PatientID
            INNER JOIN DOCTOR d ON a.DoctorID = d.DoctorID
            INNER JOIN STAFF s ON d.DoctorID = s.StaffID";

    private static string ScopeClause(int? doctorId, int? patientId)
    {
        var clause = string.Empty;
        if (doctorId.HasValue) clause += " AND a.DoctorID = @DoctorId";
        if (patientId.HasValue) clause += " AND a.PatientID = @PatientId";
        return clause;
    }

    public async Task<IEnumerable<AppointmentDto>> GetAllAsync(int? doctorId = null, int? patientId = null)
    {
        using var connection = new MySqlConnection(_connectionString);
        var sql = SelectBlock + @"
            WHERE 1 = 1" + ScopeClause(doctorId, patientId) + @"
            ORDER BY a.`DateTime` DESC";
        return await connection.QueryAsync<AppointmentDto>(sql, new { DoctorId = doctorId, PatientId = patientId });
    }

    // D6: an "IST day" becomes a UTC half-open range. The column stays bare,
    // so IX_Appointment_DateTime keeps working (no DATE() wrap, no CURDATE()).
    private async Task<IEnumerable<AppointmentDto>> GetRangeAsync(
        DateTime startUtc, DateTime endUtc, int? doctorId, int? patientId)
    {
        using var connection = new MySqlConnection(_connectionString);
        var sql = SelectBlock + @"
            WHERE a.`DateTime` >= @StartUtc AND a.`DateTime` < @EndUtc"
            + ScopeClause(doctorId, patientId) + @"
            ORDER BY a.`DateTime`";
        return await connection.QueryAsync<AppointmentDto>(sql,
            new { StartUtc = startUtc, EndUtc = endUtc, DoctorId = doctorId, PatientId = patientId });
    }

    public Task<IEnumerable<AppointmentDto>> GetTodayAsync(int? doctorId = null, int? patientId = null)
    {
        var (start, end) = IstClock.TodayRangeUtc();
        return GetRangeAsync(start, end, doctorId, patientId);
    }

    public Task<IEnumerable<AppointmentDto>> GetTomorrowAsync(int? doctorId = null, int? patientId = null)
    {
        var (start, end) = IstClock.TomorrowRangeUtc();
        return GetRangeAsync(start, end, doctorId, patientId);
    }

    public Task<IEnumerable<AppointmentDto>> GetByDateAsync(DateTime istDate, int? doctorId = null, int? patientId = null)
    {
        var (start, end) = IstClock.UtcRangeForIstDay(istDate);
        return GetRangeAsync(start, end, doctorId, patientId);
    }

    public async Task<AppointmentDto?> GetByIdAsync(int id)
    {
        using var connection = new MySqlConnection(_connectionString);
        var sql = SelectBlock + " WHERE a.AppointmentID = @Id";
        return await connection.QuerySingleOrDefaultAsync<AppointmentDto>(sql, new { Id = id });
    }

    public Task<int> ScheduleAsync(ScheduleAppointmentRequest request) =>
        InsertAppointmentAsync(request.PatientId, request.DoctorId, request.DateTime,
            request.Reason, request.AppointmentType, request.Duration, "Scheduled");

    public Task<int> BookAsRequestedAsync(int patientId, BookAppointmentRequest request) =>
        InsertAppointmentAsync(patientId, request.DoctorId, request.DateTime,
            request.Reason, request.AppointmentType, 30, "Requested");

    public async Task<bool> TransitionAsync(int id, string[] from, string to)
    {
        using var connection = new MySqlConnection(_connectionString);
        var affected = await connection.ExecuteAsync(
            "UPDATE APPOINTMENT SET Status = @To WHERE AppointmentID = @Id AND Status IN @From",
            new { Id = id, To = to, From = from });
        return affected > 0;
    }

    private async Task<int> InsertAppointmentAsync(int patientId, int doctorId, DateTime dateTimeUtc,
        string? reason, string? appointmentType, int duration, string status)
    {
        using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync();

        // Friendly pre-check for the common conflict (unique index is the backstop).
        var doctorBusy = await connection.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM APPOINTMENT WHERE DoctorID = @DoctorId AND `DateTime` = @DateTime",
            new { DoctorId = doctorId, DateTime = dateTimeUtc });
        if (doctorBusy > 0)
            throw new ConflictException("The doctor already has an appointment at that time. Please pick a different slot.");

        // Pick a room that is free at this exact slot (prefer 'Available' status).
        var roomSql = @"
            SELECT r.RoomID FROM ROOM r
            WHERE r.RoomID NOT IN (
                SELECT RoomID FROM APPOINTMENT WHERE `DateTime` = @DateTime
            )
            ORDER BY (r.AvailabilityStatus = 'Available') DESC, r.RoomID
            LIMIT 1";
        var roomId = await connection.ExecuteScalarAsync<int?>(roomSql, new { DateTime = dateTimeUtc });
        if (!roomId.HasValue)
            throw new ConflictException("No room is free at that time. Please pick a different slot.");

        var sql = @"
            INSERT INTO APPOINTMENT (
                PatientID, DoctorID, RoomID, `DateTime`, Reason,
                Status, AppointmentType, Duration
            )
            VALUES (
                @PatientId, @DoctorId, @RoomId, @DateTime, @Reason,
                @Status, @AppointmentType, @Duration
            );
            SELECT LAST_INSERT_ID();";

        return await connection.ExecuteScalarAsync<int>(sql, new {
            PatientId = patientId,
            DoctorId = doctorId,
            RoomId = roomId,
            DateTime = dateTimeUtc,
            Reason = reason,
            Status = status,
            AppointmentType = appointmentType,
            Duration = duration
        });
    }

    public async Task<bool> UpdateStatusAsync(int id, string status)
    {
        using var connection = new MySqlConnection(_connectionString);
        var sql = "UPDATE APPOINTMENT SET Status = @Status WHERE AppointmentID = @Id";
        var affected = await connection.ExecuteAsync(sql, new { Id = id, Status = status });
        return affected > 0;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        using var connection = new MySqlConnection(_connectionString);
        var sql = "DELETE FROM APPOINTMENT WHERE AppointmentID = @Id";
        var affected = await connection.ExecuteAsync(sql, new { Id = id });
        return affected > 0;
    }
}

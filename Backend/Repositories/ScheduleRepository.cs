using MySqlConnector;
using Dapper;
using HospitalManagement.API.Exceptions;
using HospitalManagement.API.Models.DTOs;
using HospitalManagement.API.Models.Requests;
using HospitalManagement.API.Repositories.Interfaces;
using HospitalManagement.API.Time;

namespace HospitalManagement.API.Repositories;

public class ScheduleRepository : IScheduleRepository
{
    private static readonly string[] ActiveStatuses =
        { "Requested", "Scheduled", "Confirmed", "CheckedIn" };

    private readonly string _connectionString;

    public ScheduleRepository(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("HospitalDb")!;
    }

    public async Task<DoctorScheduleDto?> GetScheduleAsync(int doctorId)
    {
        using var connection = new MySqlConnection(_connectionString);
        return await connection.QuerySingleOrDefaultAsync<DoctorScheduleDto>(@"
            SELECT DoctorID as DoctorId, WorkDays,
                   TIME_FORMAT(StartTime, '%H:%i') as StartTime,
                   TIME_FORMAT(EndTime, '%H:%i') as EndTime,
                   SlotMinutes
            FROM DOCTOR_SCHEDULE WHERE DoctorID = @Id",
            new { Id = doctorId });
    }

    public async Task UpdateScheduleAsync(int doctorId, UpdateScheduleRequest request)
    {
        using var connection = new MySqlConnection(_connectionString);
        await connection.ExecuteAsync(@"
            INSERT INTO DOCTOR_SCHEDULE (DoctorID, WorkDays, StartTime, EndTime, SlotMinutes)
            VALUES (@Id, @WorkDays, @Start, @End, @Slot)
            ON DUPLICATE KEY UPDATE WorkDays = @WorkDays, StartTime = @Start,
                                    EndTime = @End, SlotMinutes = @Slot",
            new { Id = doctorId, WorkDays = string.Join(",", request.WorkDays),
                  Start = request.StartTime, End = request.EndTime, Slot = request.SlotMinutes });
    }

    public async Task<IEnumerable<DoctorLeaveDto>> GetLeavesAsync(int doctorId)
    {
        using var connection = new MySqlConnection(_connectionString);
        return await connection.QueryAsync<DoctorLeaveDto>(@"
            SELECT LeaveID as LeaveId, DoctorID as DoctorId, LeaveDate, Reason
            FROM DOCTOR_LEAVE
            WHERE DoctorID = @Id AND LeaveDate >= @TodayIst
            ORDER BY LeaveDate",
            new { Id = doctorId, TodayIst = IstClock.TodayIstDate() });
    }

    // Filing leave cancels that IST day's active appointments in the same transaction (D13).
    public async Task<(int LeaveId, int CancelledAppointments)> AddLeaveAsync(
        int doctorId, DateTime istDate, string? reason)
    {
        using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync();
        using var transaction = await connection.BeginTransactionAsync();

        int leaveId;
        try
        {
            leaveId = await connection.ExecuteScalarAsync<int>(@"
                INSERT INTO DOCTOR_LEAVE (DoctorID, LeaveDate, Reason)
                VALUES (@Id, @Date, @Reason);
                SELECT LAST_INSERT_ID();",
                new { Id = doctorId, Date = istDate.Date, Reason = reason }, transaction);
        }
        catch (MySqlException ex) when (ex.Number == 1062)
        {
            throw new ConflictException("Leave is already filed for that date.");
        }

        var (startUtc, endUtc) = IstClock.UtcRangeForIstDay(istDate.Date);
        var cancelled = await connection.ExecuteAsync(@"
            UPDATE APPOINTMENT SET Status = 'Cancelled'
            WHERE DoctorID = @Id AND `DateTime` >= @StartUtc AND `DateTime` < @EndUtc
              AND Status IN @Active",
            new { Id = doctorId, StartUtc = startUtc, EndUtc = endUtc, Active = ActiveStatuses },
            transaction);

        await transaction.CommitAsync();
        return (leaveId, cancelled);
    }

    public async Task<bool> RemoveLeaveAsync(int doctorId, int leaveId)
    {
        using var connection = new MySqlConnection(_connectionString);
        var affected = await connection.ExecuteAsync(
            "DELETE FROM DOCTOR_LEAVE WHERE LeaveID = @LeaveId AND DoctorID = @DoctorId",
            new { LeaveId = leaveId, DoctorId = doctorId });
        return affected > 0;
    }

    /// <summary>
    /// Bookable UTC instants for one IST calendar day: the doctor's fixed weekly
    /// pattern, minus leave, minus already-booked slots, minus the past (D13 —
    /// taken slots are simply never offered).
    /// </summary>
    public async Task<IEnumerable<DateTime>> GetSlotsAsync(int doctorId, DateTime istDate)
    {
        using var connection = new MySqlConnection(_connectionString);

        var schedule = await GetScheduleRowAsync(connection, doctorId);
        if (schedule is null) return Enumerable.Empty<DateTime>();

        var dayName = istDate.Date.DayOfWeek switch
        {
            DayOfWeek.Monday => "Mon", DayOfWeek.Tuesday => "Tue", DayOfWeek.Wednesday => "Wed",
            DayOfWeek.Thursday => "Thu", DayOfWeek.Friday => "Fri", DayOfWeek.Saturday => "Sat",
            _ => "Sun"
        };
        if (!schedule.Value.WorkDays.Split(',').Contains(dayName))
            return Enumerable.Empty<DateTime>();

        var onLeave = await connection.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM DOCTOR_LEAVE WHERE DoctorID = @Id AND LeaveDate = @Date",
            new { Id = doctorId, Date = istDate.Date });
        if (onLeave > 0) return Enumerable.Empty<DateTime>();

        var (startUtc, endUtc) = IstClock.UtcRangeForIstDay(istDate.Date);
        var booked = (await connection.QueryAsync<DateTime>(@"
            SELECT `DateTime` FROM APPOINTMENT
            WHERE DoctorID = @Id AND `DateTime` >= @StartUtc AND `DateTime` < @EndUtc
              AND Status IN @Active",
            new { Id = doctorId, StartUtc = startUtc, EndUtc = endUtc, Active = ActiveStatuses }))
            .ToHashSet();

        var slots = new List<DateTime>();
        var cursorIst = istDate.Date + schedule.Value.Start;
        var endIst = istDate.Date + schedule.Value.End;
        var notBefore = DateTime.UtcNow.AddMinutes(15); // no booking the immediate past/present

        while (cursorIst.AddMinutes(schedule.Value.SlotMinutes) <= endIst)
        {
            var slotUtc = IstClock.IstToUtc(cursorIst);
            if (slotUtc > notBefore && !booked.Contains(slotUtc))
                slots.Add(slotUtc);
            cursorIst = cursorIst.AddMinutes(schedule.Value.SlotMinutes);
        }
        return slots;
    }

    private static async Task<(string WorkDays, TimeSpan Start, TimeSpan End, int SlotMinutes)?>
        GetScheduleRowAsync(MySqlConnection connection, int doctorId)
    {
        return await connection.QuerySingleOrDefaultAsync<(string, TimeSpan, TimeSpan, int)?>(@"
            SELECT WorkDays, StartTime, EndTime, SlotMinutes
            FROM DOCTOR_SCHEDULE WHERE DoctorID = @Id",
            new { Id = doctorId });
    }
}

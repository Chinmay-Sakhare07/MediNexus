using MySqlConnector;
using Dapper;
using HospitalManagement.API.Models.DTOs;
using HospitalManagement.API.Repositories.Interfaces;
using HospitalManagement.API.Time;

namespace HospitalManagement.API.Repositories;

public class LabRepository : ILabRepository
{
    private readonly string _connectionString;

    public LabRepository(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("HospitalDb")!;
    }

    // technicianId null => Admin sees the whole lab; a tech sees their own queue.
    public async Task<IEnumerable<LabQueueItemDto>> GetQueueAsync(int? technicianId)
    {
        using var connection = new MySqlConnection(_connectionString);
        var sql = @"
            SELECT t.LabTestID as LabTestId, t.AppointmentID as AppointmentId,
                   CONCAT(p.FirstName,' ',p.LastName) as PatientName,
                   CONCAT(ds.FirstName,' ',ds.LastName) as DoctorName,
                   t.TestType, t.Status, t.NormalRange, t.Units, t.Result,
                   a.`DateTime` as AppointmentDateTime
            FROM LAB_TEST t
            INNER JOIN APPOINTMENT a ON t.AppointmentID = a.AppointmentID
            INNER JOIN PATIENT p ON a.PatientID = p.PatientID
            INNER JOIN STAFF ds ON a.DoctorID = ds.StaffID
            WHERE t.Status IN ('Pending','In Progress')" +
            (technicianId.HasValue ? " AND t.LabTechnicianID = @TechId" : "") + @"
            ORDER BY a.`DateTime`, t.LabTestID";
        return await connection.QueryAsync<LabQueueItemDto>(sql, new { TechId = technicianId });
    }

    public async Task<bool> StartAsync(int labTestId, int? technicianId)
    {
        using var connection = new MySqlConnection(_connectionString);
        var sql = @"UPDATE LAB_TEST SET Status = 'In Progress'
                    WHERE LabTestID = @Id AND Status = 'Pending'" +
                  (technicianId.HasValue ? " AND LabTechnicianID = @TechId" : "");
        return await connection.ExecuteAsync(sql, new { Id = labTestId, TechId = technicianId }) > 0;
    }

    public async Task<bool> EnterResultAsync(int labTestId, int? technicianId, string result, string? comments)
    {
        using var connection = new MySqlConnection(_connectionString);
        var sql = @"UPDATE LAB_TEST
                    SET Status = 'Completed', Result = @Result, Comments = @Comments,
                        ResultDate = @TodayIst
                    WHERE LabTestID = @Id AND Status IN ('Pending','In Progress')" +
                  (technicianId.HasValue ? " AND LabTechnicianID = @TechId" : "");
        return await connection.ExecuteAsync(sql, new
        {
            Id = labTestId, TechId = technicianId, Result = result,
            Comments = comments, TodayIst = IstClock.TodayIstDate()
        }) > 0;
    }
}

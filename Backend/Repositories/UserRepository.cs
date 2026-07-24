using MySqlConnector;
using Dapper;
using HospitalManagement.API.Models.DTOs;
using HospitalManagement.API.Repositories.Interfaces;

namespace HospitalManagement.API.Repositories;

public class UserRepository : IUserRepository
{
    private readonly string _connectionString;

    public UserRepository(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("HospitalDb")!;
    }

    public async Task<UserAccountRecord?> GetByLoginAsync(string usernameOrEmail)
    {
        using var connection = new MySqlConnection(_connectionString);

        // DisplayName resolves from the linked STAFF or PATIENT row,
        // falling back to the username for unlinked accounts.
        var sql = @"
            SELECT
                u.UserID,
                u.Username,
                u.Email,
                u.PasswordHash,
                u.Role,
                u.StaffID,
                u.PatientID,
                u.IsActive,
                COALESCE(
                    CONCAT(s.FirstName, ' ', s.LastName),
                    CONCAT(p.FirstName, ' ', p.LastName),
                    u.Username
                ) AS DisplayName
            FROM USER_ACCOUNT u
            LEFT JOIN STAFF   s ON u.StaffID   = s.StaffID
            LEFT JOIN PATIENT p ON u.PatientID = p.PatientID
            WHERE u.Username = @Login OR u.Email = @Login
            LIMIT 1";

        return await connection.QuerySingleOrDefaultAsync<UserAccountRecord>(
            sql, new { Login = usernameOrEmail });
    }

    public async Task UpdateLastLoginAsync(int userId, DateTime utcNow)
    {
        using var connection = new MySqlConnection(_connectionString);
        // D6: instants are stored as UTC; the app supplies the value.
        await connection.ExecuteAsync(
            "UPDATE USER_ACCOUNT SET LastLogin = @UtcNow WHERE UserID = @UserId",
            new { UtcNow = utcNow, UserId = userId });
    }

    public async Task<bool> UpdatePasswordHashAsync(int userId, string newHash)
    {
        using var connection = new MySqlConnection(_connectionString);
        var affected = await connection.ExecuteAsync(
            "UPDATE USER_ACCOUNT SET PasswordHash = @Hash WHERE UserID = @UserId",
            new { Hash = newHash, UserId = userId });
        return affected > 0;
    }
}

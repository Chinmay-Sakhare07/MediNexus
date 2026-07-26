using MySqlConnector;
using Dapper;
using HospitalManagement.API.Models.DTOs;
using HospitalManagement.API.Models.Requests;
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

    public async Task<UserAccountRecord?> GetByIdAsync(int userId)
    {
        using var connection = new MySqlConnection(_connectionString);
        var sql = @"
            SELECT
                u.UserID, u.Username, u.Email, u.PasswordHash, u.Role,
                u.StaffID, u.PatientID, u.IsActive,
                COALESCE(
                    CONCAT(s.FirstName, ' ', s.LastName),
                    CONCAT(p.FirstName, ' ', p.LastName),
                    u.Username
                ) AS DisplayName
            FROM USER_ACCOUNT u
            LEFT JOIN STAFF   s ON u.StaffID   = s.StaffID
            LEFT JOIN PATIENT p ON u.PatientID = p.PatientID
            WHERE u.UserID = @UserId
            LIMIT 1";
        return await connection.QuerySingleOrDefaultAsync<UserAccountRecord>(sql, new { UserId = userId });
    }

    // Everyone an admin may view as: active, non-admin accounts.
    public async Task<IEnumerable<AuthUserDto>> GetSwitchTargetsAsync()
    {
        using var connection = new MySqlConnection(_connectionString);
        var sql = @"
            SELECT
                u.UserID as UserId, u.Username, u.Role,
                u.StaffID as StaffId, u.PatientID as PatientId,
                COALESCE(
                    CONCAT(s.FirstName, ' ', s.LastName),
                    CONCAT(p.FirstName, ' ', p.LastName),
                    u.Username
                ) AS DisplayName
            FROM USER_ACCOUNT u
            LEFT JOIN STAFF   s ON u.StaffID   = s.StaffID
            LEFT JOIN PATIENT p ON u.PatientID = p.PatientID
            WHERE u.IsActive = 1 AND u.Role <> 'Admin'
            ORDER BY u.Role, DisplayName";
        return await connection.QueryAsync<AuthUserDto>(sql);
    }

    public async Task<IEnumerable<UserAdminDto>> GetAllUsersAsync(bool includeInactive)
    {
        using var connection = new MySqlConnection(_connectionString);
        var sql = @"
            SELECT
                u.UserID as UserId, u.Username, u.Email, u.Role,
                u.StaffID as StaffId, u.PatientID as PatientId,
                u.IsActive, u.LastLogin, u.CreatedAt,
                COALESCE(
                    CONCAT(s.FirstName, ' ', s.LastName),
                    CONCAT(p.FirstName, ' ', p.LastName),
                    u.Username
                ) AS DisplayName
            FROM USER_ACCOUNT u
            LEFT JOIN STAFF   s ON u.StaffID   = s.StaffID
            LEFT JOIN PATIENT p ON u.PatientID = p.PatientID" +
            (includeInactive ? "" : " WHERE u.IsActive = 1") + @"
            ORDER BY u.IsActive DESC, u.Role, DisplayName";
        return await connection.QueryAsync<UserAdminDto>(sql);
    }

    public async Task<int> CreateUserAsync(CreateUserRequest request, string passwordHash)
    {
        using var connection = new MySqlConnection(_connectionString);
        var sql = @"
            INSERT INTO USER_ACCOUNT (Username, Email, PasswordHash, Role, StaffID, PatientID, IsActive)
            VALUES (@Username, @Email, @PasswordHash, @Role, @StaffId, @PatientId, 1);
            SELECT LAST_INSERT_ID();";
        return await connection.ExecuteScalarAsync<int>(sql, new {
            request.Username, request.Email, PasswordHash = passwordHash,
            request.Role, request.StaffId, request.PatientId
        });
    }

    public async Task<bool> UpdateUserAsync(int userId, UpdateUserRequest request)
    {
        using var connection = new MySqlConnection(_connectionString);
        var sql = @"
            UPDATE USER_ACCOUNT SET
                Email = @Email, Role = @Role, StaffID = @StaffId, PatientID = @PatientId
            WHERE UserID = @UserId";
        var affected = await connection.ExecuteAsync(sql, new {
            UserId = userId, request.Email, request.Role,
            request.StaffId, request.PatientId
        });
        return affected > 0;
    }

    // Soft deletion: the account keeps its history; it just can't sign in.
    public async Task<bool> SetActiveAsync(int userId, bool isActive)
    {
        using var connection = new MySqlConnection(_connectionString);
        var affected = await connection.ExecuteAsync(
            "UPDATE USER_ACCOUNT SET IsActive = @IsActive WHERE UserID = @UserId",
            new { UserId = userId, IsActive = isActive });
        return affected > 0;
    }

    public async Task<int> CountActiveAdminsAsync()
    {
        using var connection = new MySqlConnection(_connectionString);
        return await connection.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM USER_ACCOUNT WHERE Role = 'Admin' AND IsActive = 1");
    }
}

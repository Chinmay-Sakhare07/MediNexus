using HospitalManagement.API.Models.DTOs;
using HospitalManagement.API.Models.Requests;

namespace HospitalManagement.API.Repositories.Interfaces;

public interface IUserRepository
{
    Task<UserAccountRecord?> GetByLoginAsync(string usernameOrEmail);
    Task<UserAccountRecord?> GetByIdAsync(int userId);
    Task<IEnumerable<AuthUserDto>> GetSwitchTargetsAsync();
    Task<IEnumerable<UserAdminDto>> GetAllUsersAsync(bool includeInactive);
    Task<int> CreateUserAsync(CreateUserRequest request, string passwordHash);
    Task<bool> UpdateUserAsync(int userId, UpdateUserRequest request);
    Task<bool> SetActiveAsync(int userId, bool isActive);
    Task<int> CountActiveAdminsAsync();
    Task UpdateLastLoginAsync(int userId, DateTime utcNow);
    Task<bool> UpdatePasswordHashAsync(int userId, string newHash);
}

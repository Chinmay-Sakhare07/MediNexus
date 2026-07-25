using HospitalManagement.API.Models.DTOs;

namespace HospitalManagement.API.Repositories.Interfaces;

public interface IUserRepository
{
    Task<UserAccountRecord?> GetByLoginAsync(string usernameOrEmail);
    Task<UserAccountRecord?> GetByIdAsync(int userId);
    Task<IEnumerable<AuthUserDto>> GetSwitchTargetsAsync();
    Task UpdateLastLoginAsync(int userId, DateTime utcNow);
    Task<bool> UpdatePasswordHashAsync(int userId, string newHash);
}

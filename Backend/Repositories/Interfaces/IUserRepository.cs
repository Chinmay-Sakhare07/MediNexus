using HospitalManagement.API.Models.DTOs;

namespace HospitalManagement.API.Repositories.Interfaces;

public interface IUserRepository
{
    Task<UserAccountRecord?> GetByLoginAsync(string usernameOrEmail);
    Task UpdateLastLoginAsync(int userId, DateTime utcNow);
    Task<bool> UpdatePasswordHashAsync(int userId, string newHash);
}

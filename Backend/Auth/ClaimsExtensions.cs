using System.Security.Claims;

namespace HospitalManagement.API.Auth;

public static class ClaimsExtensions
{
    public static string GetRole(this ClaimsPrincipal user) =>
        user.FindFirstValue(ClaimTypes.Role) ?? string.Empty;

    public static bool IsInRoleName(this ClaimsPrincipal user, string role) =>
        string.Equals(user.GetRole(), role, StringComparison.Ordinal);

    public static int? GetStaffId(this ClaimsPrincipal user) =>
        int.TryParse(user.FindFirstValue("staffId"), out var id) ? id : null;

    public static int? GetPatientId(this ClaimsPrincipal user) =>
        int.TryParse(user.FindFirstValue("patientId"), out var id) ? id : null;

    public static int? GetUserId(this ClaimsPrincipal user) =>
        int.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;

    public static string GetUsername(this ClaimsPrincipal user) =>
        user.FindFirstValue(ClaimTypes.Name) ?? string.Empty;
}

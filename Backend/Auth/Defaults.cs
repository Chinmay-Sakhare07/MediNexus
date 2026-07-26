namespace HospitalManagement.API.Auth;

public static class Defaults
{
    /// <summary>
    /// Every newly created account starts with this password, and "reset
    /// password" returns an account to it. Users change their own via
    /// POST /api/auth/change-password (and the sidebar UI).
    /// </summary>
    public const string UserPassword = "MediNexus@2026";
}

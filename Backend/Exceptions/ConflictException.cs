namespace HospitalManagement.API.Exceptions;

/// <summary>Business-rule conflict (e.g. double-booked slot). Middleware maps it to HTTP 409.</summary>
public class ConflictException : Exception
{
    public ConflictException(string message) : base(message) { }
}

using System.Text.Json;
using MySqlConnector;
using HospitalManagement.API.Exceptions;
using HospitalManagement.API.Json;
using HospitalManagement.API.Models;

namespace HospitalManagement.API.Middleware;

/// <summary>
/// Single place errors become responses. Controllers no longer catch-and-500:
/// this middleware logs the truth (LogBase-ready) and returns a consistent
/// ApiResponse with a friendly message — never a raw exception message.
/// </summary>
public class ExceptionHandlingMiddleware
{
    private static readonly JsonSerializerOptions JsonOpts = BuildJsonOptions();

    private static JsonSerializerOptions BuildJsonOptions()
    {
        var o = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        o.Converters.Add(new UtcDateTimeConverter());
        o.Converters.Add(new NullableUtcDateTimeConverter());
        return o;
    }

    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex) when (!context.Response.HasStarted)
        {
            var (status, message) = Map(ex);

            if (status >= 500)
                _logger.LogError(ex, "UNHANDLED_EXCEPTION method={Method} path={Path}",
                    context.Request.Method, context.Request.Path);
            else
                _logger.LogWarning("REQUEST_REJECTED status={Status} method={Method} path={Path} reason={Reason}",
                    status, context.Request.Method, context.Request.Path, ex.Message);

            context.Response.Clear();
            context.Response.StatusCode = status;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync(
                JsonSerializer.Serialize(ApiResponse<object>.ErrorResponse(message), JsonOpts));
        }
    }

    private static (int Status, string Message) Map(Exception ex) => ex switch
    {
        ConflictException ce => (StatusCodes.Status409Conflict, ce.Message),

        MySqlException { Number: 1062 } my =>
            (StatusCodes.Status409Conflict,
             my.Message.Contains("UX_Appointment", StringComparison.OrdinalIgnoreCase)
                 ? "That time slot is already booked. Please pick a different time."
                 : "A record with the same unique value already exists."),

        MySqlException { Number: 1451 } =>
            (StatusCodes.Status409Conflict,
             "This record is referenced by other data and cannot be deleted."),

        MySqlException { Number: 1452 } =>
            (StatusCodes.Status422UnprocessableEntity,
             "A referenced record does not exist. Please check the selected values."),

        MySqlException { Number: 3819 } =>
            (StatusCodes.Status422UnprocessableEntity,
             "One of the values violates a data rule. Please review the input."),

        BadHttpRequestException =>
            (StatusCodes.Status400BadRequest, "The request was malformed."),

        _ => (StatusCodes.Status500InternalServerError,
              "Something went wrong on our side. Please try again.")
    };
}

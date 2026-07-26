using System.Collections.Concurrent;
using System.Globalization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using HospitalManagement.API.Logging;
using HospitalManagement.API.Models;

namespace HospitalManagement.API.Controllers;

/// <summary>
/// Browser -> backend log proxy. The React app reports errors here; we map
/// them onto the v1 contract and push them into the SAME channel the backend
/// loggers use. The browser never talks to LogBase and never sees the key.
/// Anonymous by design — errors happen before login too.
/// </summary>
[ApiController]
[Route("api/client-logs")]
public class ClientLogsController : ControllerBase
{
    private const int MaxEvents = 20;
    private const long MaxBodyBytes = 32_768;
    private const int MaxEventsPerMinutePerIp = 60;
    private static readonly string[] Levels = { "DEBUG", "INFO", "WARNING", "ERROR", "CRITICAL" };

    // Per-IP sliding-minute event counter. In-memory on purpose; the IP is
    // used transiently for throttling and never stored on any event.
    private static readonly ConcurrentDictionary<string, (long WindowStart, int Count)> Buckets = new();

    private readonly ILogBaseSink _sink;

    public ClientLogsController(ILogBaseSink sink) => _sink = sink;

    public class ClientLogBatch
    {
        public List<ClientLogItem> Events { get; set; } = new();
    }

    public class ClientLogItem
    {
        public string? Timestamp { get; set; }
        public string? Level { get; set; }
        public string? Message { get; set; }
        public string? Stack { get; set; }
        public string? Url { get; set; }
        public string? SessionId { get; set; }
    }

    [HttpPost]
    [AllowAnonymous]
    public ActionResult<ApiResponse<int>> Ingest([FromBody] ClientLogBatch batch)
    {
        if (Request.ContentLength is > MaxBodyBytes)
            return StatusCode(StatusCodes.Status413PayloadTooLarge,
                ApiResponse<int>.ErrorResponse("Payload too large (32 KB max)"));

        if (batch?.Events is null || batch.Events.Count == 0)
            return BadRequest(ApiResponse<int>.ErrorResponse("No events"));

        if (batch.Events.Count > MaxEvents)
            return StatusCode(StatusCodes.Status413PayloadTooLarge,
                ApiResponse<int>.ErrorResponse($"Too many events ({MaxEvents} max per request)"));

        if (!TryConsumeBudget(batch.Events.Count))
            return StatusCode(StatusCodes.Status429TooManyRequests,
                ApiResponse<int>.ErrorResponse("Client log rate limit reached; try later"));

        var userAgent = LogBaseLogger.Truncate(
            Request.Headers.UserAgent.ToString() ?? string.Empty, 256);

        var accepted = 0;
        foreach (var item in batch.Events)
        {
            var level = (item.Level ?? "ERROR").ToUpperInvariant();
            if (!Levels.Contains(level)) level = "ERROR";

            string? timestamp = null;
            if (DateTime.TryParse(item.Timestamp, CultureInfo.InvariantCulture,
                    DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out var parsed))
                timestamp = parsed.ToString("yyyy-MM-dd'T'HH:mm:ss.fff'Z'");

            var fields = new Dictionary<string, string> { ["user_agent"] = userAgent };
            if (!string.IsNullOrWhiteSpace(item.Url))
                fields["url"] = LogBaseLogger.Truncate(item.Url.Split('?')[0], 300); // no query strings
            if (!string.IsNullOrWhiteSpace(item.SessionId))
                fields["session_id"] = LogBaseLogger.Truncate(item.SessionId, 64);

            var evt = LogBaseEvent.Create(
                "medinexus-frontend",
                level,
                "browser",
                LogBaseLogger.Truncate(item.Message ?? "(no message)", 16_384),
                string.IsNullOrWhiteSpace(item.Stack) ? null : new LogBaseException
                {
                    Type = "BrowserError",
                    Message = LogBaseLogger.Truncate(item.Message ?? string.Empty, 2_048),
                    Stacktrace = LogBaseLogger.Truncate(item.Stack, 8_192)
                },
                traceId: null,
                requestId: null,
                fields,
                timestamp);

            if (_sink.TryEnqueue(evt)) accepted++;
        }

        // 202 even when the sink is disabled: the browser's job is done either way.
        return Accepted(ApiResponse<int>.SuccessResponse(accepted, "Received"));
    }

    private bool TryConsumeBudget(int eventCount)
    {
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        var updated = Buckets.AddOrUpdate(ip,
            _ => (now, eventCount),
            (_, bucket) => now - bucket.WindowStart >= 60
                ? (now, eventCount)
                : (bucket.WindowStart, bucket.Count + eventCount));

        if (Buckets.Count > 10_000) Buckets.Clear(); // crude memory guard
        return updated.Count <= MaxEventsPerMinutePerIp;
    }
}

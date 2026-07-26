using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Channels;

namespace HospitalManagement.API.Logging;

/// <summary>
/// Ships log events to LogBase (my own log analytics platform) over its v1
/// ingest contract. Design rules: batched (≤100 events / 2s), bounded queue
/// (10k, drop-oldest), retry with jitter only on retryable failures,
/// at-most-once, wake-then-send for a scale-to-zero target — and logging can
/// never block, slow, or fail a hospital request. The provider never logs
/// through ILogger (loop risk); internal trouble goes to Console.Error only,
/// and never includes the API key.
/// </summary>
public class LogBaseOptions
{
    public bool Enabled { get; set; }
    public string BaseUrl { get; set; } = string.Empty;   // LOGBASE_URL (no /ingest)
    public string ApiKey { get; set; } = string.Empty;    // LOGBASE_API_KEY
    public string ServiceName { get; set; } = "medinexus-api";
    public string MinimumLevel { get; set; } = "Information";

    public static LogBaseOptions FromConfiguration(IConfiguration config) => new()
    {
        Enabled = string.Equals(config["LOGBASE_ENABLED"], "true", StringComparison.OrdinalIgnoreCase),
        BaseUrl = (config["LOGBASE_URL"] ?? string.Empty).TrimEnd('/'),
        ApiKey = config["LOGBASE_API_KEY"] ?? string.Empty,
        ServiceName = config["LOGBASE_SERVICE"] ?? "medinexus-api",
        MinimumLevel = config["LOGBASE_MIN_LEVEL"] ?? "Information"
    };
}

/// <summary>Lets other components (the /api/client-logs proxy) push events
/// into the same channel the backend loggers use.</summary>
public interface ILogBaseSink
{
    bool Enabled { get; }
    bool TryEnqueue(LogBaseEvent evt);
}

public sealed class NoopLogBaseSink : ILogBaseSink
{
    public bool Enabled => false;
    public bool TryEnqueue(LogBaseEvent evt) => false;
}

public static class LogBaseExtensions
{
    public static WebApplicationBuilder AddLogBase(this WebApplicationBuilder builder)
    {
        var options = LogBaseOptions.FromConfiguration(builder.Configuration);
        if (!options.Enabled || string.IsNullOrWhiteSpace(options.BaseUrl)
            || string.IsNullOrWhiteSpace(options.ApiKey))
        {
            builder.Services.AddSingleton<ILogBaseSink, NoopLogBaseSink>();
            return builder; // not configured -> zero footprint
        }

        builder.Services.AddHttpContextAccessor();
        builder.Services.AddSingleton(sp =>
            new LogBaseLoggerProvider(options, sp.GetRequiredService<IHttpContextAccessor>()));
        builder.Services.AddSingleton<ILoggerProvider>(sp => sp.GetRequiredService<LogBaseLoggerProvider>());
        builder.Services.AddSingleton<ILogBaseSink>(sp => sp.GetRequiredService<LogBaseLoggerProvider>());
        return builder;
    }
}

public sealed class LogBaseLoggerProvider : ILoggerProvider, ILogBaseSink
{
    private readonly LogBaseOptions _options;
    private readonly IHttpContextAccessor _httpContext;
    private readonly LogLevel _minLevel;
    private readonly Channel<LogBaseEvent> _channel;
    private readonly Task _shipper;
    private readonly CancellationTokenSource _cts = new();
    private long _dropped;                       // channel overflow + abandoned batches
    private DateTime _lastSuccessUtc = DateTime.MinValue;
    private static readonly TimeSpan ColdThreshold = TimeSpan.FromMinutes(5);
    private static readonly HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(15) };
    private static readonly Random Jitter = new();

    public LogBaseLoggerProvider(LogBaseOptions options, IHttpContextAccessor httpContext)
    {
        _options = options;
        _httpContext = httpContext;
        _minLevel = Enum.TryParse<LogLevel>(options.MinimumLevel, true, out var lvl)
            ? lvl : LogLevel.Information;
        _channel = Channel.CreateBounded<LogBaseEvent>(
            new BoundedChannelOptions(10_000)
            {
                FullMode = BoundedChannelFullMode.DropOldest,
                SingleReader = true
            },
            _ => Interlocked.Increment(ref _dropped));
        _shipper = Task.Run(ShipLoopAsync);
    }

    public bool Enabled => true;

    public bool TryEnqueue(LogBaseEvent evt) => _channel.Writer.TryWrite(evt);

    public ILogger CreateLogger(string categoryName) =>
        new LogBaseLogger(categoryName, this, _options, _minLevel, _httpContext);

    // ---- background shipping ----

    private async Task ShipLoopAsync()
    {
        var reader = _channel.Reader;
        var batch = new List<LogBaseEvent>(100);

        while (!_cts.IsCancellationRequested)
        {
            batch.Clear();
            try
            {
                if (!await reader.WaitToReadAsync(_cts.Token)) break;
                var window = Task.Delay(TimeSpan.FromSeconds(2), _cts.Token);
                while (batch.Count < 100 && !window.IsCompleted)
                {
                    if (reader.TryRead(out var evt)) batch.Add(evt);
                    else
                    {
                        var readable = reader.WaitToReadAsync(_cts.Token).AsTask();
                        if (await Task.WhenAny(readable, window) == window) break;
                    }
                }
            }
            catch (OperationCanceledException) { break; }

            if (batch.Count > 0) await SendSplittingAsync(new List<LogBaseEvent>(batch), CancellationToken.None);
        }

        // Shutdown: stop accepting, best-effort flush within the hard cap.
        _channel.Writer.TryComplete();
        using var flushCts = new CancellationTokenSource(TimeSpan.FromSeconds(3));
        batch.Clear();
        while (reader.TryRead(out var evt))
        {
            batch.Add(evt);
            if (batch.Count == 100)
            {
                await SendSplittingAsync(new List<LogBaseEvent>(batch), flushCts.Token);
                batch.Clear();
                if (flushCts.IsCancellationRequested) return;
            }
        }
        if (batch.Count > 0 && !flushCts.IsCancellationRequested)
            await SendSplittingAsync(batch, flushCts.Token);
    }

    /// <summary>Respects the 1 MB body cap by halving oversized batches;
    /// each POST group gets its own batch_id (reused across its retries).</summary>
    private async Task SendSplittingAsync(List<LogBaseEvent> events, CancellationToken ct)
    {
        var envelope = new LogBaseEnvelope { BatchId = Guid.NewGuid().ToString(), Events = events };
        var body = JsonSerializer.SerializeToUtf8Bytes(envelope, JsonOpts);

        if (body.Length > 1_000_000 && events.Count > 1)
        {
            var mid = events.Count / 2;
            await SendSplittingAsync(events.GetRange(0, mid), ct);
            await SendSplittingAsync(events.GetRange(mid, events.Count - mid), ct);
            return;
        }
        if (body.Length > 1_000_000)
        {
            Interlocked.Increment(ref _dropped); // single pathological event
            return;
        }

        await SendWithRetryAsync(body, events.Count, ct);
    }

    private async Task SendWithRetryAsync(byte[] body, int eventCount, CancellationToken ct)
    {
        // Wake-then-send: a scale-to-zero LogBase (Fly) has probably slept if
        // we haven't delivered in a while. Fly's proxy holds requests while
        // the machine boots, so one generous ping usually IS the warm-up.
        if (DateTime.UtcNow - _lastSuccessUtc > ColdThreshold)
        {
            try { await Http.GetAsync($"{_options.BaseUrl}/health", ct); }
            catch { /* outcome irrelevant; the point was the knock */ }
        }

        var delays = new[] { 1.0, 2.0, 4.0 }; // seconds, ±20% jitter, then drop
        for (var attempt = 0; ; attempt++)
        {
            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Post, $"{_options.BaseUrl}/ingest")
                {
                    Content = new ByteArrayContent(body)
                };
                request.Content.Headers.ContentType =
                    new System.Net.Http.Headers.MediaTypeHeaderValue("application/json") { CharSet = "utf-8" };
                request.Headers.TryAddWithoutValidation("X-API-Key", _options.ApiKey);

                var response = await Http.SendAsync(request, ct);
                if (response.IsSuccessStatusCode) // any 2xx (200 sync / 202 async)
                {
                    _lastSuccessUtc = DateTime.UtcNow;
                    ReportDropsIfAny();
                    return;
                }

                var status = (int)response.StatusCode;
                var retryable = status == 429 || status >= 500;
                if (!retryable)
                {
                    // Other 4xx will not succeed on retry: drop immediately.
                    Interlocked.Add(ref _dropped, eventCount);
                    Console.Error.WriteLine(
                        $"logbase-shipper: batch of {eventCount} rejected with HTTP {status}; dropped");
                    return;
                }
            }
            catch (OperationCanceledException) { Interlocked.Add(ref _dropped, eventCount); return; }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"logbase-shipper: send failed ({ex.GetType().Name})");
            }

            if (attempt >= delays.Length)
            {
                Interlocked.Add(ref _dropped, eventCount); // abandoned after retries
                return;
            }
            var jitter = 1 + (Jitter.NextDouble() * 0.4 - 0.2); // ±20%
            try { await Task.Delay(TimeSpan.FromSeconds(delays[attempt] * jitter), ct); }
            catch (OperationCanceledException) { Interlocked.Add(ref _dropped, eventCount); return; }
        }
    }

    private void ReportDropsIfAny()
    {
        var n = Interlocked.Exchange(ref _dropped, 0);
        if (n == 0) return;
        var confession = LogBaseEvent.Create(
            _options.ServiceName, "WARNING", "logbase-shipper",
            $"logbase-shipper: dropped {n} events since last report",
            traceId: null, requestId: null, fields: null);
        if (!_channel.Writer.TryWrite(confession))
            Interlocked.Add(ref _dropped, n); // channel full again; keep the tally
    }

    internal static readonly JsonSerializerOptions JsonOpts = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.Never
    };

    public void Dispose()
    {
        _cts.Cancel();
        try { _shipper.Wait(TimeSpan.FromSeconds(4)); } catch { }
        _cts.Dispose();
    }
}

public sealed class LogBaseLogger : ILogger
{
    private readonly string _category;
    private readonly ILogBaseSink _sink;
    private readonly LogBaseOptions _options;
    private readonly LogLevel _minLevel;
    private readonly IHttpContextAccessor _httpContext;

    public LogBaseLogger(string category, ILogBaseSink sink, LogBaseOptions options,
        LogLevel minLevel, IHttpContextAccessor httpContext)
    {
        _category = category;
        _sink = sink;
        _options = options;
        _minLevel = minLevel;
        _httpContext = httpContext;
    }

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

    public bool IsEnabled(LogLevel logLevel)
    {
        if (logLevel < _minLevel) return false;
        if (_category.StartsWith("System.Net.Http")) return false; // self-loop guard
        if ((_category.StartsWith("Microsoft") || _category.StartsWith("System"))
            && logLevel < LogLevel.Warning) return false;
        return true;
    }

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state,
        Exception? exception, Func<TState, Exception?, string> formatter)
    {
        if (!IsEnabled(logLevel)) return;

        Dictionary<string, object?>? fields = null;
        if (state is IReadOnlyList<KeyValuePair<string, object?>> pairs)
        {
            foreach (var pair in pairs)
            {
                if (pair.Key == "{OriginalFormat}") continue;
                fields ??= new Dictionary<string, object?>();
                if (fields.Count >= 25) break; // stays well under the 4 KB fields cap
                // Keep primitives as-is (dict[str, Any] on the server); stringify the rest.
                fields[pair.Key] = pair.Value is null or string or bool
                    or int or long or double or decimal or float
                    ? pair.Value
                    : Truncate(pair.Value.ToString() ?? string.Empty, 256);
            }
        }

        // No exception field on the server: fold type into the message, and
        // put the stacktrace in fields (mixed-type dict, 8 KB cap here).
        var message = formatter(state, exception);
        if (exception is not null)
        {
            message += $" | {exception.GetType().Name}: {exception.Message}";
            fields ??= new Dictionary<string, object?>();
            fields["exception_type"] = exception.GetType().FullName ?? exception.GetType().Name;
            fields["stacktrace"] = Truncate(exception.ToString(), 8_192);
        }

        var evt = LogBaseEvent.Create(
            _options.ServiceName,
            logLevel switch
            {
                LogLevel.Trace => "TRACE",
                LogLevel.Debug => "DEBUG",
                LogLevel.Information => "INFO",
                LogLevel.Warning => "WARNING",
                LogLevel.Error => "ERROR",
                _ => "CRITICAL"
            },
            _category,
            Truncate(message, 16_384),
            Activity.Current?.TraceId.ToString(),
            _httpContext.HttpContext?.TraceIdentifier,
            fields);

        _sink.TryEnqueue(evt);
    }

    internal static string Truncate(string value, int max) =>
        value.Length <= max ? value : value[..max];
}

// ---- v1 wire contract (exact property names via attributes) ----

public sealed class LogBaseEnvelope
{
    [JsonPropertyName("batch_id")] public string BatchId { get; set; } = string.Empty;
    [JsonPropertyName("events")] public List<LogBaseEvent> Events { get; set; } = new();
}

public sealed class LogBaseEvent
{
    [JsonPropertyName("event_id")] public string EventId { get; set; } = string.Empty;
    [JsonPropertyName("timestamp")] public string Timestamp { get; set; } = string.Empty;
    [JsonPropertyName("service")] public string Service { get; set; } = string.Empty;
    [JsonPropertyName("severity")] public string Severity { get; set; } = string.Empty;
    [JsonPropertyName("host")] public string Host { get; set; } = "unknown";
    [JsonPropertyName("logger")] public string? Logger { get; set; }
    [JsonPropertyName("message")] public string Message { get; set; } = string.Empty;
    [JsonPropertyName("trace_id")] public string? TraceId { get; set; }
    [JsonPropertyName("request_id")] public string? RequestId { get; set; }
    // dict[str, Any] on the server: mixed-value structured data lives here.
    [JsonPropertyName("fields")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Dictionary<string, object?>? Fields { get; set; }
    [JsonPropertyName("schema")] public int Schema { get; set; } = 1;

    private static readonly string HostName =
        Environment.GetEnvironmentVariable("RENDER_INSTANCE_ID")
        ?? Environment.GetEnvironmentVariable("LOGBASE_SERVICE")
        ?? Environment.MachineName;

    public static LogBaseEvent Create(string service, string severity, string? logger,
        string message, string? traceId, string? requestId,
        Dictionary<string, object?>? fields, string? timestamp = null) => new()
    {
        EventId = Guid.CreateVersion7().ToString(),
        Timestamp = timestamp ?? DateTime.UtcNow.ToString("yyyy-MM-dd'T'HH:mm:ss.fff'Z'"),
        Service = service,
        Severity = severity,
        Host = HostName,
        Logger = logger,
        Message = message,
        TraceId = traceId,
        RequestId = requestId,
        Fields = fields
    };
}

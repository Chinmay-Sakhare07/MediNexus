using System.Text.Json;
using StackExchange.Redis;

namespace HospitalManagement.API.Caching;

/// <summary>
/// Cache-aside over Valkey (Redis-compatible, Aiven free tier). Gracefully
/// absent: no configuration or a dead connection means every call quietly
/// passes through to the database — caching is an optimization, never a
/// dependency.
/// </summary>
public interface ICacheService
{
    Task<T?> GetAsync<T>(string key);
    Task SetAsync<T>(string key, T value, TimeSpan ttl);
    Task RemoveAsync(string key);
}

public sealed class RedisCacheService : ICacheService, IDisposable
{
    private readonly Lazy<Task<IConnectionMultiplexer?>>? _connection;
    private readonly ILogger<RedisCacheService> _logger;
    private static readonly JsonSerializerOptions JsonOpts = new(JsonSerializerDefaults.Web);

    public RedisCacheService(IConfiguration config, ILogger<RedisCacheService> logger)
    {
        _logger = logger;
        var url = config["Cache:RedisUrl"];
        if (string.IsNullOrWhiteSpace(url)) return; // disabled

        _connection = new Lazy<Task<IConnectionMultiplexer?>>(() => ConnectAsync(url));
    }

    private async Task<IConnectionMultiplexer?> ConnectAsync(string url)
    {
        try
        {
            var options = ToOptions(url);
            options.AbortOnConnectFail = false;
            options.ConnectTimeout = 3000;
            var mux = await ConnectionMultiplexer.ConnectAsync(options);
            _logger.LogInformation("Valkey cache connected — dashboard, doctors and medicines are now cached");
            return mux;
        }
        catch (Exception ex)
        {
            _logger.LogWarning("Valkey cache unavailable ({Reason}) — running without caching", ex.Message);
            return null;
        }
    }

    // Accepts Aiven's rediss:// URI or a raw StackExchange config string.
    private static ConfigurationOptions ToOptions(string url)
    {
        if (url.StartsWith("redis://") || url.StartsWith("rediss://"))
        {
            var uri = new Uri(url);
            var options = new ConfigurationOptions { Ssl = uri.Scheme == "rediss" };
            options.EndPoints.Add(uri.Host, uri.Port);
            var userInfo = uri.UserInfo.Split(':', 2);
            if (userInfo.Length == 2) options.Password = Uri.UnescapeDataString(userInfo[1]);
            else if (userInfo.Length == 1 && userInfo[0].Length > 0) options.Password = Uri.UnescapeDataString(userInfo[0]);
            return options;
        }
        return ConfigurationOptions.Parse(url);
    }

    private async Task<IDatabase?> DbAsync()
    {
        if (_connection is null) return null;
        var mux = await _connection.Value;
        return mux?.GetDatabase();
    }

    public async Task<T?> GetAsync<T>(string key)
    {
        try
        {
            var db = await DbAsync();
            if (db is null) return default;
            var value = await db.StringGetAsync(key);
            return value.IsNullOrEmpty ? default : JsonSerializer.Deserialize<T>(value!, JsonOpts);
        }
        catch { return default; }
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan ttl)
    {
        try
        {
            var db = await DbAsync();
            if (db is null) return;
            await db.StringSetAsync(key, JsonSerializer.Serialize(value, JsonOpts), ttl);
        }
        catch { /* cache is optional */ }
    }

    public async Task RemoveAsync(string key)
    {
        try
        {
            var db = await DbAsync();
            if (db is null) return;
            await db.KeyDeleteAsync(key);
        }
        catch { }
    }

    public void Dispose()
    {
        if (_connection is { IsValueCreated: true })
        {
            try { _connection.Value.Result?.Dispose(); } catch { }
        }
    }
}

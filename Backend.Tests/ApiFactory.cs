using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Hosting;
using Xunit;

namespace HospitalManagement.API.Tests;

/// <summary>
/// Boots the real API in-process against a real MySQL (the CI service
/// container, or any database named by TEST_DB_CONNECTION). Development
/// environment gives the built-in JWT fallback secret; the login rate limit
/// is raised so token acquisition across tests never trips 429.
/// </summary>
public class ApiFactory : WebApplicationFactory<Program>
{
    public static readonly string ConnectionString =
        Environment.GetEnvironmentVariable("TEST_DB_CONNECTION")
        ?? "Server=127.0.0.1;Port=3306;Database=medinexus;User ID=root;Password=medinexus_test;SslMode=None;";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.UseSetting("ConnectionStrings:HospitalDb", ConnectionString);
        builder.UseSetting("RateLimit:LoginPermitPerMinute", "1000");
    }
}

[CollectionDefinition("api")]
public class ApiCollection : ICollectionFixture<ApiFactory> { }

/// <summary>Small helpers shared by every test: login, bearer calls, envelope reads.</summary>
public static class Api
{
    public const string DefaultPassword = "MediNexus@2026";
    private static readonly Dictionary<string, string> TokenCache = new();

    public static async Task<string> TokenAsync(HttpClient client, string user, string? password = null)
    {
        var cacheable = password is null;
        if (cacheable && TokenCache.TryGetValue(user, out var cached)) return cached;

        var response = await client.PostAsJsonAsync("/api/auth/login",
            new { login = user, password = password ?? DefaultPassword });
        response.EnsureSuccessStatusCode();
        var doc = await response.Content.ReadFromJsonAsync<JsonElement>();
        var token = doc.GetProperty("data").GetProperty("token").GetString()!;
        if (cacheable) TokenCache[user] = token;
        return token;
    }

    public static HttpRequestMessage Request(HttpMethod method, string url, string? token, object? body = null)
    {
        var request = new HttpRequestMessage(method, url);
        if (token != null)
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        if (body != null)
            request.Content = JsonContent.Create(body);
        return request;
    }

    public static async Task<JsonElement> ReadAsync(HttpResponseMessage response) =>
        await response.Content.ReadFromJsonAsync<JsonElement>();

    public static JsonElement Data(JsonElement envelope) => envelope.GetProperty("data");
}

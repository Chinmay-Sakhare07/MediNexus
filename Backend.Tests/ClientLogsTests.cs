using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace HospitalManagement.API.Tests;

[Collection("api")]
public class ClientLogsTests
{
    private readonly HttpClient _client;

    public ClientLogsTests(ApiFactory factory) => _client = factory.CreateClient();

    private static object Event(string message) => new
    {
        timestamp = DateTime.UtcNow.ToString("o"),
        level = "ERROR",
        message,
        stack = "Error: boom\n  at CI test",
        url = "/patients?secret=should-be-stripped",
        session_id = "ci-session"
    };

    [Fact]
    public async Task Anonymous_browser_errors_are_accepted_with_202()
    {
        var response = await _client.PostAsJsonAsync("/api/client-logs",
            new { events = new[] { Event("CI browser error one"), Event("CI browser error two") } });
        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
    }

    [Fact]
    public async Task More_than_twenty_events_is_rejected_with_413()
    {
        var events = Enumerable.Range(0, 21).Select(i => Event($"CI overflow {i}")).ToArray();
        var response = await _client.PostAsJsonAsync("/api/client-logs", new { events });
        Assert.Equal(HttpStatusCode.RequestEntityTooLarge, response.StatusCode);
    }

    [Fact]
    public async Task Empty_batch_is_a_bad_request()
    {
        var response = await _client.PostAsJsonAsync("/api/client-logs", new { events = Array.Empty<object>() });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}

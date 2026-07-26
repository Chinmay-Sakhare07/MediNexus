using System.Net;
using Xunit;

namespace HospitalManagement.API.Tests;

/// <summary>Create with the default password -> self-service change ->
/// admin reset-to-default -> soft delete -> reactivate.</summary>
[Collection("api")]
public class UsersLifecycleTests
{
    private readonly HttpClient _client;

    public UsersLifecycleTests(ApiFactory factory) => _client = factory.CreateClient();

    [Fact]
    public async Task Default_password_change_reset_and_soft_delete_lifecycle()
    {
        var admin = await Api.TokenAsync(_client, "admin");
        var username = $"ci.user.{Guid.NewGuid():N}"[..20];

        // Create WITHOUT a password -> the default applies.
        var created = await _client.SendAsync(Api.Request(HttpMethod.Post, "/api/users", admin, new
        {
            username, email = $"{username}@ci.local", role = "Receptionist"
        }));
        created.EnsureSuccessStatusCode();
        var userId = Api.Data(await Api.ReadAsync(created)).GetInt32();

        // Signs in with MediNexus@2026.
        var token = await Api.TokenAsync(_client, username, Api.DefaultPassword);

        // Changes their own password; default stops working, new one works.
        (await _client.SendAsync(Api.Request(HttpMethod.Post, "/api/auth/change-password", token,
            new { currentPassword = Api.DefaultPassword, newPassword = "CiChanged@12345" })))
            .EnsureSuccessStatusCode();
        var oldLogin = await _client.PostAsync("/api/auth/login",
            JsonContent(username, Api.DefaultPassword));
        Assert.Equal(HttpStatusCode.Unauthorized, oldLogin.StatusCode);
        await Api.TokenAsync(_client, username, "CiChanged@12345");

        // Admin resets -> default works again.
        (await _client.SendAsync(Api.Request(HttpMethod.Post,
            $"/api/users/{userId}/reset-password", admin))).EnsureSuccessStatusCode();
        await Api.TokenAsync(_client, username, Api.DefaultPassword);

        // Soft delete: sign-in refused, history kept; reactivate restores.
        (await _client.SendAsync(Api.Request(HttpMethod.Delete, $"/api/users/{userId}", admin)))
            .EnsureSuccessStatusCode();
        var deactivated = await _client.PostAsync("/api/auth/login",
            JsonContent(username, Api.DefaultPassword));
        Assert.Equal(HttpStatusCode.Unauthorized, deactivated.StatusCode);

        (await _client.SendAsync(Api.Request(HttpMethod.Put, $"/api/users/{userId}/activate", admin)))
            .EnsureSuccessStatusCode();
        await Api.TokenAsync(_client, username, Api.DefaultPassword);
    }

    [Fact]
    public async Task Admin_cannot_deactivate_their_own_account()
    {
        var admin = await Api.TokenAsync(_client, "admin");
        var me = await _client.SendAsync(Api.Request(HttpMethod.Get, "/api/auth/me", admin));
        var myId = Api.Data(await Api.ReadAsync(me)).GetProperty("userId").GetInt32();

        var response = await _client.SendAsync(Api.Request(HttpMethod.Delete, $"/api/users/{myId}", admin));
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private static StringContent JsonContent(string login, string password) =>
        new(System.Text.Json.JsonSerializer.Serialize(new { login, password }),
            System.Text.Encoding.UTF8, "application/json");
}

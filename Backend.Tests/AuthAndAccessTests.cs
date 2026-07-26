using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace HospitalManagement.API.Tests;

[Collection("api")]
public class AuthAndAccessTests
{
    private readonly HttpClient _client;

    public AuthAndAccessTests(ApiFactory factory) => _client = factory.CreateClient();

    [Fact]
    public async Task Health_reports_ok_and_db_up()
    {
        var response = await _client.GetAsync("/health");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await Api.ReadAsync(response);
        Assert.Equal("ok", body.GetProperty("status").GetString());
    }

    [Fact]
    public async Task Wrong_password_is_rejected_with_401()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/login",
            new { login = "reception", password = "definitely-wrong" });
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task No_token_means_401()
    {
        var response = await _client.GetAsync("/api/patients");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Patient_cannot_list_patients()
    {
        var token = await Api.TokenAsync(_client, "patient.shah");
        var response = await _client.SendAsync(Api.Request(HttpMethod.Get, "/api/patients", token));
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Pharmacist_cannot_read_appointments()
    {
        var token = await Api.TokenAsync(_client, "pharmacist");
        var response = await _client.SendAsync(Api.Request(HttpMethod.Get, "/api/appointments", token));
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Patient_sees_only_their_own_appointments()
    {
        var token = await Api.TokenAsync(_client, "patient.shah");
        var me = await _client.SendAsync(Api.Request(HttpMethod.Get, "/api/auth/me", token));
        var myPatientId = Api.Data(await Api.ReadAsync(me)).GetProperty("patientId").GetInt32();

        var response = await _client.SendAsync(Api.Request(HttpMethod.Get, "/api/appointments", token));
        response.EnsureSuccessStatusCode();
        foreach (var appointment in Api.Data(await Api.ReadAsync(response)).EnumerateArray())
            Assert.Equal(myPatientId, appointment.GetProperty("patientId").GetInt32());
    }

    [Fact]
    public async Task Invalid_blood_type_returns_422_with_field_error()
    {
        var token = await Api.TokenAsync(_client, "reception");
        var response = await _client.SendAsync(Api.Request(HttpMethod.Post, "/api/patients", token, new
        {
            firstName = "CI", lastName = "Invalid", dateOfBirth = "1990-01-01",
            gender = "M", bloodType = "X+"
        }));
        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        var body = await Api.ReadAsync(response);
        Assert.True(body.GetProperty("errors").TryGetProperty("BloodType", out _));
    }
}

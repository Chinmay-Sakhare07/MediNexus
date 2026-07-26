using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace HospitalManagement.API.Tests;

[Collection("api")]
public class BookingTests
{
    private readonly HttpClient _client;

    public BookingTests(ApiFactory factory) => _client = factory.CreateClient();

    internal static async Task<(int DoctorId, string Slot)> FirstOpenSlotAsync(HttpClient client, string token)
    {
        var me = await client.SendAsync(Api.Request(HttpMethod.Get, "/api/auth/me",
            await Api.TokenAsync(client, "dr.sharma")));
        var doctorId = Api.Data(await Api.ReadAsync(me)).GetProperty("staffId").GetInt32();

        for (var offset = 1; offset <= 8; offset++)
        {
            var date = DateTime.UtcNow.Date.AddDays(offset).ToString("yyyy-MM-dd");
            var response = await client.SendAsync(Api.Request(HttpMethod.Get,
                $"/api/appointments/slots?doctorId={doctorId}&date={date}", token));
            response.EnsureSuccessStatusCode();
            var slots = Api.Data(await Api.ReadAsync(response)).EnumerateArray()
                .Select(s => s.GetString()!).ToList();
            if (slots.Count > 0) return (doctorId, slots[0]);
        }
        throw new InvalidOperationException("No open slots found in the next 8 days");
    }

    [Fact]
    public async Task Booked_slots_are_never_offered_again_and_cannot_be_rebooked()
    {
        var reception = await Api.TokenAsync(_client, "reception");
        var (doctorId, slot) = await FirstOpenSlotAsync(_client, reception);

        var first = await _client.SendAsync(Api.Request(HttpMethod.Post, "/api/appointments", reception, new
        {
            patientId = 1, doctorId, dateTime = slot, duration = 30,
            reason = "CI slot test", appointmentType = "Consultation"
        }));
        first.EnsureSuccessStatusCode();

        // The slot vanishes from the offer list (D13)...
        var date = DateTime.Parse(slot).ToString("yyyy-MM-dd");
        var offered = await _client.SendAsync(Api.Request(HttpMethod.Get,
            $"/api/appointments/slots?doctorId={doctorId}&date={date}", reception));
        var remaining = Api.Data(await Api.ReadAsync(offered)).EnumerateArray()
            .Select(s => s.GetString()).ToList();
        Assert.DoesNotContain(slot, remaining);

        // ...and a second booking of the same slot is refused.
        var second = await _client.SendAsync(Api.Request(HttpMethod.Post, "/api/appointments", reception, new
        {
            patientId = 2, doctorId, dateTime = slot, duration = 30,
            reason = "CI slot clash", appointmentType = "Consultation"
        }));
        Assert.False(second.IsSuccessStatusCode);
        var body = await Api.ReadAsync(second);
        Assert.Contains("not available", body.GetProperty("message").GetString());
    }
}

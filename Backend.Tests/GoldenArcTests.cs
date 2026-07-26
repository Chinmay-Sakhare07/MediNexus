using System.Text.Json;
using Xunit;

namespace HospitalManagement.API.Tests;

/// <summary>
/// The whole hospital in one test: book -> check-in -> vitals -> consult ->
/// prescribe -> complete & bill (copay regression) -> pharmacy confirm/ready/
/// dispense (atomic stock decrement) -> pay by card (2.5% surcharge).
/// </summary>
[Collection("api")]
public class GoldenArcTests
{
    private readonly HttpClient _client;

    public GoldenArcTests(ApiFactory factory) => _client = factory.CreateClient();

    [Fact]
    public async Task Full_visit_arc_works_end_to_end()
    {
        var reception = await Api.TokenAsync(_client, "reception");
        var doctor = await Api.TokenAsync(_client, "dr.sharma");
        var nurse = await Api.TokenAsync(_client, "nurse.anderson");
        var pharmacist = await Api.TokenAsync(_client, "pharmacist");

        // Book a real offered slot for seeded patient 1 (insured post-migration-08).
        var (doctorId, slot) = await BookingTests.FirstOpenSlotAsync(_client, reception);
        var booked = await _client.SendAsync(Api.Request(HttpMethod.Post, "/api/appointments", reception, new
        {
            patientId = 1, doctorId, dateTime = slot, duration = 30,
            reason = "CI golden arc", appointmentType = "Consultation"
        }));
        booked.EnsureSuccessStatusCode();
        var appointmentId = Api.Data(await Api.ReadAsync(booked)).GetInt32();

        // Check-in -> vitals -> start -> consultation.
        (await _client.SendAsync(Api.Request(HttpMethod.Put,
            $"/api/appointments/{appointmentId}/checkin", reception))).EnsureSuccessStatusCode();

        (await _client.SendAsync(Api.Request(HttpMethod.Post,
            $"/api/files/{appointmentId}/vitals", nurse,
            new { bloodPressure = "120/80", pulse = "72" }))).EnsureSuccessStatusCode();

        (await _client.SendAsync(Api.Request(HttpMethod.Put,
            $"/api/appointments/{appointmentId}/start", doctor))).EnsureSuccessStatusCode();

        (await _client.SendAsync(Api.Request(HttpMethod.Post,
            $"/api/files/{appointmentId}/consultation", doctor,
            new { diagnosis = "CI verified condition", followUpRequired = false }))).EnsureSuccessStatusCode();

        // Pick a dispensable medicine and prescribe 1 unit.
        var meds = await _client.SendAsync(Api.Request(HttpMethod.Get, "/api/pharmacy/medicines", doctor));
        meds.EnsureSuccessStatusCode();
        var medicine = Api.Data(await Api.ReadAsync(meds)).EnumerateArray()
            .First(m => m.GetProperty("stockQuantity").GetInt32() >= 1 &&
                        (m.GetProperty("expiryDate").ValueKind == JsonValueKind.Null ||
                         m.GetProperty("expiryDate").GetDateTime() > DateTime.UtcNow));
        var medicineId = medicine.GetProperty("medicineId").GetInt32();
        var stockBefore = medicine.GetProperty("stockQuantity").GetInt32();

        (await _client.SendAsync(Api.Request(HttpMethod.Post,
            $"/api/files/{appointmentId}/prescription", doctor, new
            {
                validDays = 30,
                lines = new[] { new { medicineId, quantity = 1, dosage = "1 tab",
                                      frequency = "OD", duration = "5 days" } }
            }))).EnsureSuccessStatusCode();

        // Complete the visit -> consultation bill with an insurance claim (copay regression).
        (await _client.SendAsync(Api.Request(HttpMethod.Post,
            "/api/billing/complete-appointment", reception, new
            {
                appointmentId, consultationFee = 500, additionalFees = 0,
                additionalFeesDescription = "", discountPercentage = 0
            }))).EnsureSuccessStatusCode();

        var file = await _client.SendAsync(Api.Request(HttpMethod.Get, $"/api/files/{appointmentId}", reception));
        file.EnsureSuccessStatusCode();
        var fileData = Api.Data(await Api.ReadAsync(file));
        var consultationBill = fileData.GetProperty("bills").EnumerateArray()
            .First(b => b.GetProperty("billType").GetString() == "Consultation");
        Assert.True(consultationBill.GetProperty("insuranceCovered").GetDecimal() > 0,
            "Copay regression: the consultation bill must carry an insurance claim");

        // Pharmacy pipeline: confirm -> ready -> dispense.
        var prescriptionId = fileData.GetProperty("prescription").GetProperty("prescriptionId").GetInt32();
        (await _client.SendAsync(Api.Request(HttpMethod.Put,
            $"/api/pharmacy/prescriptions/{prescriptionId}/confirm", pharmacist))).EnsureSuccessStatusCode();
        (await _client.SendAsync(Api.Request(HttpMethod.Put,
            $"/api/pharmacy/prescriptions/{prescriptionId}/ready", pharmacist))).EnsureSuccessStatusCode();

        var dispensed = await _client.SendAsync(Api.Request(HttpMethod.Post,
            $"/api/pharmacy/prescriptions/{prescriptionId}/dispense", pharmacist));
        dispensed.EnsureSuccessStatusCode();
        var receipt = Api.Data(await Api.ReadAsync(dispensed));
        var pharmacyBillId = receipt.GetProperty("billId").GetInt32();
        var due = receipt.GetProperty("patientResponsibility").GetDecimal();

        // Stock decremented atomically (cache invalidation included).
        var medsAfter = await _client.SendAsync(Api.Request(HttpMethod.Get, "/api/pharmacy/medicines", pharmacist));
        var stockAfter = Api.Data(await Api.ReadAsync(medsAfter)).EnumerateArray()
            .First(m => m.GetProperty("medicineId").GetInt32() == medicineId)
            .GetProperty("stockQuantity").GetInt32();
        Assert.Equal(stockBefore - 1, stockAfter);

        // Pay the pharmacy bill by card: 2.5% service charge, server-computed.
        var paid = await _client.SendAsync(Api.Request(HttpMethod.Post, "/api/billing/pay", reception,
            new { billId = pharmacyBillId, method = "Card" }));
        paid.EnsureSuccessStatusCode();
        var payment = Api.Data(await Api.ReadAsync(paid));
        var surcharge = payment.GetProperty("cardSurcharge").GetDecimal();
        var total = payment.GetProperty("totalCharged").GetDecimal();
        Assert.Equal(Math.Round(due * 0.025m, 2), surcharge);
        Assert.Equal(due + surcharge, total);
    }
}

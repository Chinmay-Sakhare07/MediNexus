namespace HospitalManagement.API.Models.Requests;

/// <summary>Patient self-booking: patientId comes from the JWT, never the body.</summary>
public class BookAppointmentRequest
{
    public int DoctorId { get; set; }
    public DateTime DateTime { get; set; }   // must be one of the offered slots (UTC)
    public string? Reason { get; set; }
    public string? AppointmentType { get; set; }
}

public class RecordVitalsRequest
{
    public string? BloodPressure { get; set; } // "128/84"
    public string? Pulse { get; set; }
    public string? Temperature { get; set; }
    public string? Spo2 { get; set; }
    public string? Notes { get; set; }
}

public class SaveConsultationRequest
{
    public string Diagnosis { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public string? TreatmentPlan { get; set; }
    public bool FollowUpRequired { get; set; }
}

public class OrderLabTestsRequest
{
    public List<LabTestOrder> Tests { get; set; } = new();
}

public class LabTestOrder
{
    public string TestType { get; set; } = string.Empty;
    public string? NormalRange { get; set; }
    public string? Units { get; set; }
}

public class EnterLabResultRequest
{
    public string Result { get; set; } = string.Empty;
    public string? Comments { get; set; }
}

public class CreatePrescriptionRequest
{
    public int ValidDays { get; set; } = 30;
    public List<PrescriptionLine> Lines { get; set; } = new();
}

public class PrescriptionLine
{
    public int MedicineId { get; set; }
    public int Quantity { get; set; } = 1;
    public string? Dosage { get; set; }
    public string? Frequency { get; set; }
    public string? Duration { get; set; }
    public string? Instructions { get; set; }
}

public class RejectPrescriptionRequest
{
    public string Reason { get; set; } = string.Empty;
}

public class AdjustStockRequest
{
    /// <summary>Positive = restock, negative = correction. Result may never go below zero.</summary>
    public int Adjustment { get; set; }
    public string? Note { get; set; }
}

public class UpdateScheduleRequest
{
    public List<string> WorkDays { get; set; } = new(); // subset of Mon..Sun
    public string StartTime { get; set; } = "09:00";
    public string EndTime { get; set; } = "17:00";
    public int SlotMinutes { get; set; } = 30;
}

public class AddLeaveRequest
{
    public DateTime LeaveDate { get; set; }  // IST calendar date
    public string? Reason { get; set; }
}

public class PayBillRequest
{
    public int BillId { get; set; }
    /// <summary>Cash or Card. Card adds a 2.5% service charge, computed server-side.
    /// Insurance participates via the auto-filed claim on the bill itself.</summary>
    public string Method { get; set; } = string.Empty;
}

public class ImpersonateRequest
{
    public int UserId { get; set; }
}

public class CreateUserRequest
{
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public int? StaffId { get; set; }
    public int? PatientId { get; set; }
}

public class UpdateUserRequest
{
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public int? StaffId { get; set; }
    public int? PatientId { get; set; }
    /// <summary>Optional: set to reset the user's password.</summary>
    public string? NewPassword { get; set; }
}

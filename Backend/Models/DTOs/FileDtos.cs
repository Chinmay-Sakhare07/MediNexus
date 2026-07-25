namespace HospitalManagement.API.Models.DTOs;

/// <summary>The Patient File: one visit's full timeline, assembled as a projection.</summary>
public class FileDto
{
    public int AppointmentId { get; set; }
    public int PatientId { get; set; }
    public string PatientName { get; set; } = string.Empty;
    public int DoctorId { get; set; }
    public string DoctorName { get; set; } = string.Empty;
    public DateTime DateTime { get; set; }
    public string? Reason { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? AppointmentType { get; set; }
    public List<string> Allergies { get; set; } = new();
    public FileRecordDto? Record { get; set; }
    public List<FileLabTestDto> LabTests { get; set; } = new();
    public FilePrescriptionDto? Prescription { get; set; }
    public List<FileBillDto> Bills { get; set; } = new();
}

public class FileRecordDto
{
    public int RecordId { get; set; }
    public string? VitalSigns { get; set; }
    public string? Diagnosis { get; set; }
    public string? Notes { get; set; }
    public string? TreatmentPlan { get; set; }
    public bool FollowUpRequired { get; set; }
}

public class FileLabTestDto
{
    public int LabTestId { get; set; }
    public string? TestType { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? Result { get; set; }
    public string? Units { get; set; }
    public string? NormalRange { get; set; }
    public string? Comments { get; set; }
    public DateTime? ResultDate { get; set; }
    public string TechnicianName { get; set; } = string.Empty;
}

public class FilePrescriptionDto
{
    public int PrescriptionId { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? RejectReason { get; set; }
    public DateTime DateIssued { get; set; }
    public DateTime? ValidUntil { get; set; }
    public List<FilePrescriptionLineDto> Lines { get; set; } = new();
}

public class FilePrescriptionLineDto
{
    public int MedicineId { get; set; }
    public string MedicineName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public string? Dosage { get; set; }
    public string? Frequency { get; set; }
    public string? Duration { get; set; }
    public string? Instructions { get; set; }
    public decimal UnitPrice { get; set; }
}

public class FileBillDto
{
    public int BillId { get; set; }
    public string BillType { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public decimal InsuranceCovered { get; set; }
    public decimal PatientResponsibility { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? PaymentMethod { get; set; }
    public decimal CardSurcharge { get; set; }
}

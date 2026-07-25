namespace HospitalManagement.API.Models.DTOs;

public class MedicineDto
{
    public int MedicineId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Category { get; set; }
    public decimal UnitPrice { get; set; }
    public int StockQuantity { get; set; }
    public DateTime? ExpiryDate { get; set; }
}

public class PharmacyQueueItemDto
{
    public int PrescriptionId { get; set; }
    public int AppointmentId { get; set; }
    public string PatientName { get; set; } = string.Empty;
    public string DoctorName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? RejectReason { get; set; }
    public DateTime DateIssued { get; set; }
    public DateTime? ValidUntil { get; set; }
    public int ItemCount { get; set; }
    public decimal EstimatedTotal { get; set; }
}

public class DispenseResultDto
{
    public int BillId { get; set; }
    public decimal Amount { get; set; }
    public decimal InsuranceCovered { get; set; }
    public decimal PatientResponsibility { get; set; }
}

public class LabQueueItemDto
{
    public int LabTestId { get; set; }
    public int AppointmentId { get; set; }
    public string PatientName { get; set; } = string.Empty;
    public string DoctorName { get; set; } = string.Empty;
    public string? TestType { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? NormalRange { get; set; }
    public string? Units { get; set; }
    public string? Result { get; set; }
    public DateTime AppointmentDateTime { get; set; }
}

public class DoctorScheduleDto
{
    public int DoctorId { get; set; }
    public string WorkDays { get; set; } = string.Empty;
    public string StartTime { get; set; } = string.Empty; // "09:00"
    public string EndTime { get; set; } = string.Empty;   // "17:00"
    public int SlotMinutes { get; set; }
}

public class DoctorLeaveDto
{
    public int LeaveId { get; set; }
    public int DoctorId { get; set; }
    public DateTime LeaveDate { get; set; }
    public string? Reason { get; set; }
}

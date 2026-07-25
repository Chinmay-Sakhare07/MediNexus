using FluentValidation;
using HospitalManagement.API.Models.DTOs;
using HospitalManagement.API.Models.Requests;

namespace HospitalManagement.API.Validation;

// Loose-but-protective rules: they block nonsense without fighting the
// existing UI's value conventions. Tightened per-module as pages evolve.

public class RegisterPatientValidator : AbstractValidator<RegisterPatientRequest>
{
    public RegisterPatientValidator()
    {
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(50);
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Gender).NotEmpty().MaximumLength(10);
        RuleFor(x => x.DateOfBirth)
            .LessThanOrEqualTo(_ => DateTime.UtcNow)
            .WithMessage("Date of birth cannot be in the future")
            .GreaterThan(new DateTime(1900, 1, 1))
            .WithMessage("Date of birth looks implausible");
        RuleFor(x => x.Email).EmailAddress().MaximumLength(100)
            .When(x => !string.IsNullOrWhiteSpace(x.Email));
        RuleFor(x => x.PhoneNumber).Matches(@"^[0-9+\-() ]{7,15}$")
            .WithMessage("Phone number must be 7-15 digits")
            .When(x => !string.IsNullOrWhiteSpace(x.PhoneNumber));
        RuleFor(x => x.BloodType)
            .Must(bt => new[] { "A+", "A-", "B+", "B-", "AB+", "AB-", "O+", "O-" }.Contains(bt))
            .WithMessage("Blood type must be one of A+, A-, B+, B-, AB+, AB-, O+, O-")
            .When(x => !string.IsNullOrWhiteSpace(x.BloodType));
        RuleFor(x => x.Address).MaximumLength(200);
        RuleFor(x => x.EmergencyContact).MaximumLength(100);
        RuleFor(x => x.PrimaryPhysicianId).GreaterThan(0)
            .When(x => x.PrimaryPhysicianId.HasValue);
    }
}

public class UpdatePatientValidator : AbstractValidator<UpdatePatientRequest>
{
    public UpdatePatientValidator()
    {
        Include(new RegisterPatientBaseRules());
    }

    // Reuse the register rules by mapping — same shape, same rules.
    private class RegisterPatientBaseRules : AbstractValidator<UpdatePatientRequest>
    {
        public RegisterPatientBaseRules()
        {
            RuleFor(x => x.FirstName).NotEmpty().MaximumLength(50);
            RuleFor(x => x.LastName).NotEmpty().MaximumLength(50);
            RuleFor(x => x.Gender).NotEmpty().MaximumLength(10);
            RuleFor(x => x.DateOfBirth)
                .LessThanOrEqualTo(_ => DateTime.UtcNow)
                .WithMessage("Date of birth cannot be in the future")
                .GreaterThan(new DateTime(1900, 1, 1))
                .WithMessage("Date of birth looks implausible");
            RuleFor(x => x.Email).EmailAddress().MaximumLength(100)
                .When(x => !string.IsNullOrWhiteSpace(x.Email));
            RuleFor(x => x.PhoneNumber).Matches(@"^[0-9+\-() ]{7,15}$")
                .WithMessage("Phone number must be 7-15 digits")
                .When(x => !string.IsNullOrWhiteSpace(x.PhoneNumber));
            RuleFor(x => x.BloodType)
                .Must(bt => new[] { "A+", "A-", "B+", "B-", "AB+", "AB-", "O+", "O-" }.Contains(bt))
                .WithMessage("Blood type must be one of A+, A-, B+, B-, AB+, AB-, O+, O-")
                .When(x => !string.IsNullOrWhiteSpace(x.BloodType));
            RuleFor(x => x.Address).MaximumLength(200);
            RuleFor(x => x.EmergencyContact).MaximumLength(100);
            RuleFor(x => x.PrimaryPhysicianId).GreaterThan(0)
                .When(x => x.PrimaryPhysicianId.HasValue);
        }
    }
}

public class ScheduleAppointmentValidator : AbstractValidator<ScheduleAppointmentRequest>
{
    public ScheduleAppointmentValidator()
    {
        RuleFor(x => x.PatientId).GreaterThan(0);
        RuleFor(x => x.DoctorId).GreaterThan(0);
        RuleFor(x => x.DateTime)
            .GreaterThan(_ => DateTime.UtcNow)
            .WithMessage("Appointment time must be in the future");
        RuleFor(x => x.Duration).InclusiveBetween(5, 480)
            .WithMessage("Duration must be between 5 and 480 minutes");
        RuleFor(x => x.Reason).MaximumLength(200);
        RuleFor(x => x.AppointmentType).MaximumLength(50);
    }
}

public class CompleteAppointmentValidator : AbstractValidator<CompleteAppointmentRequest>
{
    public CompleteAppointmentValidator()
    {
        RuleFor(x => x.AppointmentId).GreaterThan(0);
        RuleFor(x => x.ConsultationFee).GreaterThanOrEqualTo(0);
        RuleFor(x => x.AdditionalFees).GreaterThanOrEqualTo(0);
        RuleFor(x => x.DiscountPercentage).InclusiveBetween(0, 100);
        RuleFor(x => x.AdditionalFeesDescription).MaximumLength(200);
    }
}

public class ProcessPaymentValidator : AbstractValidator<ProcessPaymentRequest>
{
    public ProcessPaymentValidator()
    {
        RuleFor(x => x.BillId).GreaterThan(0);
        RuleFor(x => x.AmountPaid).GreaterThan(0);
        RuleFor(x => x.PaymentMethod).NotEmpty().MaximumLength(30);
    }
}

public class AssignInsuranceValidator : AbstractValidator<AssignInsuranceRequest>
{
    public AssignInsuranceValidator()
    {
        RuleFor(x => x.PatientId).GreaterThan(0);
        RuleFor(x => x.PolicyId).GreaterThan(0);
    }
}

public class LoginValidator : AbstractValidator<LoginRequest>
{
    public LoginValidator()
    {
        RuleFor(x => x.Login).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Password).NotEmpty().MaximumLength(100);
    }
}

public class ChangePasswordValidator : AbstractValidator<ChangePasswordRequest>
{
    public ChangePasswordValidator()
    {
        RuleFor(x => x.CurrentPassword).NotEmpty();
        RuleFor(x => x.NewPassword).NotEmpty().MinimumLength(8).MaximumLength(100)
            .WithMessage("New password must be 8-100 characters");
    }
}

// ---- Patient File workflow validators (Phase 4) ----

public class BookAppointmentValidator : AbstractValidator<BookAppointmentRequest>
{
    public BookAppointmentValidator()
    {
        RuleFor(x => x.DoctorId).GreaterThan(0);
        RuleFor(x => x.DateTime).GreaterThan(_ => DateTime.UtcNow)
            .WithMessage("Appointment time must be in the future");
        RuleFor(x => x.Reason).MaximumLength(200);
        RuleFor(x => x.AppointmentType).MaximumLength(50);
    }
}

public class RecordVitalsValidator : AbstractValidator<RecordVitalsRequest>
{
    public RecordVitalsValidator()
    {
        RuleFor(x => x.BloodPressure).MaximumLength(20);
        RuleFor(x => x.Pulse).MaximumLength(10);
        RuleFor(x => x.Temperature).MaximumLength(10);
        RuleFor(x => x.Spo2).MaximumLength(10);
        RuleFor(x => x.Notes).MaximumLength(120);
        RuleFor(x => x)
            .Must(x => !string.IsNullOrWhiteSpace(x.BloodPressure) || !string.IsNullOrWhiteSpace(x.Pulse)
                    || !string.IsNullOrWhiteSpace(x.Temperature) || !string.IsNullOrWhiteSpace(x.Spo2))
            .WithMessage("Record at least one vital sign")
            .OverridePropertyName("Vitals");
    }
}

public class SaveConsultationValidator : AbstractValidator<SaveConsultationRequest>
{
    public SaveConsultationValidator()
    {
        RuleFor(x => x.Diagnosis).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Notes).MaximumLength(200);
        RuleFor(x => x.TreatmentPlan).MaximumLength(200);
    }
}

public class OrderLabTestsValidator : AbstractValidator<OrderLabTestsRequest>
{
    public OrderLabTestsValidator()
    {
        RuleFor(x => x.Tests).NotEmpty().WithMessage("Order at least one test");
        RuleFor(x => x.Tests.Count).LessThanOrEqualTo(10).WithMessage("At most 10 tests per order");
        RuleForEach(x => x.Tests).ChildRules(t =>
        {
            t.RuleFor(y => y.TestType).NotEmpty().MaximumLength(100);
            t.RuleFor(y => y.NormalRange).MaximumLength(50);
            t.RuleFor(y => y.Units).MaximumLength(20);
        });
    }
}

public class EnterLabResultValidator : AbstractValidator<EnterLabResultRequest>
{
    public EnterLabResultValidator()
    {
        RuleFor(x => x.Result).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Comments).MaximumLength(200);
    }
}

public class CreatePrescriptionValidator : AbstractValidator<CreatePrescriptionRequest>
{
    public CreatePrescriptionValidator()
    {
        RuleFor(x => x.ValidDays).InclusiveBetween(1, 90);
        RuleFor(x => x.Lines).NotEmpty().WithMessage("Add at least one medicine");
        RuleFor(x => x.Lines.Count).LessThanOrEqualTo(15).WithMessage("At most 15 medicines per prescription");
        RuleForEach(x => x.Lines).ChildRules(l =>
        {
            l.RuleFor(y => y.MedicineId).GreaterThan(0);
            l.RuleFor(y => y.Quantity).InclusiveBetween(1, 100);
            l.RuleFor(y => y.Dosage).MaximumLength(50);
            l.RuleFor(y => y.Frequency).MaximumLength(50);
            l.RuleFor(y => y.Duration).MaximumLength(50);
            l.RuleFor(y => y.Instructions).MaximumLength(200);
        });
    }
}

public class RejectPrescriptionValidator : AbstractValidator<RejectPrescriptionRequest>
{
    public RejectPrescriptionValidator()
    {
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(200);
    }
}

public class AdjustStockValidator : AbstractValidator<AdjustStockRequest>
{
    public AdjustStockValidator()
    {
        RuleFor(x => x.Adjustment).NotEqual(0).WithMessage("Adjustment cannot be zero");
        RuleFor(x => x.Adjustment).InclusiveBetween(-10000, 10000);
        RuleFor(x => x.Note).MaximumLength(200);
    }
}

public class UpdateScheduleValidator : AbstractValidator<UpdateScheduleRequest>
{
    private static readonly string[] Days = { "Mon", "Tue", "Wed", "Thu", "Fri", "Sat", "Sun" };

    public UpdateScheduleValidator()
    {
        RuleFor(x => x.WorkDays).NotEmpty().WithMessage("Pick at least one working day");
        RuleFor(x => x.WorkDays)
            .Must(d => d.All(x => Days.Contains(x)))
            .WithMessage("Days must be Mon..Sun");
        RuleFor(x => x.SlotMinutes).InclusiveBetween(5, 120);
        RuleFor(x => x)
            .Must(x => TimeSpan.TryParse(x.StartTime, out var s) && TimeSpan.TryParse(x.EndTime, out var e) && e > s)
            .WithMessage("End time must be after start time (HH:mm)")
            .OverridePropertyName("EndTime");
    }
}

public class AddLeaveValidator : AbstractValidator<AddLeaveRequest>
{
    public AddLeaveValidator()
    {
        // Loose lower bound; the controller anchors "today" to IST precisely.
        RuleFor(x => x.LeaveDate).GreaterThan(DateTime.UtcNow.AddDays(-2))
            .WithMessage("Leave date cannot be in the past");
        RuleFor(x => x.Reason).MaximumLength(200);
    }
}

public class PayBillValidator : AbstractValidator<PayBillRequest>
{
    public PayBillValidator()
    {
        RuleFor(x => x.BillId).GreaterThan(0);
        RuleFor(x => x.Method).Must(m => m == "Cash" || m == "Card")
            .WithMessage("Method must be Cash or Card");
    }
}

public class ImpersonateValidator : AbstractValidator<ImpersonateRequest>
{
    public ImpersonateValidator()
    {
        RuleFor(x => x.UserId).GreaterThan(0);
    }
}

// ---- User administration (CRUD with soft deletion) ----

public class CreateUserValidator : AbstractValidator<CreateUserRequest>
{
    private static readonly string[] ValidRoles =
        { "Admin", "Doctor", "Nurse", "LabTech", "Pharmacist", "Receptionist", "Patient" };

    public CreateUserValidator()
    {
        RuleFor(x => x.Username).NotEmpty().Length(3, 50)
            .Matches("^[a-zA-Z0-9._-]+$")
            .WithMessage("Username may contain letters, digits, dots, dashes and underscores");
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(100);
        RuleFor(x => x.Password).NotEmpty().MinimumLength(8).MaximumLength(100);
        RuleFor(x => x.Role).Must(r => ValidRoles.Contains(r))
            .WithMessage("Role must be one of: " + string.Join(", ", ValidRoles));
        RuleFor(x => x)
            .Must(x => !(x.StaffId.HasValue && x.PatientId.HasValue))
            .WithMessage("A user links to a staff member or a patient, not both")
            .OverridePropertyName("StaffId");
        // Row-level security depends on these links: without them a Doctor or
        // LabTech would scope to nothing (or everything). Patients need their row.
        RuleFor(x => x.StaffId).NotNull()
            .When(x => x.Role == "Doctor" || x.Role == "LabTech")
            .WithMessage("Doctor and LabTech accounts must link to a staff record (StaffId)");
        RuleFor(x => x.PatientId).NotNull()
            .When(x => x.Role == "Patient")
            .WithMessage("Patient accounts must link to a patient record (PatientId)");
    }
}

public class UpdateUserValidator : AbstractValidator<UpdateUserRequest>
{
    private static readonly string[] ValidRoles =
        { "Admin", "Doctor", "Nurse", "LabTech", "Pharmacist", "Receptionist", "Patient" };

    public UpdateUserValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(100);
        RuleFor(x => x.Role).Must(r => ValidRoles.Contains(r))
            .WithMessage("Role must be one of: " + string.Join(", ", ValidRoles));
        RuleFor(x => x)
            .Must(x => !(x.StaffId.HasValue && x.PatientId.HasValue))
            .WithMessage("A user links to a staff member or a patient, not both")
            .OverridePropertyName("StaffId");
        RuleFor(x => x.StaffId).NotNull()
            .When(x => x.Role == "Doctor" || x.Role == "LabTech")
            .WithMessage("Doctor and LabTech accounts must link to a staff record (StaffId)");
        RuleFor(x => x.PatientId).NotNull()
            .When(x => x.Role == "Patient")
            .WithMessage("Patient accounts must link to a patient record (PatientId)");
        RuleFor(x => x.NewPassword).MinimumLength(8).MaximumLength(100)
            .When(x => !string.IsNullOrEmpty(x.NewPassword));
    }
}

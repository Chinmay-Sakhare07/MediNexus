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

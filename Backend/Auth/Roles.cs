namespace HospitalManagement.API.Auth;

/// <summary>
/// Role names exactly as stored in USER_ACCOUNT.Role (CHK_User_Role).
/// Composite constants keep [Authorize(Roles = ...)] readable and typo-proof.
/// The full access matrix lives in SCOPE.md §1.
/// </summary>
public static class Roles
{
    public const string Admin        = "Admin";
    public const string Doctor       = "Doctor";
    public const string Nurse        = "Nurse";
    public const string LabTech      = "LabTech";
    public const string Pharmacist   = "Pharmacist";
    public const string Receptionist = "Receptionist";
    public const string Patient      = "Patient";

    // Front desk: full administrative CRUD.
    public const string FrontDesk = Admin + "," + Receptionist;

    // Everyone with a staff badge (no patients).
    public const string AllStaff = Admin + "," + Receptionist + "," + Doctor + "," +
                                   Nurse + "," + LabTech + "," + Pharmacist;

    // Appointment world: everyone except Pharmacist (matrix).
    public const string AppointmentsRead = Admin + "," + Receptionist + "," + Doctor + "," +
                                           Nurse + "," + LabTech + "," + Patient;

    // Billing / Insurance world.
    public const string BillingRead   = Admin + "," + Receptionist + "," + Doctor + "," + Patient;
    public const string BillingCreate = Admin + "," + Receptionist + "," + Doctor; // complete-appointment
    public const string InsuranceRead = Admin + "," + Receptionist + "," + Doctor + "," + Patient;

    // Clinical workflow (Patient File).
    public const string VitalsWriters = Admin + "," + Nurse + "," + Doctor;
    public const string FileRead = AllStaff + "," + Patient;   // own-guards in controllers
    public const string Pharmacy = Admin + "," + Pharmacist;
    public const string MedicineRead = Admin + "," + Pharmacist + "," + Doctor;
    public const string Lab = Admin + "," + LabTech;

    // Doctors directory.
    public const string DoctorsRead = Admin + "," + Receptionist + "," + Doctor + "," +
                                      Nurse + "," + Patient;
}

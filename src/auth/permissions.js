// Single source of truth for role -> module access on the frontend.
// Mirrors the access matrix in SCOPE.md §1; the backend enforces the same
// matrix with [Authorize] — this file only decides what the UI shows.

export const ROLES = {
  ADMIN: 'Admin',
  DOCTOR: 'Doctor',
  NURSE: 'Nurse',
  LAB_TECH: 'LabTech',
  PHARMACIST: 'Pharmacist',
  RECEPTIONIST: 'Receptionist',
  PATIENT: 'Patient',
};

const ALL = Object.values(ROLES);

export const MODULE_ROLES = {
  dashboard:    ALL,
  doctors:      [ROLES.ADMIN, ROLES.RECEPTIONIST, ROLES.DOCTOR, ROLES.NURSE, ROLES.PATIENT],
  patients:     [ROLES.ADMIN, ROLES.RECEPTIONIST, ROLES.DOCTOR, ROLES.NURSE, ROLES.LAB_TECH, ROLES.PHARMACIST],
  insurance:    [ROLES.ADMIN, ROLES.RECEPTIONIST, ROLES.DOCTOR, ROLES.PATIENT],
  appointments: [ROLES.ADMIN, ROLES.RECEPTIONIST, ROLES.DOCTOR, ROLES.NURSE, ROLES.LAB_TECH, ROLES.PATIENT],
  billing:      [ROLES.ADMIN, ROLES.RECEPTIONIST, ROLES.DOCTOR, ROLES.PATIENT],
};

export const canAccess = (role, module) =>
  (MODULE_ROLES[module] || []).includes(role);

// Write-permission helpers for hiding buttons the API would reject anyway.
export const canManagePatients = (role) => [ROLES.ADMIN, ROLES.RECEPTIONIST].includes(role);
export const canScheduleAppointments = (role) => [ROLES.ADMIN, ROLES.RECEPTIONIST].includes(role);
export const canCompleteAppointments = (role) => [ROLES.ADMIN, ROLES.RECEPTIONIST, ROLES.DOCTOR].includes(role);
export const canProcessPayments = (role) => [ROLES.ADMIN, ROLES.RECEPTIONIST].includes(role);
export const canManageInsurance = (role) => [ROLES.ADMIN, ROLES.RECEPTIONIST].includes(role);

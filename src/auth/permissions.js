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
  lab: ['Admin', 'LabTech'],
  pharmacy: ['Admin', 'Pharmacist'],
  schedule: ['Admin', 'Doctor'],
  users: ['Admin'],
};

export const canAccess = (role, module) =>
  (MODULE_ROLES[module] || []).includes(role);

// Write-permission helpers for hiding buttons the API would reject anyway.
export const canManagePatients = (role) => [ROLES.ADMIN, ROLES.RECEPTIONIST].includes(role);
export const canScheduleAppointments = (role) => [ROLES.ADMIN, ROLES.RECEPTIONIST].includes(role);
export const canCompleteAppointments = (role) => [ROLES.ADMIN, ROLES.RECEPTIONIST, ROLES.DOCTOR].includes(role);
export const canProcessPayments = (role) => [ROLES.ADMIN, ROLES.RECEPTIONIST].includes(role);
export const canManageInsurance = (role) => [ROLES.ADMIN, ROLES.RECEPTIONIST].includes(role);

// Patient File workflow helpers (mirror of the backend matrix)
export const canApproveAppointments = (role) => ['Admin', 'Receptionist'].includes(role);
export const canCheckIn = (role) => ['Admin', 'Receptionist'].includes(role);
export const canRecordVitals = (role) => ['Admin', 'Nurse', 'Doctor'].includes(role);
export const canConsult = (role) => ['Admin', 'Doctor'].includes(role);
export const canDispense = (role) => ['Admin', 'Pharmacist'].includes(role);
export const canAdjustStock = (role) => ['Admin', 'Pharmacist'].includes(role);
export const canRequestAppointment = (role) => role === 'Patient';

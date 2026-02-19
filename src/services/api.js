import axios from 'axios';

const API_BASE_URL = 'https://medinexus-api-dfefgbbyc5auhncw.eastus2-01.azurewebsites.net';

const api = axios.create({
  baseURL: API_BASE_URL,
  headers: {
    'Content-Type': 'application/json',
  },
});

// Dashboard
export const getDashboard = () => api.get('/dashboard');

// Patients
export const getPatients = () => api.get('/patients');
export const getPatient = (id) => api.get(`/patients/${id}`);
export const registerPatient = (data) => api.post('/patients', data);
export const deletePatient = (id) => api.delete(`/patients/${id}`);

// Doctors
export const getDoctors = () => api.get('/doctors');
export const getAvailableDoctors = () => api.get('/doctors/available');

// Appointments
export const getAppointments = () => api.get('/appointments');
export const getTodayAppointments = () => api.get('/appointments/today');
export const getTomorrowAppointments = () => api.get('/appointments/tomorrow');
export const getAppointmentsByDate = (date) => api.get(`/appointments/date/${date}`);
export const scheduleAppointment = (data) => api.post('/appointments', data);
export const updateAppointmentStatus = (id, status) => api.put(`/appointments/${id}/status`, JSON.stringify(status));
export const deleteAppointment = (id) => api.delete(`/appointments/${id}`);

// Billing
export const getAllBills = () => api.get('/billing');
export const getBillsByPatient = (patientId) => api.get(`/billing/patient/${patientId}`);
export const getBill = (id) => api.get(`/billing/${id}`);
export const completeAppointmentWithBilling = (data) => api.post('/billing/complete-appointment', data);
export const processPayment = (data) => api.post('/billing/pay', data);

// Insurance
export const getInsuranceProviders = () => api.get('/insurance/providers');
export const getInsurancePolicies = () => api.get('/insurance/policies');
export const getPoliciesByProvider = (providerId) => api.get(`/insurance/policies/provider/${providerId}`);
export const getPatientInsurance = (patientId) => api.get(`/insurance/patient/${patientId}`);
export const assignInsurance = (data) => api.post('/insurance/assign', data);
export const removeInsurance = (patientId, policyId) => api.delete(`/insurance/patient/${patientId}/policy/${policyId}`);

export default api;
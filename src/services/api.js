import axios from 'axios';

// Build-time env var (Vite): set VITE_API_BASE_URL in Netlify → Site settings →
// Environment variables, e.g. https://medinexus-api.onrender.com/api
// Falls back to the local .NET dev server (launchSettings.json → port 5155).
const API_BASE_URL = import.meta.env.VITE_API_BASE_URL || 'http://localhost:5155/api';

const api = axios.create({
  baseURL: API_BASE_URL,
  headers: {
    'Content-Type': 'application/json',
  },
});

// Attach the JWT to every request.
api.interceptors.request.use((config) => {
  const token = localStorage.getItem('mn_token');
  if (token) config.headers.Authorization = `Bearer ${token}`;
  return config;
});

// Cold-start resilience (free hosting): retry idempotent GETs when the
// server is asleep (network error / 502 / 503 / 504), and tell the UI so it
// can show a "waking up" banner (Layout listens for this event).
const sleep = (ms) => new Promise((r) => setTimeout(r, ms));

api.interceptors.response.use(
  (response) => response,
  async (error) => {
    const config = error?.config;
    const status = error?.response?.status;
    const isGet = (config?.method || '').toLowerCase() === 'get';
    const coldStart = !error?.response || [502, 503, 504].includes(status);

    if (isGet && coldStart && config && (config.__retryCount || 0) < 2) {
      config.__retryCount = (config.__retryCount || 0) + 1;
      window.dispatchEvent(new CustomEvent('mn:server-waking'));
      await sleep(4000 * config.__retryCount);
      return api(config);
    }
    return Promise.reject(error);
  }
);

// Expired/invalid token -> clear session and return to login.
// (Skip for the login call itself so bad credentials show inline.)
api.interceptors.response.use(
  (response) => response,
  (error) => {
    const status = error?.response?.status;
    const url = error?.config?.url || '';
    if (status === 401 && !url.includes('/auth/login')) {
      localStorage.removeItem('mn_token');
      localStorage.removeItem('mn_user');
      if (window.location.pathname !== '/login') {
        window.location.href = '/login';
      }
    }
    return Promise.reject(error);
  }
);

// Auth
export const login = (credentials) => api.post('/auth/login', credentials);
export const getMe = () => api.get('/auth/me');
export const changePassword = (data) => api.post('/auth/change-password', data);

// Dashboard
export const getDashboard = () => api.get('/dashboard');

// Patients
export const getPatients = () => api.get('/patients');
export const getPatient = (id) => api.get(`/patients/${id}`);
export const registerPatient = (data) => api.post('/patients', data);
export const updatePatient = (id, data) => api.put(`/patients/${id}`, data);
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

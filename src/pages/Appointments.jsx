import { useState, useEffect } from 'react';
import { Plus, Calendar as CalendarIcon, Check, X, DollarSign, Filter } from 'lucide-react';
import { 
  getAppointments,
  getTodayAppointments,
  getTomorrowAppointments,
  getAppointmentsByDate,
  scheduleAppointment, 
  updateAppointmentStatus,
  deleteAppointment,
  getPatients,
  getDoctors,
  getPatientInsurance,
  completeAppointmentWithBilling
} from '../services/api';

export default function Appointments() {
  const [appointments, setAppointments] = useState([]);
  const [patients, setPatients] = useState([]);
  const [doctors, setDoctors] = useState([]);
  const [loading, setLoading] = useState(true);
  const [showModal, setShowModal] = useState(false);
  const [showCompleteModal, setShowCompleteModal] = useState(false);
  const [selectedAppointment, setSelectedAppointment] = useState(null);
  const [patientInsurance, setPatientInsurance] = useState(null);
  const [dateFilter, setDateFilter] = useState('all');
  const [customDate, setCustomDate] = useState('');
  const [formData, setFormData] = useState({
    patientId: '',
    doctorId: '',
    dateTime: '',
    reason: '',
    appointmentType: 'Consultation',
    duration: 30
  });
  const [billingData, setBillingData] = useState({
    consultationFee: '',
    additionalFees: 0,
    additionalFeesDescription: '',
    discountPercentage: 0
  });

  useEffect(() => {
    loadAppointments();
    loadPatients();
    loadDoctors();
  }, []);

  const loadAppointments = async (filter = 'all', date = null) => {
    try {
      setLoading(true);
      let response;
      
      switch(filter) {
        case 'today':
          response = await getTodayAppointments();
          break;
        case 'tomorrow':
          response = await getTomorrowAppointments();
          break;
        case 'custom':
          if (date) {
            response = await getAppointmentsByDate(date);
          } else {
            response = await getAppointments();
          }
          break;
        default:
          response = await getAppointments();
      }
      
      setAppointments(response.data.data);
    } catch (error) {
      console.error('Error loading appointments:', error);
    } finally {
      setLoading(false);
    }
  };

  const handleFilterChange = (filter) => {
    setDateFilter(filter);
    if (filter === 'custom') {
      // Wait for user to select date
      return;
    }
    loadAppointments(filter);
  };

  const handleCustomDateChange = (date) => {
    setCustomDate(date);
    if (date) {
      loadAppointments('custom', date);
    }
  };

  const loadPatients = async () => {
    try {
      const response = await getPatients();
      setPatients(response.data.data);
    } catch (error) {
      console.error('Error loading patients:', error);
    }
  };

  const loadDoctors = async () => {
    try {
      const response = await getDoctors();
      setDoctors(response.data.data);
    } catch (error) {
      console.error('Error loading doctors:', error);
    }
  };

  const handleSubmit = async (e) => {
    e.preventDefault();
    try {
      await scheduleAppointment(formData);
      setShowModal(false);
      loadAppointments(dateFilter, customDate || null);
      setFormData({
        patientId: '',
        doctorId: '',
        dateTime: '',
        reason: '',
        appointmentType: 'Consultation',
        duration: 30
      });
      alert('Appointment scheduled successfully!');
    } catch (error) {
      alert(error.response?.data?.message || 'Error scheduling appointment');
    }
  };

  const handleStatusUpdate = async (id, status) => {
    try {
      await updateAppointmentStatus(id, status);
      loadAppointments(dateFilter, customDate || null);
      alert(`Appointment ${status.toLowerCase()}`);
    } catch (error) {
      alert('Error updating appointment');
    }
  };

  const handleDelete = async (id) => {
    if (window.confirm('Are you sure you want to delete this appointment?')) {
      try {
        await deleteAppointment(id);
        loadAppointments(dateFilter, customDate || null);
        alert('Appointment deleted successfully');
      } catch (error) {
        alert('Error deleting appointment');
      }
    }
  };

  const openCompleteModal = async (appointment) => {
    setSelectedAppointment(appointment);
    
    // Load patient insurance
    try {
      const response = await getPatientInsurance(appointment.patientId);
      const primaryInsurance = response.data.data.find(ins => ins.isPrimary);
      setPatientInsurance(primaryInsurance || null);
    } catch (error) {
      setPatientInsurance(null);
    }

    // Pre-fill consultation fee from doctor
    const doctor = doctors.find(d => d.doctorId === appointment.doctorId);
    setBillingData({
      consultationFee: doctor?.consultationFee || '',
      additionalFees: 0,
      additionalFeesDescription: '',
      discountPercentage: 0
    });

    setShowCompleteModal(true);
  };

  const calculateBill = () => {
    const consultation = parseFloat(billingData.consultationFee) || 0;
    const additional = parseFloat(billingData.additionalFees) || 0;
    const discount = parseFloat(billingData.discountPercentage) || 0;

    const subtotal = consultation + additional;
    const discountAmount = (subtotal * discount) / 100;
    const afterDiscount = subtotal - discountAmount;
    const tax = afterDiscount * 0.07; // 7% tax
    const total = afterDiscount + tax;

    let insuranceCoverage = 0;
    let patientOwes = total;

    if (patientInsurance) {
      const insurancePercentage = (100 - patientInsurance.copayPercentage) / 100;
      insuranceCoverage = total * insurancePercentage;
      patientOwes = total - insuranceCoverage;
    }

    return {
      subtotal,
      discountAmount,
      afterDiscount,
      tax,
      total,
      insuranceCoverage,
      patientOwes
    };
  };

  const handleCompleteAppointment = async (e) => {
    e.preventDefault();
    try {
      await completeAppointmentWithBilling({
        appointmentId: selectedAppointment.appointmentId,
        consultationFee: parseFloat(billingData.consultationFee),
        additionalFees: parseFloat(billingData.additionalFees),
        additionalFeesDescription: billingData.additionalFeesDescription,
        discountPercentage: parseFloat(billingData.discountPercentage)
      });
      setShowCompleteModal(false);
      loadAppointments(dateFilter, customDate || null);
      alert('Appointment completed and bill generated successfully!');
    } catch (error) {
      alert(error.response?.data?.message || 'Error completing appointment');
    }
  };

  const getStatusColor = (status) => {
    switch (status) {
      case 'Scheduled': return 'bg-blue-100 text-blue-700';
      case 'Confirmed': return 'bg-green-100 text-green-700';
      case 'Completed': return 'bg-gray-100 text-gray-700';
      case 'Cancelled': return 'bg-red-100 text-red-700';
      default: return 'bg-gray-100 text-gray-700';
    }
  };

  if (loading) {
    return <div className="flex justify-center items-center h-64">Loading...</div>;
  }

  const billSummary = showCompleteModal ? calculateBill() : null;

  return (
    <div>
      <div className="flex justify-between items-center mb-8">
        <h1 className="text-3xl font-bold text-gray-800">Appointments</h1>
        <button
          onClick={() => setShowModal(true)}
          className="flex items-center gap-2 bg-blue-600 text-white px-6 py-3 rounded-lg hover:bg-blue-700 transition"
        >
          <Plus className="w-5 h-5" />
          Schedule Appointment
        </button>
      </div>

      {/* Date Filter */}
      <div className="bg-white rounded-lg shadow-md p-6 mb-6">
        <div className="flex items-center gap-2 mb-4">
          <Filter className="w-5 h-5 text-gray-600" />
          <h3 className="font-medium text-gray-800">Filter by Date</h3>
        </div>
        <div className="flex flex-wrap gap-3">
          <button
            onClick={() => handleFilterChange('all')}
            className={`px-4 py-2 rounded-lg transition ${
              dateFilter === 'all'
                ? 'bg-blue-600 text-white'
                : 'bg-gray-100 text-gray-700 hover:bg-gray-200'
            }`}
          >
            All Appointments
          </button>
          <button
            onClick={() => handleFilterChange('today')}
            className={`px-4 py-2 rounded-lg transition ${
              dateFilter === 'today'
                ? 'bg-blue-600 text-white'
                : 'bg-gray-100 text-gray-700 hover:bg-gray-200'
            }`}
          >
            Today
          </button>
          <button
            onClick={() => handleFilterChange('tomorrow')}
            className={`px-4 py-2 rounded-lg transition ${
              dateFilter === 'tomorrow'
                ? 'bg-blue-600 text-white'
                : 'bg-gray-100 text-gray-700 hover:bg-gray-200'
            }`}
          >
            Tomorrow
          </button>
          <div className="flex items-center gap-2">
            <button
              onClick={() => handleFilterChange('custom')}
              className={`px-4 py-2 rounded-lg transition ${
                dateFilter === 'custom'
                  ? 'bg-blue-600 text-white'
                  : 'bg-gray-100 text-gray-700 hover:bg-gray-200'
              }`}
            >
              Custom Date
            </button>
            {dateFilter === 'custom' && (
              <input
                type="date"
                value={customDate}
                onChange={(e) => handleCustomDateChange(e.target.value)}
                className="px-3 py-2 border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500"
              />
            )}
          </div>
        </div>
      </div>

      {/* Appointments Table */}
      <div className="bg-white rounded-lg shadow-md overflow-hidden">
        <table className="w-full">
          <thead className="bg-gray-50 border-b">
            <tr>
              <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase">Patient</th>
              <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase">Doctor</th>
              <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase">Date & Time</th>
              <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase">Reason</th>
              <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase">Status</th>
              <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase">Actions</th>
            </tr>
          </thead>
          <tbody className="divide-y divide-gray-200">
            {appointments.map((appointment) => (
              <tr key={appointment.appointmentId} className="hover:bg-gray-50">
                <td className="px-6 py-4 text-sm font-medium text-gray-800">{appointment.patientName}</td>
                <td className="px-6 py-4 text-sm text-gray-600">{appointment.doctorName}</td>
                <td className="px-6 py-4 text-sm text-gray-600">
                  {new Date(appointment.dateTime).toLocaleString()}
                </td>
                <td className="px-6 py-4 text-sm text-gray-600">{appointment.reason}</td>
                <td className="px-6 py-4">
                  <span className={`px-3 py-1 rounded-full text-xs font-medium ${getStatusColor(appointment.status)}`}>
                    {appointment.status}
                  </span>
                </td>
                <td className="px-6 py-4">
                  <div className="flex gap-2">
                    {appointment.status === 'Scheduled' && (
                      <button
                        onClick={() => handleStatusUpdate(appointment.appointmentId, 'Confirmed')}
                        className="text-green-600 hover:text-green-800"
                        title="Confirm"
                      >
                        <Check className="w-5 h-5" />
                      </button>
                    )}
                    {appointment.status === 'Confirmed' && (
                      <button
                        onClick={() => openCompleteModal(appointment)}
                        className="text-blue-600 hover:text-blue-800"
                        title="Complete & Bill"
                      >
                        <DollarSign className="w-5 h-5" />
                      </button>
                    )}
                    {appointment.status !== 'Completed' && (
                      <>
                        <button
                          onClick={() => handleStatusUpdate(appointment.appointmentId, 'Cancelled')}
                          className="text-red-600 hover:text-red-800"
                          title="Cancel"
                        >
                          <X className="w-5 h-5" />
                        </button>
                      </>
                    )}
                  </div>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>

      {/* Schedule Appointment Modal */}
      {showModal && (
        <div className="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center z-50">
          <div className="bg-white rounded-lg p-8 max-w-md w-full">
            <h2 className="text-2xl font-bold mb-6">Schedule Appointment</h2>
            <form onSubmit={handleSubmit} className="space-y-4">
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-1">Patient *</label>
                <select
                  required
                  value={formData.patientId}
                  onChange={(e) => setFormData({...formData, patientId: e.target.value})}
                  className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500"
                >
                  <option value="">Select patient</option>
                  {patients.map(patient => (
                    <option key={patient.patientId} value={patient.patientId}>
                      {patient.firstName} {patient.lastName}
                    </option>
                  ))}
                </select>
              </div>

              <div>
                <label className="block text-sm font-medium text-gray-700 mb-1">Doctor *</label>
                <select
                  required
                  value={formData.doctorId}
                  onChange={(e) => setFormData({...formData, doctorId: e.target.value})}
                  className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500"
                >
                  <option value="">Select doctor</option>
                  {doctors.map(doctor => (
                    <option key={doctor.doctorId} value={doctor.doctorId}>
                      Dr. {doctor.firstName} {doctor.lastName} - {doctor.specialization}
                    </option>
                  ))}
                </select>
              </div>

              <div>
                <label className="block text-sm font-medium text-gray-700 mb-1">Date & Time *</label>
                <input
                  type="datetime-local"
                  required
                  value={formData.dateTime}
                  onChange={(e) => setFormData({...formData, dateTime: e.target.value})}
                  className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500"
                />
              </div>

              <div>
                <label className="block text-sm font-medium text-gray-700 mb-1">Reason</label>
                <input
                  type="text"
                  value={formData.reason}
                  onChange={(e) => setFormData({...formData, reason: e.target.value})}
                  className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500"
                />
              </div>

              <div>
                <label className="block text-sm font-medium text-gray-700 mb-1">Type *</label>
                <select
                  required
                  value={formData.appointmentType}
                  onChange={(e) => setFormData({...formData, appointmentType: e.target.value})}
                  className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500"
                >
                  <option value="Consultation">Consultation</option>
                  <option value="Follow-up">Follow-up</option>
                  <option value="Emergency">Emergency</option>
                </select>
              </div>

              <div>
                <label className="block text-sm font-medium text-gray-700 mb-1">Duration (minutes) *</label>
                <input
                  type="number"
                  required
                  min="15"
                  step="15"
                  value={formData.duration}
                  onChange={(e) => setFormData({...formData, duration: e.target.value})}
                  className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500"
                />
              </div>

              <div className="flex justify-end gap-3 pt-4">
                <button
                  type="button"
                  onClick={() => setShowModal(false)}
                  className="px-6 py-2 border border-gray-300 rounded-lg hover:bg-gray-50"
                >
                  Cancel
                </button>
                <button
                  type="submit"
                  className="px-6 py-2 bg-blue-600 text-white rounded-lg hover:bg-blue-700"
                >
                  Schedule
                </button>
              </div>
            </form>
          </div>
        </div>
      )}

      {/* Complete Appointment Modal */}
      {showCompleteModal && selectedAppointment && (
        <div className="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center z-50 overflow-y-auto">
          <div className="bg-white rounded-lg p-8 max-w-2xl w-full my-8">
            <h2 className="text-2xl font-bold mb-6">Complete Appointment & Generate Bill</h2>
            
            <form onSubmit={handleCompleteAppointment} className="space-y-6">
              {/* Appointment Details */}
              <div className="bg-gray-50 p-4 rounded-lg">
                <h3 className="font-medium text-gray-800 mb-2">Appointment Details</h3>
                <div className="text-sm space-y-1">
                  <p><span className="font-medium">Patient:</span> {selectedAppointment.patientName}</p>
                  <p><span className="font-medium">Doctor:</span> {selectedAppointment.doctorName}</p>
                  <p><span className="font-medium">Date:</span> {new Date(selectedAppointment.dateTime).toLocaleString()}</p>
                  <p><span className="font-medium">Reason:</span> {selectedAppointment.reason}</p>
                </div>
              </div>

              {/* Insurance Info */}
              <div className={`p-4 rounded-lg ${patientInsurance ? 'bg-blue-50' : 'bg-yellow-50'}`}>
                <h3 className="font-medium text-gray-800 mb-2">Insurance Coverage</h3>
                {patientInsurance ? (
                  <div className="text-sm">
                    <p className="text-gray-800">
                      <span className="font-medium">{patientInsurance.providerName}</span> - {patientInsurance.planType}
                    </p>
                    <p className="text-gray-600">
                      Copay: {patientInsurance.copayPercentage}% (Patient pays {patientInsurance.copayPercentage}%, Insurance covers {100 - patientInsurance.copayPercentage}%)
                    </p>
                  </div>
                ) : (
                  <p className="text-yellow-700 text-sm">No insurance on file. Patient will be responsible for full amount.</p>
                )}
              </div>

              {/* Billing Inputs */}
              <div className="space-y-4">
                <h3 className="font-medium text-gray-800">Billing Details</h3>
                
                <div className="grid grid-cols-2 gap-4">
                  <div>
                    <label className="block text-sm text-gray-600 mb-1">Consultation Fee *</label>
                    <div className="relative">
                      <DollarSign className="absolute left-3 top-1/2 transform -translate-y-1/2 text-gray-400 w-4 h-4" />
                      <input
                        type="number"
                        required
                        step="0.01"
                        min="0"
                        value={billingData.consultationFee}
                        onChange={(e) => setBillingData({...billingData, consultationFee: e.target.value})}
                        className="w-full pl-9 pr-3 py-2 border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500"
                      />
                    </div>
                  </div>
                  <div>
                    <label className="block text-sm text-gray-600 mb-1">Additional Fees</label>
                    <div className="relative">
                      <DollarSign className="absolute left-3 top-1/2 transform -translate-y-1/2 text-gray-400 w-4 h-4" />
                      <input
                        type="number"
                        step="0.01"
                        min="0"
                        value={billingData.additionalFees}
                        onChange={(e) => setBillingData({...billingData, additionalFees: e.target.value})}
                        className="w-full pl-9 pr-3 py-2 border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500"
                      />
                    </div>
                  </div>
                </div>

                <div>
                  <label className="block text-sm text-gray-600 mb-1">Additional Fees Description</label>
                  <input
                    type="text"
                    value={billingData.additionalFeesDescription}
                    onChange={(e) => setBillingData({...billingData, additionalFeesDescription: e.target.value})}
                    placeholder="e.g., Lab tests, X-ray"
                    className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500"
                  />
                </div>

                <div>
                  <label className="block text-sm text-gray-600 mb-1">Discount %</label>
                  <input
                    type="number"
                    step="0.01"
                    min="0"
                    max="100"
                    value={billingData.discountPercentage}
                    onChange={(e) => setBillingData({...billingData, discountPercentage: e.target.value})}
                    className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500"
                  />
                </div>
              </div>

              {/* Bill Summary */}
              {billSummary && (
                <div className="bg-gradient-to-br from-gray-800 to-gray-900 text-white p-6 rounded-lg">
                  <h3 className="font-bold text-lg mb-4">Bill Summary</h3>
                  <div className="space-y-2 text-sm">
                    <div className="flex justify-between">
                      <span>Subtotal:</span>
                      <span>${billSummary.subtotal.toFixed(2)}</span>
                    </div>
                    {billSummary.discountAmount > 0 && (
                      <div className="flex justify-between text-green-400">
                        <span>Discount ({billingData.discountPercentage}%):</span>
                        <span>-${billSummary.discountAmount.toFixed(2)}</span>
                      </div>
                    )}
                    <div className="flex justify-between">
                      <span>Tax (7%):</span>
                      <span>${billSummary.tax.toFixed(2)}</span>
                    </div>
                    <div className="flex justify-between font-bold text-base pt-2 border-t border-gray-600">
                      <span>Total:</span>
                      <span>${billSummary.total.toFixed(2)}</span>
                    </div>
                    {patientInsurance && (
                      <>
                        <div className="flex justify-between text-blue-300 pt-2">
                          <span>Insurance Coverage:</span>
                          <span>-${billSummary.insuranceCoverage.toFixed(2)}</span>
                        </div>
                        <div className="flex justify-between font-bold text-lg pt-2 border-t border-gray-600">
                          <span>Patient Owes:</span>
                          <span className="text-yellow-300">${billSummary.patientOwes.toFixed(2)}</span>
                        </div>
                      </>
                    )}
                    {!patientInsurance && (
                      <div className="flex justify-between font-bold text-lg pt-2 border-t border-gray-600">
                        <span>Patient Owes:</span>
                        <span className="text-yellow-300">${billSummary.total.toFixed(2)}</span>
                      </div>
                    )}
                  </div>
                </div>
              )}

              <div className="flex justify-end gap-3 pt-4 border-t">
                <button
                  type="button"
                  onClick={() => setShowCompleteModal(false)}
                  className="px-6 py-2 border border-gray-300 rounded-lg hover:bg-gray-50"
                >
                  Cancel
                </button>
                <button
                  type="submit"
                  className="px-6 py-2 bg-green-600 text-white rounded-lg hover:bg-green-700 flex items-center gap-2"
                >
                  <Check className="w-5 h-5" />
                  Complete & Generate Bill
                </button>
              </div>
            </form>
          </div>
        </div>
      )}
    </div>
  );
}
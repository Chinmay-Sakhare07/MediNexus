import { useState, useEffect } from 'react';
import { Shield, Plus, Trash2 } from 'lucide-react';
import { 
  getInsuranceProviders, 
  getInsurancePolicies, 
  getPatients,
  getPatientInsurance,
  assignInsurance,
  removeInsurance 
} from '../services/api';

export default function Insurance() {
  const [providers, setProviders] = useState([]);
  const [policies, setPolicies] = useState([]);
  const [patients, setPatients] = useState([]);
  const [selectedPatient, setSelectedPatient] = useState(null);
  const [patientInsurance, setPatientInsurance] = useState([]);
  const [showAssignModal, setShowAssignModal] = useState(false);
  const [loading, setLoading] = useState(true);
  const [formData, setFormData] = useState({
    patientId: '',
    policyId: '',
    isPrimary: false
  });

  useEffect(() => {
    loadData();
  }, []);

  const loadData = async () => {
    try {
      const [providersRes, policiesRes, patientsRes] = await Promise.all([
        getInsuranceProviders(),
        getInsurancePolicies(),
        getPatients()
      ]);
      setProviders(providersRes.data.data);
      setPolicies(policiesRes.data.data);
      setPatients(patientsRes.data.data);
    } catch (error) {
      console.error('Error loading data:', error);
    } finally {
      setLoading(false);
    }
  };

  const loadPatientInsurance = async (patientId) => {
    try {
      const response = await getPatientInsurance(patientId);
      setPatientInsurance(response.data.data);
    } catch (error) {
      console.error('Error loading patient insurance:', error);
    }
  };

  const handlePatientSelect = (patientId) => {
    setSelectedPatient(patientId);
    if (patientId) {
      loadPatientInsurance(patientId);
    } else {
      setPatientInsurance([]);
    }
  };

  const handleAssignInsurance = async (e) => {
    e.preventDefault();
    try {
      await assignInsurance(formData);
      setShowAssignModal(false);
      loadPatientInsurance(formData.patientId);
      setFormData({ patientId: '', policyId: '', isPrimary: false });
      alert('Insurance assigned successfully!');
    } catch (error) {
      alert(error.response?.data?.message || 'Error assigning insurance');
    }
  };

  const handleRemoveInsurance = async (patientId, policyId) => {
    if (window.confirm('Are you sure you want to remove this insurance?')) {
      try {
        await removeInsurance(patientId, policyId);
        loadPatientInsurance(patientId);
        alert('Insurance removed successfully');
      } catch (error) {
        alert('Error removing insurance');
      }
    }
  };

  if (loading) {
    return <div className="flex justify-center items-center h-64">Loading...</div>;
  }

  return (
    <div>
      <div className="flex justify-between items-center mb-8">
        <h1 className="text-3xl font-bold text-gray-800">Insurance Management</h1>
        <button
          onClick={() => setShowAssignModal(true)}
          className="flex items-center gap-2 bg-blue-600 text-white px-6 py-3 rounded-lg hover:bg-blue-700 transition"
        >
          <Plus className="w-5 h-5" />
          Assign Insurance
        </button>
      </div>

      <div className="grid grid-cols-1 lg:grid-cols-2 gap-6 mb-6">
        {/* Insurance Providers */}
        <div className="bg-white rounded-lg shadow-md p-6">
          <h2 className="text-xl font-bold text-gray-800 mb-4">Insurance Providers</h2>
          <div className="space-y-3">
            {providers.map(provider => (
              <div key={provider.providerId} className="flex items-center gap-3 p-3 bg-gray-50 rounded-lg">
                <Shield className="w-5 h-5 text-blue-600" />
                <div>
                  <div className="font-medium text-gray-800">{provider.providerName}</div>
                  <div className="text-sm text-gray-500">{provider.contactNumber}</div>
                </div>
              </div>
            ))}
          </div>
        </div>

        {/* Patient Insurance Lookup */}
        <div className="bg-white rounded-lg shadow-md p-6">
          <h2 className="text-xl font-bold text-gray-800 mb-4">Patient Insurance Lookup</h2>
          <select
            value={selectedPatient || ''}
            onChange={(e) => handlePatientSelect(e.target.value)}
            className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500 mb-4"
          >
            <option value="">Select a patient...</option>
            {patients.map(patient => (
              <option key={patient.patientId} value={patient.patientId}>
                {patient.firstName} {patient.lastName}
              </option>
            ))}
          </select>

          {selectedPatient && (
            <div className="space-y-3">
              {patientInsurance.length === 0 ? (
                <p className="text-gray-500 text-center py-4">No insurance assigned</p>
              ) : (
                patientInsurance.map(insurance => (
                  <div key={insurance.patientInsuranceId} className="p-3 bg-blue-50 rounded-lg">
                    <div className="flex justify-between items-start">
                      <div>
                        <div className="font-medium text-gray-800">{insurance.providerName}</div>
                        <div className="text-sm text-gray-600">{insurance.planType}</div>
                        <div className="text-sm text-gray-500">Policy: {insurance.policyNumber}</div>
                        <div className="text-sm text-gray-500">Copay: {insurance.copayPercentage}%</div>
                        {insurance.isPrimary && (
                          <span className="inline-block mt-2 px-2 py-1 bg-green-100 text-green-700 text-xs rounded">
                            Primary
                          </span>
                        )}
                      </div>
                      <button
                        onClick={() => handleRemoveInsurance(selectedPatient, insurance.policyId)}
                        className="text-red-600 hover:text-red-800"
                      >
                        <Trash2 className="w-4 h-4" />
                      </button>
                    </div>
                  </div>
                ))
              )}
            </div>
          )}
        </div>
      </div>

      {/* Available Policies */}
      <div className="bg-white rounded-lg shadow-md p-6">
        <h2 className="text-xl font-bold text-gray-800 mb-4">Available Insurance Policies</h2>
        <div className="overflow-x-auto">
          <table className="w-full">
            <thead className="bg-gray-50 border-b">
              <tr>
                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase">Provider</th>
                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase">Plan Type</th>
                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase">Coverage</th>
                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase">Copay %</th>
                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase">Max Coverage</th>
                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase">Valid Until</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-gray-200">
              {policies.map(policy => (
                <tr key={policy.policyId} className="hover:bg-gray-50">
                  <td className="px-6 py-4 text-sm font-medium text-gray-800">{policy.providerName}</td>
                  <td className="px-6 py-4 text-sm text-gray-600">{policy.planType}</td>
                  <td className="px-6 py-4 text-sm text-gray-600">{policy.coverageDetails}</td>
                  <td className="px-6 py-4 text-sm text-gray-600">{policy.copayPercentage}%</td>
                  <td className="px-6 py-4 text-sm text-gray-600">${policy.maxCoverageLimit.toLocaleString()}</td>
                  <td className="px-6 py-4 text-sm text-gray-600">
                    {policy.validTo ? new Date(policy.validTo).toLocaleDateString() : 'Lifetime'}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </div>

      {/* Assign Insurance Modal */}
      {showAssignModal && (
        <div className="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center z-50">
          <div className="bg-white rounded-lg p-8 max-w-md w-full">
            <h2 className="text-2xl font-bold mb-6">Assign Insurance to Patient</h2>
            <form onSubmit={handleAssignInsurance} className="space-y-4">
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
                <label className="block text-sm font-medium text-gray-700 mb-1">Insurance Policy *</label>
                <select
                  required
                  value={formData.policyId}
                  onChange={(e) => setFormData({...formData, policyId: e.target.value})}
                  className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500"
                >
                  <option value="">Select policy</option>
                  {policies.map(policy => (
                    <option key={policy.policyId} value={policy.policyId}>
                      {policy.providerName} - {policy.planType}
                    </option>
                  ))}
                </select>
              </div>

              <div className="flex items-center gap-2">
                <input
                  type="checkbox"
                  checked={formData.isPrimary}
                  onChange={(e) => setFormData({...formData, isPrimary: e.target.checked})}
                  className="w-4 h-4 text-blue-600 border-gray-300 rounded focus:ring-blue-500"
                />
                <label className="text-sm text-gray-700">Set as primary insurance</label>
              </div>

              <div className="flex justify-end gap-3 pt-4">
                <button
                  type="button"
                  onClick={() => setShowAssignModal(false)}
                  className="px-6 py-2 border border-gray-300 rounded-lg hover:bg-gray-50"
                >
                  Cancel
                </button>
                <button
                  type="submit"
                  className="px-6 py-2 bg-blue-600 text-white rounded-lg hover:bg-blue-700"
                >
                  Assign Insurance
                </button>
              </div>
            </form>
          </div>
        </div>
      )}
    </div>
  );
}
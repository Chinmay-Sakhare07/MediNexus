import { useState, useEffect } from 'react';
import { DollarSign, CreditCard, AlertCircle, CheckCircle } from 'lucide-react';
import { getAllBills, processPayment } from '../services/api';

export default function Billing() {
  const [bills, setBills] = useState([]);
  const [loading, setLoading] = useState(true);
  const [showPaymentModal, setShowPaymentModal] = useState(false);
  const [selectedBill, setSelectedBill] = useState(null);
  const [paymentData, setPaymentData] = useState({
    amountPaid: '',
    paymentMethod: 'Cash'
  });

  useEffect(() => {
    loadBills();
  }, []);

  const loadBills = async () => {
    try {
      const response = await getAllBills();
      setBills(response.data.data);
    } catch (error) {
      console.error('Error loading bills:', error);
    } finally {
      setLoading(false);
    }
  };

  const openPaymentModal = (bill) => {
    setSelectedBill(bill);
    setPaymentData({
      amountPaid: bill.patientResponsibility.toString(),
      paymentMethod: 'Cash'
    });
    setShowPaymentModal(true);
  };

  const handlePayment = async (e) => {
    e.preventDefault();
    try {
      await processPayment({
        billId: selectedBill.billId,
        amountPaid: parseFloat(paymentData.amountPaid),
        paymentMethod: paymentData.paymentMethod
      });
      setShowPaymentModal(false);
      loadBills();
      
      // Show success message
      alert('Thanks for letting us take care of you! 💙');
    } catch (error) {
      alert(error.response?.data?.message || 'Error processing payment');
    }
  };

  const getStatusColor = (status) => {
    switch (status) {
      case 'Paid': return 'bg-green-100 text-green-700';
      case 'Pending': return 'bg-yellow-100 text-yellow-700';
      case 'Partially Paid': return 'bg-orange-100 text-orange-700';
      case 'Overdue': return 'bg-red-100 text-red-700';
      default: return 'bg-gray-100 text-gray-700';
    }
  };

  if (loading) {
    return <div className="flex justify-center items-center h-64">Loading...</div>;
  }

  const pendingBills = bills.filter(b => b.status === 'Pending' || b.status === 'Partially Paid');
  const paidBills = bills.filter(b => b.status === 'Paid');
  const totalOutstanding = pendingBills.reduce((sum, bill) => sum + bill.patientResponsibility, 0);

  return (
    <div>
      <h1 className="text-3xl font-bold text-gray-800 mb-8">Billing</h1>

      {/* Summary Cards */}
      <div className="grid grid-cols-1 md:grid-cols-3 gap-6 mb-8">
        <div className="bg-white rounded-lg shadow-md p-6">
          <div className="flex items-center justify-between">
            <div>
              <p className="text-gray-500 text-sm">Pending Bills</p>
              <p className="text-3xl font-bold text-yellow-600 mt-2">{pendingBills.length}</p>
            </div>
            <AlertCircle className="w-12 h-12 text-yellow-600 opacity-20" />
          </div>
        </div>

        <div className="bg-white rounded-lg shadow-md p-6">
          <div className="flex items-center justify-between">
            <div>
              <p className="text-gray-500 text-sm">Paid Bills</p>
              <p className="text-3xl font-bold text-green-600 mt-2">{paidBills.length}</p>
            </div>
            <CheckCircle className="w-12 h-12 text-green-600 opacity-20" />
          </div>
        </div>

        <div className="bg-white rounded-lg shadow-md p-6">
          <div className="flex items-center justify-between">
            <div>
              <p className="text-gray-500 text-sm">Total Outstanding</p>
              <p className="text-3xl font-bold text-red-600 mt-2">
                ${totalOutstanding.toFixed(2)}
              </p>
            </div>
            <DollarSign className="w-12 h-12 text-red-600 opacity-20" />
          </div>
        </div>
      </div>

      {/* Pending Bills Section */}
      {pendingBills.length > 0 && (
        <div className="mb-8">
          <h2 className="text-xl font-bold text-gray-800 mb-4">Bills Requiring Payment</h2>
          <div className="bg-white rounded-lg shadow-md overflow-hidden">
            <table className="w-full">
              <thead className="bg-gray-50 border-b">
                <tr>
                  <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase">Bill ID</th>
                  <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase">Patient</th>
                  <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase">Date</th>
                  <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase">Total Amount</th>
                  <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase">Insurance</th>
                  <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase">Patient Owes</th>
                  <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase">Status</th>
                  <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase">Actions</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-gray-200">
                {pendingBills.map((bill) => (
                  <tr key={bill.billId} className="hover:bg-gray-50">
                    <td className="px-6 py-4 text-sm font-medium text-gray-800">#{bill.billId}</td>
                    <td className="px-6 py-4 text-sm text-gray-600">{bill.patientName}</td>
                    <td className="px-6 py-4 text-sm text-gray-600">
                      {new Date(bill.dateIssued).toLocaleDateString()}
                    </td>
                    <td className="px-6 py-4 text-sm font-medium text-gray-800">
                      ${bill.amount.toFixed(2)}
                    </td>
                    <td className="px-6 py-4 text-sm text-gray-600">
                      {bill.insuranceProvider ? (
                        <div>
                          <div className="font-medium">{bill.insuranceProvider}</div>
                          <div className="text-xs text-green-600">-${bill.insuranceCovered.toFixed(2)}</div>
                        </div>
                      ) : (
                        <span className="text-gray-400">No insurance</span>
                      )}
                    </td>
                    <td className="px-6 py-4 text-sm font-bold text-red-600">
                      ${bill.patientResponsibility.toFixed(2)}
                    </td>
                    <td className="px-6 py-4">
                      <span className={`px-3 py-1 rounded-full text-xs font-medium ${getStatusColor(bill.status)}`}>
                        {bill.status}
                      </span>
                    </td>
                    <td className="px-6 py-4">
                      <button
                        onClick={() => openPaymentModal(bill)}
                        className="flex items-center gap-2 px-4 py-2 bg-green-600 text-white rounded-lg hover:bg-green-700 text-sm"
                      >
                        <CreditCard className="w-4 h-4" />
                        Pay
                      </button>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </div>
      )}

      {/* All Bills Section */}
      <div>
        <h2 className="text-xl font-bold text-gray-800 mb-4">All Bills</h2>
        <div className="bg-white rounded-lg shadow-md overflow-hidden">
          <table className="w-full">
            <thead className="bg-gray-50 border-b">
              <tr>
                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase">Bill ID</th>
                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase">Patient</th>
                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase">Appointment</th>
                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase">Date</th>
                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase">Total</th>
                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase">Insurance</th>
                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase">Patient Responsibility</th>
                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase">Status</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-gray-200">
              {bills.map((bill) => (
                <tr key={bill.billId} className="hover:bg-gray-50">
                  <td className="px-6 py-4 text-sm font-medium text-gray-800">#{bill.billId}</td>
                  <td className="px-6 py-4 text-sm text-gray-600">{bill.patientName}</td>
                  <td className="px-6 py-4 text-sm text-gray-600">
                    {bill.appointmentDate ? (
                      <div>
                        <div>{new Date(bill.appointmentDate).toLocaleDateString()}</div>
                        <div className="text-xs text-gray-400">{bill.appointmentReason}</div>
                      </div>
                    ) : (
                      'N/A'
                    )}
                  </td>
                  <td className="px-6 py-4 text-sm text-gray-600">
                    {new Date(bill.dateIssued).toLocaleDateString()}
                  </td>
                  <td className="px-6 py-4 text-sm font-medium text-gray-800">
                    ${bill.amount.toFixed(2)}
                  </td>
                  <td className="px-6 py-4 text-sm">
                    {bill.insuranceProvider ? (
                      <div>
                        <div className="text-gray-600">{bill.insuranceProvider}</div>
                        <div className="text-xs text-green-600 font-medium">
                          Covered: ${bill.insuranceCovered.toFixed(2)}
                        </div>
                      </div>
                    ) : (
                      <span className="text-gray-400">No insurance</span>
                    )}
                  </td>
                  <td className="px-6 py-4 text-sm font-bold text-gray-800">
                    ${bill.patientResponsibility.toFixed(2)}
                  </td>
                  <td className="px-6 py-4">
                    <span className={`px-3 py-1 rounded-full text-xs font-medium ${getStatusColor(bill.status)}`}>
                      {bill.status}
                    </span>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </div>

      {/* Payment Modal - remains the same... */}
      {showPaymentModal && selectedBill && (
        <div className="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center z-50">
          <div className="bg-white rounded-lg p-8 max-w-md w-full">
            <h2 className="text-2xl font-bold mb-6">Process Payment</h2>
            
            <div className="bg-gray-50 p-4 rounded-lg mb-6">
              <h3 className="font-medium text-gray-800 mb-2">Bill Details</h3>
              <div className="text-sm space-y-1">
                <p><span className="font-medium">Bill ID:</span> #{selectedBill.billId}</p>
                <p><span className="font-medium">Patient:</span> {selectedBill.patientName}</p>
                <p><span className="font-medium">Total Amount:</span> ${selectedBill.amount.toFixed(2)}</p>
                {selectedBill.insuranceProvider && (
                  <>
                    <p><span className="font-medium">Insurance:</span> {selectedBill.insuranceProvider}</p>
                    <p><span className="font-medium">Insurance Covered:</span> -${selectedBill.insuranceCovered.toFixed(2)}</p>
                  </>
                )}
                <p className="pt-2 border-t">
                  <span className="font-medium">Patient Responsibility:</span>
                  <span className="text-lg font-bold text-red-600 ml-2">
                    ${selectedBill.patientResponsibility.toFixed(2)}
                  </span>
                </p>
              </div>
            </div>

            <form onSubmit={handlePayment} className="space-y-4">
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-1">Amount to Pay *</label>
                <div className="relative">
                  <DollarSign className="absolute left-3 top-1/2 transform -translate-y-1/2 text-gray-400 w-5 h-5" />
                  <input
                    type="number"
                    required
                    step="0.01"
                    min="0.01"
                    max={selectedBill.patientResponsibility}
                    value={paymentData.amountPaid}
                    onChange={(e) => setPaymentData({...paymentData, amountPaid: e.target.value})}
                    className="w-full pl-10 pr-3 py-2 border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500"
                  />
                </div>
                <p className="text-xs text-gray-500 mt-1">
                  Maximum: ${selectedBill.patientResponsibility.toFixed(2)}
                </p>
              </div>

              <div>
                <label className="block text-sm font-medium text-gray-700 mb-1">Payment Method *</label>
                <select
                  required
                  value={paymentData.paymentMethod}
                  onChange={(e) => setPaymentData({...paymentData, paymentMethod: e.target.value})}
                  className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500"
                >
                  <option value="Cash">Cash</option>
                  <option value="Credit Card">Credit Card</option>
                  <option value="Debit Card">Debit Card</option>
                  <option value="Insurance">Insurance</option>
                  <option value="Check">Check</option>
                </select>
              </div>

              <div className="flex justify-end gap-3 pt-4">
                <button
                  type="button"
                  onClick={() => setShowPaymentModal(false)}
                  className="px-6 py-2 border border-gray-300 rounded-lg hover:bg-gray-50"
                >
                  Cancel
                </button>
                <button
                  type="submit"
                  className="px-6 py-2 bg-green-600 text-white rounded-lg hover:bg-green-700 flex items-center gap-2"
                >
                  <CreditCard className="w-5 h-5" />
                  Process Payment
                </button>
              </div>
            </form>
          </div>
        </div>
      )}
    </div>
  );
}
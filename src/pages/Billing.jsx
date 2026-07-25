import { useAuth } from '../context/AuthContext';
import { canProcessPayments } from '../auth/permissions';
import { formatIstDate } from '../utils/datetime';
import { useState, useEffect } from 'react';
import { DollarSign, CreditCard, AlertCircle, CheckCircle } from 'lucide-react';
import { getAllBills, processPayment } from '../services/api';

export default function Billing() {
  const { user } = useAuth();
  const canPay = canProcessPayments(user?.role);
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
      const res = await processPayment({
        billId: selectedBill.billId,
        method: paymentData.paymentMethod,
      });
      setShowPaymentModal(false);
      loadBills();
      alert(res.data.message || 'Payment taken — thanks for letting us take care of you! 💙');
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
                      {formatIstDate(bill.dateIssued)}
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
                      {canPay && (
                      <button
                        onClick={() => openPaymentModal(bill)}
                        className="flex items-center gap-2 px-4 py-2 bg-green-600 text-white rounded-lg hover:bg-green-700 text-sm"
                      >
                        <CreditCard className="w-4 h-4" />
                        Pay
                      </button>
                      )}
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
                        <div>{formatIstDate(bill.appointmentDate)}</div>
                        <div className="text-xs text-gray-400">{bill.appointmentReason}</div>
                      </div>
                    ) : (
                      'N/A'
                    )}
                  </td>
                  <td className="px-6 py-4 text-sm text-gray-600">
                    {formatIstDate(bill.dateIssued)}
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
              <div className="space-y-2">
                {['Cash', 'Card'].map((m) => (
                  <label key={m}
                    className={`flex items-center justify-between border rounded-lg px-4 py-3 cursor-pointer ${paymentData.paymentMethod === m ? 'border-blue-600 ring-1 ring-blue-600' : 'border-gray-300 hover:border-gray-400'}`}>
                    <span className="flex items-center gap-2 text-sm font-medium">
                      {m === 'Card' ? <CreditCard className="w-4 h-4 text-gray-500" /> : <DollarSign className="w-4 h-4 text-gray-500" />}
                      {m}
                      {m === 'Card' && <span className="text-xs text-gray-500 font-normal">+2.5% service charge</span>}
                    </span>
                    <input type="radio" name="method" value={m}
                      checked={paymentData.paymentMethod === m}
                      onChange={() => setPaymentData({ ...paymentData, paymentMethod: m })} />
                  </label>
                ))}
              </div>

              <div className="bg-gray-50 rounded-lg p-4 text-sm space-y-1">
                <p className="flex justify-between">
                  <span>Amount due</span>
                  <span className="font-medium">${selectedBill.patientResponsibility.toFixed(2)}</span>
                </p>
                {paymentData.paymentMethod === 'Card' && (
                  <>
                    <p className="flex justify-between text-gray-500">
                      <span>Card service charge (2.5%)</span>
                      <span>${(selectedBill.patientResponsibility * 0.025).toFixed(2)}</span>
                    </p>
                    <p className="flex justify-between font-bold border-t pt-1">
                      <span>Total charged</span>
                      <span>${(selectedBill.patientResponsibility * 1.025).toFixed(2)}</span>
                    </p>
                  </>
                )}
                <p className="text-xs text-gray-400 pt-1">Insurance (when on file) is already applied via the claim above.</p>
              </div>

              <div className="flex gap-3 justify-end">
                <button type="button" onClick={() => setShowPaymentModal(false)}
                  className="px-4 py-2 text-gray-600 hover:bg-gray-100 rounded-lg">
                  Cancel
                </button>
                <button type="submit"
                  className="px-6 py-2 bg-green-600 text-white rounded-lg hover:bg-green-700">
                  Take payment
                </button>
              </div>
            </form>
          </div>
        </div>
      )}
    </div>
  );
}
import { useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import { Pill, PackageOpen, CheckCircle2, XCircle, PackageCheck, HandCoins } from 'lucide-react';
import { getPharmacyQueue, confirmPrescription, rejectPrescription, markPrescriptionReady, dispensePrescription, getMedicines, adjustStock } from '../services/api';
import { useAuth } from '../context/AuthContext';
import { canAdjustStock } from '../auth/permissions';
import { formatIstDate } from '../utils/datetime';

export default function Pharmacy() {
  const { user } = useAuth();
  const [tab, setTab] = useState('queue');
  const [queue, setQueue] = useState([]);
  const [medicines, setMedicines] = useState([]);
  const [adjusting, setAdjusting] = useState(null); // {medicineId, name}
  const [adjustment, setAdjustment] = useState('');
  const [note, setNote] = useState('');
  const [busyId, setBusyId] = useState(null);

  const load = () => {
    getPharmacyQueue().then((r) => setQueue(r.data.data)).catch(() => {});
    getMedicines().then((r) => setMedicines(r.data.data)).catch(() => {});
  };
  useEffect(load, []);

  const run = async (id, fn, okMsg) => {
    setBusyId(id);
    try {
      const res = await fn();
      alert(res?.data?.message || okMsg);
      load();
    } catch (e) { alert(e.response?.data?.message || 'Action failed'); }
    finally { setBusyId(null); }
  };

  const onReject = (id) => {
    const reason = prompt('Reason for rejection (the doctor will see this):');
    if (reason?.trim()) run(id, () => rejectPrescription(id, reason.trim()), 'Rejected');
  };

  const submitAdjust = async () => {
    try {
      const res = await adjustStock(adjusting.medicineId, { adjustment: +adjustment, note });
      alert(res.data.message);
      setAdjusting(null); setAdjustment(''); setNote('');
      load();
    } catch (e) { alert(e.response?.data?.message || 'Could not adjust stock'); }
  };

  return (
    <div>
      <h1 className="text-3xl font-bold text-gray-800 mb-6">Pharmacy</h1>

      <div className="flex gap-2 mb-6">
        {[['queue', 'Prescription queue'], ['inventory', 'Inventory']].map(([k, label]) => (
          <button key={k} onClick={() => setTab(k)}
            className={`px-4 py-2 rounded-lg text-sm font-medium ${tab === k ? 'mn-tab-active' : 'mn-tab'}`}>
            {label}
          </button>
        ))}
      </div>

      {tab === 'queue' && (
        <div className="mn-card overflow-hidden">
          <table className="w-full">
            <thead className="bg-gray-50 text-left text-xs text-gray-500 uppercase">
              <tr>
                <th className="px-6 py-3">Rx</th><th className="px-6 py-3">Patient</th>
                <th className="px-6 py-3">Doctor</th><th className="px-6 py-3">Items</th>
                <th className="px-6 py-3">Est. total</th><th className="px-6 py-3">Status</th>
                <th className="px-6 py-3">Actions</th>
              </tr>
            </thead>
            <tbody>
              {queue.length === 0 && (
                <tr><td colSpan="7" className="px-6 py-8 text-center text-gray-400">Queue is empty — nothing to prepare.</td></tr>
              )}
              {queue.map((q) => (
                <tr key={q.prescriptionId} className="border-t hover:bg-gray-50">
                  <td className="px-6 py-4 text-sm">
                    <Link to={`/files/${q.appointmentId}`} className="text-blue-600 hover:underline">#{q.prescriptionId}</Link>
                    <div className="text-xs text-gray-400">issued {formatIstDate(q.dateIssued)}</div>
                  </td>
                  <td className="px-6 py-4 text-sm">{q.patientName}</td>
                  <td className="px-6 py-4 text-sm text-gray-600">{q.doctorName}</td>
                  <td className="px-6 py-4 text-sm">{q.itemCount}</td>
                  <td className="px-6 py-4 text-sm">${q.estimatedTotal.toFixed(2)}</td>
                  <td className="px-6 py-4">
                    <span className="px-3 py-1 rounded-full text-xs font-medium bg-emerald-100 text-emerald-700">{q.status}</span>
                  </td>
                  <td className="px-6 py-4">
                    <div className="flex gap-3 text-sm">
                      {q.status === 'SentToPharmacy' && (
                        <>
                          <button disabled={busyId === q.prescriptionId} title="Confirm (checks stock & validity)"
                            onClick={() => run(q.prescriptionId, () => confirmPrescription(q.prescriptionId), 'Confirmed')}
                            className="text-emerald-600 hover:text-emerald-800"><CheckCircle2 className="w-5 h-5" /></button>
                          <button title="Reject" onClick={() => onReject(q.prescriptionId)}
                            className="text-red-600 hover:text-red-800"><XCircle className="w-5 h-5" /></button>
                        </>
                      )}
                      {q.status === 'Confirmed' && (
                        <>
                          <button disabled={busyId === q.prescriptionId} title="Mark ready for pickup"
                            onClick={() => run(q.prescriptionId, () => markPrescriptionReady(q.prescriptionId), 'Ready for pickup')}
                            className="text-blue-600 hover:text-blue-800"><PackageCheck className="w-5 h-5" /></button>
                          <button title="Reject" onClick={() => onReject(q.prescriptionId)}
                            className="text-red-600 hover:text-red-800"><XCircle className="w-5 h-5" /></button>
                        </>
                      )}
                      {q.status === 'Ready' && (
                        <button disabled={busyId === q.prescriptionId} title="Dispense at pickup (creates pharmacy bill)"
                          onClick={() => run(q.prescriptionId, () => dispensePrescription(q.prescriptionId), 'Dispensed')}
                          className="text-gray-800 hover:text-black"><HandCoins className="w-5 h-5" /></button>
                      )}
                    </div>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}

      {tab === 'inventory' && (
        <div className="mn-card overflow-hidden">
          <table className="w-full">
            <thead className="bg-gray-50 text-left text-xs text-gray-500 uppercase">
              <tr>
                <th className="px-6 py-3">Medicine</th><th className="px-6 py-3">Category</th>
                <th className="px-6 py-3">Unit price</th><th className="px-6 py-3">Stock</th>
                <th className="px-6 py-3">Expiry</th><th className="px-6 py-3"></th>
              </tr>
            </thead>
            <tbody>
              {medicines.map((m) => (
                <tr key={m.medicineId} className="border-t hover:bg-gray-50">
                  <td className="px-6 py-4 text-sm font-medium">{m.name}</td>
                  <td className="px-6 py-4 text-sm text-gray-600">{m.category || '—'}</td>
                  <td className="px-6 py-4 text-sm">${m.unitPrice.toFixed(2)}</td>
                  <td className="px-6 py-4 text-sm">
                    <span className={m.stockQuantity <= 10 ? 'text-red-600 font-semibold' : ''}>{m.stockQuantity}</span>
                    {m.stockQuantity <= 10 && <span className="text-xs text-red-500 ml-2">low</span>}
                  </td>
                  <td className="px-6 py-4 text-sm text-gray-600">{m.expiryDate ? formatIstDate(m.expiryDate) : '—'}</td>
                  <td className="px-6 py-4">
                    {canAdjustStock(user?.role) && (
                      <button onClick={() => setAdjusting({ medicineId: m.medicineId, name: m.name })}
                        className="text-blue-600 hover:text-blue-800 text-sm inline-flex items-center gap-1">
                        <PackageOpen className="w-4 h-4" /> Adjust
                      </button>
                    )}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}

      {adjusting && (
        <div className="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center z-50">
          <div className="bg-white rounded-lg p-8 max-w-sm w-full">
            <h2 className="text-xl font-bold mb-1 flex items-center gap-2"><Pill className="w-5 h-5" /> Adjust stock</h2>
            <p className="text-sm text-gray-500 mb-4">{adjusting.name}</p>
            <input type="number" placeholder="+50 restock / -3 correction" value={adjustment}
              onChange={(e) => setAdjustment(e.target.value)}
              className="w-full px-3 py-2 border border-gray-300 rounded-lg mb-3 text-sm" />
            <input placeholder="Note (optional)" value={note} onChange={(e) => setNote(e.target.value)}
              className="w-full px-3 py-2 border border-gray-300 rounded-lg mb-4 text-sm" />
            <div className="flex gap-3 justify-end">
              <button onClick={() => { setAdjusting(null); setAdjustment(''); setNote(''); }}
                className="px-4 py-2 text-gray-600 hover:bg-gray-100 rounded-lg text-sm">Cancel</button>
              <button onClick={submitAdjust} disabled={!adjustment || +adjustment === 0}
                className="px-4 py-2 bg-blue-600 text-white rounded-lg text-sm hover:bg-blue-700 disabled:opacity-50">Apply</button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}

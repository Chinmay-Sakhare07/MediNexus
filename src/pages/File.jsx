import { useCallback, useEffect, useState } from 'react';
import { useParams, Link } from 'react-router-dom';
import { pendingActions } from '../utils/pending';
import { AlertTriangle, ArrowLeft, FlaskConical, Pill, Stethoscope, HeartPulse, ReceiptText } from 'lucide-react';
import { getFile, recordVitals, saveConsultation, orderLabTests, createPrescription, getMedicines } from '../services/api';
import { useAuth } from '../context/AuthContext';
import { canRecordVitals, canConsult, ROLES } from '../auth/permissions';
import { formatIstDateTime, formatIstDate } from '../utils/datetime';

const statusBadge = (s) => ({
  Requested: 'bg-purple-100 text-purple-700', Scheduled: 'bg-blue-100 text-blue-700',
  Confirmed: 'bg-blue-100 text-blue-700', CheckedIn: 'bg-cyan-100 text-cyan-700',
  InConsultation: 'bg-amber-100 text-amber-700', Completed: 'bg-gray-200 text-gray-700',
  Cancelled: 'bg-red-100 text-red-700', 'No-Show': 'bg-red-100 text-red-700',
}[s] || 'bg-gray-100 text-gray-700');

export default function File() {
  const { appointmentId } = useParams();
  const { user } = useAuth();
  const [file, setFile] = useState(null);
  const [medicines, setMedicines] = useState([]);
  const [vitals, setVitals] = useState({ bloodPressure: '', pulse: '', temperature: '', spo2: '', notes: '' });
  const [consult, setConsult] = useState({ diagnosis: '', notes: '', treatmentPlan: '', followUpRequired: false });
  const [labOrders, setLabOrders] = useState([{ testType: '', normalRange: '', units: '' }]);
  const [rxLines, setRxLines] = useState([{ medicineId: '', quantity: 1, dosage: '', frequency: '', duration: '' }]);
  const [busy, setBusy] = useState('');

  const isDoctor = user?.role === ROLES.DOCTOR || user?.role === ROLES.ADMIN;
  const showVitalsForm = canRecordVitals(user?.role);

  const load = useCallback(async () => {
    try {
      const res = await getFile(appointmentId);
      const f = res.data.data;
      setFile(f);
      if (f.record) {
        setConsult({
          diagnosis: f.record.diagnosis || '', notes: f.record.notes || '',
          treatmentPlan: f.record.treatmentPlan || '', followUpRequired: !!f.record.followUpRequired,
        });
      }
    } catch (e) {
      alert(e.response?.data?.message || 'Could not open the file');
    }
  }, [appointmentId]);

  useEffect(() => { load(); }, [load]);
  useEffect(() => {
    if (isDoctor) getMedicines().then((r) => setMedicines(r.data.data)).catch(() => {});
  }, [isDoctor]);

  const act = async (label, fn) => {
    setBusy(label);
    try { await fn(); await load(); }
    catch (e) { alert(e.response?.data?.message || `Could not ${label}`); }
    finally { setBusy(''); }
  };

  const submitVitals = () => act('save vitals', () => recordVitals(appointmentId, vitals));
  const submitConsult = () => act('save consultation', () => saveConsultation(appointmentId, consult));
  const submitLabs = () => act('order tests', () =>
    orderLabTests(appointmentId, { tests: labOrders.filter((t) => t.testType.trim()) }));
  const submitRx = () => act('send prescription', () =>
    createPrescription(appointmentId, {
      validDays: 30,
      lines: rxLines.filter((l) => l.medicineId).map((l) => ({ ...l, medicineId: +l.medicineId, quantity: +l.quantity })),
    }));

  if (!file) return <div className="text-gray-500">Opening file…</div>;

  const canEditClinically = ['CheckedIn', 'InConsultation'].includes(file.status);

  return (
    <div className="max-w-4xl">
      <Link to="/appointments" className="inline-flex items-center gap-1 text-sm text-blue-600 hover:underline mb-4">
        <ArrowLeft className="w-4 h-4" /> Appointments
      </Link>

      <div id="top" className="mn-card p-6 mb-6">
        <div className="flex justify-between items-start">
          <div>
            <h1 className="text-2xl font-bold text-gray-800">{file.patientName}</h1>
            <p className="text-gray-600 text-sm mt-1">
              Visit #{file.appointmentId} · {formatIstDateTime(file.dateTime)} · {file.doctorName}
            </p>
            {file.reason && <p className="text-gray-500 text-sm mt-1">Reason: {file.reason}</p>}
          </div>
          <span className={`px-3 py-1 rounded-full text-xs font-medium ${statusBadge(file.status)}`}>{file.status}</span>
        </div>
        {file.allergies.length > 0 && (
          <div className="mt-4 flex items-center gap-2 bg-red-50 border border-red-200 text-red-700 px-4 py-2 rounded-lg text-sm">
            <AlertTriangle className="w-4 h-4 shrink-0" />
            <span><strong>Allergies:</strong> {file.allergies.join(', ')}</span>
          </div>
        )}
      </div>

      {/* Vitals */}
      {/* What exactly is pending on this visit — clickable, derived live */}
      <div className="mn-card mn-accent p-4 mb-6">
        <p className="mn-kicker mb-2">Pending on this visit</p>
        <div className="flex flex-wrap gap-2">
          {pendingActions(file).map((a, i) => (
            <button
              key={i}
              onClick={() => document.getElementById(a.target)?.scrollIntoView({ behavior: 'smooth', block: 'start' })}
              className="mn-btn mn-btn-quiet mn-btn-sm"
            >
              {a.label}
            </button>
          ))}
        </div>
      </div>

      <section id="vitals" className="mn-card p-6 mb-6">
        <h2 className="flex items-center gap-2 font-semibold text-gray-800 mb-3"><HeartPulse className="w-5 h-5 text-rose-600" /> Vitals</h2>
        {file.record?.vitalSigns
          ? <p className="text-gray-700 text-sm mb-3">{file.record.vitalSigns}</p>
          : <p className="text-gray-400 text-sm mb-3">Not recorded yet.</p>}
        {showVitalsForm && canEditClinically && (
          <div className="grid grid-cols-2 md:grid-cols-5 gap-3">
            {[['bloodPressure', 'BP (128/84)'], ['pulse', 'Pulse'], ['temperature', 'Temp'], ['spo2', 'SpO2'], ['notes', 'Notes']].map(([k, ph]) => (
              <input key={k} placeholder={ph} value={vitals[k]}
                onChange={(e) => setVitals({ ...vitals, [k]: e.target.value })}
                className="px-3 py-2 border border-gray-300 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-blue-500" />
            ))}
            <button onClick={submitVitals} disabled={!!busy}
              className="col-span-2 md:col-span-5 md:w-40 bg-rose-600 text-white rounded-lg py-2 text-sm hover:bg-rose-700 disabled:opacity-50">
              {busy === 'save vitals' ? 'Saving…' : 'Save vitals'}
            </button>
          </div>
        )}
      </section>

      {/* Consultation */}
      <section id="consult" className="mn-card p-6 mb-6">
        <h2 className="flex items-center gap-2 font-semibold text-gray-800 mb-3"><Stethoscope className="w-5 h-5 text-blue-600" /> Consultation</h2>
        {isDoctor && canConsult(user?.role) && canEditClinically ? (
          <div className="space-y-3">
            <input placeholder="Diagnosis *" value={consult.diagnosis}
              onChange={(e) => setConsult({ ...consult, diagnosis: e.target.value })}
              className="w-full px-3 py-2 border border-gray-300 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-blue-500" />
            <textarea placeholder="Treatment plan" rows={2} value={consult.treatmentPlan}
              onChange={(e) => setConsult({ ...consult, treatmentPlan: e.target.value })}
              className="w-full px-3 py-2 border border-gray-300 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-blue-500" />
            <textarea placeholder="Notes" rows={2} value={consult.notes}
              onChange={(e) => setConsult({ ...consult, notes: e.target.value })}
              className="w-full px-3 py-2 border border-gray-300 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-blue-500" />
            <label className="flex items-center gap-2 text-sm text-gray-700">
              <input type="checkbox" checked={consult.followUpRequired}
                onChange={(e) => setConsult({ ...consult, followUpRequired: e.target.checked })} />
              Follow-up required
            </label>
            <button onClick={submitConsult} disabled={!!busy || !consult.diagnosis.trim()}
              className="mn-btn mn-btn-primary mn-btn-sm">
              {busy === 'save consultation' ? 'Saving…' : 'Save consultation'}
            </button>
          </div>
        ) : file.record?.diagnosis ? (
          <div className="text-sm text-gray-700 space-y-1">
            <p><span className="font-medium">Diagnosis:</span> {file.record.diagnosis}</p>
            {file.record.treatmentPlan && <p><span className="font-medium">Plan:</span> {file.record.treatmentPlan}</p>}
            {file.record.notes && <p><span className="font-medium">Notes:</span> {file.record.notes}</p>}
            {file.record.followUpRequired ? <p className="text-amber-700">Follow-up required</p> : null}
          </div>
        ) : <p className="text-gray-400 text-sm">Not written yet.</p>}
      </section>

      {/* Lab tests */}
      <section id="labs" className="mn-card p-6 mb-6">
        <h2 className="flex items-center gap-2 font-semibold text-gray-800 mb-3"><FlaskConical className="w-5 h-5 text-violet-600" /> Lab tests</h2>
        {file.labTests.length > 0 && (
          <table className="w-full text-sm mb-4">
            <thead><tr className="text-left text-gray-500">
              <th className="py-1 pr-3">Test</th><th className="py-1 pr-3">Status</th>
              <th className="py-1 pr-3">Result</th><th className="py-1">Normal</th>
            </tr></thead>
            <tbody>
              {file.labTests.map((t) => (
                <tr key={t.labTestId} className="border-t">
                  <td className="py-2 pr-3">{t.testType}</td>
                  <td className="py-2 pr-3">{t.status}</td>
                  <td className="py-2 pr-3">{t.result ? `${t.result} ${t.units || ''}` : '—'}</td>
                  <td className="py-2 text-gray-500">{t.normalRange || '—'}</td>
                </tr>
              ))}
            </tbody>
          </table>
        )}
        {isDoctor && canEditClinically && (
          <div className="space-y-2">
            {labOrders.map((t, i) => (
              <div key={i} className="grid grid-cols-3 gap-2">
                <input placeholder="Test type *" value={t.testType}
                  onChange={(e) => setLabOrders(labOrders.map((x, j) => j === i ? { ...x, testType: e.target.value } : x))}
                  className="px-3 py-2 border border-gray-300 rounded-lg text-sm" />
                <input placeholder="Normal range" value={t.normalRange}
                  onChange={(e) => setLabOrders(labOrders.map((x, j) => j === i ? { ...x, normalRange: e.target.value } : x))}
                  className="px-3 py-2 border border-gray-300 rounded-lg text-sm" />
                <input placeholder="Units" value={t.units}
                  onChange={(e) => setLabOrders(labOrders.map((x, j) => j === i ? { ...x, units: e.target.value } : x))}
                  className="px-3 py-2 border border-gray-300 rounded-lg text-sm" />
              </div>
            ))}
            <div className="flex gap-2">
              <button onClick={() => setLabOrders([...labOrders, { testType: '', normalRange: '', units: '' }])}
                className="text-sm text-blue-600 hover:underline">+ Add test</button>
              <button onClick={submitLabs} disabled={!!busy || !labOrders.some((t) => t.testType.trim())}
                className="ml-auto bg-violet-600 text-white rounded-lg px-5 py-2 text-sm hover:bg-violet-700 disabled:opacity-50">
                {busy === 'order tests' ? 'Ordering…' : 'Order tests'}
              </button>
            </div>
          </div>
        )}
        {file.labTests.length === 0 && !isDoctor && <p className="text-gray-400 text-sm">No tests ordered.</p>}
      </section>

      {/* Prescription */}
      <section id="prescription" className="mn-card p-6 mb-6">
        <h2 className="flex items-center gap-2 font-semibold text-gray-800 mb-3"><Pill className="w-5 h-5 text-emerald-600" /> Prescription</h2>
        {file.prescription ? (
          <div className="text-sm">
            <p className="mb-2">
              <span className={`px-2 py-0.5 rounded-full text-xs font-medium ${
                file.prescription.status === 'Rejected' ? 'bg-red-100 text-red-700'
                : file.prescription.status === 'Dispensed' ? 'bg-gray-200 text-gray-700'
                : 'bg-emerald-100 text-emerald-700'}`}>{file.prescription.status}</span>
              {file.prescription.rejectReason && <span className="text-red-600 ml-2">{file.prescription.rejectReason}</span>}
              <span className="text-gray-500 ml-2">valid until {formatIstDate(file.prescription.validUntil)}</span>
            </p>
            <ul className="space-y-1">
              {file.prescription.lines.map((l) => (
                <li key={l.medicineId} className="border-t pt-1 text-gray-700">
                  {l.medicineName} × {l.quantity}
                  <span className="text-gray-500"> — {[l.dosage, l.frequency, l.duration].filter(Boolean).join(', ')}</span>
                </li>
              ))}
            </ul>
          </div>
        ) : isDoctor && canEditClinically ? (
          <div className="space-y-2">
            {rxLines.map((l, i) => (
              <div key={i} className="grid grid-cols-6 gap-2">
                <select value={l.medicineId}
                  onChange={(e) => setRxLines(rxLines.map((x, j) => j === i ? { ...x, medicineId: e.target.value } : x))}
                  className="col-span-2 px-2 py-2 border border-gray-300 rounded-lg text-sm">
                  <option value="">Medicine…</option>
                  {medicines.map((m) => (
                    <option key={m.medicineId} value={m.medicineId}>
                      {m.name} (stock {m.stockQuantity})
                    </option>
                  ))}
                </select>
                <input type="number" min="1" value={l.quantity} title="Quantity"
                  onChange={(e) => setRxLines(rxLines.map((x, j) => j === i ? { ...x, quantity: e.target.value } : x))}
                  className="px-2 py-2 border border-gray-300 rounded-lg text-sm" />
                <input placeholder="Dosage" value={l.dosage}
                  onChange={(e) => setRxLines(rxLines.map((x, j) => j === i ? { ...x, dosage: e.target.value } : x))}
                  className="px-2 py-2 border border-gray-300 rounded-lg text-sm" />
                <input placeholder="Frequency" value={l.frequency}
                  onChange={(e) => setRxLines(rxLines.map((x, j) => j === i ? { ...x, frequency: e.target.value } : x))}
                  className="px-2 py-2 border border-gray-300 rounded-lg text-sm" />
                <input placeholder="Duration" value={l.duration}
                  onChange={(e) => setRxLines(rxLines.map((x, j) => j === i ? { ...x, duration: e.target.value } : x))}
                  className="px-2 py-2 border border-gray-300 rounded-lg text-sm" />
              </div>
            ))}
            <div className="flex gap-2">
              <button onClick={() => setRxLines([...rxLines, { medicineId: '', quantity: 1, dosage: '', frequency: '', duration: '' }])}
                className="text-sm text-blue-600 hover:underline">+ Add medicine</button>
              <button onClick={submitRx} disabled={!!busy || !rxLines.some((l) => l.medicineId)}
                className="ml-auto bg-emerald-600 text-white rounded-lg px-5 py-2 text-sm hover:bg-emerald-700 disabled:opacity-50">
                {busy === 'send prescription' ? 'Sending…' : 'Send to pharmacy'}
              </button>
            </div>
          </div>
        ) : <p className="text-gray-400 text-sm">No prescription.</p>}
      </section>

      {/* Bills */}
      <section id="bills" className="mn-card p-6 mb-6">
        <h2 className="flex items-center gap-2 font-semibold text-gray-800 mb-3"><ReceiptText className="w-5 h-5 text-gray-600" /> Bills</h2>
        {file.bills.length === 0 ? <p className="text-gray-400 text-sm">No bills yet.</p> : (
          <table className="w-full text-sm">
            <thead><tr className="text-left text-gray-500">
              <th className="py-1 pr-3">#</th><th className="py-1 pr-3">Type</th><th className="py-1 pr-3">Amount</th>
              <th className="py-1 pr-3">Insurance</th><th className="py-1 pr-3">You pay</th><th className="py-1">Status</th>
            </tr></thead>
            <tbody>
              {file.bills.map((b) => (
                <tr key={b.billId} className="border-t">
                  <td className="py-2 pr-3">{b.billId}</td>
                  <td className="py-2 pr-3">{b.billType}</td>
                  <td className="py-2 pr-3">${b.amount.toFixed(2)}</td>
                  <td className="py-2 pr-3 text-green-700">-${b.insuranceCovered.toFixed(2)}</td>
                  <td className="py-2 pr-3 font-medium">${b.patientResponsibility.toFixed(2)}</td>
                  <td className="py-2">{b.status}{b.paymentMethod ? ` (${b.paymentMethod})` : ''}</td>
                </tr>
              ))}
            </tbody>
          </table>
        )}
        <p className="text-xs text-gray-400 mt-2">Payments are taken at the front desk (Billing page).</p>
      </section>
    </div>
  );
}

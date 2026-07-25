import { useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import { FlaskConical, Play } from 'lucide-react';
import { getLabQueue, startLabTest, enterLabResult } from '../services/api';
import { formatIstDateTime } from '../utils/datetime';

export default function Lab() {
  const [queue, setQueue] = useState([]);
  const [entering, setEntering] = useState(null); // test row
  const [result, setResult] = useState('');
  const [comments, setComments] = useState('');

  const load = () => getLabQueue().then((r) => setQueue(r.data.data)).catch(() => {});
  useEffect(load, []);

  const start = async (id) => {
    try { await startLabTest(id); load(); }
    catch (e) { alert(e.response?.data?.message || 'Could not start'); }
  };

  const submit = async () => {
    try {
      await enterLabResult(entering.labTestId, { result, comments });
      setEntering(null); setResult(''); setComments('');
      load();
    } catch (e) { alert(e.response?.data?.message || 'Could not save result'); }
  };

  return (
    <div>
      <h1 className="text-3xl font-bold text-gray-800 mb-6 flex items-center gap-2">
        <FlaskConical className="w-7 h-7 text-violet-600" /> Lab work queue
      </h1>

      <div className="mn-card overflow-hidden">
        <table className="w-full">
          <thead className="bg-gray-50 text-left text-xs text-gray-500 uppercase">
            <tr>
              <th className="px-6 py-3">Test</th><th className="px-6 py-3">Patient</th>
              <th className="px-6 py-3">Visit</th><th className="px-6 py-3">Ordered by</th>
              <th className="px-6 py-3">Status</th><th className="px-6 py-3">Actions</th>
            </tr>
          </thead>
          <tbody>
            {queue.length === 0 && (
              <tr><td colSpan="6" className="px-6 py-8 text-center text-gray-400">Nothing pending — the bench is clear.</td></tr>
            )}
            {queue.map((t) => (
              <tr key={t.labTestId} className="border-t hover:bg-gray-50">
                <td className="px-6 py-4 text-sm font-medium">
                  {t.testType}
                  {t.normalRange && <div className="text-xs text-gray-400">normal: {t.normalRange} {t.units || ''}</div>}
                </td>
                <td className="px-6 py-4 text-sm">{t.patientName}</td>
                <td className="px-6 py-4 text-sm">
                  <Link to={`/files/${t.appointmentId}`} className="text-blue-600 hover:underline">
                    {formatIstDateTime(t.appointmentDateTime)}
                  </Link>
                </td>
                <td className="px-6 py-4 text-sm text-gray-600">{t.doctorName}</td>
                <td className="px-6 py-4 text-sm">{t.status}</td>
                <td className="px-6 py-4">
                  <div className="flex gap-3">
                    {t.status === 'Pending' && (
                      <button onClick={() => start(t.labTestId)} title="Start"
                        className="text-blue-600 hover:text-blue-800"><Play className="w-5 h-5" /></button>
                    )}
                    {t.status !== 'Completed' && (
                      <button onClick={() => setEntering(t)}
                        className="text-sm text-emerald-700 hover:underline">Enter result</button>
                    )}
                  </div>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>

      {entering && (
        <div className="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center z-50">
          <div className="bg-white rounded-lg p-8 max-w-sm w-full">
            <h2 className="text-xl font-bold mb-1">Result — {entering.testType}</h2>
            <p className="text-sm text-gray-500 mb-4">{entering.patientName}
              {entering.normalRange && ` · normal ${entering.normalRange} ${entering.units || ''}`}</p>
            <input placeholder={`Result${entering.units ? ` (${entering.units})` : ''} *`} value={result}
              onChange={(e) => setResult(e.target.value)}
              className="w-full px-3 py-2 border border-gray-300 rounded-lg mb-3 text-sm" />
            <input placeholder="Comments (optional)" value={comments} onChange={(e) => setComments(e.target.value)}
              className="w-full px-3 py-2 border border-gray-300 rounded-lg mb-4 text-sm" />
            <div className="flex gap-3 justify-end">
              <button onClick={() => { setEntering(null); setResult(''); setComments(''); }}
                className="px-4 py-2 text-gray-600 hover:bg-gray-100 rounded-lg text-sm">Cancel</button>
              <button onClick={submit} disabled={!result.trim()}
                className="px-4 py-2 bg-emerald-600 text-white rounded-lg text-sm hover:bg-emerald-700 disabled:opacity-50">
                Save & complete
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}

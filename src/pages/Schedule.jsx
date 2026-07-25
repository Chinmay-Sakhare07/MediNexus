import { useEffect, useState } from 'react';
import { CalendarClock, CalendarOff, Trash2 } from 'lucide-react';
import { getDoctors, getDoctorSchedule, updateDoctorSchedule, getDoctorLeaves, addDoctorLeave, removeDoctorLeave } from '../services/api';
import { useAuth } from '../context/AuthContext';
import { ROLES } from '../auth/permissions';
import { formatIstDate } from '../utils/datetime';

const DAYS = ['Mon', 'Tue', 'Wed', 'Thu', 'Fri', 'Sat', 'Sun'];

export default function Schedule() {
  const { user } = useAuth();
  const isAdmin = user?.role === ROLES.ADMIN;
  const [doctors, setDoctors] = useState([]);
  const [doctorId, setDoctorId] = useState(isAdmin ? '' : user?.staffId);
  const [form, setForm] = useState(null);
  const [leaves, setLeaves] = useState([]);
  const [leaveDate, setLeaveDate] = useState('');
  const [leaveReason, setLeaveReason] = useState('');

  useEffect(() => { if (isAdmin) getDoctors().then((r) => setDoctors(r.data.data)).catch(() => {}); }, [isAdmin]);

  useEffect(() => {
    if (!doctorId) return;
    getDoctorSchedule(doctorId).then((r) => {
      const s = r.data.data;
      setForm({ workDays: s.workDays.split(','), startTime: s.startTime, endTime: s.endTime, slotMinutes: s.slotMinutes });
    }).catch(() => setForm({ workDays: ['Mon','Tue','Wed','Thu','Fri','Sat'], startTime: '09:00', endTime: '17:00', slotMinutes: 30 }));
    getDoctorLeaves(doctorId).then((r) => setLeaves(r.data.data)).catch(() => {});
  }, [doctorId]);

  const toggleDay = (d) => setForm({
    ...form,
    workDays: form.workDays.includes(d) ? form.workDays.filter((x) => x !== d) : [...form.workDays, d],
  });

  const saveSchedule = async () => {
    try {
      const res = await updateDoctorSchedule(doctorId, { ...form, slotMinutes: +form.slotMinutes });
      alert(res.data.message);
    } catch (e) { alert(e.response?.data?.message || 'Could not save schedule'); }
  };

  const fileLeave = async () => {
    if (!window.confirm('Filing leave will cancel ALL active appointments on that day. Continue?')) return;
    try {
      const res = await addDoctorLeave(doctorId, { leaveDate, reason: leaveReason });
      alert(res.data.message);
      setLeaveDate(''); setLeaveReason('');
      getDoctorLeaves(doctorId).then((r) => setLeaves(r.data.data));
    } catch (e) { alert(e.response?.data?.message || 'Could not file leave'); }
  };

  const deleteLeave = async (leaveId) => {
    try {
      await removeDoctorLeave(doctorId, leaveId);
      setLeaves(leaves.filter((l) => l.leaveId !== leaveId));
    } catch (e) { alert(e.response?.data?.message || 'Could not remove leave'); }
  };

  return (
    <div className="max-w-3xl">
      <h1 className="text-3xl font-bold text-gray-800 mb-6 flex items-center gap-2">
        <CalendarClock className="w-7 h-7 text-blue-600" /> {isAdmin ? 'Doctor schedules' : 'My schedule'}
      </h1>

      {isAdmin && (
        <select value={doctorId} onChange={(e) => setDoctorId(e.target.value)}
          className="mb-6 px-3 py-2 border border-gray-300 rounded-lg bg-white text-sm">
          <option value="">Select a doctor…</option>
          {doctors.map((d) => (
            <option key={d.doctorId} value={d.doctorId}>Dr. {d.firstName} {d.lastName} — {d.specialization}</option>
          ))}
        </select>
      )}

      {form && doctorId && (
        <>
          <div className="bg-white rounded-lg shadow-md p-6 mb-6">
            <h2 className="font-semibold text-gray-800 mb-4">Weekly pattern</h2>
            <div className="flex gap-2 mb-4 flex-wrap">
              {DAYS.map((d) => (
                <button key={d} onClick={() => toggleDay(d)}
                  className={`px-3 py-1.5 rounded-lg text-sm font-medium ${form.workDays.includes(d) ? 'bg-blue-600 text-white' : 'bg-gray-100 text-gray-500 hover:bg-gray-200'}`}>
                  {d}
                </button>
              ))}
            </div>
            <div className="grid grid-cols-3 gap-4 mb-4">
              <label className="text-sm text-gray-600">Start
                <input type="time" value={form.startTime} onChange={(e) => setForm({ ...form, startTime: e.target.value })}
                  className="mt-1 w-full px-3 py-2 border border-gray-300 rounded-lg" />
              </label>
              <label className="text-sm text-gray-600">End
                <input type="time" value={form.endTime} onChange={(e) => setForm({ ...form, endTime: e.target.value })}
                  className="mt-1 w-full px-3 py-2 border border-gray-300 rounded-lg" />
              </label>
              <label className="text-sm text-gray-600">Slot (min)
                <input type="number" min="5" max="120" step="5" value={form.slotMinutes}
                  onChange={(e) => setForm({ ...form, slotMinutes: e.target.value })}
                  className="mt-1 w-full px-3 py-2 border border-gray-300 rounded-lg" />
              </label>
            </div>
            <button onClick={saveSchedule} disabled={form.workDays.length === 0}
              className="bg-blue-600 text-white rounded-lg px-5 py-2 text-sm hover:bg-blue-700 disabled:opacity-50">
              Save schedule
            </button>
            <p className="text-xs text-gray-400 mt-2">All times are hospital time (IST). Booked slots stay honored; the pattern shapes future availability.</p>
          </div>

          <div className="bg-white rounded-lg shadow-md p-6">
            <h2 className="font-semibold text-gray-800 mb-4 flex items-center gap-2">
              <CalendarOff className="w-5 h-5 text-amber-600" /> Leave
            </h2>
            <div className="flex gap-3 mb-4 flex-wrap">
              <input type="date" value={leaveDate} onChange={(e) => setLeaveDate(e.target.value)}
                className="px-3 py-2 border border-gray-300 rounded-lg text-sm" />
              <input placeholder="Reason (optional)" value={leaveReason} onChange={(e) => setLeaveReason(e.target.value)}
                className="flex-1 min-w-40 px-3 py-2 border border-gray-300 rounded-lg text-sm" />
              <button onClick={fileLeave} disabled={!leaveDate}
                className="bg-amber-600 text-white rounded-lg px-5 py-2 text-sm hover:bg-amber-700 disabled:opacity-50">
                File leave
              </button>
            </div>
            <p className="text-xs text-red-500 mb-3">Filing leave cancels all active appointments on that day.</p>
            {leaves.length === 0 ? <p className="text-gray-400 text-sm">No upcoming leave.</p> : (
              <ul className="space-y-2">
                {leaves.map((l) => (
                  <li key={l.leaveId} className="flex items-center justify-between border rounded-lg px-4 py-2 text-sm">
                    <span>{formatIstDate(l.leaveDate)}{l.reason ? ` — ${l.reason}` : ''}</span>
                    <button onClick={() => deleteLeave(l.leaveId)} className="text-red-600 hover:text-red-800">
                      <Trash2 className="w-4 h-4" />
                    </button>
                  </li>
                ))}
              </ul>
            )}
          </div>
        </>
      )}
    </div>
  );
}

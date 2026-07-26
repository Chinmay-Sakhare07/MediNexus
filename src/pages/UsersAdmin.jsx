import { useEffect, useState } from 'react';
import { UserPlus, Pencil, UserX, UserCheck, KeyRound } from 'lucide-react';
import { getUsers, createUser, updateUser, deactivateUser, reactivateUser, resetUserPassword } from '../services/api';
import { formatIstDateTime } from '../utils/datetime';

const ROLES = ['Admin', 'Doctor', 'Nurse', 'LabTech', 'Pharmacist', 'Receptionist', 'Patient'];
const needsStaff = (r) => r === 'Doctor' || r === 'LabTech';
const emptyForm = { username: '', email: '', role: 'Receptionist', staffId: '', patientId: '' };

export default function UsersAdmin() {
  const [users, setUsers] = useState([]);
  const [showInactive, setShowInactive] = useState(false);
  const [loading, setLoading] = useState(true);
  const [showModal, setShowModal] = useState(false);
  const [editingId, setEditingId] = useState(null);
  const [form, setForm] = useState(emptyForm);
  const [errors, setErrors] = useState({});
  const [saving, setSaving] = useState(false);

  const load = () => {
    setLoading(true);
    getUsers(showInactive)
      .then((res) => setUsers(res.data?.data || []))
      .finally(() => setLoading(false));
  };
  useEffect(load, [showInactive]);

  const openCreate = () => { setEditingId(null); setForm(emptyForm); setErrors({}); setShowModal(true); };
  const openEdit = (u) => {
    setEditingId(u.userId);
    setForm({ username: u.username, email: u.email, role: u.role,
              staffId: u.staffId ?? '', patientId: u.patientId ?? '' });
    setErrors({});
    setShowModal(true);
  };

  const handleSubmit = async (e) => {
    e.preventDefault();
    setSaving(true);
    setErrors({});
    const link = {
      staffId: form.staffId === '' ? null : Number(form.staffId),
      patientId: form.patientId === '' ? null : Number(form.patientId),
    };
    try {
      if (editingId) {
        await updateUser(editingId, { email: form.email, role: form.role, ...link });
      } else {
        const res = await createUser({
          username: form.username, email: form.email, role: form.role, ...link,
        });
        alert(res.data?.message || 'User created');
      }
      setShowModal(false);
      load();
    } catch (err) {
      const data = err?.response?.data;
      if (data?.errors) setErrors(data.errors);
      else alert(data?.message || 'Could not save the user');
    } finally {
      setSaving(false);
    }
  };

  const toggleActive = async (u) => {
    const verb = u.isActive ? 'Deactivate' : 'Reactivate';
    if (!window.confirm(`${verb} ${u.username}? ${u.isActive ? 'They will no longer be able to sign in; history is kept.' : ''}`)) return;
    try {
      await (u.isActive ? deactivateUser(u.userId) : reactivateUser(u.userId));
      load();
    } catch (err) {
      alert(err?.response?.data?.message || `Could not ${verb.toLowerCase()} the user`);
    }
  };

  const fieldError = (name) =>
    errors[name]?.length ? <p className="text-xs mt-1" style={{ color: 'var(--mn-brick)' }}>{errors[name][0]}</p> : null;

  return (
    <div>
      <div className="flex items-center justify-between mb-6">
        <div>
          <h1 className="text-2xl font-bold">Users</h1>
          <p className="text-sm text-gray-500">Accounts, roles and access. Deletion is soft — history stays.</p>
        </div>
        <button onClick={openCreate} className="mn-btn mn-btn-primary">
          <UserPlus className="w-5 h-5" /> New user
        </button>
      </div>

      <label className="inline-flex items-center gap-2 mb-4 text-sm cursor-pointer">
        <input type="checkbox" checked={showInactive} onChange={(e) => setShowInactive(e.target.checked)} />
        Show deactivated accounts
      </label>

      <div className="mn-card overflow-hidden">
        <table className="w-full">
          <thead className="mn-thead-bg">
            <tr>
              {['User', 'Role', 'Email', 'Links', 'Last sign-in', 'Status', 'Actions'].map((h) => (
                <th key={h} className="px-4 py-3 text-left">{h}</th>
              ))}
            </tr>
          </thead>
          <tbody>
            {loading ? (
              <tr><td colSpan={7} className="px-4 py-8 text-center text-gray-500">Loading…</td></tr>
            ) : users.map((u) => (
              <tr key={u.userId} style={{ borderTop: '1px solid var(--mn-rule)', opacity: u.isActive ? 1 : 0.55 }}>
                <td className="px-4 py-3">
                  <div className="font-medium">{u.displayName}</div>
                  <div className="text-xs text-gray-500">{u.username}</div>
                </td>
                <td className="px-4 py-3 text-sm">{u.role}</td>
                <td className="px-4 py-3 text-sm">{u.email}</td>
                <td className="px-4 py-3 text-xs text-gray-500">
                  {u.staffId ? `Staff #${u.staffId}` : u.patientId ? `Patient #${u.patientId}` : '—'}
                </td>
                <td className="px-4 py-3 text-xs text-gray-500">
                  {u.lastLogin ? formatIstDateTime(u.lastLogin) : 'Never'}
                </td>
                <td className="px-4 py-3">
                  <span className="px-2 py-0.5 text-xs font-semibold"
                    style={{
                      background: u.isActive ? 'var(--mn-teal-wash)' : 'var(--mn-paper-deep)',
                      color: u.isActive ? 'var(--mn-teal-deep)' : 'var(--mn-ink-soft)',
                      borderRadius: 2,
                    }}>
                    {u.isActive ? 'Active' : 'Deactivated'}
                  </span>
                </td>
                <td className="px-4 py-3">
                  <div className="flex gap-3">
                    <button onClick={() => openEdit(u)} className="text-blue-600 hover:text-blue-800" title="Edit">
                      <Pencil className="w-4 h-4" />
                    </button>
                    <button
                      onClick={async () => {
                        if (!window.confirm(`Reset ${u.username}'s password to the default (MediNexus@2026)?`)) return;
                        try {
                          const res = await resetUserPassword(u.userId);
                          alert(res.data?.message || 'Password reset');
                        } catch (err) {
                          alert(err?.response?.data?.message || 'Could not reset the password');
                        }
                      }}
                      className="text-blue-600 hover:text-blue-800"
                      title="Reset password to default"
                    >
                      <KeyRound className="w-4 h-4" />
                    </button>
                    <button onClick={() => toggleActive(u)}
                      className={u.isActive ? 'text-red-600 hover:text-red-800' : 'text-green-600'}
                      title={u.isActive ? 'Deactivate (soft delete)' : 'Reactivate'}>
                      {u.isActive ? <UserX className="w-4 h-4" /> : <UserCheck className="w-4 h-4" />}
                    </button>
                  </div>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>

      {showModal && (
        <div className="fixed inset-0 flex items-center justify-center z-50" style={{ background: 'rgba(32,38,43,0.55)' }}>
          <div className="mn-card mn-accent p-6 w-full max-w-md" style={{ background: 'var(--mn-surface)' }}>
            <h2 className="text-xl font-bold mb-4">{editingId ? 'Edit user' : 'New user'}</h2>
            <form onSubmit={handleSubmit} className="space-y-3">
              {!editingId && (
                <div>
                  <label className="block text-sm font-medium mb-1">Username *</label>
                  <input required value={form.username}
                    onChange={(e) => setForm({ ...form, username: e.target.value })}
                    className="w-full border px-3 py-2" />
                  {fieldError('Username')}
                </div>
              )}
              <div>
                <label className="block text-sm font-medium mb-1">Email *</label>
                <input required type="email" value={form.email}
                  onChange={(e) => setForm({ ...form, email: e.target.value })}
                  className="w-full border px-3 py-2" />
                {fieldError('Email')}
              </div>
              {!editingId && (
                <p className="text-xs px-3 py-2" style={{ background: 'var(--mn-amber-wash)', color: '#7A5210', borderRadius: 2 }}>
                  New users start with the default password <strong>MediNexus@2026</strong> — ask them to change it after their first sign-in.
                </p>
              )}
              <div>
                <label className="block text-sm font-medium mb-1">Role *</label>
                <select value={form.role} onChange={(e) => setForm({ ...form, role: e.target.value })}
                  className="w-full border px-3 py-2">
                  {ROLES.map((r) => <option key={r}>{r}</option>)}
                </select>
                {fieldError('Role')}
              </div>
              {(needsStaff(form.role) || ['Nurse', 'Pharmacist', 'Receptionist', 'Admin'].includes(form.role)) && (
                <div>
                  <label className="block text-sm font-medium mb-1">
                    Staff ID {needsStaff(form.role) ? '*' : '(optional)'}
                  </label>
                  <input type="number" value={form.staffId}
                    onChange={(e) => setForm({ ...form, staffId: e.target.value, patientId: '' })}
                    className="w-full border px-3 py-2" />
                  {fieldError('StaffId')}
                </div>
              )}
              {form.role === 'Patient' && (
                <div>
                  <label className="block text-sm font-medium mb-1">Patient ID *</label>
                  <input type="number" value={form.patientId}
                    onChange={(e) => setForm({ ...form, patientId: e.target.value, staffId: '' })}
                    className="w-full border px-3 py-2" />
                  {fieldError('PatientId')}
                </div>
              )}
              <div className="flex justify-end gap-2 pt-2">
                <button type="button" onClick={() => setShowModal(false)} className="mn-btn mn-btn-quiet">Cancel</button>
                <button type="submit" disabled={saving} className="mn-btn mn-btn-primary">
                  {saving ? 'Saving…' : editingId ? 'Save changes' : 'Create user'}
                </button>
              </div>
            </form>
          </div>
        </div>
      )}
    </div>
  );
}

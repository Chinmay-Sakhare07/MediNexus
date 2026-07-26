import { useEffect, useState } from 'react';
import { Link, useLocation } from 'react-router-dom';
import {
  LayoutDashboard, Users, Calendar, DollarSign, Stethoscope, Shield,
  Activity, LogOut, FlaskConical, Pill, CalendarClock, UserRoundSearch, UserCog, KeyRound,
} from 'lucide-react';
import { useAuth } from '../../context/AuthContext';
import { canAccess } from '../../auth/permissions';
import { getSwitchTargets, changePassword } from '../../services/api';

const MENU = [
  { path: '/', module: 'dashboard', icon: LayoutDashboard, label: 'Dashboard' },
  { path: '/doctors', module: 'doctors', icon: Stethoscope, label: 'Doctors' },
  { path: '/patients', module: 'patients', icon: Users, label: 'Patients' },
  { path: '/insurance', module: 'insurance', icon: Shield, label: 'Insurance' },
  { path: '/appointments', module: 'appointments', icon: Calendar, label: 'Appointments' },
  { path: '/billing', module: 'billing', icon: DollarSign, label: 'Billing' },
  { path: '/lab', module: 'lab', icon: FlaskConical, label: 'Lab' },
  { path: '/pharmacy', module: 'pharmacy', icon: Pill, label: 'Pharmacy' },
  { path: '/schedule', module: 'schedule', icon: CalendarClock, label: 'My Schedule' },
  { path: '/users', module: 'users', icon: UserCog, label: 'Users' },
];

export default function Sidebar() {
  const location = useLocation();
  const { user, logout, impersonate, isImpersonating } = useAuth();
  const [targets, setTargets] = useState([]);
  const [showPw, setShowPw] = useState(false);
  const [pwForm, setPwForm] = useState({ current: '', next: '', confirm: '' });
  const [pwBusy, setPwBusy] = useState(false);

  const submitPw = async (e) => {
    e.preventDefault();
    if (pwForm.next !== pwForm.confirm) { alert('New passwords do not match'); return; }
    setPwBusy(true);
    try {
      await changePassword({ currentPassword: pwForm.current, newPassword: pwForm.next });
      alert('Password changed');
      setShowPw(false);
      setPwForm({ current: '', next: '', confirm: '' });
    } catch (err) {
      alert(err?.response?.data?.message || 'Could not change the password');
    } finally {
      setPwBusy(false);
    }
  };
  const [switching, setSwitching] = useState(false);

  const isAdmin = user?.role === 'Admin';

  useEffect(() => {
    if (isAdmin && !isImpersonating) {
      getSwitchTargets()
        .then((res) => setTargets(res.data?.data || []))
        .catch(() => setTargets([]));
    }
  }, [isAdmin, isImpersonating]);

  const handleSwitch = async (e) => {
    const userId = Number(e.target.value);
    if (!userId) return;
    setSwitching(true);
    try {
      await impersonate(userId);
      window.location.href = '/';
    } catch (err) {
      alert(err?.response?.data?.message || 'Could not switch account');
      setSwitching(false);
    }
  };

  const visibleItems = MENU.filter((item) => user && canAccess(user.role, item.module));

  return (
    <div
      className="w-64 h-screen fixed left-0 top-0 flex flex-col"
      style={{ background: 'var(--mn-ink-2)', borderRight: '1px solid #2A3138' }}
    >
      <div className="p-6" style={{ borderBottom: '1px solid #2A3138' }}>
        <div className="flex items-center gap-3">
          <div style={{ background: 'var(--mn-teal)', padding: 8 }}>
            <Activity className="w-6 h-6 text-white" />
          </div>
          <div>
            <h1 className="text-lg font-bold text-white tracking-tight">MediNexus</h1>
            <p className="mn-kicker" style={{ color: '#8B949C' }}>Group Six Hospital</p>
          </div>
        </div>
      </div>

      <nav className="mt-4 flex-1 overflow-y-auto">
        {visibleItems.map((item) => {
          const Icon = item.icon;
          const isActive = location.pathname === item.path;
          return (
            <Link
              key={item.path}
              to={item.path}
              className="flex items-center gap-3 px-6 py-3 transition-colors"
              style={{
                color: isActive ? '#FFFFFF' : '#9AA3AB',
                background: isActive ? 'rgba(14,110,104,0.22)' : 'transparent',
                borderLeft: isActive ? '3px solid var(--mn-teal)' : '3px solid transparent',
              }}
              onMouseEnter={(e) => { if (!isActive) e.currentTarget.style.color = '#E8EAEC'; }}
              onMouseLeave={(e) => { if (!isActive) e.currentTarget.style.color = '#9AA3AB'; }}
            >
              <Icon className="w-5 h-5" />
              <span className="font-medium text-sm">{item.label}</span>
            </Link>
          );
        })}
      </nav>

      {isAdmin && !isImpersonating && (
        <div className="px-4 pb-3">
          <p className="mn-kicker mb-1" style={{ color: '#8B949C' }}>
            <UserRoundSearch className="w-3 h-3 inline mr-1" />
            View as
          </p>
          <select
            onChange={handleSwitch}
            disabled={switching}
            defaultValue=""
            className="w-full text-sm px-2 py-2"
            style={{ background: '#262C32', color: '#E8EAEC', border: '1px solid #2A3138', borderRadius: 2 }}
          >
            <option value="">{switching ? 'Switching…' : 'Choose an account…'}</option>
            {targets.map((t) => (
              <option key={t.userId} value={t.userId}>
                {t.displayName} — {t.role}
              </option>
            ))}
          </select>
        </div>
      )}

      <div className="p-4" style={{ borderTop: '1px solid #2A3138' }}>
        {user && (
          <div className="flex items-center justify-between gap-2">
            <div className="min-w-0">
              <p className="text-sm font-medium truncate text-white">{user.displayName || user.username}</p>
              <p className="mn-kicker" style={{ color: 'var(--mn-teal)', filter: 'brightness(1.6)' }}>{user.role}</p>
            </div>
            <button
              onClick={() => setShowPw(true)}
              title="Change password"
              className="p-2 transition-colors"
              style={{ color: '#9AA3AB', borderRadius: 2 }}
              onMouseEnter={(e) => { e.currentTarget.style.color = '#fff'; e.currentTarget.style.background = '#262C32'; }}
              onMouseLeave={(e) => { e.currentTarget.style.color = '#9AA3AB'; e.currentTarget.style.background = 'transparent'; }}
            >
              <KeyRound className="w-5 h-5" />
            </button>
            <button
              onClick={logout}
              title="Sign out"
              className="p-2 transition-colors"
              style={{ color: '#9AA3AB', borderRadius: 2 }}
              onMouseEnter={(e) => { e.currentTarget.style.color = '#fff'; e.currentTarget.style.background = '#262C32'; }}
              onMouseLeave={(e) => { e.currentTarget.style.color = '#9AA3AB'; e.currentTarget.style.background = 'transparent'; }}
            >
              <LogOut className="w-5 h-5" />
            </button>
          </div>
        )}
      </div>

      {showPw && (
        <div className="fixed inset-0 flex items-center justify-center z-50" style={{ background: 'rgba(32,38,43,0.55)' }}>
          <div className="mn-card mn-accent p-6 w-full max-w-sm mn-scope" style={{ background: 'var(--mn-surface)' }}>
            <h2 className="text-lg font-bold mb-4" style={{ color: 'var(--mn-ink)' }}>Change password</h2>
            <form onSubmit={submitPw} className="space-y-3">
              <input type="password" required placeholder="Current password" value={pwForm.current}
                onChange={(e) => setPwForm({ ...pwForm, current: e.target.value })}
                className="w-full border px-3 py-2" />
              <input type="password" required minLength={8} placeholder="New password (min 8 chars)" value={pwForm.next}
                onChange={(e) => setPwForm({ ...pwForm, next: e.target.value })}
                className="w-full border px-3 py-2" />
              <input type="password" required placeholder="Confirm new password" value={pwForm.confirm}
                onChange={(e) => setPwForm({ ...pwForm, confirm: e.target.value })}
                className="w-full border px-3 py-2" />
              <div className="flex justify-end gap-2 pt-1">
                <button type="button" onClick={() => setShowPw(false)} className="mn-btn mn-btn-quiet mn-btn-sm">Cancel</button>
                <button type="submit" disabled={pwBusy} className="mn-btn mn-btn-primary mn-btn-sm">
                  {pwBusy ? 'Saving…' : 'Change password'}
                </button>
              </div>
            </form>
          </div>
        </div>
      )}
    </div>
  );
}
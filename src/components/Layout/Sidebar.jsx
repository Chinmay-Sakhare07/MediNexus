import { Link, useLocation } from 'react-router-dom';
import {
  LayoutDashboard,
  Users,
  Calendar,
  DollarSign,
  Stethoscope,
  Shield,
  Activity,
  LogOut, FlaskConical, Pill, CalendarClock } from 'lucide-react';
import { useAuth } from '../../context/AuthContext';
import { canAccess } from '../../auth/permissions';

const MENU = [
  { path: '/', module: 'dashboard', icon: LayoutDashboard, label: 'Dashboard' },
  { path: '/doctors', module: 'doctors', icon: Stethoscope, label: 'Doctors' },
  { path: '/patients', module: 'patients', icon: Users, label: 'Patients' },
  { path: '/insurance', module: 'insurance', icon: Shield, label: 'Insurance' },
  { path: '/appointments', module: 'appointments', icon: Calendar, label: 'Appointments' },
  { path: '/billing', module: 'billing', icon: DollarSign, label: 'Billing' },
  { path: '/lab', label: 'Lab', module: 'lab', icon: FlaskConical },
  { path: '/pharmacy', label: 'Pharmacy', module: 'pharmacy', icon: Pill },
  { path: '/schedule', label: 'My Schedule', module: 'schedule', icon: CalendarClock },
];

export default function Sidebar() {
  const location = useLocation();
  const { user, logout } = useAuth();

  const visibleItems = MENU.filter((item) => user && canAccess(user.role, item.module));

  return (
    <div className="w-64 bg-gradient-to-b from-blue-900 to-blue-800 text-white h-screen fixed left-0 top-0 shadow-xl flex flex-col">
      <div className="p-6 border-b border-blue-700">
        <div className="flex items-center gap-3">
          <Activity className="w-10 h-10 text-blue-300" />
          <div>
            <h1 className="text-xl font-bold">Group Six</h1>
            <p className="text-xs text-blue-300">Multispeciality Hospital</p>
          </div>
        </div>
      </div>

      <nav className="mt-6 flex-1 overflow-y-auto">
        {visibleItems.map((item) => {
          const Icon = item.icon;
          const isActive = location.pathname === item.path;

          return (
            <Link
              key={item.path}
              to={item.path}
              className={`flex items-center gap-3 px-6 py-3 transition-all ${
                isActive
                  ? 'bg-blue-700 border-r-4 border-blue-300 text-white'
                  : 'text-blue-200 hover:bg-blue-800 hover:text-white'
              }`}
            >
              <Icon className="w-5 h-5" />
              <span className="font-medium">{item.label}</span>
            </Link>
          );
        })}
      </nav>

      <div className="p-4 border-t border-blue-700">
        {user && (
          <div className="flex items-center justify-between gap-2">
            <div className="min-w-0">
              <p className="text-sm font-medium truncate">{user.displayName || user.username}</p>
              <p className="text-xs text-blue-300">{user.role}</p>
            </div>
            <button
              onClick={logout}
              title="Sign out"
              className="p-2 rounded-lg text-blue-200 hover:bg-blue-700 hover:text-white transition-colors"
            >
              <LogOut className="w-5 h-5" />
            </button>
          </div>
        )}
        <p className="text-[10px] text-blue-400 text-center mt-3">© 2025 Project by Team Six</p>
      </div>
    </div>
  );
}

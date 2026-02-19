import { Link, useLocation } from 'react-router-dom';
import { 
  LayoutDashboard, 
  Users, 
  Calendar, 
  DollarSign, 
  Stethoscope,
  Shield,
  Activity
} from 'lucide-react';

const menuItems = [
  { path: '/', icon: LayoutDashboard, label: 'Dashboard' },
  { path: '/doctors', icon: Stethoscope, label: 'Doctors' },
  { path: '/patients', icon: Users, label: 'Patients' },
  { path: '/insurance', icon: Shield, label: 'Insurance' },
  { path: '/appointments', icon: Calendar, label: 'Appointments' },
  { path: '/billing', icon: DollarSign, label: 'Billing' },
];

export default function Sidebar() {
  const location = useLocation();

  return (
    <div className="w-64 bg-gradient-to-b from-blue-900 to-blue-800 text-white h-screen fixed left-0 top-0 shadow-xl">
      <div className="p-6 border-b border-blue-700">
        <div className="flex items-center gap-3">
          <Activity className="w-10 h-10 text-blue-300" />
          <div>
            <h1 className="text-xl font-bold">Group Six</h1>
            <p className="text-xs text-blue-300">Multispeciality Hospital</p>
          </div>
        </div>
      </div>
      
      <nav className="mt-6">
        {menuItems.map((item) => {
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

      <div className="absolute bottom-0 left-0 right-0 p-6 border-t border-blue-700">
        <p className="text-xs text-blue-300 text-center">
          © 2025 Project by Team Six
        </p>
      </div>
    </div>
  );
}
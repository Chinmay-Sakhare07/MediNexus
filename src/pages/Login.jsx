import { useState } from 'react';
import { useNavigate, Navigate } from 'react-router-dom';
import { Activity, LogIn, ChevronDown, ChevronUp } from 'lucide-react';
import { useAuth } from '../context/AuthContext';

const DEMO_ACCOUNTS = [
  { username: 'reception', role: 'Receptionist' },
  { username: 'dr.sharma', role: 'Doctor' },
  { username: 'nurse.anderson', role: 'Nurse' },
  { username: 'lab.kumar', role: 'Lab Technician' },
  { username: 'pharmacist', role: 'Pharmacist' },
  { username: 'patient.shah', role: 'Patient' },
];

export default function Login() {
  const { user, login } = useAuth();
  const navigate = useNavigate();
  const [loginId, setLoginId] = useState('');
  const [password, setPassword] = useState('');
  const [error, setError] = useState('');
  const [loading, setLoading] = useState(false);
  const [showDemo, setShowDemo] = useState(false);

  if (user) return <Navigate to="/" replace />;

  const handleSubmit = async (e) => {
    e.preventDefault();
    setError('');
    setLoading(true);
    try {
      await login(loginId.trim(), password);
      navigate('/', { replace: true });
    } catch (err) {
      const status = err?.response?.status;
      if (status === 429) {
        setError('Too many attempts. Please wait a minute and try again.');
      } else if (status === 401) {
        setError('Invalid username or password.');
      } else {
        setError(err?.response?.data?.message || 'Could not sign in. The server may be waking up — try again in a moment.');
      }
    } finally {
      setLoading(false);
    }
  };

  const fillDemo = (username) => {
    setLoginId(username);
    setPassword('MediNexus@2026');
    setShowDemo(false);
  };

  return (
    <div className="min-h-screen flex items-center justify-center bg-gradient-to-br from-blue-900 via-blue-800 to-blue-600 p-4">
      <div className="w-full max-w-md">
        <div className="bg-white rounded-2xl shadow-2xl p-8">
          <div className="flex items-center gap-3 mb-8 justify-center">
            <Activity className="w-10 h-10 text-blue-700" />
            <div>
              <h1 className="text-2xl font-bold text-gray-800">MediNexus</h1>
              <p className="text-xs text-gray-500">Group Six Multispeciality Hospital</p>
            </div>
          </div>

          <form onSubmit={handleSubmit} className="space-y-4">
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">
                Username or email
              </label>
              <input
                type="text"
                value={loginId}
                onChange={(e) => setLoginId(e.target.value)}
                autoComplete="username"
                autoFocus
                required
                className="w-full px-4 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-blue-500 focus:border-blue-500 outline-none"
                placeholder="e.g. reception"
              />
            </div>

            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">
                Password
              </label>
              <input
                type="password"
                value={password}
                onChange={(e) => setPassword(e.target.value)}
                autoComplete="current-password"
                required
                className="w-full px-4 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-blue-500 focus:border-blue-500 outline-none"
                placeholder="••••••••"
              />
            </div>

            {error && (
              <div className="bg-red-50 border border-red-200 text-red-700 text-sm rounded-lg px-4 py-3">
                {error}
              </div>
            )}

            <button
              type="submit"
              disabled={loading}
              className="w-full flex items-center justify-center gap-2 bg-blue-700 hover:bg-blue-800 disabled:opacity-60 text-white font-medium py-2.5 rounded-lg transition-colors"
            >
              <LogIn className="w-4 h-4" />
              {loading ? 'Signing in…' : 'Sign in'}
            </button>
          </form>

          <div className="mt-6 border-t pt-4">
            <button
              type="button"
              onClick={() => setShowDemo((v) => !v)}
              className="w-full flex items-center justify-center gap-1 text-sm text-blue-700 hover:text-blue-900"
            >
              Demo accounts {showDemo ? <ChevronUp className="w-4 h-4" /> : <ChevronDown className="w-4 h-4" />}
            </button>
            {showDemo && (
              <div className="mt-3 grid grid-cols-2 gap-2">
                {DEMO_ACCOUNTS.map((a) => (
                  <button
                    key={a.username}
                    type="button"
                    onClick={() => fillDemo(a.username)}
                    className="text-left px-3 py-2 rounded-lg border border-gray-200 hover:border-blue-400 hover:bg-blue-50 transition-colors"
                  >
                    <p className="text-sm font-medium text-gray-800">{a.username}</p>
                    <p className="text-xs text-gray-500">{a.role}</p>
                  </button>
                ))}
                <p className="col-span-2 text-xs text-gray-400 text-center mt-1">
                  Tap an account to pre-fill its credentials.
                </p>
              </div>
            )}
          </div>
        </div>
      </div>
    </div>
  );
}

import { createContext, useContext, useState } from 'react';
import { login as apiLogin, impersonateUser } from '../services/api';

const AuthContext = createContext(null);

const readJson = (key) => {
  try {
    const raw = localStorage.getItem(key);
    return raw ? JSON.parse(raw) : null;
  } catch {
    return null;
  }
};

export function AuthProvider({ children }) {
  const [user, setUser] = useState(() => readJson('mn_user'));
  const [adminUser, setAdminUser] = useState(() => readJson('mn_admin_user'));

  const login = async (loginId, password) => {
    const res = await apiLogin({ login: loginId, password });
    const payload = res.data?.data; // ApiResponse<LoginResponse>
    if (!payload?.token) {
      throw new Error(res.data?.message || 'Login failed');
    }
    localStorage.setItem('mn_token', payload.token);
    localStorage.setItem('mn_user', JSON.stringify(payload.user));
    setUser(payload.user);
    return payload.user;
  };

  // Admin account switching: stash the admin session, adopt the target's.
  const impersonate = async (userId) => {
    const res = await impersonateUser(userId);
    const payload = res.data?.data;
    if (!payload?.token) throw new Error(res.data?.message || 'Could not switch account');

    localStorage.setItem('mn_admin_token', localStorage.getItem('mn_token') || '');
    localStorage.setItem('mn_admin_user', localStorage.getItem('mn_user') || '');
    setAdminUser(readJson('mn_user'));

    localStorage.setItem('mn_token', payload.token);
    localStorage.setItem('mn_user', JSON.stringify(payload.user));
    setUser(payload.user);
    return payload.user;
  };

  const returnToAdmin = () => {
    const token = localStorage.getItem('mn_admin_token');
    const stored = localStorage.getItem('mn_admin_user');
    if (!token || !stored) return;
    localStorage.setItem('mn_token', token);
    localStorage.setItem('mn_user', stored);
    localStorage.removeItem('mn_admin_token');
    localStorage.removeItem('mn_admin_user');
    setUser(JSON.parse(stored));
    setAdminUser(null);
  };

  const logout = () => {
    ['mn_token', 'mn_user', 'mn_admin_token', 'mn_admin_user'].forEach((k) =>
      localStorage.removeItem(k));
    setUser(null);
    setAdminUser(null);
  };

  const isImpersonating = !!adminUser;

  return (
    <AuthContext.Provider value={{ user, login, logout, impersonate, returnToAdmin, isImpersonating, adminUser }}>
      {children}
    </AuthContext.Provider>
  );
}

export const useAuth = () => {
  const ctx = useContext(AuthContext);
  if (!ctx) throw new Error('useAuth must be used inside <AuthProvider>');
  return ctx;
};

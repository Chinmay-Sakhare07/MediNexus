import { createContext, useContext, useState } from 'react';
import { login as apiLogin } from '../services/api';

const AuthContext = createContext(null);

const readStoredUser = () => {
  try {
    const raw = localStorage.getItem('mn_user');
    return raw ? JSON.parse(raw) : null;
  } catch {
    return null;
  }
};

export function AuthProvider({ children }) {
  const [user, setUser] = useState(readStoredUser);

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

  const logout = () => {
    localStorage.removeItem('mn_token');
    localStorage.removeItem('mn_user');
    setUser(null);
  };

  return (
    <AuthContext.Provider value={{ user, login, logout }}>
      {children}
    </AuthContext.Provider>
  );
}

export const useAuth = () => {
  const ctx = useContext(AuthContext);
  if (!ctx) throw new Error('useAuth must be used inside <AuthProvider>');
  return ctx;
};

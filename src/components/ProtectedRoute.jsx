import { Navigate, Outlet } from 'react-router-dom';
import { useAuth } from '../context/AuthContext';
import { canAccess } from '../auth/permissions';
import Layout from './Layout/Layout';

// Gate for everything behind login: no user -> /login.
// Renders the app chrome (sidebar) around child routes.
export function ProtectedLayout() {
  const { user } = useAuth();
  if (!user) return <Navigate to="/login" replace />;
  return (
    <Layout>
      <Outlet />
    </Layout>
  );
}

// Per-module role gate. Sends users without access back to the dashboard,
// which every role can see.
export function RequireModule({ module, children }) {
  const { user } = useAuth();
  if (!user) return <Navigate to="/login" replace />;
  if (!canAccess(user.role, module)) return <Navigate to="/" replace />;
  return children;
}

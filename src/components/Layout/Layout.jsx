import { useEffect, useState } from 'react';
import Sidebar from './Sidebar';
import { useAuth } from '../../context/AuthContext';

export default function Layout({ children }) {
  const [waking, setWaking] = useState(false);
  const { isImpersonating, user, adminUser, returnToAdmin } = useAuth();

  useEffect(() => {
    let timer;
    const onWaking = () => {
      setWaking(true);
      clearTimeout(timer);
      timer = setTimeout(() => setWaking(false), 10000);
    };
    window.addEventListener('mn:server-waking', onWaking);
    return () => {
      window.removeEventListener('mn:server-waking', onWaking);
      clearTimeout(timer);
    };
  }, []);

  const handleReturn = () => {
    returnToAdmin();
    window.location.href = '/';
  };

  return (
    <div className="flex mn-scope">
      <Sidebar />
      <div className="ml-64 flex-1 min-h-screen" style={{ background: 'var(--mn-paper)' }}>
        {isImpersonating && (
          <div className="mn-banner-amber text-sm px-8 py-2 flex items-center justify-between">
            <span>
              Viewing as <strong>{user?.displayName || user?.username}</strong> ({user?.role})
              {adminUser ? ` — signed in as ${adminUser.displayName || adminUser.username}` : ''}
            </span>
            <button onClick={handleReturn} className="mn-btn mn-btn-quiet mn-btn-sm">
              Return to admin
            </button>
          </div>
        )}
        {waking && (
          <div className="mn-banner-amber text-sm px-8 py-2">
            The server is waking up (free hosting) — retrying automatically, this can take up to a minute…
          </div>
        )}
        <main className="p-8">
          {children}
        </main>
      </div>
    </div>
  );
}

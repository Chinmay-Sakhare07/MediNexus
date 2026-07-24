import { useEffect, useState } from 'react';
import Sidebar from './Sidebar';

export default function Layout({ children }) {
  const [waking, setWaking] = useState(false);

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

  return (
    <div className="flex">
      <Sidebar />
      <div className="ml-64 flex-1 min-h-screen bg-gray-50">
        {waking && (
          <div className="bg-amber-100 border-b border-amber-300 text-amber-800 text-sm px-8 py-2">
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

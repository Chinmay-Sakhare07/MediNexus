import { useState, useEffect } from 'react';
import { Users, Stethoscope, Calendar, DollarSign } from 'lucide-react';
import { getDashboard } from '../services/api';

export default function Dashboard() {
  const [stats, setStats] = useState(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    loadDashboard();
  }, []);

  const loadDashboard = async () => {
    try {
      const response = await getDashboard();
      setStats(response.data.data);
    } catch (error) {
      console.error('Error loading dashboard:', error);
    } finally {
      setLoading(false);
    }
  };

  if (loading) {
    return <div className="flex justify-center items-center h-64">Loading...</div>;
  }

  const cards = [
    { title: 'Total Patients', value: stats?.totalPatients || 0, icon: Users, color: 'blue' },
    { title: 'Total Doctors', value: stats?.totalDoctors || 0, icon: Stethoscope, color: 'green' },
    { title: 'Today\'s Appointments', value: stats?.todayAppointments || 0, icon: Calendar, color: 'purple' },
    { title: 'Pending Bills', value: stats?.pendingBills || 0, icon: DollarSign, color: 'orange' },
  ];

  return (
    <div>
      <h1 className="text-3xl font-bold text-gray-800 mb-8">Dashboard</h1>
      
      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-6 mb-8">
        {cards.map((card) => {
          const Icon = card.icon;
          return (
            <div key={card.title} className="bg-white rounded-lg shadow-md p-6 hover:shadow-lg transition-shadow">
              <div className="flex items-center justify-between">
                <div>
                  <p className="text-gray-500 text-sm">{card.title}</p>
                  <p className="text-3xl font-bold text-gray-800 mt-2">{card.value}</p>
                </div>
                <div className={`p-3 rounded-full bg-${card.color}-100`}>
                  <Icon className={`w-8 h-8 text-${card.color}-600`} />
                </div>
              </div>
            </div>
          );
        })}
      </div>

      <div className="bg-white rounded-lg shadow-md p-6">
        <h2 className="text-xl font-bold text-gray-800 mb-4">Revenue Overview</h2>
        <div className="text-center py-8">
          <p className="text-gray-500 mb-2">Total Revenue</p>
          <p className="text-4xl font-bold text-green-600">
            ${stats?.totalRevenue?.toLocaleString() || 0}
          </p>
        </div>
      </div>
    </div>
  );
}
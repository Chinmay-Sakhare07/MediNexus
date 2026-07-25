import { BrowserRouter as Router, Routes, Route } from 'react-router-dom';
import { AuthProvider } from './context/AuthContext';
import { ProtectedLayout, RequireModule } from './components/ProtectedRoute';
import Login from './pages/Login';
import Dashboard from './pages/Dashboard';
import Doctors from './pages/Doctors';
import Patients from './pages/Patients';
import Insurance from './pages/Insurance';
import Appointments from './pages/Appointments';
import Billing from './pages/Billing';
import File from './pages/File';
import Pharmacy from './pages/Pharmacy';
import Lab from './pages/Lab';
import Schedule from './pages/Schedule';
import UsersAdmin from './pages/UsersAdmin';

function App() {
  return (
    <AuthProvider>
      <Router>
        <Routes>
          <Route path="/login" element={<Login />} />

          <Route element={<ProtectedLayout />}>
            <Route path="/" element={<Dashboard />} />
            <Route path="/doctors" element={<RequireModule module="doctors"><Doctors /></RequireModule>} />
            <Route path="/patients" element={<RequireModule module="patients"><Patients /></RequireModule>} />
            <Route path="/insurance" element={<RequireModule module="insurance"><Insurance /></RequireModule>} />
            <Route path="/appointments" element={<RequireModule module="appointments"><Appointments /></RequireModule>} />
            <Route path="/billing" element={<RequireModule module="billing"><Billing /></RequireModule>} />
            <Route path="/files/:appointmentId" element={<File />} />
            <Route path="/lab" element={<RequireModule module="lab"><Lab /></RequireModule>} />
            <Route path="/pharmacy" element={<RequireModule module="pharmacy"><Pharmacy /></RequireModule>} />
            <Route path="/schedule" element={<RequireModule module="schedule"><Schedule /></RequireModule>} />
            <Route path="/users" element={<RequireModule module="users"><UsersAdmin /></RequireModule>} />
          </Route>
        </Routes>
      </Router>
    </AuthProvider>
  );
}

export default App;

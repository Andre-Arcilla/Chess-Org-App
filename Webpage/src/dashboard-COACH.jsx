import React, { useEffect, useState } from 'react';
import { createRoot } from 'react-dom/client';
import { HashRouter, Routes, Route, Navigate } from 'react-router-dom';
import { supabase } from './db';
import DashboardLayout from './components/DashboardLayout-COACH';
import Overview from './pages-COACH/Overview';
import Members from './pages-COACH/Members';
import Registrations from './pages-COACH/Registrations';
import Tournaments from './pages-COACH/Tournaments';
import Announcements from './pages-COACH/Announcements';

const Bouncer = ({ children }) => {
  const [loading, setLoading] = useState(true);
  const [authorized, setAuthorized] = useState(false); 
  const [adminData, setAdminData] = useState(null);

  useEffect(() => {
    const checkSession = async () => {
      try {
        const storedUser = localStorage.getItem('currentUser');
        
        if (!storedUser) {
          window.location.href = '/index.html';
          return;
        }

        const profile = JSON.parse(storedUser);

        // Verify user has Admin role
        // if (profile.Role !== 'Admin') {
        //   console.error('Unauthorized access');
        //   localStorage.removeItem('currentUser');
        //   window.location.href = '/index.html';
        //   return;
        // }

        setAdminData(profile);
        setAuthorized(true);
      } catch (err) {
        console.error('Bouncer error:', err);
        window.location.href = '/index.html';
      } finally {
        setLoading(false);
      }
    };

    checkSession();
  }, []);

  if (loading) {
    return (
      <div className="overlay">
        <div className="spinner"></div>
        <p>Verifying Grandmaster Status...</p>
      </div>
    );
  }

  if (!authorized) return null;

  return React.cloneElement(children, { adminData });
};

const App = () => {
  return (
    <HashRouter>
      <Routes>
        {/* FIX: Bouncer MUST tightly wrap DashboardLayout like a burrito */}
        <Route 
          path="/" 
          element={
            <Bouncer>
              <DashboardLayout />
            </Bouncer>
          }
        >
          <Route index element={<Overview />} />
          <Route path="members" element={<Members />} />
          <Route path="registrations" element={<Registrations />} />
          <Route path="tournaments" element={<Tournaments />} />
          <Route path="announcements" element={<Announcements />} />
          <Route path="*" element={<Navigate to="/" replace />} />
        </Route>
      </Routes>
    </HashRouter>
  );
};

const container = document.getElementById('root');
const root = createRoot(container);
root.render(<App />);

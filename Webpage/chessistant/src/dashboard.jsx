import React, { useEffect, useState } from 'react';
import { createRoot } from 'react-dom/client';
import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom';
import { supabase } from './db';
import DashboardLayout from './components/DashboardLayout';
import Overview from './pages/Overview';
import Members from './pages/Members';
import Registrations from './pages/Registrations';
import Tournaments from './pages/Tournaments';
import OrgRoster from './pages/OrgRoster';
import Announcements from './pages/Announcements';

const Bouncer = ({ children }) => {
  const [loading, setLoading] = useState(true);
  const [authorized, setAuthorized] = useState(false);
  const [adminData, setAdminData] = useState(null);

  useEffect(() => {
    const checkSession = async () => {
      try {
        const { data: { session } } = await supabase.auth.getSession();
        if (!session) {
          window.location.href = '/index.html';
          return;
        }

        const { data: profile, error } = await supabase
          .schema('Chessistant')
          .from('Profiles')
          .select('UserID, StudName, Role, StudNum') // <--- Added StudNum!
          .eq('Email', session.user.email)
          .single();

        if (error || profile?.Role !== 'Admin') {
          console.error('Unauthorized access');
          await supabase.auth.signOut();
          window.location.href = '/index.html';
          return;
        }

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
    <BrowserRouter>
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
          <Route path="roster" element={<OrgRoster />} />
          <Route path="announcements" element={<Announcements />} />
          <Route path="*" element={<Navigate to="/" replace />} />
        </Route>
      </Routes>
    </BrowserRouter>
  );
};

const container = document.getElementById('root');
const root = createRoot(container);
root.render(<App />);

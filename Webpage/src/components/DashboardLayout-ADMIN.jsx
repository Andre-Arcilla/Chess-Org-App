import React from 'react';
import { Link, NavLink, Outlet, useNavigate } from 'react-router-dom';
import { supabase } from '../db';

const DashboardLayout = ({ adminData, setAdminData }) => {
  const navigate = useNavigate();

  const handleLogout = async () => {
    await supabase.auth.signOut();
    window.location.href = '/index.html';
    localStorage.removeItem('currentUser');
  };

  return (
    <div className="dashboard-layout">
      <aside className="top-bar">
        <NavLink to="/" className="brand" style={{ all: 'unset', cursor: 'pointer', display: 'flex', flexDirection: 'row', alignItems: 'center', gap: '5px', height: '100%' }}>
          <img src="src/assets/chess-club-logo.png" alt="Chess Logo" className="logo" />
          <h1>Chessistant</h1>
        </NavLink>
        
        <nav>
          <NavLink to="/" end className={({ isActive }) => isActive ? 'active' : ''}>
            Overview
          </NavLink>
          <NavLink to="/members" className={({ isActive }) => isActive ? 'active' : ''}>
            Members
          </NavLink>
          <NavLink to="/registrations" className={({ isActive }) => isActive ? 'active' : ''}>
            Registrations
          </NavLink>
          <NavLink to="/tournaments" className={({ isActive }) => isActive ? 'active' : ''}>
            Tournaments
          </NavLink>
          <NavLink to="/announcements" className={({ isActive }) => isActive ? 'active' : ''}>
            Announcements
          </NavLink>
        </nav>
        <div className="sidebar-footer">
          <button onClick={handleLogout} className="btn-link">Sign out</button>
        </div>
      </aside>

      <main className="main-content">
        <section className="content-body">
          <Outlet context={{ adminData, setAdminData }} />
        </section>
      </main>
    </div>
  );
};

export default DashboardLayout;
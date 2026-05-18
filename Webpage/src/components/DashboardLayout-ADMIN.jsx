import React from 'react';
import { NavLink, Outlet, useNavigate } from 'react-router-dom';
import { supabase } from '../db';

const DashboardLayout = ({ adminData }) => {
  const navigate = useNavigate();

  const handleLogout = async () => {
    await supabase.auth.signOut();
    window.location.href = '/index.html';
  };

  return (
    <div className="dashboard-layout">
      <aside className="top-bar">
        <div className="brand">
            <img src="src/assets/chess-club-logo.png" alt="Chess Logo" class="logo"></img>
            <h1>Chessistant</h1>
        </div>
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
          {/* <NavLink to="/roster" className={({ isActive }) => isActive ? 'active' : ''}>
            Org Roster
          </NavLink> */}
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
          <Outlet context={{ adminData }} />
        </section>
      </main>
    </div>
  );
};

export default DashboardLayout;

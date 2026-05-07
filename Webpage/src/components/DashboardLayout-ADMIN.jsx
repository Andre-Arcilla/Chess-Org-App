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
      <aside className="sidebar">
        <div className="brand">
          <h1>Chessistant</h1>
        </div>
        <nav>
          <ul>
            <li>
              <NavLink to="/" end className={({ isActive }) => isActive ? 'active' : ''}>
                Overview
              </NavLink>
            </li>
            <li>
              <NavLink to="/members" className={({ isActive }) => isActive ? 'active' : ''}>
                Members
              </NavLink>
            </li>
            <li>
              <NavLink to="/registrations" className={({ isActive }) => isActive ? 'active' : ''}>
                Registrations
              </NavLink>
            </li>
            <li>
              <NavLink to="/roster" className={({ isActive }) => isActive ? 'active' : ''}>
                Org Roster
              </NavLink>
            </li>
            <li>
              <NavLink to="/tournaments" className={({ isActive }) => isActive ? 'active' : ''}>
                Tournaments
              </NavLink>
            </li>
            <li>
              <NavLink to="/announcements" className={({ isActive }) => isActive ? 'active' : ''}>
                Announcements
              </NavLink>
            </li>
          </ul>
        </nav>
        <div className="sidebar-footer">
          <button onClick={handleLogout} className="btn-link">Leave Club</button>
        </div>
      </aside>

      <main className="main-content">
        <header className="top-header">
          <div className="welcome-text">
            <h2>Welcome back, <span className="admin-highlight">{adminData?.StudName || 'Admin'}</span></h2>
          </div>
          <div className="badge-area">
            <span className="role-tag">Grandmaster Admin</span>
          </div>
        </header>

        <section className="content-body">
          <Outlet context={{ adminData }} />
        </section>
      </main>
    </div>
  );
};

export default DashboardLayout;

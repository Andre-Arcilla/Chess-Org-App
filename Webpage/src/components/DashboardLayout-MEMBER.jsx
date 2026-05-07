import React from 'react';
import { NavLink, Outlet } from 'react-router-dom';

const MemberDashboardLayout = ({ memberData }) => {
  const handleLogout = () => {
    localStorage.removeItem('currentUser');
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
                Home
              </NavLink>
            </li>
            <li>
              <NavLink to="/profile" className={({ isActive }) => isActive ? 'active' : ''}>
                My Profile
              </NavLink>
            </li>
            <li>
              <NavLink to="/tournaments" className={({ isActive }) => isActive ? 'active' : ''}>
                Tournaments
              </NavLink>
            </li>
          </ul>
        </nav>
        <div className="sidebar-footer">
          <button onClick={handleLogout} className="btn-link">Sign Out</button>
        </div>
      </aside>

      <main className="main-content">
        <header className="top-header">
          <div className="welcome-text">
            <h2>Welcome, <span className="admin-highlight">{memberData?.StudName || 'Member'}</span></h2>
          </div>
          <div className="badge-area">
            <span className="role-tag">Member</span>
          </div>
        </header>

        <section className="content-body">
          <Outlet context={{ memberData }} />
        </section>
      </main>
    </div>
  );
};

export default MemberDashboardLayout;

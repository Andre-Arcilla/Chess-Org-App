import React, { useState, useEffect, useRef } from 'react';
import { NavLink, Outlet, useNavigate } from 'react-router-dom';
import { supabase } from '../db';

const DashboardLayout = ({ adminData, setAdminData }) => {
  const navigate = useNavigate();
  const [menuOpen, setMenuOpen] = useState(false);
  const menuRef = useRef(null);

  const handleLogout = async () => {
    await supabase.auth.signOut();
    localStorage.removeItem('currentUser');
    window.location.href = '/index.html';
  };

  // Close dropdown when clicking outside
  useEffect(() => {
    const handleClickOutside = (e) => {
      if (menuRef.current && !menuRef.current.contains(e.target)) {
        setMenuOpen(false);
      }
    };
    document.addEventListener('mousedown', handleClickOutside);
    return () => document.removeEventListener('mousedown', handleClickOutside);
  }, []);

  const initial = adminData?.StudName?.charAt(0)?.toUpperCase() ?? '?';

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

        {/* Avatar with dropdown */}
        <div ref={menuRef} style={{ position: 'relative', height: '100%', display: 'flex', alignItems: 'center' }}>
          {/* Avatar button */}
          <button
            onClick={() => setMenuOpen(prev => !prev)}
            style={{
              all: 'unset',
              height: '100%',
              aspectRatio: '1/1',
              borderRadius: '500px',
              background: menuOpen
                ? 'linear-gradient(135deg, var(--oak), var(--gold-muted))'
                : 'linear-gradient(135deg, var(--mahogany), var(--oak))',
              border: `3px solid ${menuOpen ? 'var(--gold)' : 'rgba(212,175,55,0.5)'}`,
              display: 'flex',
              alignItems: 'center',
              justifyContent: 'center',
              fontSize: '1.5rem',
              color: '#fff',
              fontWeight: 'bold',
              cursor: 'pointer',
              userSelect: 'none',
              transition: 'all 0.25s ease',
              boxShadow: menuOpen ? '0 0 0 4px rgba(212,175,55,0.25)' : 'none',
              flexShrink: 0,
            }}
            aria-label="Account menu"
            aria-expanded={menuOpen}
          >
            {initial}
          </button>

          {/* Dropdown panel */}
          {menuOpen && (
            <div className='dropdown-panel' style={{
              position: 'absolute',
              top: 'calc(100% + 8px)',
              right: 0,
              width: '250px',
              background: '#fff',
              borderRadius: '12px',
              boxShadow: '0 12px 40px rgba(0,0,0,0.35)',
              border: '2px solid var(--gold)',
              overflow: 'hidden',
              zIndex: 500,
              animation: 'dropdownFadeIn 0.15s ease',
            }}>
              {/* User info header */}
              <div style={{
                padding: '14px 18px',
                background: 'linear-gradient(135deg, var(--mahogany), var(--oak))',
                borderBottom: '2px solid var(--gold)',
              }}>
                <div style={{ color: 'var(--gold)', fontSize: '0.7rem', fontWeight: 700, textTransform: 'uppercase', letterSpacing: '1.5px' }}>
                  Signed in as
                </div>
                <div style={{ color: '#fff', fontWeight: 700, fontSize: '1rem', marginTop: '3px', overflow: 'hidden', textOverflow: 'ellipsis', whiteSpace: 'nowrap' }}>
                  {adminData?.StudName ?? '—'}
                </div>
                <div style={{ color: 'rgba(255,255,255,0.6)', fontSize: '0.8rem', marginTop: '2px', overflow: 'hidden', textOverflow: 'ellipsis', whiteSpace: 'nowrap' }}>
                  {adminData?.Email ?? ''}
                </div>
              </div>

              {/* My Profile */}
              <NavLink
                to="/profile"
                onClick={() => setMenuOpen(false)}
                style={({ isActive }) => ({
                  display: 'block',
                  padding: '12px 18px',
                  color: isActive ? 'var(--oak)' : 'var(--text)',
                  background: isActive ? '#fff0f0' : 'transparent',
                  fontWeight: isActive ? 700 : 500,
                  fontSize: '0.95rem',
                  textDecoration: 'none',
                  letterSpacing: '0.5px',
                  transition: 'background 0.15s',
                  borderRadius: '0',
                  textTransform: 'none'
                })}
                onMouseEnter={e => { e.currentTarget.style.background = '#fff0f0'; }}
                onMouseLeave={e => { e.currentTarget.style.background = ''; }}
              >
                My Profile
              </NavLink>

              {/* Sign Out */}
              <button
                onClick={handleLogout}
                style={{
                  all: 'unset',
                  display: 'block',
                  width: '100%',
                  padding: '12px 18px',
                  color: 'var(--error)',
                  fontWeight: 600,
                  fontSize: '0.95rem',
                  cursor: 'pointer',
                  boxSizing: 'border-box',
                  letterSpacing: '0.5px',
                  transition: 'background 0.15s',
                }}
                onMouseEnter={e => e.currentTarget.style.background = '#fff0f0'}
                onMouseLeave={e => e.currentTarget.style.background = ''}
              >
                Sign Out
              </button>
            </div>
          )}
        </div>
      </aside>

      {/* Dropdown animation keyframe — injected once */}
      <style>{`
        @keyframes dropdownFadeIn {
          from { opacity: 0; transform: translateY(-6px); }
          to   { opacity: 1; transform: translateY(0); }
        }
      `}</style>

      <main className="main-content">
        <section className="content-body">
          <Outlet context={{ adminData, setAdminData }} />
        </section>
      </main>
    </div>
  );
};

export default DashboardLayout;
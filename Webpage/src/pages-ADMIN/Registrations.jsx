import React, { useEffect, useState, useMemo } from 'react';
import { supabase } from '../db';

const Registrations = () => {
  const [registrations, setRegistrations] = useState([]);
  const [loading, setLoading] = useState(true);

  // NEW: State to track the active sort column and direction
  const [sortConfig, setSortConfig] = useState({ key: null, direction: 'ascending' });

  const fetchRegistrations = async (silent = false) => {
    try {
      if (!silent) setLoading(true);
      
      const { data, error } = await supabase
        .schema('Chessistant')
        .from('Registrations')
        .select('*')
        .order('Date', { ascending: false });
          
      if (error) throw error;
      setRegistrations(data || []);
    } catch (err) {
      console.error('Error fetching registrations:', err.message);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchRegistrations();
  }, []);

  const handleAccept = async (reg) => {
    try {
      const { error: profileError } = await supabase
        .schema('Chessistant')
        .from('Profiles')
        .insert([{
          StudName: reg.StudName,
          StudNum: reg.StudNum,
          Email: reg.Email,
          Password: reg.Password,
          Role: 'Member',
          Rating: 100, 
          PuzzlesWin: 0,
          PuzzlesTotal: 0,
          Date: new Date().toISOString(), 
          LastModified: Date.now()
        }]);

      if (profileError) throw profileError;

      const { error: rosterError } = await supabase
        .schema('Chessistant')
        .from('OrgRoster')
        .insert([{
          StudName: reg.StudName,
          StudNum: reg.StudNum,
          LastModified: Date.now()
        }]);

      if (rosterError) throw rosterError;

      const { error: deleteError } = await supabase
        .schema('Chessistant')
        .from('Registrations')
        .delete()
        .eq('RegID', reg.RegID);

      if (deleteError) throw deleteError;

      fetchRegistrations(true);
      alert('Application Accepted and Profile Created.');
    } catch (err) {
      console.error('Error accepting registration:', err.message);
      alert('Action failed: ' + (err.message || 'Unknown error'));
    }
  };

  const handleDeny = async (regId) => {
    if (!window.confirm('Are you sure you want to deny this application?')) return;
    
    try {
      const { error } = await supabase
        .schema('Chessistant')
        .from('Registrations')
        .delete()
        .eq('RegID', regId);

      if (error) throw error;
      
      fetchRegistrations(true);
    } catch (err) {
      console.error('Error denying registration:', err.message);
      alert('Action failed: ' + (err.message || 'Unknown error'));
    }
  };

  // NEW: Function to handle when a header is clicked
  const requestSort = (key) => {
    let direction = 'ascending';
    if (sortConfig.key === key && sortConfig.direction === 'ascending') {
      direction = 'descending';
    }
    setSortConfig({ key, direction });
  };

  // NEW: useMemo hook to sort registrations based on sortConfig
  const sortedRegistrations = useMemo(() => {
    let sortableRegistrations = [...registrations];
    
    if (sortConfig.key !== null) {
      sortableRegistrations.sort((a, b) => {
        let aValue = a[sortConfig.key];
        let bValue = b[sortConfig.key];

        if (aValue === null || aValue === undefined) aValue = '';
        if (bValue === null || bValue === undefined) bValue = '';

        if (typeof aValue === 'string') aValue = aValue.toLowerCase();
        if (typeof bValue === 'string') bValue = bValue.toLowerCase();

        if (aValue < bValue) {
          return sortConfig.direction === 'ascending' ? -1 : 1;
        }
        if (aValue > bValue) {
          return sortConfig.direction === 'ascending' ? 1 : -1;
        }
        return 0;
      });
    }
    
    return sortableRegistrations;
  }, [registrations, sortConfig]);

  // NEW: Helper to display the visual sort indicator
  const getSortIndicator = (columnKey) => {
    if (sortConfig.key === columnKey) {
      return sortConfig.direction === 'ascending' ? ' ↑' : ' ↓';
    }
    return ' ↕'; 
  };

  const displayDate = (dateVal) => {
    if (!dateVal) return '';
    const d = new Date(dateVal);
    return isNaN(d.getTime()) ? '' : d.toLocaleDateString();
  };

  if (loading) return (
    <div className="overlay">
      <div className="spinner"></div>
      <p>Loading Registrations...</p>
    </div>
  );

  return (
    <div className="card">
      <h3 style={{ fontSize: '2rem' }}>Pending Registrations</h3>
      <div style={{ marginTop: '20px' }}>
        {registrations.length === 0 ? (
          <p>No pending applications.</p>
        ) : (
          <table style={{ width: '100%', borderCollapse: 'collapse', textAlign: 'left' }}>
            <thead>
              <tr style={{ borderBottom: '2px solid var(--oak)' }}>
                {/* UPDATED: Headers are now clickable with sort indicators */}
                <th 
                  onClick={() => requestSort('StudName')} 
                  style={{ padding: '10px', width: '25%', cursor: 'pointer', userSelect: 'none' }}
                >
                  Name {getSortIndicator('StudName')}
                </th>
                <th 
                  onClick={() => requestSort('Email')} 
                  style={{ padding: '10px', width: '25%', cursor: 'pointer', userSelect: 'none' }}
                >
                  Email {getSortIndicator('Email')}
                </th>
                <th 
                  onClick={() => requestSort('StudNum')} 
                  style={{ padding: '10px', width: '15%', cursor: 'pointer', userSelect: 'none' }}
                >
                  Student ID {getSortIndicator('StudNum')}
                </th>
                <th 
                  onClick={() => requestSort('Date')} 
                  style={{ padding: '10px', width: '15%', textAlign: 'center', cursor: 'pointer', userSelect: 'none' }}
                >
                  Date {getSortIndicator('Date')}
                </th>
                <th style={{ padding: '10px', width: '10%', textAlign: 'center' }}>Actions</th>
              </tr>
            </thead>
            <tbody>
              {/* UPDATED: Map over sortedRegistrations instead of registrations */}
              {sortedRegistrations.map((reg) => (
                <tr key={reg.RegID} style={{ borderBottom: '1.5px solid var(--gold)' }}>
                  <td style={{ padding: '10px' }}>{reg.StudName}</td>
                  <td style={{ padding: '10px' }}>{reg.Email}</td>
                  <td style={{ padding: '10px' }}>{reg.StudNum}</td>
                  <td style={{ padding: '10px', textAlign: 'center' }}>{displayDate(reg.Date)}</td>
                  <td style={{ padding: '10px' }}>
                    <div style={{ display: 'flex', justifyContent: 'space-around', alignItems: 'center', gap: '10px' }}>
                      <button onClick={() => handleAccept(reg)} style={{ fontSize: '0.85rem', width: 'stretch' }}>Accept</button>
                      <button onClick={() => handleDeny(reg.RegID)} style={{ fontSize: '0.85rem', width: 'stretch', background: 'var(--oak)' }}>Deny</button>
                    </div>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        )}
      </div>
    </div>
  );
};

export default Registrations;
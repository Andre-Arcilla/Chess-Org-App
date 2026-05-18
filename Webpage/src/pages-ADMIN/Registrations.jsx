import React, { useEffect, useState } from 'react';
import { supabase } from '../db';

const Registrations = () => {
  const [registrations, setRegistrations] = useState([]);
  const [loading, setLoading] = useState(true);

  const fetchRegistrations = async (silent = false) => {
    try {
      // Only trigger full-screen loading on initial mount, not during updates
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
      // 1. Insert into Profiles
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

      // 2. Insert into OrgRoster
      const { error: rosterError } = await supabase
        .schema('Chessistant')
        .from('OrgRoster')
        .insert([{
          StudName: reg.StudName,
          StudNum: reg.StudNum,
          LastModified: Date.now()
        }]);

      if (rosterError) throw rosterError;

      // 3. Delete from Registrations
      const { error: deleteError } = await supabase
        .schema('Chessistant')
        .from('Registrations')
        .delete()
        .eq('RegID', reg.RegID);

      if (deleteError) throw deleteError;

      // Fetch silently in the background to update the table instantly
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
      
      // Fetch silently in the background here too
      fetchRegistrations(true);
    } catch (err) {
      console.error('Error denying registration:', err.message);
      alert('Action failed: ' + (err.message || 'Unknown error'));
    }
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
                <th style={{ padding: '10px', width: '25%' }}>Name</th>
                <th style={{ padding: '10px', width: '25%' }}>Email</th>
                <th style={{ padding: '10px', width: '15%' }}>Student ID</th>
                <th style={{ padding: '10px', width: '15%', textAlign: 'center' }}>Date</th>
                <th style={{ padding: '10px', width: '10%', textAlign: 'center' }}>Actions</th>
              </tr>
            </thead>
            <tbody>
              {registrations.map((reg) => (
                <tr key={reg.RegID} style={{ borderBottom: '1.5px solid var(--gold)' }}>
                  <td style={{ padding: '10px' }}>{reg.StudName}</td>
                  <td style={{ padding: '10px' }}>{reg.Email}</td>
                  <td style={{ padding: '10px' }}>{reg.StudNum}</td>
                  <td style={{ padding: '10px' }}>{displayDate(reg.Date)}</td>
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
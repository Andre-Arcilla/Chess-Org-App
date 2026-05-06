import React, { useEffect, useState } from 'react';
import { supabase } from '../db';

const Registrations = () => {
  const [registrations, setRegistrations] = useState([]);
  const [loading, setLoading] = useState(true);

  const fetchRegistrations = async () => {
    try {
      setLoading(true);
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
          Rating: 1200, 
          PuzzlesWin: 0,
          PuzzlesTotal: 0,
          Date: new Date().toISOString(), // Date is standard string
          LastModified: Date.now()        // FIX: LastModified requires BigInt integer
        }]);

      if (profileError) throw profileError;

      // 2. Insert into OrgRoster
      const { error: rosterError } = await supabase
        .schema('Chessistant')
        .from('OrgRoster')
        .insert([{
          StudName: reg.StudName,
          StudNum: reg.StudNum,
          LastModified: Date.now()        // FIX: LastModified requires BigInt integer
        }]);

      if (rosterError) throw rosterError;

      // 3. Delete from Registrations
      const { error: deleteError } = await supabase
        .schema('Chessistant')
        .from('Registrations')
        .delete()
        .eq('RegID', reg.RegID);

      if (deleteError) throw deleteError;

      fetchRegistrations();
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
      
      fetchRegistrations();
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

  if (loading) return <p>Loading Applications...</p>;

  return (
    <div className="card">
      <h3>Pending Registrations</h3>
      <div style={{ marginTop: '20px' }}>
        {registrations.length === 0 ? (
          <p>No pending applications.</p>
        ) : (
          <table style={{ width: '100%', borderCollapse: 'collapse', textAlign: 'left' }}>
            <thead>
              <tr style={{ borderBottom: '2px solid var(--oak)' }}>
                <th style={{ padding: '10px' }}>Name</th>
                <th style={{ padding: '10px' }}>Student ID</th>
                <th style={{ padding: '10px' }}>Email</th>
                <th style={{ padding: '10px' }}>Date</th>
                <th style={{ padding: '10px' }}>Actions</th>
              </tr>
            </thead>
            <tbody>
              {registrations.map((reg) => (
                <tr key={reg.RegID} style={{ borderBottom: '1px solid var(--antique-white)' }}>
                  <td style={{ padding: '10px' }}>{reg.StudName}</td>
                  <td style={{ padding: '10px' }}>{reg.StudNum}</td>
                  <td style={{ padding: '10px' }}>{reg.Email}</td>
                  <td style={{ padding: '10px' }}>{displayDate(reg.Date)}</td>
                  <td style={{ padding: '10px' }}>
                    <button onClick={() => handleAccept(reg)} style={{ padding: '5px 10px', fontSize: '0.8rem', width: 'auto', marginRight: '5px' }}>Accept</button>
                    <button onClick={() => handleDeny(reg.RegID)} style={{ padding: '5px 10px', fontSize: '0.8rem', width: 'auto', background: 'var(--error)' }}>Deny</button>
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
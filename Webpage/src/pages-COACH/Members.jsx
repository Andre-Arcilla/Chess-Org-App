import React, { useEffect, useState, useMemo } from 'react';
import { useOutletContext } from 'react-router-dom';
import { supabase } from '../db';

const Members = () => {
  const { adminData, setAdminData } = useOutletContext();
  const [members, setMembers] = useState([]);
  const [loading, setLoading] = useState(true);
  const [editingId, setEditingId] = useState(null);
  const [editForm, setEditForm] = useState({});
  
  // State to track the active sort column and direction
  const [sortConfig, setSortConfig] = useState({ key: null, direction: 'ascending' });

  const fetchMembers = async () => {
    try {
      setLoading(true);
      const orFilter = adminData?.StudNum 
        ? `Role.eq.Member,StudNum.eq.${adminData.StudNum}`
        : 'Role.eq.Member';
      const minimumDelay = new Promise(resolve => setTimeout(resolve, 750));
      const [_, { data, error }] = await Promise.all([
        minimumDelay,
        supabase
          .schema('Chessistant')
          .from('Profiles')
          .select('*').or(orFilter)
          .order('StudName', { ascending: true })
      ]);
      if (error) throw error;
      setMembers(data || []);
    } catch (err) {
      console.error('Error fetching members:', err.message);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchMembers();
  }, []);

  const startEdit = (member) => {
    setEditingId(member.UserID);
    setEditForm(member);
  };

  const cancelEdit = () => {
    setEditingId(null);
    setEditForm({});
  };

  const handleUpdate = async () => {
    try {
      const originalMember = members.find(m => m.UserID === editingId);

      if (originalMember?.StudNum === adminData?.StudNum && editForm.Role !== originalMember?.Role) {
        alert('You cannot change your own role.');
        return;
      }

      if (originalMember?.Role === 'Admin' && editForm.Role !== 'Admin') {
        const { count, error: countError } = await supabase
          .schema('Chessistant')
          .from('Profiles')
          .select('*', { count: 'exact', head: true })
          .eq('Role', 'Admin');

        if (countError) throw countError;

        if (count <= 1) {
          alert('Cannot remove the last admin. At least one admin account must remain.');
          return;
        }
      }

      const updatedTimestamp = Date.now();
      const { error } = await supabase
        .schema('Chessistant')
        .from('Profiles')
        .update({
          Email: editForm.Email,
          StudName: editForm.StudName,
          StudNum: editForm.StudNum,
          Rating: editForm.Rating,
          LastModified: updatedTimestamp 
        })
        .eq('UserID', editingId);

      if (error) throw error;
      
      // Update global session data if editing self
      if (originalMember?.StudNum === adminData?.StudNum) {
        const updatedUser = {
          ...adminData,
          Email: editForm.Email,
          StudName: editForm.StudName,
          StudNum: editForm.StudNum,
        };
        localStorage.setItem('currentUser', JSON.stringify(updatedUser));
        if (setAdminData) setAdminData(updatedUser);
      }

      setMembers(prevMembers => 
        prevMembers.map(member => 
          member.UserID === editingId 
            ? { ...member, ...editForm, LastModified: updatedTimestamp } 
            : member
        )
      );

      setEditingId(null);

    } catch (err) {
      console.error('Error updating member:', err.message);
      alert('Update failed: ' + (err.message || 'Unknown error'));
    }
  };

  // Function to handle when a header is clicked
  const requestSort = (key) => {
    let direction = 'ascending';
    if (sortConfig.key === key && sortConfig.direction === 'ascending') {
      direction = 'descending';
    }
    setSortConfig({ key, direction });
  };

  // useMemo hook to sort members based on sortConfig
  const sortedMembers = useMemo(() => {
    let sortableMembers = members.filter(m => m.StudNum !== adminData?.StudNum);
    
    sortableMembers.sort((a, b) => {
      // Perform column sorting
      if (sortConfig.key !== null) {
        let aValue = a[sortConfig.key];
        let bValue = b[sortConfig.key];

        // Handle null/undefined values safely
        if (aValue === null || aValue === undefined) aValue = '';
        if (bValue === null || bValue === undefined) bValue = '';

        // Make string sorting case-insensitive
        if (typeof aValue === 'string') aValue = aValue.toLowerCase();
        if (typeof bValue === 'string') bValue = bValue.toLowerCase();

        if (aValue < bValue) {
          return sortConfig.direction === 'ascending' ? -1 : 1;
        }
        if (aValue > bValue) {
          return sortConfig.direction === 'ascending' ? 1 : -1;
        }
      }
      return 0;
    });
    
    return sortableMembers;
  }, [members, sortConfig, adminData]);

  // Helper to display the visual sort indicator (Arrows)
  const getSortIndicator = (columnKey) => {
    if (sortConfig.key === columnKey) {
      return sortConfig.direction === 'ascending' ? ' ↑' : ' ↓';
    }
    return ' ↕';
  };

  if (loading) return (
    <div className="overlay">
      <div className="spinner"></div>
      <p>Loading Members...</p>
    </div>
  );

  return (
    <div className="card">
      <h3 style={{ fontSize: '2rem' }}>Member Management</h3>
      <div style={{ overflowX: 'auto', marginTop: '20px' }}>
        <table style={{ width: '100%', borderCollapse: 'collapse', textAlign: 'left', tableLayout: 'fixed' }}>
          <thead>
            <tr style={{ borderBottom: '2px solid var(--oak)' }}>
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
                style={{ padding: '10px', width: '12%', cursor: 'pointer', userSelect: 'none' }}
              >
                Student ID {getSortIndicator('StudNum')}
              </th>
              <th 
                onClick={() => requestSort('Rating')} 
                style={{ padding: '10px', width: '10%', textAlign: 'center', cursor: 'pointer', userSelect: 'none' }}
              >
                Rating {getSortIndicator('Rating')}
              </th>
              <th style={{ padding: '10px', width: '15%', textAlign: 'center' }}>Actions</th>
            </tr>
          </thead>
          <tbody>
            {sortedMembers.map((member) => (
              <tr 
                key={member.UserID} 
                style={{ 
                  height: '55px', 
                  borderBottom: '1.5px solid var(--gold)'
                }}
              >
                <td style={{ padding: '0 10px' }}>
                  {member.StudName}
                </td>
                <td style={{ padding: '0 10px' }}>
                  {member.Email}
                </td>
                <td style={{ padding: '0 10px' }}>
                  {member.StudNum}
                </td>
                <td style={{ padding: '0 10px', textAlign: 'center' }}>
                  {/* Rating is editable for ANY row */}
                  {editingId === member.UserID ? (
                    <input 
                      type="number"
                      value={editForm.Rating || 0} 
                      onChange={(e) => setEditForm({...editForm, Rating: parseInt(e.target.value) || 0})}
                      style={{ width: '100%', padding: '8px', boxSizing: 'border-box', textAlign: 'center' }}
                    />
                  ) : member.Rating}
                </td>
                <td style={{ padding: '0 10px' }}>
                  {editingId === member.UserID ? (
                    <div style={{ display: 'flex', justifyContent: 'space-around', alignItems: 'center', gap: '10px' }}>
                      <button onClick={handleUpdate} style={{ fontSize: '0.85rem', width: 'stretch' }}>Save</button>
                      <button onClick={cancelEdit} style={{ fontSize: '0.85rem', width: 'stretch', background: 'var(--oak)' }}>Cancel</button>
                    </div>
                  ) : (
                    <button onClick={() => startEdit(member)} style={{ fontSize: '0.85rem', width: 'stretch' }}>Edit</button>
                  )}
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </div>
  );
};

export default Members;
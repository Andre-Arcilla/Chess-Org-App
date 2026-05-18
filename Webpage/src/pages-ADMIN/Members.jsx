import React, { useEffect, useState } from 'react';
import { supabase } from '../db';

const Members = () => {
  const [members, setMembers] = useState([]);
  const [loading, setLoading] = useState(true);
  const [editingId, setEditingId] = useState(null);
  const [editForm, setEditForm] = useState({});

  const fetchMembers = async () => {
    try {
      setLoading(true);
      const minimumDelay = new Promise(resolve => setTimeout(resolve, 750));
      const [_, { data, error }] = await Promise.all([
        minimumDelay,
        supabase
          .schema('Chessistant')
          .from('Profiles')
          .select('*')
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
      // 1. If demoting an admin, check how many admins remain
      const originalMember = members.find(m => m.UserID === editingId);
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

      // 2. Perform the database update
      const updatedTimestamp = Date.now();
      const { error } = await supabase
        .schema('Chessistant')
        .from('Profiles')
        .update({
          Email: editForm.Email,
          StudName: editForm.StudName,
          StudNum: editForm.StudNum,
          Role: editForm.Role,
          Rating: editForm.Rating,
          LastModified: updatedTimestamp 
        })
        .eq('UserID', editingId);

      if (error) throw error;
      
      // 3. Update the local UI state directly without setting loading = true
      setMembers(prevMembers => 
        prevMembers.map(member => 
          member.UserID === editingId 
            ? { ...member, ...editForm, LastModified: updatedTimestamp } 
            : member
        )
      );

      // 4. Exit edit mode cleanly
      setEditingId(null);

    } catch (err) {
      console.error('Error updating member:', err.message);
      alert('Update failed: ' + (err.message || 'Unknown error'));
    }
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
              <th style={{ padding: '10px', width: '25%' }}>Name</th>
              <th style={{ padding: '10px', width: '25%' }}>Email</th>
              <th style={{ padding: '10px', width: '12%' }}>Student ID</th>
              <th style={{ padding: '10px', width: '13%', textAlign: 'center' }}>Role</th>
              <th style={{ padding: '10px', width: '10%', textAlign: 'center' }}>Rating</th>
              <th style={{ padding: '10px', width: '15%', textAlign: 'center' }}>Actions</th>
            </tr>
          </thead>
          <tbody>
            {members.map((member) => (
              /* FIX 1: Locked the row height to exactly 55px to prevent stretching */
              <tr key={member.UserID} style={{ height: '55px', borderBottom: '1.5px solid var(--gold)' }}>
                {/* FIX 2: Changed cell paddings to '0 10px' so the row height dictates the vertical spacing */}
                <td style={{ padding: '0 10px' }}>
                  {editingId === member.UserID ? (
                    <input
                      value={editForm.StudName || ''} 
                      onChange={(e) => setEditForm({...editForm, StudName: e.target.value})}
                      style={{ width: '100%', padding: '8px', boxSizing: 'border-box' }}
                    />
                  ) : member.StudName}
                </td>
                <td style={{ padding: '0 10px' }}>
                  {editingId === member.UserID ? (
                    <input 
                      value={editForm.Email || ''} 
                      onChange={(e) => setEditForm({...editForm, Email: e.target.value})}
                      style={{ width: '100%', padding: '8px', boxSizing: 'border-box' }}
                    />
                  ) : member.Email}
                </td>
                <td style={{ padding: '0 10px' }}>
                  {editingId === member.UserID ? (
                    <input 
                      value={editForm.StudNum || ''} 
                      onChange={(e) => setEditForm({...editForm, StudNum: e.target.value})}
                      style={{ width: '100%', padding: '8px', boxSizing: 'border-box' }}
                    />
                  ) : member.StudNum}
                </td>
                <td style={{ padding: '0 10px' }}>
                  {editingId === member.UserID ? (
                    <select 
                      value={editForm.Role || ''} 
                      onChange={(e) => setEditForm({...editForm, Role: e.target.value})}
                      style={{ width: '100%', padding: '8px', boxSizing: 'border-box', textAlign: 'center' }}
                    >
                      <option value="Admin">Admin</option>
                      <option value="Member">Member</option>
                      <option value="Player">Player</option>
                    </select>
                  ) : (
                    <span className="role-tag">{member.Role}</span>
                  )}
                </td>
                <td style={{ padding: '0 10px', textAlign: 'center' }}>
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
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
      const { data, error } = await supabase
        .schema('Chessistant')
        .from('Profiles')
        .select('*')
        .order('StudName', { ascending: true });
      
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
      // If demoting an admin, check how many admins remain
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

      const { error } = await supabase
        .schema('Chessistant')
        .from('Profiles')
        .update({
          Email: editForm.Email,
          StudName: editForm.StudName,
          Role: editForm.Role,
          Rating: editForm.Rating,
          LastModified: Date.now() // Use Unix timestamp for bigint type
        })
        .eq('UserID', editingId);

      if (error) throw error;
      
      setEditingId(null);
      fetchMembers();
    } catch (err) {
      console.error('Error updating member:', err.message);
      alert('Update failed: ' + (err.message || 'Unknown error'));
    }
  };

  if (loading) return <p>Loading Members...</p>;

  return (
    <div className="card">
      <h3>Member Management</h3>
      <div style={{ overflowX: 'auto', marginTop: '20px' }}>
        <table style={{ width: '100%', borderCollapse: 'collapse', textAlign: 'left' }}>
          <thead>
            <tr style={{ borderBottom: '2px solid var(--oak)' }}>
              <th style={{ padding: '10px', width: '30%' }}>Name</th>
              <th style={{ padding: '10px', width: '30%' }}>Email</th>
              <th style={{ padding: '10px', width: '10%', textAlign: 'center' }}>Role</th>
              <th style={{ padding: '10px', width: '8%', textAlign: 'center' }}>Rating</th>
              <th style={{ padding: '10px', width: '15%', textAlign: 'center' }}>Actions</th>
            </tr>
          </thead>
          <tbody>
            {members.map((member) => (
              <tr key={member.UserID} style={{ borderBottom: '1px solid var(--antique-white)' }}>
                <td style={{ padding: '10px' }}>
                  {editingId === member.UserID ? (
                    <input
                      value={editForm.StudName || ''} 
                      onChange={(e) => setEditForm({...editForm, StudName: e.target.value})}
                      style={{ width: '100%', padding: '8px', boxSizing: 'border-box' }}
                    />
                  ) : member.StudName}
                </td>
                <td style={{ padding: '10px' }}>
                  {editingId === member.UserID ? (
                    <input 
                      value={editForm.Email || ''} 
                      onChange={(e) => setEditForm({...editForm, Email: e.target.value})}
                      style={{ width: '100%', padding: '8px', boxSizing: 'border-box' }}
                    />
                  ) : member.Email}
                </td>
                <td style={{ padding: '10px' }}>
                  {editingId === member.UserID ? (
                    <select 
                      value={editForm.Role || ''} 
                      onChange={(e) => setEditForm({...editForm, Role: e.target.value})}
                      style={{ width: '100%', padding: '8px', boxSizing: 'border-box' }}
                    >
                      <option value="Admin">Admin</option>
                      <option value="Member">Member</option>
                      <option value="Player">Player</option>
                    </select>
                  ) : (
                    <span className="role-tag">{member.Role}</span>
                  )}
                </td>
                <td style={{ padding: '10px' }}>
                  {editingId === member.UserID ? (
                    <input 
                      type="number"
                      value={editForm.Rating || 0} 
                      onChange={(e) => setEditForm({...editForm, Rating: parseInt(e.target.value)})}
                      style={{ width: '100%', padding: '8px', boxSizing: 'border-box' }}
                    />
                  ) : member.Rating}
                </td>
                <td style={{ padding: '10px' }}>
                  {editingId === member.UserID ? (
                    <>
                      <button onClick={handleUpdate} style={{ padding: '5px 10px', fontSize: '0.8rem', width: 'auto', margin: '5px 5px 5px 0px' }}>Save</button>
                      <button onClick={cancelEdit} style={{ padding: '5px 10px', fontSize: '0.8rem', width: 'auto', margin: '5px 0px', background: 'var(--oak)' }}>Cancel</button>
                    </>
                  ) : (
                    <button onClick={() => startEdit(member)} style={{ padding: '5px 10px', fontSize: '0.8rem', width: '100%', margin: '5px 5px 5px 0px' }}>Edit</button>
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

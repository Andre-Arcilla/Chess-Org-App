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
        .select('*');
      
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
      const { error } = await supabase
        .schema('Chessistant')
        .from('Profiles')
        .update({
          StudName: editForm.StudName,
          Role: editForm.Role,
          Rating: editForm.Rating,
          LastModified: new Date().toISOString() // Use ISO string for TIMESTAMP
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
              <th style={{ padding: '10px' }}>Name</th>
              <th style={{ padding: '10px' }}>Email</th>
              <th style={{ padding: '10px' }}>Role</th>
              <th style={{ padding: '10px' }}>Rating</th>
              <th style={{ padding: '10px' }}>Actions</th>
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
                    />
                  ) : member.StudName}
                </td>
                <td style={{ padding: '10px' }}>{member.Email}</td>
                <td style={{ padding: '10px' }}>
                  {editingId === member.UserID ? (
                    <select 
                      value={editForm.Role || ''} 
                      onChange={(e) => setEditForm({...editForm, Role: e.target.value})}
                      style={{ padding: '8px', width: '100%' }}
                    >
                      <option value="Admin">Admin</option>
                      <option value="Member">Member</option>
                      <option value="Player">Player</option>
                    </select>
                  ) : (
                    <span className="role-tag" style={{ fontSize: '0.7rem' }}>{member.Role}</span>
                  )}
                </td>
                <td style={{ padding: '10px' }}>
                  {editingId === member.UserID ? (
                    <input 
                      type="number"
                      value={editForm.Rating || 0} 
                      onChange={(e) => setEditForm({...editForm, Rating: parseInt(e.target.value)})}
                    />
                  ) : member.Rating}
                </td>
                <td style={{ padding: '10px' }}>
                  {editingId === member.UserID ? (
                    <>
                      <button onClick={handleUpdate} style={{ padding: '5px 10px', fontSize: '0.8rem', width: 'auto', marginRight: '5px' }}>Save</button>
                      <button onClick={cancelEdit} style={{ padding: '5px 10px', fontSize: '0.8rem', width: 'auto', background: 'var(--oak)' }}>Cancel</button>
                    </>
                  ) : (
                    <button onClick={() => startEdit(member)} style={{ padding: '5px 10px', fontSize: '0.8rem', width: 'auto' }}>Edit</button>
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

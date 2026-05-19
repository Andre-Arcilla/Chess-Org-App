import React, { useEffect, useState, useMemo, useRef } from 'react';
import { useOutletContext } from 'react-router-dom';
import { supabase } from '../db';

const Members = () => {
  const { adminData, setAdminData } = useOutletContext();
  const [members, setMembers] = useState([]);
  const [loading, setLoading] = useState(true);
  const [editingId, setEditingId] = useState(null);
  const [editForm, setEditForm] = useState({});
  
  const [sortConfig, setSortConfig] = useState({ key: null, direction: 'ascending' });

  // Refs to capture DOM nodes for constraint validation reporting
  const nameRef = useRef(null);
  const emailRef = useRef(null);
  const studNumRef = useRef(null);

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
      // Reset any previous custom validities
      if (nameRef.current) nameRef.current.setCustomValidity('');
      if (emailRef.current) emailRef.current.setCustomValidity('');
      if (studNumRef.current) studNumRef.current.setCustomValidity('');

      // 1. Don't allow empty fields validation
      if (!editForm.StudName || !editForm.StudName.trim()) {
        if (nameRef.current) {
          nameRef.current.setCustomValidity('Name cannot be empty.');
          nameRef.current.reportValidity();
        }
        return;
      }

      if (!editForm.Email || !editForm.Email.trim()) {
        if (emailRef.current) {
          emailRef.current.setCustomValidity('Email cannot be empty.');
          emailRef.current.reportValidity();
        }
        return;
      }

      if (!editForm.StudNum || !editForm.StudNum.trim()) {
        if (studNumRef.current) {
          studNumRef.current.setCustomValidity('Student ID cannot be empty.');
          studNumRef.current.reportValidity();
        }
        return;
      }

      // 2. Email domain validation (Only allow @umak.edu.ph)
      const sanitizedEmail = editForm.Email.trim();
      if (!sanitizedEmail.endsWith('@umak.edu.ph')) {
        if (emailRef.current) {
          emailRef.current.setCustomValidity('Only emails using the @umak.edu.ph domain are allowed.');
          emailRef.current.reportValidity();
        }
        return;
      }

      // 3. Student ID Format validation (1 letter followed by 8 numbers)
      const sanitizedStudNum = editForm.StudNum.trim();
      const studNumRegex = /^[A-Za-z]\d{8}$/;
      if (!studNumRegex.test(sanitizedStudNum)) {
        if (studNumRef.current) {
          studNumRef.current.setCustomValidity('Student ID must be 1 letter followed by exactly 8 digits (e.g., A12345678).');
          studNumRef.current.reportValidity();
        }
        return;
      }

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
          Email: sanitizedEmail,
          StudName: editForm.StudName.trim(),
          StudNum: sanitizedStudNum,
          Role: editForm.Role,
          Rating: editForm.Rating,
          LastModified: updatedTimestamp 
        })
        .eq('UserID', editingId);

      if (error) throw error;

      if (originalMember?.StudNum === adminData?.StudNum) {
        const updatedUser = {
          ...adminData,
          Email: sanitizedEmail,
          StudName: editForm.StudName.trim(),
          StudNum: sanitizedStudNum,
          Role: editForm.Role,
        };
        localStorage.setItem('currentUser', JSON.stringify(updatedUser));
        if (setAdminData) setAdminData(updatedUser);
      }
      
      setMembers(prevMembers => 
        prevMembers.map(member => 
          member.UserID === editingId 
            ? { ...member, ...editForm, Email: sanitizedEmail, StudName: editForm.StudName.trim(), StudNum: sanitizedStudNum, LastModified: updatedTimestamp } 
            : member
        )
      );

      setEditingId(null);

    } catch (err) {
      console.error('Error updating member:', err.message);
      alert('Update failed: ' + (err.message || 'Unknown error'));
    }
  };

  // requestSort handles cycling through roles
  const requestSort = (key) => {
    if (key === 'Role') {
      const roleCycle = ['Admin', 'Coach', 'Member', 'Disabled'];
      let nextTargetRole = roleCycle[0];

      if (sortConfig.key === 'Role') {
        const currentIndex = roleCycle.indexOf(sortConfig.direction);
        if (currentIndex !== -1 && currentIndex < roleCycle.length - 1) {
          nextTargetRole = roleCycle[currentIndex + 1];
        } else {
          nextTargetRole = roleCycle[0]; 
        }
      }
      setSortConfig({ key, direction: nextTargetRole });
    } else {
      let direction = 'ascending';
      if (sortConfig.key === key && sortConfig.direction === 'ascending') {
        direction = 'descending';
      }
      setSortConfig({ key, direction });
    }
  };

  // useMemo interprets specific role targets or typical asc/desc columns
  const sortedMembers = useMemo(() => {
    let sortableMembers = members.filter(m => m.StudNum !== adminData?.StudNum);
    
    sortableMembers.sort((a, b) => {
      if (sortConfig.key !== null) {
        if (sortConfig.key === 'Role') {
          const topRole = sortConfig.direction;
          
          if (a.Role === topRole && b.Role !== topRole) return -1;
          if (b.Role === topRole && a.Role !== topRole) return 1;
          
          const hierarchy = { 'Admin': 1, 'Coach': 2, 'Member': 3, 'Disabled': 4 };
          const weightA = a.Role === topRole ? 0 : (hierarchy[a.Role] || 5);
          const weightB = b.Role === topRole ? 0 : (hierarchy[b.Role] || 5);
          
          if (weightA !== weightB) {
            return weightA - weightB;
          }

          let nameA = (a.StudName || '').toLowerCase();
          let nameB = (b.StudName || '').toLowerCase();
          if (nameA < nameB) return -1;
          if (nameA > nameB) return 1;
          return 0;

        } else {
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
        }
      }
      return 0;
    });
    
    return sortableMembers;
  }, [members, sortConfig, adminData]);

  const getSortIndicator = (columnKey) => {
    if (sortConfig.key === columnKey) {
      if (columnKey === 'Role') {
        return ` ↕`; 
      }
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
                onClick={() => requestSort('Role')} 
                style={{ padding: '10px', width: '13%', textAlign: 'center', cursor: 'pointer', userSelect: 'none', whiteSpace: 'nowrap' }}
              >
                Role {getSortIndicator('Role')}
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
                  {editingId === member.UserID ? (
                    <input
                      ref={nameRef}
                      value={editForm.StudName || ''} 
                      onChange={(e) => {
                        e.target.setCustomValidity('');
                        setEditForm({...editForm, StudName: e.target.value});
                      }}
                      style={{ width: '100%', padding: '8px', boxSizing: 'border-box' }}
                      placeholder="John Smith"
                      type="text"
                      required
                    />
                  ) : member.StudName}
                </td>
                <td style={{ padding: '0 10px' }}>
                  {editingId === member.UserID ? (
                    <input 
                      ref={emailRef}
                      value={editForm.Email || ''} 
                      onChange={(e) => {
                        e.target.setCustomValidity('');
                        setEditForm({...editForm, Email: e.target.value});
                      }}
                      style={{ width: '100%', padding: '8px', boxSizing: 'border-box' }}
                      placeholder="grandmaster@umak.edu.ph"
                      type="email"
                      required
                    />
                  ) : member.Email}
                </td>
                <td style={{ padding: '0 10px' }}>
                  {editingId === member.UserID ? (
                    <input 
                      ref={studNumRef}
                      value={editForm.StudNum || ''} 
                      onChange={(e) => {
                        e.target.setCustomValidity('');
                        setEditForm({...editForm, StudNum: e.target.value});
                      }}
                      style={{ width: '100%', padding: '8px', boxSizing: 'border-box' }}
                      placeholder="A12345678"
                      type="text"
                      required
                    />
                  ) : member.StudNum}
                </td>
                <td style={{ padding: '0 10px' }}>
                  {editingId === member.UserID ? (
                    <select 
                      value={editForm.Role || ''} 
                      onChange={(e) => setEditForm({...editForm, Role: e.target.value})}
                      style={{ 
                        width: '100%', 
                        padding: '8px', 
                        boxSizing: 'border-box', 
                        textAlign: 'center'
                      }}
                    >
                      <option value="Admin">Admin</option>
                      <option value="Coach">Coach</option>
                      <option value="Member">Member</option>
                      <option value="Disabled">Disabled</option>
                    </select>
                  ) : (
                    <span className={`role-tag role-tag--${member.Role?.toLowerCase()}`}>{member.Role}</span>
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
import React, { useEffect, useState, useRef } from 'react';
import { useOutletContext } from 'react-router-dom';
import { supabase } from '../db';

const Profile = () => {
  const { adminData } = useOutletContext();
  const [profile, setProfile] = useState(null);
  const [loading, setLoading] = useState(true);
  const [isEditing, setIsEditing] = useState(false);
  const [editForm, setEditForm] = useState({});
  const [saveSuccess, setSaveSuccess] = useState(false);

  // Refs to target inputs for native browser validation tooltips
  const nameRef    = useRef(null);
  const emailRef   = useRef(null);
  const studNumRef = useRef(null);
  const ratingRef  = useRef(null);

  const fetchProfile = async () => {
    try {
      setLoading(true);
      const minimumDelay = new Promise(resolve => setTimeout(resolve, 750));
      const [_, { data, error }] = await Promise.all([
        minimumDelay,
        supabase
          .schema('Chessistant')
          .from('Profiles')
          .select('*')
          .eq('StudNum', adminData?.StudNum)
          .single()
      ]);
      if (error) throw error;
      setProfile(data);
    } catch (err) {
      console.error('Error fetching profile:', err.message);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchProfile();
  }, []);

  const startEdit = () => {
    setEditForm(profile);
    setIsEditing(true);
    setSaveSuccess(false);
  };

  const cancelEdit = () => {
    setIsEditing(false);
    setEditForm({});
  };

  // Clear native validity on each keystroke — same pattern as the login script
  const handleChange = (field, value, ref) => {
    ref.current?.setCustomValidity('');
    setEditForm(prev => ({ ...prev, [field]: value }));
  };

  const handleUpdate = async () => {
    setSaveSuccess(false);

    const name    = editForm.StudName?.toString().trim();
    const email   = editForm.Email?.toString().trim();
    const studNum = editForm.StudNum?.toString().trim();
    const rating  = parseInt(editForm.Rating, 10);

    // Validate each field and show a native tooltip on the offending input
    if (!name) {
      nameRef.current.setCustomValidity('Full name cannot be empty.');
      nameRef.current.reportValidity();
      return;
    }
    if (!email) {
      emailRef.current.setCustomValidity('Email cannot be empty.');
      emailRef.current.reportValidity();
      return;
    }
    if (!studNum) {
      studNumRef.current.setCustomValidity('Student ID cannot be empty.');
      studNumRef.current.reportValidity();
      return;
    }
    if (editForm.Rating === '' || isNaN(rating)) {
      ratingRef.current.setCustomValidity('Rating must be a valid number.');
      ratingRef.current.reportValidity();
      return;
    }
    if (rating < 100) {
      ratingRef.current.setCustomValidity('Rating must be at least 100.');
      ratingRef.current.reportValidity();
      return;
    }

    try {
      const updatedTimestamp = Date.now();
      const { error } = await supabase
        .schema('Chessistant')
        .from('Profiles')
        .update({
          Email: email,
          StudName: name,
          StudNum: studNum,
          Rating: rating,
          LastModified: updatedTimestamp,
        })
        .eq('UserID', profile.UserID);

      if (error) throw error;

      setProfile({
        ...profile,
        StudName: name,
        Email: email,
        StudNum: studNum,
        Rating: rating,
        LastModified: updatedTimestamp,
      });
      setIsEditing(false);
      setSaveSuccess(true);
      setTimeout(() => setSaveSuccess(false), 3000);
    } catch (err) {
      console.error('Error updating profile:', err.message);
      // Surface a DB/network error on the name field as a general anchor
      nameRef.current?.setCustomValidity('Update failed: ' + (err.message || 'Unknown error'));
      nameRef.current?.reportValidity();
    }
  };

  if (loading) return (
    <div className="overlay">
      <div className="spinner"></div>
      <p>Loading Profile...</p>
    </div>
  );

  if (!profile) return (
    <div className="content-body">
      <div className="card">
        <p>Could not load profile.</p>
      </div>
    </div>
  );

  return (
    <div className="card" style={{ maxWidth: '640px', margin: '0 auto', width: '100%' }}>

      {/* Header */}
      <div style={{ display: 'flex', alignItems: 'center', gap: '20px', marginBottom: '30px', borderBottom: '2px solid var(--gold)', paddingBottom: '20px' }}>
        <div style={{
          width: '72px',
          height: '72px',
          borderRadius: '50%',
          background: 'linear-gradient(135deg, var(--mahogany), var(--oak))',
          border: '3px solid var(--gold)',
          display: 'flex',
          alignItems: 'center',
          justifyContent: 'center',
          fontSize: '2rem',
          color: '#fff',
          fontWeight: 'bold',
          flexShrink: 0,
          userSelect: 'none',
        }}>
          {profile.StudName?.charAt(0)?.toUpperCase() ?? '?'}
        </div>
        <div>
          <h3 style={{ fontSize: '1.8rem', margin: 0 }}>{profile.StudName}</h3>
          <span className="role-tag" style={{ marginTop: '6px', display: 'inline-block', width: 'auto', padding: '4px 14px' }}>
            {profile.Role}
          </span>
        </div>
      </div>

      {/* Fields */}
      <div style={{ display: 'flex', flexDirection: 'column', gap: '18px' }}>

        {/* Full Name */}
        <div>
          <label style={{ display: 'block', marginBottom: '4px', fontWeight: 700, color: 'var(--mahogany)', textTransform: 'uppercase', fontSize: '0.72rem', letterSpacing: '1.5px' }}>
            Full Name
          </label>
          {isEditing ? (
            <input
              ref={nameRef}
              type="text"
              value={editForm.StudName ?? ''}
              onChange={(e) => handleChange('StudName', e.target.value, nameRef)}
              style={{ width: '100%', height: '46px', boxSizing: 'border-box', padding: '12px 14px' }}
              placeholder="John Smith"
            />
          ) : (
            <div style={{ padding: '12px 14px', background: 'transparent', borderRadius: '10px', border: '2px solid transparent', fontSize: '1rem', color: 'var(--text)', height: '46px', boxSizing: 'border-box' }}>
              {profile.StudName ?? <span style={{ color: 'var(--text-muted)', fontStyle: 'italic' }}>Not set</span>}
            </div>
          )}
        </div>

        {/* Email */}
        <div>
          <label style={{ display: 'block', marginBottom: '4px', fontWeight: 700, color: 'var(--mahogany)', textTransform: 'uppercase', fontSize: '0.72rem', letterSpacing: '1.5px' }}>
            Email
          </label>
          {isEditing ? (
            <input
              ref={emailRef}
              type="email"
              value={editForm.Email ?? ''}
              onChange={(e) => handleChange('Email', e.target.value, emailRef)}
              style={{ width: '100%', height: '46px', boxSizing: 'border-box', padding: '12px 14px' }}
              placeholder="grandmaster@chess.club"
            />
          ) : (
            <div style={{ padding: '12px 14px', background: 'transparent', borderRadius: '10px', border: '2px solid transparent', fontSize: '1rem', color: 'var(--text)', height: '46px', boxSizing: 'border-box' }}>
              {profile.Email ?? <span style={{ color: 'var(--text-muted)', fontStyle: 'italic' }}>Not set</span>}
            </div>
          )}
        </div>

        {/* Student ID */}
        <div>
          <label style={{ display: 'block', marginBottom: '4px', fontWeight: 700, color: 'var(--mahogany)', textTransform: 'uppercase', fontSize: '0.72rem', letterSpacing: '1.5px' }}>
            Student ID
          </label>
          {isEditing ? (
            <input
              ref={studNumRef}
              type="text"
              value={editForm.StudNum ?? ''}
              onChange={(e) => handleChange('StudNum', e.target.value, studNumRef)}
              style={{ width: '100%', height: '46px', boxSizing: 'border-box', padding: '12px 14px' }}
              placeholder="A123456789"
            />
          ) : (
            <div style={{ padding: '12px 14px', background: 'transparent', borderRadius: '10px', border: '2px solid transparent', fontSize: '1rem', color: 'var(--text)', height: '46px', boxSizing: 'border-box' }}>
              {profile.StudNum ?? <span style={{ color: 'var(--text-muted)', fontStyle: 'italic' }}>Not set</span>}
            </div>
          )}
        </div>

        {/* Rating */}
        <div>
          <label style={{ display: 'block', marginBottom: '4px', fontWeight: 700, color: 'var(--mahogany)', textTransform: 'uppercase', fontSize: '0.72rem', letterSpacing: '1.5px' }}>
            Rating
          </label>
          <div style={{ padding: '12px 14px', background: 'transparent', borderRadius: '10px', border: '2px solid transparent', fontSize: '1rem', color: 'var(--text)', height: '46px', boxSizing: 'border-box' }}>
            {profile.Rating ?? <span style={{ color: 'var(--text-muted)', fontStyle: 'italic' }}>Not set</span>}
          </div>
        </div>

        {/* Last Modified */}
        {profile.LastModified && (
          <div style={{ fontSize: '1rem', color: 'var(--text-muted)', marginTop: '4px' }}>
            Last updated: {new Date(profile.LastModified).toLocaleString()}
          </div>
        )}
      </div>

      {/* Action buttons */}
      <div style={{ display: 'flex', gap: '12px', marginTop: '28px' }}>
        {isEditing ? (
          <>
            <button onClick={handleUpdate} style={{ flex: 1 }}>Save</button>
            <button onClick={cancelEdit} style={{ flex: 1, background: 'var(--oak)' }}>Cancel</button>
          </>
        ) : (
          <button onClick={startEdit} style={{ flex: 1 }}>Edit Profile</button>
        )}
      </div>
    </div>
  );
};

export default Profile;
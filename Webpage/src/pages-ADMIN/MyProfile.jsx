import React, { useEffect, useState, useRef } from 'react';
import { useOutletContext } from 'react-router-dom';
import { supabase } from '../db';
import { Eye, EyeOff } from 'lucide-react';

const Profile = () => {
  const { adminData, setAdminData } = useOutletContext();
  const [profile, setProfile] = useState(null);
  const [loading, setLoading] = useState(true);
  const [isEditing, setIsEditing] = useState(false);
  const [editForm, setEditForm] = useState({});
  const [saveSuccess, setSaveSuccess] = useState(false);

  // States for password visibility toggles
  const [showViewPassword, setShowViewPassword] = useState(false);
  const [showNewPassword, setShowNewPassword] = useState(false);
  const [showConfirmPassword, setShowConfirmPassword] = useState(false);

  // Refs to target inputs for native browser validation tooltips
  const nameRef            = useRef(null);
  const emailRef           = useRef(null);
  const studNumRef         = useRef(null);
  const ratingRef          = useRef(null);
  const passwordRef        = useRef(null);
  const confirmPasswordRef = useRef(null);

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
    setEditForm({
      ...profile,
      NewPassword: '',
      ConfirmPassword: ''
    });
    setIsEditing(true);
    setSaveSuccess(false);
    setShowNewPassword(false);
    setShowConfirmPassword(false);
  };

  const cancelEdit = () => {
    setIsEditing(false);
    setEditForm({});
    nameRef.current?.setCustomValidity('');
    emailRef.current?.setCustomValidity('');
    studNumRef.current?.setCustomValidity('');
    passwordRef.current?.setCustomValidity('');
    confirmPasswordRef.current?.setCustomValidity('');
    setShowViewPassword(false);
    setShowNewPassword(false);
    setShowConfirmPassword(false);
  };

  // Clear native validity on each keystroke — same pattern as the login script
  const handleChange = (field, value, ref) => {
    ref.current?.setCustomValidity('');
    setEditForm(prev => ({ ...prev, [field]: value }));
  };

  const handleUpdate = async () => {
    setSaveSuccess(false);

    const name         = editForm.StudName?.toString().trim();
    const email        = editForm.Email?.toString().trim();
    const studNum      = editForm.StudNum?.toString().trim();
    const rating       = parseInt(editForm.Rating, 10);
    const newPassword  = editForm.NewPassword;
    const confirmPass  = editForm.ConfirmPassword;

    // 1. Prevent Empty Input Fields
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

    // 2. Format Validation: Student ID Pattern (1 Letter + 8 Numbers)
    const studIdRegex = /^[A-Za-z]\d{8}$/;
    if (!studIdRegex.test(studNum)) {
      studNumRef.current.setCustomValidity('Student ID format must be 1 letter followed by 8 numbers (e.g., A12345678).');
      studNumRef.current.reportValidity();
      return;
    }

    // 3. Format Validation: Restrict email domain to umak.edu.ph
    if (!email.toLowerCase().endsWith('@umak.edu.ph')) {
      emailRef.current.setCustomValidity('Emails are restricted to the official institutional domain (@umak.edu.ph).');
      emailRef.current.reportValidity();
      return;
    }

    // 4. New Password validations (Only runs if user attempts to modify password)
    if (newPassword || confirmPass) {
      if (!newPassword) {
        passwordRef.current.setCustomValidity('Please fill out your new password.');
        passwordRef.current.reportValidity();
        return;
      }
      if (newPassword.length < 8) {
        passwordRef.current.setCustomValidity('Password must be at least 8 characters long.');
        passwordRef.current.reportValidity();
        return;
      }
      if (newPassword !== confirmPass) {
        confirmPasswordRef.current.setCustomValidity('Passwords do not match.');
        confirmPasswordRef.current.reportValidity();
        return;
      }
    }

    try {
      const updatedTimestamp = Date.now();
      const updatePayload = {
        Email: email,
        StudName: name,
        StudNum: studNum,
        Rating: rating,
        LastModified: updatedTimestamp,
      };

      // Append password to query if changed safely
      if (newPassword) {
        updatePayload.Password = newPassword;
      }

      const { error } = await supabase
        .schema('Chessistant')
        .from('Profiles')
        .update(updatePayload)
        .eq('UserID', profile.UserID);

      if (error) throw error;

      const updatedUser = {
        ...adminData,
        StudName: name,
        Email: email,
        StudNum: studNum,
      };

      // Update global session data
      localStorage.setItem('currentUser', JSON.stringify(updatedUser));
      if (setAdminData) setAdminData(updatedUser);

      setProfile({
        ...profile,
        StudName: name,
        Email: email,
        StudNum: studNum,
        Rating: rating,
        LastModified: updatedTimestamp,
        ...(newPassword && { Password: newPassword })
      });
      setIsEditing(false);
      setSaveSuccess(true);
      setTimeout(() => setSaveSuccess(false), 3000);
    } catch (err) {
      console.error('Error updating profile:', err.message);
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
  
  const getRoleGradient = (role, isOpen) => {
    switch (role?.toLowerCase()) {
      case 'admin':
        return isOpen 
          ? 'linear-gradient(135deg, #003a8c, #0050b3)' 
          : 'linear-gradient(135deg, #00183b, #002965)';
      case 'member':
        return isOpen 
          ? 'linear-gradient(135deg, #5b966d, #73b386)' 
          : 'linear-gradient(135deg, #2d4c37, #4a7c59)';
      case 'disabled':
        return isOpen 
          ? 'linear-gradient(135deg, #999999, #b3b3b3)' 
          : 'linear-gradient(135deg, #555555, #888888)';
      case 'coach':
      default:
        return isOpen 
          ? 'linear-gradient(135deg, var(--oak), var(--gold-muted))' 
          : 'linear-gradient(135deg, var(--mahogany), var(--oak))';
    }
  };

  return (
    <div className="card" style={{ maxWidth: '750px', margin: '0 auto', width: '100%' }}>

      {/* Header */}
      <div style={{ display: 'flex', alignItems: 'center', gap: '20px', marginBottom: '30px', borderBottom: '2px solid var(--gold)', paddingBottom: '20px' }}>
        <div style={{
          width: '72px',
          height: '72px',
          borderRadius: '50%',
          background: getRoleGradient(adminData.Role, false),
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
        <div style={{ display: 'flex', flexDirection: 'row', justifyContent: 'space-between', width: '100%' }}>
          <div style={{ background: 'transparent', borderRadius: '10px', border: '2px solid transparent', fontSize: '1rem', color: 'var(--text)', boxSizing: 'border-box' }}>
            <h3 style={{ fontSize: '1.8rem', margin: 0 }}>{profile.StudName}</h3>
            <h4>Rating: {profile.Rating ?? <span style={{ color: 'var(--text-muted)', fontStyle: 'italic' }}>Not set</span>}</h4>
          </div>
          <span className={`role-tag role-tag--${adminData.Role?.toLowerCase()}`} style={{ display: 'inline-block', width: 'auto', height: 'fit-content', padding: '5px 20px' }}>
            {profile.Role}
          </span>
        </div>
      </div>

      {/* Fields */}
      <div style={{ display: 'flex', flexDirection: 'column', gap: '18px' }}>
        
        {/* Full Name Row */}
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
              placeholder="e.g. Juan Dela Cruz"
            />
          ) : (
            <div style={{ padding: '12px 14px', background: 'transparent', borderRadius: '10px', border: '2px solid transparent', fontSize: '1rem', color: 'var(--text)', height: '46px', boxSizing: 'border-box' }}>
              {profile.StudName ?? <span style={{ color: 'var(--text-muted)', fontStyle: 'italic' }}>Not set</span>}
            </div>
          )}
        </div>

        {/* Two-Column Form Field Configuration */}
        <div style={{ display: 'flex', flexDirection: 'row', gap: '18px' }}>
          <div style={{ display: 'flex', flexDirection: 'column', gap: '18px', width: '100%' }}>
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
                  placeholder="name@umak.edu.ph"
                />
              ) : (
                <div style={{ padding: '12px 14px', background: 'transparent', borderRadius: '10px', border: '2px solid transparent', fontSize: '1rem', color: 'var(--text)', height: '46px', boxSizing: 'border-box' }}>
                  {profile.Email ?? <span style={{ color: 'var(--text-muted)', fontStyle: 'italic' }}>Not set</span>}
                </div>
              )}
            </div>

            {/* New Password / Current Password */}
            <div>
              {isEditing ? (
                <>
                  <label style={{ display: 'block', marginBottom: '4px', fontWeight: 700, color: 'var(--mahogany)', textTransform: 'uppercase', fontSize: '0.72rem', letterSpacing: '1.5px' }}>
                    New Password
                  </label>
                  <div style={{ position: 'relative' }}>
                    <input
                      ref={passwordRef}
                      type={showNewPassword ? "text" : "password"}
                      value={editForm.NewPassword ?? ''}
                      onChange={(e) => handleChange('NewPassword', e.target.value, passwordRef)}
                      style={{ width: '100%', height: '46px', boxSizing: 'border-box', padding: '12px 40px 12px 14px' }}
                      placeholder="Leave blank to keep unchanged"
                    />
                    <span
                      onClick={() => setShowNewPassword(p => !p)}
                      style={{ position: 'absolute', right: '12px', top: '50%', transform: 'translateY(-50%)', cursor: 'pointer', color: 'var(--text-muted)', display: 'flex', alignItems: 'center' }}
                    >
                      {showNewPassword ? <EyeOff size={18} /> : <Eye size={18} />}
                    </span>
                  </div>
                </>
              ) : (
                <>
                  <label style={{ display: 'block', marginBottom: '4px', fontWeight: 700, color: 'var(--mahogany)', textTransform: 'uppercase', fontSize: '0.72rem', letterSpacing: '1.5px' }}>
                    Password
                  </label>
                  <div style={{ position: 'relative' }}>
                    <div style={{ padding: '12px 40px 12px 14px', background: 'transparent', borderRadius: '10px', border: '2px solid transparent', fontSize: '1rem', color: 'var(--text)', height: '46px', boxSizing: 'border-box', display: 'flex', alignItems: 'center' }}>
                      {showViewPassword ? (profile.Password || <span style={{ color: 'var(--text-muted)', fontStyle: 'italic' }}>Not set</span>) : '••••••••'}
                    </div>
                    <span
                      onClick={() => setShowViewPassword(p => !p)}
                      style={{ position: 'absolute', right: '12px', top: '50%', transform: 'translateY(-50%)', cursor: 'pointer', color: 'var(--text-muted)', display: 'flex', alignItems: 'center' }}
                    >
                      {showViewPassword ? <EyeOff size={18} /> : <Eye size={18} />}
                    </span>
                  </div>
                </>
              )}
            </div>
          </div>
          
          <div style={{ display: 'flex', flexDirection: 'column', gap: '18px',  width: '100%' }}>
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
                  placeholder="A12345678"
                />
              ) : (
                <div style={{ padding: '12px 14px', background: 'transparent', borderRadius: '10px', border: '2px solid transparent', fontSize: '1rem', color: 'var(--text)', height: '46px', boxSizing: 'border-box' }}>
                  {profile.StudNum ?? <span style={{ color: 'var(--text-muted)', fontStyle: 'italic' }}>Not set</span>}
                </div>
              )}
            </div>

            {/* Confirm Password */}
            <div>
              {isEditing ? (
                <>
                  <label style={{ display: 'block', marginBottom: '4px', fontWeight: 700, color: 'var(--mahogany)', textTransform: 'uppercase', fontSize: '0.72rem', letterSpacing: '1.5px' }}>
                    Confirm Password
                  </label>
                  <div style={{ position: 'relative' }}>
                    <input
                      ref={confirmPasswordRef}
                      type={showConfirmPassword ? "text" : "password"}
                      value={editForm.ConfirmPassword ?? ''}
                      onChange={(e) => handleChange('ConfirmPassword', e.target.value, confirmPasswordRef)}
                      style={{ width: '100%', height: '46px', boxSizing: 'border-box', padding: '12px 40px 12px 14px' }}
                      placeholder="Confirm new password"
                    />
                    <span
                      onClick={() => setShowConfirmPassword(p => !p)}
                      style={{ position: 'absolute', right: '12px', top: '50%', transform: 'translateY(-50%)', cursor: 'pointer', color: 'var(--text-muted)', display: 'flex', alignItems: 'center' }}
                    >
                      {showConfirmPassword ? <EyeOff size={18} /> : <Eye size={18} />}
                    </span>
                  </div>
                </>
              ) : (
                <></>
              )}
            </div>
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
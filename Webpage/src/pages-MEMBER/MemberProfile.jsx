import React from 'react';
import { useOutletContext } from 'react-router-dom';

const MemberProfile = () => {
  const { memberData } = useOutletContext();

  return (
    <div className="member-profile">
      <h1>My Profile</h1>
      
      <div className="profile-card">
        <h2>{memberData?.StudName}</h2>
        <p><strong>Student Number:</strong> {memberData?.StudNum}</p>
        <p><strong>Email:</strong> {memberData?.Email}</p>
        <p><strong>Role:</strong> {memberData?.Role}</p>
      </div>

      <div className="profile-section">
        <h3>Edit Profile</h3>
        <p>Profile editing feature coming soon...</p>
      </div>
    </div>
  );
};

export default MemberProfile;

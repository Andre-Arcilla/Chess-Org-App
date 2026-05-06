import React from 'react';
import { useOutletContext } from 'react-router-dom';

const MemberHome = () => {
  const { memberData } = useOutletContext();

  return (
    <div className="member-home">
      <h1>Member Home</h1>
      <p>Welcome to your member portal!</p>
      
      <div className="member-info">
        <h3>Quick Stats</h3>
        <ul>
          <li>Name: {memberData?.StudName}</li>
          <li>Student Number: {memberData?.StudNum}</li>
          <li>Email: {memberData?.Email}</li>
        </ul>
      </div>
    </div>
  );
};

export default MemberHome;

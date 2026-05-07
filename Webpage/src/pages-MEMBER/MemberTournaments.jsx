import React from 'react';
import { useOutletContext } from 'react-router-dom';

const MemberTournaments = () => {
  const { memberData } = useOutletContext();

  return (
    <div className="member-tournaments">
      <h1>Tournaments</h1>
      
      <div className="tournaments-section">
        <h2>Available Tournaments</h2>
        <p>No tournaments available at this time.</p>
      </div>

      <div className="tournaments-section">
        <h2>My Past Tournaments</h2>
        <p>You haven't participated in any tournaments yet.</p>
      </div>
    </div>
  );
};

export default MemberTournaments;

import React from 'react';

const Overview = () => {
  return (
    <div className="card">
      <h3 style={{ fontSize: '2rem' }}>Club Statistics</h3>
      <p>Current overview of your elite chess organization.</p>
      
      <div className="placeholder-stats">
        <div className="stat-item">
          <span className="label">Registered Members</span>
          <span className="value">124</span>
        </div>
        <div className="stat-item">
          <span className="label">Active Matches</span>
          <span className="value">12</span>
        </div>
        <div className="stat-item">
          <span className="label">Pending Applications</span>
          <span className="value">3</span>
        </div>
      </div>
    </div>
  );
};

export default Overview;

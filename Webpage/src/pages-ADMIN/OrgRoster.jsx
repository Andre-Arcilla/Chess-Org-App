import React, { useEffect, useState } from 'react';
import { supabase } from '../db';

const OrgRoster = () => {
  const [roster, setRoster] = useState([]);
  const [loading, setLoading] = useState(true);
  const [selectedMember, setSelectedMember] = useState(null);
  const [games, setGames] = useState([]);
  const [loadingGames, setLoadingGames] = useState(false);

  const fetchRoster = async () => {
    try {
      setLoading(true);
      const { data, error } = await supabase
        .schema('Chessistant')
        .from('OrgRoster')
        .select('*');
      if (error) throw error;
      setRoster(data || []);
    } catch (err) {
      console.error('Error fetching roster:', err.message);
    } finally {
      setLoading(false);
    }
  };

  const fetchGames = async (studNum) => {
    try {
      setLoadingGames(true);
      const { data, error } = await supabase
        .schema('Chessistant')
        .from('ChessGames')
        .select('*')
        .eq('StudNum', studNum)
        .order('Date', { ascending: false });
      if (error) throw error;
      setGames(data || []);
    } catch (err) {
      console.error('Error fetching games:', err.message);
    } finally {
      setLoadingGames(false);
    }
  };

  useEffect(() => {
    fetchRoster();
  }, []);

  const handleSelectMember = (member) => {
    setSelectedMember(member);
    fetchGames(member.StudNum);
  };

  const displayDate = (dateVal) => {
    if (!dateVal) return '';
    const d = new Date(dateVal);
    return isNaN(d.getTime()) ? '' : d.toLocaleDateString();
  };

  if (loading) return <p>Loading Roster...</p>;

  return (
    <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '30px' }}>
      <div className="card">
        <h3>Organization Roster</h3>
        <p>Select a member to view their game history.</p>
        <div style={{ marginTop: '20px' }}>
          {roster.map((member) => (
            <div 
              key={member.MmbrID} 
              onClick={() => handleSelectMember(member)}
              style={{ 
                padding: '15px', 
                background: selectedMember?.MmbrID === member.MmbrID ? 'var(--antique-white)' : 'white',
                border: '1px solid var(--oak)',
                cursor: 'pointer',
                marginBottom: '10px',
                transition: 'all 0.2s'
              }}
            >
              <strong>{member.StudName}</strong>
              <div style={{ fontSize: '0.8rem', color: 'var(--text-muted)' }}>ID: {member.StudNum}</div>
            </div>
          ))}
        </div>
      </div>

      <div className="card">
        <h3>Game History</h3>
        {!selectedMember ? (
          <p>No member selected.</p>
        ) : loadingGames ? (
          <p>Loading History for {selectedMember.StudName}...</p>
        ) : (
          <div>
            <h4>Matches for {selectedMember.StudName}</h4>
            {games.length === 0 ? (
              <p style={{ marginTop: '20px' }}>No recorded games found.</p>
            ) : (
              <div style={{ marginTop: '20px' }}>
                {games.map((game) => (
                  <div key={game.GameID} style={{ borderBottom: '1px solid var(--oak)', padding: '10px 0' }}>
                    <div style={{ display: 'flex', justifyContent: 'space-between' }}>
                      <strong>Game #{game.GameNum}</strong>
                      <span className="label">{displayDate(game.Date)}</span>
                    </div>
                    <div style={{ fontSize: '0.9rem', marginTop: '5px' }}>
                      Result: <span style={{ color: game.Result === 'Win' ? 'green' : (game.Result === 'Loss' ? 'var(--error)' : 'orange'), fontWeight: 'bold' }}>{game.Result}</span>
                      <span style={{ marginLeft: '10px', fontStyle: 'italic' }}>Color: {game.PlayerColor}</span>
                    </div>
                    <div style={{ fontSize: '0.8rem', color: 'var(--text-muted)', marginTop: '5px' }}>
                      PGN: {game.PGN?.substring(0, 50)}...
                    </div>
                  </div>
                ))}
              </div>
            )}
          </div>
        )}
      </div>
    </div>
  );
};

export default OrgRoster;

import React, { useEffect, useState } from 'react';
import { supabase } from '../db';
import RoundRobinCrossTable from '../components/RoundRobinCrossTable';

const Tournaments = () => {
  const [tournaments, setTournaments] = useState([]);
  const [loading, setLoading] = useState(true);
  const [selectedTournament, setSelectedTournament] = useState(null);
  const [view, setView] = useState('upcoming');
  const [results, setResults] = useState({});
  const [registeredTournaments, setRegisteredTournaments] = useState(new Set());
  const [studentNum, setStudentNum] = useState(null);
  const [participants, setParticipants] = useState([]);

  const fetchParticipants = async (tourId) => {
    if (!tourId) return;
    try {
      const { data, error } = await supabase
        .schema('Chessistant')
        .from('TournamentParticipants')
        .select('TPID,StudNum')
        .eq('TourID', tourId);

      if (error) throw error;

      const studentIds = Array.from(new Set((data || []).map((p) => p.StudNum).filter(Boolean)));
      let profileMap = {};

      if (studentIds.length > 0) {
        const { data: profiles, error: profileError } = await supabase
          .schema('Chessistant')
          .from('Profiles')
          .select('StudNum,StudName')
          .in('StudNum', studentIds);

        if (profileError) throw profileError;

        profileMap = Object.fromEntries((profiles || []).map((profile) => [profile.StudNum, profile.StudName]));
      }

      setParticipants((data || []).map((p) => ({
        StudNum: p.StudNum,
        StudName: profileMap[p.StudNum] || 'Unknown Player'
      })));
    } catch (err) {
      console.error('Error fetching participants:', err);
    }
  };

  const fetchTournaments = async () => {
    try {
      setLoading(true);
      const now = new Date().toISOString();
      let query = supabase
        .schema('Chessistant')
        .from('Tournaments')
        .select('*');

      if (view === 'upcoming') {
        query = query.gte('Date', now).order('Date', { ascending: true });
      } else {
        query = query.lt('Date', now).order('Date', { ascending: false });
      }
      
      const { data, error } = await query;
      
      if (error) throw error;
      setTournaments(data || []);
    } catch (err) {
      console.error('Error fetching tournaments:', err);
    } finally {
      setLoading(false);
    }
  };

  const fetchRegistrations = async (studNum) => {
    try {
      // Trying both names since there was confusion earlier
      let { data, error } = await supabase
        .schema('Chessistant')
        .from('TournamentParticipants')
        .select('TourID')
        .eq('StudNum', studNum);
      
      if (error) {
        const { data: data2, error: error2 } = await supabase
          .schema('Chessistant')
          .from('TournamentParticipants')
          .select('TourID')
          .eq('StudNum', studNum);
        if (error2) throw error2;
        data = data2;
      }
      
      const tourIds = new Set(data?.map(r => r.TourID) || []);
      setRegisteredTournaments(tourIds);
    } catch (err) {
      console.error('Error fetching registrations:', err);
    }
  };

  const toggleRegistration = async (tourId) => {
    if (!studentNum) {
      console.error('No student number available');
      return;
    }

    const table = 'TournamentParticipants'; // Primary choice

    try {
      if (registeredTournaments.has(tourId)) {
        // Unregister
        let { error } = await supabase
          .schema('Chessistant')
          .from(table)
          .delete()
          .eq('TourID', tourId)
          .eq('StudNum', studentNum);
        
        if (error) {
          const { error: error2 } = await supabase
            .schema('Chessistant')
            .from('TournamentParticipants')
            .delete()
            .eq('TourID', tourId)
            .eq('StudNum', studentNum);
          if (error2) throw error2;
        }
        
        setRegisteredTournaments(prev => {
          const updated = new Set(prev);
          updated.delete(tourId);
          return updated;
        });
      } else {
        // Register
        let { error } = await supabase
          .schema('Chessistant')
          .from(table)
          .insert([{ TourID: tourId, StudNum: studentNum }]);
        
        if (error) {
          const { error: error2 } = await supabase
            .schema('Chessistant')
            .from('TournamentParticipants')
            .insert([{ TourID: tourId, StudNum: studentNum }]);
          if (error2) throw error2;
        }
        
        setRegisteredTournaments(prev => new Set([...prev, tourId]));
      }
      // Manually trigger participant refresh for immediate UI feedback
      fetchParticipants(tourId);
    } catch (err) {
      console.error('Error toggling registration:', err);
    }
  };

  useEffect(() => {
    fetchTournaments();
  }, [view]);

  useEffect(() => {
    const initializeStudent = async () => {
      try {
        const userData = localStorage.getItem('currentUser');
        if (userData) {
          const student = JSON.parse(userData);
          setStudentNum(student.StudNum);
          await fetchRegistrations(student.StudNum);
        }
      } catch (err) {
        console.error('Error initializing student data:', err);
      }
    };

    initializeStudent();
  }, []);

  // Real-time subscription for participants
  useEffect(() => {
    if (!selectedTournament?.TourID) return;

    fetchParticipants(selectedTournament.TourID);

    const subscription = supabase
      .channel('participants_changes')
      .on(
        'postgres_changes',
        {
          event: '*',
          schema: 'Chessistant',
          table: 'TournamentParticipants'
        },
        () => {
          fetchParticipants(selectedTournament.TourID);
        }
      )
      .on(
        'postgres_changes',
        {
          event: '*',
          schema: 'Chessistant',
          table: 'TournamentParticipants'
        },
        () => {
          fetchParticipants(selectedTournament.TourID);
        }
      )
      .subscribe();

    return () => {
      supabase.removeChannel(subscription);
    };
  }, [selectedTournament?.TourID]);

  const openModal = (tour) => {
    setSelectedTournament(tour);
  };

  const closeModal = () => {
    setSelectedTournament(null);
  };

  const displayDate = (dateVal) => {
    if (!dateVal) return '';
    const d = new Date(dateVal);
    return isNaN(d.getTime()) ? '' : d.toLocaleString([], { 
      month: 'short', 
      day: 'numeric', 
      year: 'numeric', 
      hour: '2-digit', 
      hour12: true
    });
  };

  return (
    <div className="card">
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', alignItems: 'flex-start', overflowY: 'hidden', scrollbarGutter: 'stable both-edges' }}>
        <div style={{ display: 'flex', flexDirection: 'column', gap: '10px'}}>
          <h3 style={{ fontSize: '2rem' }}>Tournament Management</h3>
          <div style={{ display: 'flex', flexDirection: 'row', gap: '10px', alignItems: 'center' }}> 
            <h3 style={{ fontSize: '1rem', margin: 0 }}>Sort by:</h3>
            <button 
              onClick={() => setView('upcoming')}
              style={{ 
                width: 'fit-content', 
                height: 'fit-content', 
                padding: '6px 12px', 
                fontSize: '0.75rem',
                backgroundColor: view === 'upcoming' ? 'var(--gold)' : 'var(--oak-muted, #5d4037)',
                color: view === 'upcoming' ? 'var(--mahogany)' : 'var(--antique-white, #f5f5dc)',
                border: '2px solid',
                borderColor: view === 'upcoming' ? 'var(--mahogany)' : 'var(--oak)',
                cursor: 'pointer',
                fontWeight: view === 'upcoming' ? 'bold' : 'normal',
                transition: 'background-color 0.3s ease, color 0.3s ease, border-color 0.3s ease',
                boxSizing: 'border-box'
              }}
            >
              Upcoming Tournaments
            </button>
            <button 
              onClick={() => setView('past')}
              style={{ 
                width: 'fit-content', 
                height: 'fit-content', 
                padding: '6px 12px', 
                fontSize: '0.75rem',
                backgroundColor: view === 'past' ? 'var(--gold)' : 'var(--oak-muted, #5d4037)',
                color: view === 'past' ? 'var(--mahogany)' : 'var(--antique-white, #f5f5dc)',
                border: '2px solid',
                borderColor: view === 'past' ? 'var(--mahogany)' : 'var(--oak)',
                cursor: 'pointer',
                fontWeight: view === 'past' ? 'bold' : 'normal',
                transition: 'background-color 0.3s ease, color 0.3s ease, border-color 0.3s ease',
                boxSizing: 'border-box'
              }}
            >
              Past Tournaments
            </button>
          </div>
        </div>
      </div>

      <div style={{ marginTop: '30px' }}>
        {loading ? (
          <p>Loading...</p>
        ) : tournaments.length === 0 ? (
          <p>No pending applications.</p>
        ) : (
          tournaments.map((tour) => (
          <div 
            key={tour.TourID} 
            className="stat-item" 
            style={{ marginBottom: '20px', borderLeft: '5px solid var(--gold)', position: 'relative', cursor: 'pointer' }}
            onClick={() => openModal(tour)}
          >
            <span className="label">{displayDate(tour.Date)}</span>
            <span className="value" style={{ fontSize: '1.5rem' }}>{tour.Title}</span>
            <p style={{ marginTop: '10px', color: 'var(--text-muted)', display: '-webkit-box', WebkitLineClamp: '1', WebkitBoxOrient: 'vertical', overflow: 'hidden' }}>{tour.Text}</p>
          </div>
          ))
        )}
      </div>

      {selectedTournament && (
        <div className="modal-overlay" onClick={closeModal}>
          <div className="modal-content" onClick={(e) => e.stopPropagation()}>
            <span className="modal-close" onClick={closeModal}>&times;</span>

            <div className="modal-info">
              {/* scrollable container for header and body */}
              <div className="modal-scroll"> 
                {/* modal header */}
                <div style={{ textAlign: 'center', padding: '20px 10px 0px 10px', flexShrink: 0 }}>
                  <h2 style={{ color: 'var(--mahogany)', fontSize: '2.5rem' }}>{selectedTournament.Title}</h2>
                  <div style={{ marginTop: '10px', width: '100%', display: 'flex', flexDirection: 'column', alignItems: 'center' }}>
                    <span className="label" style={{ color: 'var(--gold-muted)', fontWeight: 'bold' }}>{displayDate(selectedTournament.Date)}</span>
                    <span className="label" style={{ color: 'var(--gold-muted)', fontWeight: 'bold' }}>{selectedTournament.ParticipantCount} Player {selectedTournament.Style} Tournament</span>
                  </div>
                  <hr className="modal-hr" style={{ marginTop: '20px' }} />
                </div>

                {/* modal body */}
                <div className="modal-body">
                  <p style={{ whiteSpace: 'pre-wrap' }}>{selectedTournament.Text}</p>
                </div>
              </div>

              <div className="tournament-bracket" style={{ flexBasis: "60%", overflow: 'auto' }}>
                {selectedTournament.Style === 'Round Robin' ? (
                  <RoundRobinCrossTable 
                    participantCount={selectedTournament.ParticipantCount}
                    results={results}
                    setResults={setResults}
                    readOnly={true}
                    participants={participants}
                  />
                ) : (
                  <div style={{ 
                    height: '100%', 
                    display: 'flex', 
                    flexDirection: 'column',
                    justifyContent: 'center', 
                    alignItems: 'center', 
                    padding: '40px',
                    textAlign: 'center',
                    color: 'var(--text-muted)'
                  }}>
                    <h4 style={{ fontFamily: 'var(--font-serif)', color: 'var(--mahogany)', marginBottom: '10px' }}>Swiss System Bracket</h4>
                    <p style={{ fontStyle: 'italic' }}>Bracket generation for Swiss System is currently being optimized for large participant counts. Check back soon for the automated pairing display!</p>
                  </div>
                )}
              </div>

            </div>
            
            {/* modal footer */}
            <div style={{ padding: '0px 10px 20px 10px', backgroundColor: 'var(--parchment)', flexShrink: 0, overflowY: 'hidden', scrollbarGutter: 'stable both-edges' }}>
              <hr className="modal-hr" />
              <br></br>
              <div style={{ display: 'flex', justifyContent: 'flex-end', gap: '10px' }}>
                <button 
                  onClick={() => toggleRegistration(selectedTournament.TourID)}
                  style={{ padding: '10px 20px', margin: '0', fontSize: '0.9rem', width: 'auto', }}>
                  {registeredTournaments.has(selectedTournament.TourID) ? 'Unregister' : 'Register'}
                </button>
              </div>
            </div>
          </div>
        </div>
      )}
    </div>
  );
};

export default Tournaments;

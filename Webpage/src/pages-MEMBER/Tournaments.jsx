import React, { useEffect, useState } from 'react';
import { useOutletContext, useLocation } from 'react-router-dom';
import { supabase } from '../db';
import RoundRobinCrossTable from '../components/RoundRobinCrossTable';
import { Megaphone, Trophy, Users, Calendar, Save, Pencil, Trash2, Check, X, User } from 'lucide-react';

const Tournaments = () => {
  const { adminData } = useOutletContext();
  const [tournaments, setTournaments] = useState([]);
  const [loading, setLoading] = useState(true);
  const [selectedTournament, setSelectedTournament] = useState(null);
  const [view, setView] = useState('upcoming');
  const [results, setResults] = useState({});
  const [participants, setParticipants] = useState([]);
  const [registeredTournaments, setRegisteredTournaments] = useState(new Set());
  const [studentNum, setStudentNum] = useState(null);
  const [canUnregisterCurrent, setCanUnregisterCurrent] = useState(true);

  const location = useLocation();

  // ─── Data Fetching ────────────────────────────────────────────────────────

  const fetchMatches = async (tourId, currentParticipants) => {
    if (!tourId || !currentParticipants || currentParticipants.length === 0) return;
    try {
      const { data, error } = await supabase
        .schema('Chessistant')
        .from('TournamentMatches')
        .select('*')
        .eq('tourid', tourId);

      if (error) throw error;

      const newResults = {};
      const studNumToIndex = Object.fromEntries(currentParticipants.map((p, i) => [p.StudNum, i]));

      (data || []).forEach(match => {
        const idx1 = studNumToIndex[match.player1];
        const idx2 = studNumToIndex[match.player2];

        if (idx1 !== undefined && idx2 !== undefined) {
          newResults[`${idx1}-${idx2}`] = match.player1result.toString();
          newResults[`${idx2}-${idx1}`] = match.player2result.toString();
        }
      });

      setResults(newResults);
    } catch (err) {
      console.error('Error fetching matches:', err);
    }
  };

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

      const updatedParticipants = (data || []).map((p) => ({
        StudNum: p.StudNum,
        StudName: profileMap[p.StudNum] || 'Unknown Player'
      }));
      setParticipants(updatedParticipants);

      // Fetch matches after participants are loaded so we can map StudNum → index
      fetchMatches(tourId, updatedParticipants);
    } catch (err) {
      console.error('Error fetching participants:', err);
    }
  };

  const fetchTournaments = async () => {
    try {
      setLoading(true);

      const minimumDelay = new Promise(resolve => setTimeout(resolve, 750));
      const [_, { data, error }] = await Promise.all([
        minimumDelay,
        supabase
          .schema('Chessistant')
          .from('Tournaments')
          .select('*')
      ]);

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
      const { data, error } = await supabase
        .schema('Chessistant')
        .from('TournamentParticipants')
        .select('TourID')
        .eq('StudNum', studNum);

      if (error) throw error;

      setRegisteredTournaments(new Set(data?.map(r => r.TourID) || []));
    } catch (err) {
      console.error('Error fetching registrations:', err);
    }
  };

  // ─── Registration Logic (from MemberTournaments) ──────────────────────────

  const checkCanUnregister = async (tourId) => {
    if (!studentNum || !tourId) return true;

    const { data: matches, error } = await supabase
      .schema('Chessistant')
      .from('TournamentMatches')
      .select('matchid')
      .eq('tourid', tourId)
      .or(`player1.eq.${studentNum},player2.eq.${studentNum}`)
      .limit(1);

    if (error) { console.error(error); return true; }

    const canUnregister = !matches || matches.length === 0;
    setCanUnregisterCurrent(canUnregister);
    return canUnregister;
  };

  const toggleRegistration = async (tourId) => {
    if (!studentNum) {
      console.error('No student number available');
      return;
    }

    try {
      if (registeredTournaments.has(tourId)) {
        const canUnregister = await checkCanUnregister(tourId);
        if (!canUnregister) {
          alert('Cannot unregister: You already have scores recorded in this tournament.');
          return;
        }

        const { error } = await supabase
          .schema('Chessistant')
          .from('TournamentParticipants')
          .delete()
          .eq('TourID', tourId)
          .eq('StudNum', studentNum);

        if (error) throw error;

        setRegisteredTournaments(prev => {
          const updated = new Set(prev);
          updated.delete(tourId);
          return updated;
        });
      } else {
        const { error } = await supabase
          .schema('Chessistant')
          .from('TournamentParticipants')
          .insert([{ TourID: tourId, StudNum: studentNum }]);

        if (error) throw error;

        setRegisteredTournaments(prev => new Set([...prev, tourId]));
      }

      fetchParticipants(tourId);
      checkCanUnregister(tourId);
    } catch (err) {
      console.error('Error toggling registration:', err);
    }
  };

  // ─── Modal ────────────────────────────────────────────────────────────────

  const openModal = (tour) => {
    // Reset to prevent "ghosting" from a previously viewed tournament
    setResults({});
    setParticipants([]);
    setCanUnregisterCurrent(true);
    setSelectedTournament(tour);
    checkCanUnregister(tour.TourID);
  };

  const closeModal = () => {
    setSelectedTournament(null);
    setResults({});
    setParticipants([]);
  };

  // ─── Effects ──────────────────────────────────────────────────────────────

  useEffect(() => {
    fetchTournaments();
  }, []);

  // Read student number from localStorage and pre-fetch registrations
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

  // Auto-open a specific tournament if navigated here with location.state.openId
  useEffect(() => {
    if (loading || !location.state?.openId || tournaments.length === 0) return;
    const tour = tournaments.find(t => t.TourID === location.state.openId);
    if (tour) openModal(tour);
  }, [loading, tournaments]);

  // Subscribe to real-time participant changes while a tournament is open
  useEffect(() => {
    if (!selectedTournament?.TourID) return;

    fetchParticipants(selectedTournament.TourID);

    const subscription = supabase
      .channel('participants_changes')
      .on(
        'postgres_changes',
        { event: '*', schema: 'Chessistant', table: 'TournamentParticipants' },
        () => { fetchParticipants(selectedTournament.TourID); }
      )
      .subscribe();

    return () => {
      supabase.removeChannel(subscription);
    };
  }, [selectedTournament?.TourID]);

  // ─── Helpers ──────────────────────────────────────────────────────────────

  const displayDate = (dateVal) => {
    if (!dateVal) return '';
    const d = new Date(dateVal);
    return isNaN(d.getTime()) ? '' : d.toLocaleString([], {
      month: 'short',
      day: 'numeric',
      year: 'numeric',
      hour: '2-digit',
      minute: '2-digit',
      hour12: true
    });
  };

  const nowStr = new Date().toISOString();
  const displayedTournaments = [...tournaments]
    .filter(tour => view === 'upcoming' ? tour.Date >= nowStr : tour.Date < nowStr)
    .sort((a, b) => view === 'upcoming'
      ? new Date(a.Date) - new Date(b.Date)   // Soonest first for upcoming
      : new Date(b.Date) - new Date(a.Date)   // Most recent first for past
    );

  if (loading) return (
    <div className="overlay">
      <div className="spinner"></div>
      <p>Loading Tournaments...</p>
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
    <div className="card">
      {/* Header + sort toggles */}
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start', overflowY: 'hidden', scrollbarGutter: 'stable both-edges' }}>
        <div style={{ display: 'flex', flexDirection: 'column', gap: '10px' }}>
          <h3 style={{ fontSize: '2rem' }}>Tournaments</h3>
          <div className="sort" style={{ display: 'flex', flexDirection: 'row', gap: '10px', alignItems: 'center' }}>
            <h3 style={{ fontSize: '1rem', margin: 0 }}>Sort by:</h3>
            <button
              onClick={() => setView('upcoming')}
              style={{
                width: 'fit-content',
                height: 'fit-content',
                padding: '6px 12px',
                fontSize: '0.75rem',
                backgroundColor: view === 'past' ? '#002965' : '#FF5A00',
                border: 'none',
                cursor: 'pointer',
                fontWeight: view === 'upcoming' ? 'bold' : 'normal',
                boxSizing: 'border-box'
              }}
            >
              Upcoming
            </button>
            <button
              onClick={() => setView('past')}
              style={{
                width: 'fit-content',
                height: 'fit-content',
                padding: '6px 12px',
                fontSize: '0.75rem',
                backgroundColor: view === 'past' ? '#FF5A00' : '#002965',
                border: 'none',
                cursor: 'pointer',
                fontWeight: view === 'past' ? 'bold' : 'normal',
                boxSizing: 'border-box'
              }}
            >
              Past
            </button>
          </div>
        </div>
      </div>

      {/* Tournament list */}
      <div className="stat-container" style={{ marginTop: '20px', display: 'flex', flexDirection: 'column', gap: '20px' }}>
        {displayedTournaments.length === 0 ? (
          <p>No tournaments found.</p>
        ) : (
          displayedTournaments.map((tour) => (
            <div
              key={tour.TourID}
              className="stat-item tour-card"
              style={{ position: 'relative', cursor: 'pointer', justifyContent: 'space-between', height: '150px', boxSizing: 'border-box', width: '100%', gap: '0', borderRadius: '15px', paddingLeft: '28px' }}
              onClick={() => openModal(tour)}
            >
              <span className="label">{displayDate(tour.Date)}</span>
              <span className="value" style={{ fontSize: '1.5rem' }}>{tour.Title}</span>
              <p style={{ display: '-webkit-box', WebkitLineClamp: '1', WebkitBoxOrient: 'vertical', overflow: 'hidden' }}>{tour.Text}</p>
            </div>
          ))
        )}
      </div>

      {/* Modal */}
      {selectedTournament && (
        <div className="modal-overlay" onClick={closeModal}>
          <div className="modal-content" onClick={(e) => e.stopPropagation()}>
            <span className="modal-close" onClick={closeModal}>&times;</span>

            <div className="ann-modal-header" style={{ background: getRoleGradient(adminData.Role, false) }}>
              <div className="ann-modal-badge"><Trophy size={15} strokeWidth={4} /> Tournament</div>
              <h2 className="ann-modal-title">{selectedTournament.Title}</h2>
            </div>

            {/* ── Meta strip ── */}
            <div className="ann-modal-meta">
              <span className="ann-modal-meta-date">
                <Calendar size={15} strokeWidth={4} /> {displayDate(selectedTournament.Date)}
              </span>
              <span className="ann-modal-meta-sep">·</span>
              <span className="ann-modal-meta-date">
                <User size={15} strokeWidth={4} /> {selectedTournament.ParticipantCount}-Player {selectedTournament.Style}
              </span>
              {selectedTournament.LastModified && (
                <>
                  <span className="ann-modal-meta-sep">·</span>
                  <span className="ann-modal-meta-edited">
                    Last updated: {new Date(selectedTournament.LastModified).toLocaleString()}
                  </span>
                </>
              )}
            </div>

            {/* ── Two-column body: description (left) + bracket (right) ── */}
            <div className="modal-info">
              <div className="modal-scroll">
                <p className="ann-modal-body-text">{selectedTournament.Text}</p>
              </div>

              {/* Right panel: bracket / cross table (read-only) */}
              <div className="tournament-bracket" style={{ flexBasis: '60%', overflow: 'auto', padding: '0 20px 0 0' }}>
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

            {/* ── Footer: register / unregister ── */}
            <div className="ann-modal-footer">
              <button
                onClick={() => toggleRegistration(selectedTournament.TourID)}
                style={{
                  padding: '10px 24px',
                  margin: 0,
                  fontSize: '0.85rem',
                  width: 'auto',
                  background: registeredTournaments.has(selectedTournament.TourID) ? 'var(--error)' : '#002965',
                  cursor: (!canUnregisterCurrent || view === 'past') ? 'default' : 'pointer'
                }}
                disabled={!canUnregisterCurrent || view === 'past'}
              >
                {registeredTournaments.has(selectedTournament.TourID) ? <><X size={15} strokeWidth={4} /> Unregister</> : <><Check size={15} strokeWidth={4} /> Register</>}
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
};

export default Tournaments;
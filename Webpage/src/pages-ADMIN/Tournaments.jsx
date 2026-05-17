import React, { useEffect, useState } from 'react';
import { useOutletContext } from 'react-router-dom';
import { supabase } from '../db';
import RoundRobinCrossTable from '../components/RoundRobinCrossTable';

const Tournaments = () => {
  const { adminData } = useOutletContext();
  const [tournaments, setTournaments] = useState([]);
  const [loading, setLoading] = useState(true);
  const [editingId, setEditingId] = useState(null);
  const [isModalEditing, setIsModalEditing] = useState(false);
  const [isModalAdding, setIsModalAdding] = useState(false);
  const [selectedTournament, setSelectedTournament] = useState(null);
  const [formData, setFormData] = useState({ 
    Title: '', 
    Date: '', 
    Hour: '09', 
    Style: 'Round Robin', 
    ParticipantCount: 10, 
    Text: '' 
  });
  const [view, setView] = useState('upcoming');
  const [results, setResults] = useState({});
  const [participants, setParticipants] = useState([]);

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
      
      // Fetch matches after participants are loaded so we can map StudNum to index
      fetchMatches(tourId, updatedParticipants);
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

  useEffect(() => {
    fetchTournaments();
  }, [view]);

  // Real-time subscription for participants
  useEffect(() => {
    if (!selectedTournament?.TourID) return;

    fetchParticipants(selectedTournament.TourID);

    const subscription = supabase
      .channel('participants_changes_admin')
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
    // Reset data for the new selection to prevent "ghosting" from previous tournament
    setResults({});
    setParticipants([]);
    setSelectedTournament(tour);
    
    // If no TourID, it's a new tournament - go into edit mode
    if (!tour.TourID) {
      setIsModalEditing(true);
      setIsModalAdding(true);
      setEditingId(null);
      // tour.Date is expected to be 'YYYY-MM-DDTHH:00' here if called from Add button
      const [d, t] = (tour.Date || '').split('T');
      setFormData({ 
        Title: tour.Title || '', 
        Date: d || '', 
        Hour: (t || '09:00').split(':')[0], 
        Style: 'Round Robin',
        ParticipantCount: tour.ParticipantCount || 10,
        Text: tour.Text || ''
      });
    } else {
      setFormData({ 
        Title: tour.Title || '', 
        Date: (tour.Date || '').split('T')[0], 
        Hour: (tour.Date || 'T09').split('T')[1].split(':')[0], 
        Style: 'Round Robin',
        ParticipantCount: tour.ParticipantCount || 10,
        Text: tour.Text || ''
      });
      setIsModalEditing(false);
      setIsModalAdding(false);
    }
  };

  const closeModal = () => {
    setSelectedTournament(null);
    setIsModalEditing(false);
    setIsModalAdding(false);
    setResults({});
    setParticipants([]);
  };

  const handleSubmit = async (e) => {
    if (e && e.preventDefault) {
      e.preventDefault();
    }

    const combinedDate = new Date(`${formData.Date}T${formData.Hour}:00`);

    if (combinedDate < new Date()) {
      alert('Cannot set a tournament date in the past!');
      return;
    }

    try {
      const payload = {
        Title: formData.Title,
        Text: formData.Text,
        Style: 'Round Robin',
        ParticipantCount: formData.ParticipantCount,
        // The Date column expects a standard Timestamp string (ISO)
        Date: combinedDate.toISOString(), 
        // FIX 1: LastModified requires a BigInt integer!
        LastModified: Date.now() 
      };

      if (editingId) {
        // FIX 2: Use StudNum for the foreign key
        payload.LastEditor = adminData?.StudNum;
        const { error } = await supabase
          .schema('Chessistant')
          .from('Tournaments')
          .update(payload)
          .eq('TourID', editingId);
        if (error) throw error;
        setSelectedTournament({ ...selectedTournament, ...payload });
      } else {
        // FIX 3: Use StudNum and satisfy the not-null constraint for BOTH fields
        payload.Author = adminData?.StudNum;
        payload.LastEditor = adminData?.StudNum;
        
        const { data, error } = await supabase
          .schema('Chessistant')
          .from('Tournaments')
          .insert([payload])
          .select();
        if (error) throw error;
        if (data && data[0]) {
          setSelectedTournament(data[0]);
          setEditingId(data[0].TourID);
        }
      }

      fetchTournaments();
      setIsModalEditing(false);
      setIsModalAdding(false);
    } catch (err) {
      console.error('Error saving tournament:', JSON.stringify(err, null, 2));
      alert('Save failed! Check the console for details.');
    }
  };

  const handleSaveScores = async () => {
    if (!selectedTournament?.TourID) return;
    
    try {
      // 1. Identify matches to save. We only save pairs where row < col to avoid duplicates
      const matchesToSave = [];
      const participantCount = participants.length;

      for (let i = 0; i < participantCount; i++) {
        for (let j = i + 1; j < participantCount; j++) {
          const res1 = results[`${i}-${j}`];
          const res2 = results[`${j}-${i}`];

          // Only save if at least one result is present
          if (res1 !== undefined || res2 !== undefined) {
            matchesToSave.push({
              tourid: selectedTournament.TourID,
              player1: participants[i].StudNum,
              player2: participants[j].StudNum,
              player1result: parseFloat(res1 || 0.5),
              player2result: parseFloat(res2 || 0.5)
            });
          }
        }
      }

      // 2. Clear existing matches for this tournament to avoid duplicates
      // In a more robust system, we would upsert based on (tourid, player1, player2)
      const { error: deleteError } = await supabase
        .schema('Chessistant')
        .from('TournamentMatches')
        .delete()
        .eq('tourid', selectedTournament.TourID);

      if (deleteError) throw deleteError;

      // 3. Insert new matches
      const { error: insertError } = await supabase
        .schema('Chessistant')
        .from('TournamentMatches')
        .insert(matchesToSave);

      if (insertError) throw insertError;

      alert('Scores saved successfully!');
    } catch (err) {
      console.error('Error saving scores:', err);
      alert('Failed to save scores.');
    }
  };

  const handleSaveMatch = async (row, col, value) => {
    if (!selectedTournament?.TourID || !participants[row] || !participants[col]) return;

    const tourid = selectedTournament.TourID;
    const [player1Index, player2Index] = row < col ? [row, col] : [col, row];
    const player1 = participants[player1Index].StudNum;
    const player2 = participants[player2Index].StudNum;

    try {
      if (value === '') {
        const { error } = await supabase
          .schema('Chessistant')
          .from('TournamentMatches')
          .delete()
          .match({ tourid, player1, player2 });

        if (error) throw error;
        return;
      }

      const normalizedValue = value === '1/2' ? 0.5 : parseFloat(value);
      const isRowLess = row < col;
      const player1result = isRowLess
        ? normalizedValue
        : normalizedValue === 0.5
          ? 0.5
          : 1 - normalizedValue;
      const player2result = isRowLess
        ? normalizedValue === 0.5
          ? 0.5
          : 1 - normalizedValue
        : normalizedValue;

      const { error: deleteError } = await supabase
        .schema('Chessistant')
        .from('TournamentMatches')
        .delete()
        .match({ tourid, player1, player2 });

      if (deleteError) throw deleteError;

      const { error: insertError } = await supabase
        .schema('Chessistant')
        .from('TournamentMatches')
        .insert([{ tourid, player1, player2, player1result, player2result }]);

      if (insertError) throw insertError;
    } catch (err) {
      console.error('Error saving match result:', err);
    }
  };

  const handleEdit = (tour, e) => {
    if (e) e.stopPropagation();
    const date = new Date(tour.Date);
    if (isNaN(date.getTime())) {
      setFormData({ 
        Title: tour.Title, 
        Date: '', 
        Hour: '09', 
        Style: 'Round Robin',
        ParticipantCount: tour.ParticipantCount || 10,
        Text: tour.Text 
      });
    } else {
      const localDate = new Date(date.getTime() - date.getTimezoneOffset() * 60000);
      const isoStr = localDate.toISOString(); // YYYY-MM-DDTHH:mm:ss.sssZ
      const [d, t] = isoStr.split('T');
      setFormData({ 
        Title: tour.Title, 
        Date: d, 
        Hour: t.split(':')[0], 
        Style: 'Round Robin',
        ParticipantCount: tour.ParticipantCount || 10,
        Text: tour.Text 
      });
    }
    setEditingId(tour.TourID);
    setIsModalEditing(true);
  };

  const handleDelete = async (tourId, e) => {
    if (e) e.stopPropagation();
    if (!window.confirm('Are you sure you want to delete this tournament?')) return;
    try {
      const { error } = await supabase
        .schema('Chessistant')
        .from('Tournaments')
        .delete()
        .eq('TourID', tourId);
      if (error) throw error;
      fetchTournaments();
    } catch (err) {
      console.error('Error deleting tournament:', err);
    }
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

  if (loading) return (
    <div class="overlay">
      <div class="spinner"></div>
      <p>Loading Tournaments...</p>
    </div>
  );

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
        <button onClick={() => { 
          const now = new Date();
          const localNow = new Date(now.getTime() - now.getTimezoneOffset() * 60000).toISOString().split('T')[0];
          openModal({ Title: '', Date: `${localNow}T09:00`, Text: '' });
        }} style={{ width: 'fit-content', height: 'fit-content', padding: '10px 20px' }}>
          Add Tournament
        </button>
      </div>

      <div style={{ marginTop: '30px' }}>
        {tournaments.length === 0 ? (
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
                  {isModalEditing ? (
                    <div style={{ marginBottom: '10px' }}>
                      <input 
                        value={formData.Title} 
                        onChange={(e) => setFormData({...formData, Title: e.target.value})} 
                        style={{ 
                          fontSize: '2.5rem', 
                          width: '100%', 
                          textAlign: 'center', 
                          padding: '10px', 
                          background: 'var(--antique-white)', 
                          border: '1px solid var(--oak)', 
                          color: 'var(--mahogany)', 
                          fontFamily: 'var(--font-serif)',
                          fontWeight: 'bold',
                          outline: 'none'
                        }}
                        placeholder="Tournament Title"
                        required 
                      />
                      <div style={{ marginTop: '10px', width: '100%', display: 'flex', flexDirection: 'row', justifyContent: 'space-between', alignItems: 'center' }}>
                        <div style={{ width: '50%' }}>
                          <label style={{ display: 'block', marginBottom: '5px', fontSize: '0.8rem', color: 'var(--gold)', fontWeight: 'bold', textAlign: 'left' }}>Date</label>
                          <input 
                            type="date"
                            value={formData.Date} 
                            onChange={(e) => setFormData({...formData, Date: e.target.value})} 
                            min={new Date(new Date().getTime() - new Date().getTimezoneOffset() * 60000).toISOString().split('T')[0]}
                            style={{ 
                              width: '100%', 
                              padding: '10px', 
                              background: 'var(--antique-white)', 
                              border: '1px solid var(--oak)', 
                              color: 'var(--mahogany)', 
                              outline: 'none',
                              fontSize: '1rem'
                            }}
                            required 
                          />
                        </div>
                        <div style={{ width: '30%' }}>
                          <label style={{ display: 'block', marginBottom: '5px', fontSize: '0.8rem', color: 'var(--gold)', fontWeight: 'bold', textAlign: 'left' }}>Hour</label>
                          <select 
                            value={formData.Hour} 
                            onChange={(e) => setFormData({...formData, Hour: e.target.value})} 
                            style={{ 
                              width: '100%', 
                              padding: '10px', 
                              background: 'var(--antique-white)', 
                              border: '1px solid var(--oak)', 
                              color: 'var(--mahogany)', 
                              outline: 'none',
                              fontSize: '1rem',
                              cursor: 'pointer'
                            }}
                            required 
                          >
                            {Array.from({ length: 24 }, (_, i) => {
                              const hour = i.toString().padStart(2, '0');
                              const label = i === 0 ? '12 AM' : i === 12 ? '12 PM' : i < 12 ? `${i} AM` : `${i - 12} PM`;
                              return <option key={hour} value={hour}>{label}</option>;
                            })}
                          </select>
                        </div>
                        {/* <div style={{ width: '30' }}>
                          <label style={{ display: 'block', marginBottom: '5px', fontSize: '0.8rem', color: 'var(--gold)', fontWeight: 'bold', textAlign: 'left' }}>Style</label>
                          <select 
                            value={formData.Style} 
                            onChange={(e) => {
                              const newStyle = e.target.value;
                              const min = newStyle === 'Round Robin' ? 3 : 4;
                              const max = newStyle === 'Round Robin' ? 16 : 100;
                              let newParticipants = formData.ParticipantCount;
                              
                              if (newParticipants < min) newParticipants = min;
                              if (newParticipants > max) newParticipants = max;
                              
                              setFormData({
                                ...formData, 
                                Style: newStyle,
                                ParticipantCount: newParticipants
                              });
                            }} 
                            style={{ 
                              width: '100%', 
                              padding: '10px', 
                              background: 'var(--antique-white)', 
                              border: '1px solid var(--oak)', 
                              color: 'var(--mahogany)', 
                              outline: 'none',
                              fontSize: '1rem',
                              cursor: 'pointer'
                            }}
                            required 
                          >
                            <option value="Swiss System">Swiss System</option>
                            <option value="Round Robin">Round Robin</option>
                          </select>
                        </div> */}
                        <div style={{ width: '15%' }}>
                          <label style={{ display: 'block', marginBottom: '5px', fontSize: '0.8rem', color: 'var(--gold)', fontWeight: 'bold', textAlign: 'left' }}>Max Players</label>
                          <input 
                            type="number"
                            value={formData.ParticipantCount} 
                            onChange={(e) => {
                              const val = parseInt(e.target.value);
                              const min = 3;
                              const max = 16;
                              
                              if (isNaN(val)) {
                                setFormData({...formData, ParticipantCount: ''});
                              } else {
                                // Clamp the value
                                const clamped = Math.max(0, Math.min(val, max));
                                setFormData({...formData, ParticipantCount: clamped});
                              }
                            }} 
                            onBlur={(e) => {
                              const val = parseInt(e.target.value);
                              const min = 3;
                              const max = 16;
                              if (isNaN(val) || val < min) {
                                setFormData({...formData, ParticipantCount: min});
                              } else if (val > max) {
                                setFormData({...formData, ParticipantCount: max});
                              }
                            }}
                            min={3}
                            max={16}
                            style={{ 
                              width: '100%', 
                              padding: '10px', 
                              background: 'var(--antique-white)', 
                              border: '1px solid var(--oak)', 
                              color: 'var(--mahogany)', 
                              outline: 'none',
                              fontSize: '1rem'
                            }}
                            required 
                          />
                        </div>
                      </div>
                    </div>
                  ) : (
                    <>
                      <h2 style={{ color: 'var(--mahogany)', fontSize: '2.5rem' }}>{selectedTournament.Title}</h2>
                      <div style={{ marginTop: '10px', width: '100%', display: 'flex', flexDirection: 'column', alignItems: 'center' }}>
                        <span className="label" style={{ color: 'var(--gold-muted)', fontWeight: 'bold' }}>{displayDate(selectedTournament.Date)}</span>
                        <span className="label" style={{ color: 'var(--gold-muted)', fontWeight: 'bold' }}>{selectedTournament.ParticipantCount} Player {selectedTournament.Style} Tournament</span>
                      </div>
                    </>
                  )}
                  <hr className="modal-hr" style={{ marginTop: '20px' }} />
                </div>

                {/* modal body */}
                <div className="modal-body">
                  {isModalEditing ? (
                    <textarea 
                      value={formData.Text} 
                      onChange={(e) => setFormData({...formData, Text: e.target.value})} 
                      style={{ 
                        width: '100%',
                        height: '100%',
                        padding: '10px', 
                        background: 'var(--antique-white)', 
                        border: '1px solid var(--oak)', 
                        fontSize: '1rem', 
                        fontFamily: 'inherit',
                        lineHeight: '1.6',
                        color: 'var(--text)',
                        outline: 'none',
                        resize: 'none'
                      }}
                      placeholder="Tournament Description / Details"
                      required 
                    />
                  ) : (
                    <p style={{ whiteSpace: 'pre-wrap' }}>{selectedTournament.Text}</p>
                  )}
                </div>
              </div>

              <div className="tournament-bracket" style={{ flexBasis: "60%", overflow: 'auto', padding: '0 20px 0 0' }}>
                {/* {selectedTournament.Style === 'Round Robin' ? (
                  <RoundRobinCrossTable 
                    participantCount={selectedTournament.ParticipantCount}
                    results={results}
                    setResults={setResults}
                    participants={participants}
                    onSaveMatch={handleSaveMatch}
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
                )} */}
                <RoundRobinCrossTable 
                  participantCount={isModalEditing ? formData.ParticipantCount : selectedTournament.ParticipantCount}
                  results={results}
                  setResults={setResults}
                  participants={participants}
                  onSaveMatch={handleSaveMatch}
                />
              </div>

            </div>
            
            {/* modal footer */}
            <div style={{ padding: '0px 10px 20px 10px', backgroundColor: 'var(--parchment)', flexShrink: 0, overflowY: 'hidden', scrollbarGutter: 'stable both-edges' }}>
              <hr className="modal-hr" />
              <br></br>
              <div style={{ display: 'flex', justifyContent: 'flex-end', gap: '10px' }}>
                {isModalEditing ? (
                  <>
                    {isModalAdding ? (
                      <>
                        <button 
                          onClick={handleSubmit}
                          style={{ padding: '10px 20px', margin: '0', fontSize: '0.9rem', width: 'auto' }}>
                          Post
                        </button>
                        <button 
                          onClick={closeModal} 
                          style={{ padding: '10px 20px', margin: '0', fontSize: '0.9rem', width: 'auto' }}>
                          Cancel
                        </button>
                      </>
                    ) : (
                      <>
                        <button 
                          onClick={handleSubmit} 
                          style={{ padding: '10px 20px', margin: '0', fontSize: '0.9rem', width: 'auto' }}>
                          Save Changes
                        </button>
                        <button 
                          onClick={() => setIsModalEditing(false)} 
                          style={{ padding: '10px 20px', margin: '0', fontSize: '0.9rem', width: 'auto' }}>
                          Cancel
                        </button>
                      </>
                    )}
                  </>
                ) : (
                  <>
                    {selectedTournament.Style === 'Round Robin' && (
                      <button 
                        onClick={handleSaveScores} 
                        style={{ padding: '10px 20px', margin: '0', fontSize: '0.9rem', width: 'auto', background: 'var(--success, #2e7d32)', color: 'white' }}>
                        Save Scores
                      </button>
                    )}
                    <button 
                      onClick={(e) => { 
                        handleEdit(selectedTournament, e);
                      }} 
                      style={{ padding: '10px 20px', margin: '0', fontSize: '0.9rem', width: 'auto' }}>
                      Edit
                    </button>
                    <button 
                      onClick={(e) => { handleDelete(selectedTournament.TourID, e); closeModal(); }} 
                      style={{ padding: '10px 20px', margin: '0', fontSize: '0.9rem', width: 'auto', background: 'var(--error)' }}>
                      Delete
                    </button>
                  </>
                )}
              </div>
            </div>
          </div>
        </div>
      )}
    </div>
  );
};

export default Tournaments;
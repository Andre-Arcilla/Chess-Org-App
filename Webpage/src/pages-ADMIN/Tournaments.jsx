import React, { useEffect, useState } from 'react';
import { useOutletContext } from 'react-router-dom';
import { supabase } from '../db';

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
    Style: 'Swiss System', 
    ParticipantCount: 10, 
    Text: '' 
  });
  const [view, setView] = useState('upcoming');
  const [results, setResults] = useState({});

  const handleResultChange = (row, col, value) => {
    // Allow partial inputs like '0.' and '1/' so users can finish typing 0.5 or 1/2
    const allowed = ['0', '1', '0.5', '1/2', '', '0.', '1/'];
    if (!allowed.includes(value)) return;

    let normalizedValue = value;
    if (value === '1/2') normalizedValue = '0.5';

    const newResults = { ...results };
    
    if (normalizedValue === '') {
      delete newResults[`${row}-${col}`];
      delete newResults[`${col}-${row}`];
    } else {
      newResults[`${row}-${col}`] = normalizedValue;
      
      // Reciprocal logic only for COMPLETE inputs
      if (normalizedValue === '1') {
        newResults[`${col}-${row}`] = '0';
      } else if (normalizedValue === '0') {
        newResults[`${col}-${row}`] = '1';
      } else if (normalizedValue === '0.5') {
        newResults[`${col}-${row}`] = '0.5';
      }
    }
    
    setResults(newResults);
  };

  const getScore = (rowIndex) => {
    let score = 0;
    for (let i = 0; i < selectedTournament.ParticipantCount; i++) {
      const val = results[`${rowIndex}-${i}`];
      if (val === '1') score += 1;
      else if (val === '0.5' || val === '1/2') score += 0.5;
    }
    return score;
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

  const openModal = (tour) => {
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
        Style: tour.Style || 'Swiss System',
        ParticipantCount: tour.ParticipantCount || 10,
        Text: tour.Text || ''
      });
    } else {
      setFormData({ 
        Title: tour.Title || '', 
        Date: (tour.Date || '').split('T')[0], 
        Hour: (tour.Date || 'T09').split('T')[1].split(':')[0], 
        Style: tour.Style || 'Swiss System',
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
        Style: formData.Style,
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

  const handleEdit = (tour, e) => {
    if (e) e.stopPropagation();
    const date = new Date(tour.Date);
    if (isNaN(date.getTime())) {
      setFormData({ 
        Title: tour.Title, 
        Date: '', 
        Hour: '09', 
        Style: tour.Style || 'Swiss System',
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
        Style: tour.Style || 'Swiss System',
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
                        <div style={{ width: '26%' }}>
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
                        <div style={{ width: '18%' }}>
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
                        <div style={{ width: '30' }}>
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
                        </div>
                        <div style={{ width: '16' }}>
                          <label style={{ display: 'block', marginBottom: '5px', fontSize: '0.8rem', color: 'var(--gold)', fontWeight: 'bold', textAlign: 'left' }}>Max Players</label>
                          <input 
                            type="number"
                            value={formData.ParticipantCount} 
                            onChange={(e) => {
                              const val = parseInt(e.target.value);
                              const min = formData.Style === 'Round Robin' ? 3 : 4;
                              const max = formData.Style === 'Round Robin' ? 16 : 100;
                              
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
                              const min = formData.Style === 'Round Robin' ? 3 : 4;
                              const max = formData.Style === 'Round Robin' ? 16 : 100;
                              if (isNaN(val) || val < min) {
                                setFormData({...formData, ParticipantCount: min});
                              } else if (val > max) {
                                setFormData({...formData, ParticipantCount: max});
                              }
                            }}
                            min={formData.Style === 'Round Robin' ? 3 : 4}
                            max={formData.Style === 'Round Robin' ? 16 : 100}
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

              <div className="tournament-bracket" style={{ flexBasis: "60%", overflow: 'auto' }}>
                {selectedTournament.Style === 'Round Robin' ? (
                  <>
                    <div style={{ padding: '20px 20px 0 20px' }}>
                      <h4 style={{ fontFamily: 'var(--font-serif)', color: 'var(--mahogany)', marginBottom: '15px' }}>Tournament Crosstable</h4>
                    </div>
                    <table style={{ width: '100%', borderCollapse: 'separate', borderSpacing: 0, backgroundColor: 'rgba(255,255,255,0.5)', borderLeft: 'none', borderRight: 'none', borderBottom: '1px solid var(--oak)' }}>
                      <thead style={{ position: 'sticky', top: 0, zIndex: 10, backgroundColor: 'var(--mahogany)', color: 'var(--parchment)' }}>
                        <tr style={{ backgroundColor: 'var(--mahogany)', color: 'var(--parchment)' }}>
                          <th style={{ 
                            padding: '8px', 
                            border: '1px solid var(--oak)', 
                            fontSize: '0.8rem', 
                            width: '40px',
                            position: 'sticky',
                            left: 0,
                            backgroundColor: 'var(--mahogany)',
                            zIndex: 11
                          }}>Rank</th>
                          <th style={{ padding: '8px', border: '1px solid var(--oak)', fontSize: '0.8rem', width: '180px', minWidth: '180px' }}>Player</th>
                          {Array.from({ length: selectedTournament.ParticipantCount }).map((_, i) => (
                            <th key={i} style={{ padding: '8px', border: '1px solid var(--oak)', fontSize: '0.8rem', width: '40px', minWidth: '40px' }}>{i + 1}</th>
                          ))}
                          <th style={{ padding: '8px', border: '1px solid var(--oak)', fontSize: '0.8rem', width: '60px', minWidth: '60px' }}>Score</th>
                        </tr>
                      </thead>
                      <tbody>
                        {Array.from({ length: selectedTournament.ParticipantCount }).map((_, rowIndex) => (
                          <tr key={rowIndex}>
                            <td style={{ 
                              padding: '8px', 
                              border: '1px solid var(--oak)', 
                              textAlign: 'center', 
                              fontSize: '0.8rem', 
                              backgroundColor: 'var(--antique-white)',
                              position: 'sticky',
                              left: 0,
                              zIndex: 5
                            }}>
                              {rowIndex + 1}
                            </td>
                            <td style={{ padding: '8px', border: '1px solid var(--oak)', fontWeight: 'bold', fontSize: '0.8rem', backgroundColor: 'var(--antique-white)', width: '180px', minWidth: '180px' }}>
                              #{rowIndex + 1}
                            </td>
                            {Array.from({ length: selectedTournament.ParticipantCount }).map((_, colIndex) => (
                              <td 
                                key={colIndex} 
                                style={{ 
                                  padding: '0', 
                                  border: '1px solid var(--oak)', 
                                  textAlign: 'center', 
                                  fontSize: '0.8rem',
                                  width: '40px',
                                  height: '40px',
                                  minWidth: '40px',
                                  backgroundColor: rowIndex === colIndex ? 'var(--mahogany-light)' : 'transparent',
                                  color: rowIndex === colIndex ? 'var(--parchment)' : 'inherit'
                                }}
                              >
                                {rowIndex === colIndex ? '—' : (
                                  <input 
                                    value={results[`${rowIndex}-${colIndex}`] || ''}
                                    onChange={(e) => handleResultChange(rowIndex, colIndex, e.target.value)}                                    style={{
                                      width: '100%',
                                      height: '100%',
                                      textAlign: 'center',
                                      border: 'none',
                                      background: 'transparent',
                                      outline: 'none',
                                      padding: 0,
                                      color: 'inherit',
                                      fontFamily: 'inherit',
                                      fontSize: '0.85rem'
                                    }}
                                  />
                                )}
                              </td>
                            ))}
                            <td style={{ padding: '8px', border: '1px solid var(--oak)', textAlign: 'center', fontWeight: 'bold', fontSize: '0.8rem' }}>
                              {getScore(rowIndex)}
                            </td>
                          </tr>
                        ))}
                      </tbody>
                    </table>
                  </>
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
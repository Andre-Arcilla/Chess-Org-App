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
  const [formData, setFormData] = useState({ Title: '', Date: '', Text: '' });

  const fetchTournaments = async () => {
    try {
      setLoading(true);
      const { data, error } = await supabase
        .schema('Chessistant')
        .from('Tournaments')
        .select('*')
        .order('Date', { ascending: false });
      
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
  }, []);

  const openModal = (tour) => {
    setSelectedTournament(tour);
    // If no TourID, it's a new tournament - go into edit mode
    if (!tour.TourID) {
      setIsModalEditing(true);
      setIsModalAdding(true);
      setEditingId(null);
      setFormData({ Title: tour.Title || '', Date: tour.Date || '', Text: tour.Text || '' });
    } else {
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
    try {
      const payload = {
        Title: formData.Title,
        Text: formData.Text,
        // The Date column expects a standard Timestamp string (ISO)
        Date: new Date(formData.Date).toISOString(), 
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
      } else {
        // FIX 3: Use StudNum and satisfy the not-null constraint for BOTH fields
        payload.Author = adminData?.StudNum;
        payload.LastEditor = adminData?.StudNum;
        
        const { error } = await supabase
          .schema('Chessistant')
          .from('Tournaments')
          .insert([payload]);
        if (error) throw error;
      }

      setFormData({ Title: '', Date: '', Text: '' });
      setEditingId(null);
      fetchTournaments();
      closeModal();
    } catch (err) {
      console.error('Error saving tournament:', JSON.stringify(err, null, 2));
      alert('Save failed! Check the console for details.');
    }
  };

  const handleEdit = (tour, e) => {
    if (e) e.stopPropagation();
    const dateStr = tour.Date ? new Date(tour.Date).toISOString().split('T')[0] : '';
    setFormData({ Title: tour.Title, Date: dateStr, Text: tour.Text });
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
    return isNaN(d.getTime()) ? '' : d.toLocaleDateString();
  };

  return (
    <div className="card">
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', overflowY: 'hidden', scrollbarGutter: 'stable both-edges' }}>
        <h3>Tournament Management</h3>
        <button onClick={() => { 
          const today = new Date().toISOString().split('T')[0];
          openModal({ Title: '', Date: today, Text: '' });
        }} style={{ width: 'auto', padding: '10px 20px' }}>
          Add Tournament
        </button>
      </div>

      <div style={{ marginTop: '30px' }}>
        {loading ? <p>Loading...</p> : tournaments.map((tour) => (
          <div 
            key={tour.TourID} 
            className="stat-item" 
            style={{ marginBottom: '20px', borderLeft: '5px solid var(--gold)', position: 'relative', cursor: 'pointer' }}
            onClick={() => openModal(tour)}
          >
            <span className="label">{displayDate(tour.Date)}</span>
            <span className="value" style={{ fontSize: '1.5rem' }}>{tour.Title}</span>
            <p style={{ marginTop: '10px', color: 'var(--text-muted)', display: '-webkit-box', WebkitLineClamp: '2', WebkitBoxOrient: 'vertical', overflow: 'hidden' }}>{tour.Text}</p>
          </div>
        ))}
      </div>

      {selectedTournament && (
        <div className="modal-overlay" onClick={closeModal}>
          <div className="modal-content" onClick={(e) => e.stopPropagation()}>
            <span className="modal-close" onClick={closeModal}>&times;</span>

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
                        width: '95%', 
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
                  </div>
                ) : (
                  <h2 style={{ color: 'var(--mahogany)', fontSize: '2.5rem' }}>{selectedTournament.Title}</h2>
                )}
                <span className="label" style={{ color: 'var(--gold-muted)', fontWeight: 'bold' }}>{displayDate(selectedTournament.Date)}</span>
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
                  <p>{selectedTournament.Text}</p>
                )}
              </div>
            </div>

            {/* modal footer */}
            <div style={{ padding: '0px 10px 20px 10px', backgroundColor: 'var(--parchment)', flexShrink: 0, position: 'sticky', bottom: '0', overflowY: 'hidden', scrollbarGutter: 'stable both-edges' }}>
              <hr className="modal-hr" />
              <br></br>
              <div style={{ display: 'flex', justifyContent: 'flex-end', gap: '10px' }}>
                {isModalEditing ? (
                  <>
                    {isModalAdding ? (
                      <button 
                        onClick={handleSubmit}
                        style={{ padding: '10px 20px', margin: '0', fontSize: '0.9rem', width: 'auto' }}>
                        Post
                      </button>
                    ) : (
                      <button 
                        onClick={handleSubmit} 
                        style={{ padding: '10px 20px', margin: '0', fontSize: '0.9rem', width: 'auto' }}>
                        Save Changes
                      </button>
                    )}
                    <button 
                      onClick={() => setIsModalEditing(false)} 
                      style={{ padding: '10px 20px', margin: '0', fontSize: '0.9rem', width: 'auto' }}>
                      Cancel
                    </button>
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
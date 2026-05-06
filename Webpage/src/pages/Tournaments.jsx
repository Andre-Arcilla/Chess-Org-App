import React, { useEffect, useState } from 'react';
import { useOutletContext } from 'react-router-dom';
import { supabase } from '../db';

const Tournaments = () => {
  const { adminData } = useOutletContext();
  const [tournaments, setTournaments] = useState([]);
  const [loading, setLoading] = useState(true);
  const [showForm, setShowForm] = useState(false);
  const [editingId, setEditingId] = useState(null);
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

  const handleSubmit = async (e) => {
    e.preventDefault();
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
      setShowForm(false);
      fetchTournaments();
    } catch (err) {
      console.error('Error saving tournament:', JSON.stringify(err, null, 2));
      alert('Save failed! Check the console for details.');
    }
  };

  const handleEdit = (tour) => {
    // Convert DB Timestamp back to YYYY-MM-DD so the HTML date input can read it
    const dateStr = tour.Date ? new Date(tour.Date).toISOString().split('T')[0] : '';
    setFormData({ Title: tour.Title, Date: dateStr, Text: tour.Text });
    setEditingId(tour.TourID);
    setShowForm(true);
  };

  const handleDelete = async (tourId) => {
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
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
        <h3>Tournament Management</h3>
        <button onClick={() => { setShowForm(!showForm); setEditingId(null); setFormData({ Title: '', Date: '', Text: '' }); }} style={{ width: 'auto', padding: '10px 20px' }}>
          {showForm ? 'Cancel' : 'Add Tournament'}
        </button>
      </div>

      {showForm && (
        <form onSubmit={handleSubmit} style={{ marginTop: '20px', padding: '20px', border: '1px solid var(--oak)', background: 'var(--antique-white)' }}>
          <div className="form-group">
            <label>Title</label>
            <input value={formData.Title} onChange={(e) => setFormData({...formData, Title: e.target.value})} required />
          </div>
          <div className="form-group">
            <label>Date</label>
            <input type="date" value={formData.Date} onChange={(e) => setFormData({...formData, Date: e.target.value})} required />
          </div>
          <div className="form-group">
            <label>Description / Details</label>
            <textarea 
              value={formData.Text} 
              onChange={(e) => setFormData({...formData, Text: e.target.value})} 
              style={{ width: '100%', minHeight: '100px', padding: '10px', background: 'var(--parchment)', border: '1px solid var(--oak)' }}
              required 
            />
          </div>
          <button type="submit" style={{ marginTop: '10px' }}>{editingId ? 'Update Tournament' : 'Post Tournament'}</button>
        </form>
      )}

      <div style={{ marginTop: '30px' }}>
        {loading ? <p>Loading...</p> : tournaments.map((tour) => (
          <div key={tour.TourID} className="stat-item" style={{ marginBottom: '20px', borderLeft: '5px solid var(--gold)', position: 'relative' }}>
            <span className="label">{displayDate(tour.Date)}</span>
            <span className="value" style={{ fontSize: '1.5rem' }}>{tour.Title}</span>
            <p style={{ marginTop: '10px', color: 'var(--text-muted)' }}>{tour.Text}</p>
            <div style={{ marginTop: '15px' }}>
              <button onClick={() => handleEdit(tour)} style={{ padding: '5px 10px', fontSize: '0.8rem', width: 'auto', marginRight: '5px' }}>Edit</button>
              <button onClick={() => handleDelete(tour.TourID)} style={{ padding: '5px 10px', fontSize: '0.8rem', width: 'auto', background: 'var(--error)' }}>Delete</button>
            </div>
          </div>
        ))}
      </div>
    </div>
  );
};

export default Tournaments;
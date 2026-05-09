import React, { useEffect, useState } from 'react';
import { useOutletContext } from 'react-router-dom';
import { supabase } from '../db';

const Announcements = () => {
  const { adminData } = useOutletContext();
  const [announcements, setAnnouncements] = useState([]);
  const [loading, setLoading] = useState(true);
  const [showForm, setShowForm] = useState(false);
  const [editingId, setEditingId] = useState(null);
  const [formData, setFormData] = useState({ Title: '', Text: '' });

  const fetchAnnouncements = async () => {
    try {
      setLoading(true);
      const { data, error } = await supabase
        .schema('Chessistant')
        .from('Announcements')
        .select('*')
        .order('Date', { ascending: false });
      
      if (error) throw error;
      setAnnouncements(data || []);
    } catch (err) {
      console.error('Error fetching announcements:', err);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchAnnouncements();
  }, []);

  const handleSubmit = async (e) => {
    e.preventDefault();
    try {
      const payload = {
        Title: formData.Title,
        Text: formData.Text,
        // PERFECT MATCH 1: LastModified requires a BigInt number
        LastModified: Date.now() 
      };

      if (editingId) {
        payload.LastEditor = adminData?.UserID;
        const { error } = await supabase
          .schema('Chessistant')
          .from('Announcements')
          .update(payload)
          .eq('AnnID', editingId);
        if (error) throw error;
      } else {
        // FIX: Pass StudNum instead of UserID
        payload.Author = adminData?.StudNum; 
        payload.LastEditor = adminData?.StudNum; 
        
        payload.Date = new Date().toISOString(); 
        const { error } = await supabase
          .schema('Chessistant')
          .from('Announcements')
          .insert([payload]);
        if (error) throw error;
      }

      setFormData({ Title: '', Text: '' });
      setEditingId(null);
      setShowForm(false);
      fetchAnnouncements();
    } catch (err) {
      console.error('Error saving announcement:', JSON.stringify(err, null, 2));
      alert('Save failed! Check the console for details.');
    }
  };

  const handleEdit = (ann) => {
    setFormData({ Title: ann.Title, Text: ann.Text });
    setEditingId(ann.AnnID);
    setShowForm(true);
  };

  const handleDelete = async (annId) => {
    if (!window.confirm('Delete this announcement?')) return;
    try {
      const { error } = await supabase
        .schema('Chessistant')
        .from('Announcements')
        .delete()
        .eq('AnnID', annId);
      if (error) throw error;
      fetchAnnouncements();
    } catch (err) {
      console.error('Error deleting announcement:', err);
    }
  };

  const displayDate = (dateVal) => {
    if (!dateVal) return '';
    // Safely handles the Timestamp string from the database
    const d = new Date(dateVal);
    return isNaN(d.getTime()) ? '' : d.toLocaleString();
  };

  return (
    <div className="card">
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
        <h3>Club Announcements</h3>
        <button onClick={() => { setShowForm(!showForm); setEditingId(null); setFormData({ Title: '', Text: '' }); }} style={{ width: 'auto', padding: '10px 20px' }}>
          {showForm ? 'Cancel' : 'Post Announcement'}
        </button>
      </div>

      {showForm && (
        <form onSubmit={handleSubmit} style={{ marginTop: '20px', padding: '20px', border: '1px solid var(--oak)', background: 'var(--antique-white)' }}>
          <div className="form-group">
            <label>Title</label>
            <input value={formData.Title} onChange={(e) => setFormData({...formData, Title: e.target.value})} required />
          </div>
          <div className="form-group">
            <label>Content</label>
            <textarea 
              value={formData.Text} 
              onChange={(e) => setFormData({...formData, Text: e.target.value})} 
              style={{ width: '100%', minHeight: '150px', padding: '10px', background: 'var(--parchment)', border: '1px solid var(--oak)' }}
              required 
            />
          </div>
          <button type="submit" style={{ marginTop: '10px' }}>{editingId ? 'Update' : 'Publish'}</button>
        </form>
      )}

      <div style={{ marginTop: '30px' }}>
        {loading ? <p>Loading...</p> : announcements.map((ann) => (
          <div key={ann.AnnID} style={{ marginBottom: '30px', padding: '20px', border: '1px dashed var(--oak)', background: 'white' }}>
            <div style={{ display: 'flex', justifyContent: 'space-between' }}>
              <h4>{ann.Title}</h4>
              <span className="label" style={{ fontSize: '0.8rem' }}>{displayDate(ann.Date)}</span>
            </div>
            <p style={{ marginTop: '15px', whiteSpace: 'pre-wrap' }}>{ann.Text}</p>
            <div style={{ marginTop: '20px', display: 'flex', gap: '10px' }}>
              <button onClick={() => handleEdit(ann)} style={{ padding: '5px 15px', fontSize: '0.8rem', width: 'auto' }}>Edit</button>
              <button onClick={() => handleDelete(ann.AnnID)} style={{ padding: '5px 15px', fontSize: '0.8rem', width: 'auto', background: 'var(--error)' }}>Delete</button>
            </div>
          </div>
        ))}
      </div>
    </div>
  );
};

export default Announcements;
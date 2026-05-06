import React, { useEffect, useState } from 'react';
import { useOutletContext } from 'react-router-dom';
import { supabase } from '../db';

const Announcements = () => {
  const { adminData } = useOutletContext();
  const [announcements, setAnnouncements] = useState([]);
  const [loading, setLoading] = useState(true);
  const [showForm, setShowForm] = useState(false);
  const [editingId, setEditingId] = useState(null);
  const [selectedAnnouncement, setSelectedAnnouncement] = useState(null);
  const [formData, setFormData] = useState({ Title: '', Date: '', Text: '' });

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

  const openModal = (ann) => {
    setSelectedAnnouncement(ann);
  };

  const closeModal = () => {
    setSelectedAnnouncement(null);
  };

  const handleSubmit = async (e) => {
    e.preventDefault();
    try {
      const payload = {
        Title: formData.Title,
        Text: formData.Text,
        Date: new Date(formData.Date).toISOString(),
        LastModified: Date.now()
      };

      if (editingId) {
        payload.LastEditor = adminData?.StudNum;
        const { error } = await supabase
          .schema('Chessistant')
          .from('Announcements')
          .update(payload)
          .eq('AnnID', editingId);
        if (error) throw error;
      } else {
        payload.Author = adminData?.StudNum;
        payload.LastEditor = adminData?.StudNum;
        
        const { error } = await supabase
          .schema('Chessistant')
          .from('Announcements')
          .insert([payload]);
        if (error) throw error;
      }

      setFormData({ Title: '', Date: '', Text: '' });
      setEditingId(null);
      setShowForm(false);
      fetchAnnouncements();
    } catch (err) {
      console.error('Error saving announcement:', JSON.stringify(err, null, 2));
      alert('Save failed! Check the console for details.');
    }
  };

  const handleEdit = (ann, e) => {
    if (e) e.stopPropagation();
    const dateStr = ann.Date ? new Date(ann.Date).toISOString().split('T')[0] : '';
    setFormData({ Title: ann.Title, Date: dateStr, Text: ann.Text });
    setEditingId(ann.AnnID);
    setShowForm(true);
  };

  const handleDelete = async (annId, e) => {
    if (e) e.stopPropagation();
    if (!window.confirm('Are you sure you want to delete this announcement?')) return;
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
    const d = new Date(dateVal);
    return isNaN(d.getTime()) ? '' : d.toLocaleDateString();
  };

  return (
    <div className="card">
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
        <h3>Announcements</h3>
        <button onClick={() => { setShowForm(!showForm); setEditingId(null); setFormData({ Title: '', Date: '', Text: '' }); }} style={{ width: 'auto', padding: '10px 20px' }}>
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
            <label>Date</label>
            <input type="date" value={formData.Date} onChange={(e) => setFormData({...formData, Date: e.target.value})} required />
          </div>
          <div className="form-group">
            <label>Content</label>
            <textarea 
              value={formData.Text} 
              onChange={(e) => setFormData({...formData, Text: e.target.value})} 
              style={{ width: '100%', minHeight: '100px', padding: '10px', background: 'var(--parchment)', border: '1px solid var(--oak)' }}
              required 
            />
          </div>
          <button type="submit" style={{ marginTop: '10px' }}>{editingId ? 'Update Announcement' : 'Publish Announcement'}</button>
        </form>
      )}

      <div style={{ marginTop: '30px' }}>
        {loading ? <p>Loading...</p> : announcements.map((ann) => (
          <div 
            key={ann.AnnID} 
            className="stat-item" 
            style={{ marginBottom: '20px', borderLeft: '5px solid var(--gold)', position: 'relative', cursor: 'pointer' }}
            onClick={() => openModal(ann)}
          >
            <span className="label">{displayDate(ann.Date)}</span>
            <span className="value" style={{ fontSize: '1.5rem' }}>{ann.Title}</span>
            <p style={{ marginTop: '10px', color: 'var(--text-muted)', display: '-webkit-box', WebkitLineClamp: '2', WebkitBoxOrient: 'vertical', overflow: 'hidden' }}>{ann.Text}</p>
            <div style={{ marginTop: '15px' }}>
              <button onClick={(e) => handleEdit(ann, e)} style={{ padding: '5px 10px', fontSize: '0.8rem', width: 'auto', marginRight: '5px' }}>Edit</button>
              <button onClick={(e) => handleDelete(ann.AnnID, e)} style={{ padding: '5px 10px', fontSize: '0.8rem', width: 'auto', background: 'var(--error)' }}>Delete</button>
            </div>
          </div>
        ))}
      </div>

      {selectedAnnouncement && (
        <div className="modal-overlay" onClick={closeModal}>
          <div className="modal-content" onClick={(e) => e.stopPropagation()}>
            <span className="modal-close" onClick={closeModal}>&times;</span>

            {/* Announcement header */}
            <div style={{ textAlign: 'center', padding: '20px 20px 0px 20px' }}>
              <h2 style={{ color: 'var(--mahogany)', fontSize: '2.5rem' }}>{selectedAnnouncement.Title}</h2>
              <span className="label" style={{ color: 'var(--gold-muted)', fontWeight: 'bold' }}>{displayDate(selectedAnnouncement.Date)}</span>
              <hr className="modal-hr" style={{ marginTop: '10px' }} />
            </div>

            {/* Announcement content */}
            <div className="modal-body">
              {selectedAnnouncement.Text}
            </div>

            {/* Admin actions at bottom of modal */}
            <div style={{ padding: '0px 20px 20px 20px', position: 'sticky', bottom: '0px', backgroundColor: 'var(--parchment)' }}>
              <hr className="modal-hr" />
              <br></br>
              <div style={{ display: 'flex', justifyContent: 'flex-end', gap: '10px' }}>
                <button 
                  onClick={(e) => { handleEdit(selectedAnnouncement, e); closeModal(); }} 
                  style={{ padding: '10px 20px', margin: '0', fontSize: '0.9rem', width: 'auto' }}>
                  Edit
                </button>
                <button 
                  onClick={(e) => { handleDelete(selectedAnnouncement.AnnID, e); closeModal(); }} 
                  style={{ padding: '10px 20px', margin: '0', fontSize: '0.9rem', width: 'auto', background: 'var(--error)' }}>
                  Delete
                </button>
              </div>
            </div>
          </div>
        </div>
      )}
    </div>
  );
};

export default Announcements;
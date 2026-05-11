import React, { useEffect, useState } from 'react';
import { useOutletContext } from 'react-router-dom';
import { supabase } from '../db';

const Announcements = () => {
  const { adminData } = useOutletContext();
  const [announcements, setAnnouncements] = useState([]);
  const [loading, setLoading] = useState(true);
  const [editingId, setEditingId] = useState(null);
  const [isModalEditing, setIsModalEditing] = useState(false);
  const [isModalAdding, setIsModalAdding] = useState(false);
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
    // If no AnnID, it's a new announcement - go into edit mode
    if (!ann.AnnID) {
      setIsModalEditing(true);
      setIsModalAdding(true);
      setEditingId(null);
      setFormData({ Title: ann.Title || '', Date: ann.Date || '', Text: ann.Text || '' });
    } else {
      setIsModalEditing(false);
      setIsModalAdding(false);
    }
  };

  const closeModal = () => {
    setSelectedAnnouncement(null);
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
      fetchAnnouncements();
      closeModal();
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
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', alignItems: 'flex-start', overflowY: 'hidden', scrollbarGutter: 'stable both-edges' }}>
        <h3 style={{ fontSize: '2rem' }}>Announcements Management</h3>
        <button onClick={() => { 
          const today = new Date().toISOString().split('T')[0];
          openModal({ Title: '', Date: today, Text: '' });
        }} style={{ width: 'fit-content', height: 'fit-content', padding: '10px 20px' }}>
          Post Announcement
        </button>
      </div>

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
            <p style={{ marginTop: '10px', color: 'var(--text-muted)', display: '-webkit-box', WebkitLineClamp: '1', WebkitBoxOrient: 'vertical', overflow: 'hidden' }}>{ann.Text}</p>
          </div>
        ))}
      </div>

      {selectedAnnouncement && (
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
                      placeholder="Announcement Title"
                      required 
                    />
                  </div>
                ) : (
                  <h2 style={{ color: 'var(--mahogany)', fontSize: '2.5rem' }}>{selectedAnnouncement.Title}</h2>
                )}
                <span className="label" style={{ color: 'var(--gold-muted)', fontWeight: 'bold' }}>{displayDate(selectedAnnouncement.Date)}</span>
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
                    placeholder="Announcement Content"
                    required 
                  />
                ) : (
                  <p>{selectedAnnouncement.Text}</p>
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
                        setIsModalEditing(true);
                        const dateStr = selectedAnnouncement.Date ? new Date(selectedAnnouncement.Date).toISOString().split('T')[0] : '';
                        setFormData({ Title: selectedAnnouncement.Title, Date: dateStr, Text: selectedAnnouncement.Text });
                        setEditingId(selectedAnnouncement.AnnID);
                      }} 
                      style={{ padding: '10px 20px', margin: '0', fontSize: '0.9rem', width: 'auto' }}>
                      Edit
                    </button>
                    <button 
                      onClick={(e) => { handleDelete(selectedAnnouncement.AnnID, e); closeModal(); }} 
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

export default Announcements;
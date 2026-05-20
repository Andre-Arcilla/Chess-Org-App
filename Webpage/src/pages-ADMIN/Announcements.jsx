import React, { useEffect, useState } from 'react';
import { useOutletContext, useLocation } from 'react-router-dom';
import { supabase } from '../db';
import { Megaphone, Trophy, Users, Calendar, Save, Pencil, Trash2, Check, X } from 'lucide-react';

const Announcements = () => {
  const { adminData } = useOutletContext();
  const [announcements, setAnnouncements] = useState([]);
  const [loading, setLoading] = useState(true);
  const [editingId, setEditingId] = useState(null);
  const [isModalEditing, setIsModalEditing] = useState(false);
  const [isModalAdding, setIsModalAdding] = useState(false);
  const [selectedAnnouncement, setSelectedAnnouncement] = useState(null);
  const [formData, setFormData] = useState({ Title: '', Date: '', Text: '' });
  const [view, setView] = useState('upcoming');
  const location = useLocation();

  const fetchAnnouncements = async () => {
    try {
      setLoading(true);
      const minimumDelay = new Promise(resolve => setTimeout(resolve, 750));
      const [_, { data, error }] = await Promise.all([
        minimumDelay,
        supabase
          .schema('Chessistant')
          .from('Announcements')
          .select('*')
          .order('Date', { ascending: false })
      ]);
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

  useEffect(() => {
    if (loading || !location.state?.openId || announcements.length === 0) return;
    const ann = announcements.find(a => a.AnnID === location.state.openId);
    if (ann) openModal(ann);
  }, [loading, announcements]);

  const openModal = (ann) => {
    const initialDate = ann.Date || new Date().toISOString();
    setSelectedAnnouncement({ ...ann, Date: initialDate });
    // If no AnnID, it's a new announcement - go into edit mode
    if (!ann.AnnID) {
      setIsModalEditing(true);
      setIsModalAdding(true);
      setEditingId(null);
      setFormData({ Title: ann.Title || '', Date: initialDate, Text: ann.Text || '' });
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

    if (!formData.Title.trim() || !formData.Text.trim()) {
      alert('Announcement title and content cannot be empty.');
      return;
    }

    try {
      const payload = {
        Title: formData.Title,
        Text: formData.Text,
        LastModified: Date.now()
      };

      if (!editingId) {
        payload.Date = (() => {
          const now = new Date();
          if (!formData.Date) return now.toISOString();
          const selected = new Date(formData.Date);
          if (isNaN(selected.getTime())) return now.toISOString();
          selected.setHours(now.getHours(), now.getMinutes(), now.getSeconds(), now.getMilliseconds());
          return selected.toISOString();
        })();
      }

      if (editingId) {
        payload.LastEditor = adminData?.StudNum;
        const { error } = await supabase
          .schema('Chessistant')
          .from('Announcements')
          .update(payload)
          .eq('AnnID', editingId);
        if (error) throw error;
        
        // Update the specific announcement in local state
        setAnnouncements(prev => 
          prev.map(ann => ann.AnnID === editingId ? { ...ann, ...payload } : ann)
        );
        setSelectedAnnouncement({ ...selectedAnnouncement, ...payload });
      } else {
        payload.Author = adminData?.StudNum;
        payload.LastEditor = adminData?.StudNum;
        
        const { data, error } = await supabase
          .schema('Chessistant')
          .from('Announcements')
          .insert([payload])
          .select();
        if (error) throw error;
        if (data && data[0]) {
          // Add the newly created announcement to local state
          setAnnouncements(prev => [data[0], ...prev]);
          setSelectedAnnouncement(data[0]);
          setEditingId(data[0].AnnID);
        }
      }

      // REMOVED: fetchAnnouncements(); <-- This was causing the reload flash
      setIsModalEditing(false);
      setIsModalAdding(false);
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
    // Note: 'setShowForm' is not defined in state, you might want to remove or fix it
    // setShowForm(true); 
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
      
      // Remove the deleted announcement from local state instantly
      setAnnouncements(prev => prev.filter(ann => ann.AnnID !== annId));
      
      // Close the modal upon deletion
      closeModal();
      
    } catch (err) {
      console.error('Error deleting announcement:', err);
    }
  };

  const displayDate = (dateVal) => {
    if (!dateVal) return '';
    const d = new Date(dateVal);
    return isNaN(d.getTime())
      ? ''
      : d.toLocaleString([], {
          month: 'short',
          day: 'numeric',
          year: 'numeric',
          hour: '2-digit',
          minute: '2-digit',
          hour12: true
        });
  };

  const sortedAnnouncements = [...announcements].sort((a, b) => {
    const dateA = a.Date ? new Date(a.Date).getTime() : 0;
    const dateB = b.Date ? new Date(b.Date).getTime() : 0;
    return view === 'past' ? dateA - dateB : dateB - dateA;
  });

  // Fixed standard HTML 'class' to JSX 'className'
  if (loading) return (
    <div className="overlay">
      <div className="spinner"></div>
      <p>Loading Announcements...</p>
    </div>
  );

  return (
    <div className="card">
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start', overflowY: 'hidden', scrollbarGutter: 'stable both-edges' }}>
        <div style={{ display: 'flex', flexDirection: 'column', gap: '10px'}}>
          <h3 style={{ fontSize: '2rem' }}>Announcements Management</h3>
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
                fontWeight: view === 'past' ? 'normal' : 'bold',
                boxSizing: 'border-box'
              }}
            >
              Newest
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
              Oldest
            </button>
          </div>
        </div>
        <button onClick={() => {
          const now = new Date().toISOString();
          openModal({ Title: '', Date: now, Text: '' });
        }} style={{ width: 'fit-content', height: 'fit-content', padding: '10px 20px' }}>
          Post Announcement
        </button>
      </div>

      <div className="stat-container" style={{ marginTop: '20px', display: 'flex', flexDirection: 'column', gap: '20px' }}>
        {sortedAnnouncements.map((ann) => (
          <div 
            key={ann.AnnID} 
            className="stat-item ann-card" 
            style={{ position: 'relative', cursor: 'pointer', justifyContent: 'space-between', height: '150px', boxSizing: 'border-box', width: '100%', gap: '0', borderRadius: '15px', paddingLeft: '28px' }}
            onClick={() => openModal(ann)}
          >
            <span className="label">{displayDate(ann.Date)}</span>
            <span className="value" style={{ fontSize: '1.5rem' }}>{ann.Title}</span>
            <p style={{ display: '-webkit-box', WebkitLineClamp: '1', WebkitBoxOrient: 'vertical', overflow: 'hidden' }}>{ann.Text}</p>
          </div>
        ))}
      </div>

      {selectedAnnouncement && (
        <div className="modal-overlay" onClick={closeModal}>
          <div className="modal-content" style={{ width: '65%', maxWidth: '900px' }} onClick={(e) => e.stopPropagation()}>
            <span className="modal-close" onClick={closeModal}>&times;</span>

            {/* ── Header: navy gradient with title ── */}
            <div className="ann-modal-header" style={{ height: '160px' }}>
              <div className="ann-modal-badge"><Megaphone size={15} strokeWidth={4} /> Announcement</div>
              {isModalEditing ? (
                <input
                  className="ann-modal-title-input"
                  value={formData.Title}
                  onChange={(e) => setFormData({ ...formData, Title: e.target.value })}
                  placeholder="Announcement Title"
                  required
                />
              ) : (
                <h2 className="ann-modal-title">{selectedAnnouncement.Title}</h2>
              )}
            </div>

            {/* ── Meta strip: date + last modified ── */}
            <div className="ann-modal-meta">
              <span className="ann-modal-meta-date">
                <Calendar size={15} strokeWidth={4} /> {displayDate(selectedAnnouncement.Date)}
              </span>
              {selectedAnnouncement.LastModified && (
                <>
                  <span className="ann-modal-meta-sep">·</span>
                  <span className="ann-modal-meta-edited">
                    Last updated: {new Date(selectedAnnouncement.LastModified).toLocaleString()}
                  </span>
                </>
              )}
            </div>

            {/* ── Scrollable body ── */}
            <div className="modal-scroll">
              {isModalEditing ? (
                <div style={{ padding: '24px 40px', flex: 1, display: 'flex', flexDirection: 'column' }}>
                  <textarea
                    className="ann-modal-textarea"
                    value={formData.Text}
                    onChange={(e) => setFormData({ ...formData, Text: e.target.value })}
                    placeholder="Write your announcement content here…"
                    required
                  />
                </div>
              ) : (
                <p className="ann-modal-body-text">{selectedAnnouncement.Text}</p>
              )}
            </div>

            {/* ── Footer: action buttons ── */}
            <div className="ann-modal-footer">
              {isModalEditing ? (
                <>
                  <button
                    onClick={handleSubmit}
                    style={{ padding: '10px 24px', margin: 0, fontSize: '0.85rem', width: 'auto' }}>
                    <Check size={15} strokeWidth={4} /> {isModalAdding ? 'Post Announcement' : 'Save Changes'}
                  </button>
                  <button
                    onClick={isModalAdding ? closeModal : () => setIsModalEditing(false)}
                    style={{ padding: '10px 24px', margin: 0, fontSize: '0.85rem', width: 'auto', background: 'var(--text-muted)' }}>
                    <X size={15} strokeWidth={4} /> Cancel
                  </button>
                </>
              ) : (
                <>
                  <button
                    onClick={() => {
                      setIsModalEditing(true);
                      const dateStr = selectedAnnouncement.Date
                        ? new Date(selectedAnnouncement.Date).toISOString().split('T')[0]
                        : '';
                      setFormData({ Title: selectedAnnouncement.Title, Date: dateStr, Text: selectedAnnouncement.Text });
                      setEditingId(selectedAnnouncement.AnnID);
                    }}
                    style={{ padding: '10px 24px', margin: 0, fontSize: '0.85rem', width: 'auto' }}>
                    <Pencil size={15} strokeWidth={4} /> Edit
                  </button>
                  <button
                    onClick={(e) => { handleDelete(selectedAnnouncement.AnnID, e); }}
                    style={{ padding: '10px 24px', margin: 0, fontSize: '0.85rem', width: 'auto', background: 'var(--error)' }}>
                    <Trash2 size={15} strokeWidth={4} /> Delete
                  </button>
                </>
              )}
            </div>
          </div>
        </div>
      )}
    </div>
  );
};

export default Announcements;
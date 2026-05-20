import React, { useState, useEffect, useRef } from 'react';
import { useOutletContext, useNavigate } from 'react-router-dom';
import { supabase } from '../db';
import { Megaphone, Trophy, Users, Calendar, Save, Pencil, Trash2, Check, X, User } from 'lucide-react';

const Overview = () => {
  const { adminData } = useOutletContext();  
  const [posts, setPosts] = useState([]);
  const [loading, setLoading] = useState(true);
  const [currentIndex, setCurrentIndex] = useState(1);
  const [isTransitioning, setIsTransitioning] = useState(true);
  const isAnimating = useRef(false);
  const navigate = useNavigate();

  useEffect(() => {
    const fetchDashboardData = async () => {
      try {
        setLoading(true);
        const now = new Date().toISOString();

        // Create a timer promise that resolves after 1000ms
        const minimumDelay = new Promise(resolve => setTimeout(resolve, 1000));

        // Run the delay timer and both Supabase queries concurrently
        const [_, announcementsResult, tournamentsResult] = await Promise.all([
          minimumDelay,
          supabase
            .schema('Chessistant')
            .from('Announcements')
            .select('*')
            .order('Date', { ascending: false })
            .limit(5),
          supabase
            .schema('Chessistant')
            .from('Tournaments')
            .select('*')
            .gte('Date', now)
            .order('Date', { ascending: true })
            .limit(5)
        ]);

        if (announcementsResult.error) throw announcementsResult.error;
        if (tournamentsResult.error) throw tournamentsResult.error;

        const formattedAnnouncements = (announcementsResult.data || []).map(item => ({
          id: item.AnnID,
          title: item.Title || 'Announcement not available.',
          text: item.Text || 'Announcement details not available.',
          date: new Date(item.Date),
          type: 'announcement'
        }));

        const formattedTournaments = (tournamentsResult.data || []).map(item => ({
          id: item.TourID,
          title: item.Title || 'Tournament not available.',
          text: item.Text || 'Tournament details not available.',
          date: new Date(item.Date),
          type: 'tournament'
        }));

        const combined = [...formattedAnnouncements, ...formattedTournaments]
          .sort((a, b) => b.date - a.date)
          .slice(0, 5);

        setPosts(combined);
      } catch (err) {
        console.error('Error fetching dashboard items:', err);
      } finally {
        setLoading(false);
      }
    };

    fetchDashboardData();
  }, []);

  const extendedPosts = posts.length > 0 ? [posts[posts.length - 1], ...posts, posts[0]] : [];

  useEffect(() => {
    if (posts.length <= 1) return;

    const interval = setInterval(() => {
      handleNext();
    }, 4000);

    return () => clearInterval(interval);
  }, [currentIndex, posts]);

  const handleNext = () => {
    if (isAnimating.current || posts.length <= 1) return;
    isAnimating.current = true;
    setIsTransitioning(true);
    setCurrentIndex((prev) => prev + 1);
  };

  const handlePrev = () => {
    if (isAnimating.current || posts.length <= 1) return;
    isAnimating.current = true;
    setIsTransitioning(true);
    setCurrentIndex((prev) => prev - 1);
  };

  const goToSlide = (realIndex) => {
    const targetIndex = realIndex + 1;
    if (targetIndex === currentIndex) return;
    if (isAnimating.current) return;
    
    isAnimating.current = true;
    setIsTransitioning(true);
    setCurrentIndex(targetIndex);
  };

  const handleTransitionEnd = (e) => {
    if (e.target !== e.currentTarget) return; 
    
    isAnimating.current = false;

    if (currentIndex === 0) {
      setIsTransitioning(false);
      setCurrentIndex(posts.length);
    } else if (currentIndex === extendedPosts.length - 1) {
      setIsTransitioning(false);
      setCurrentIndex(1);
    }
  };

  let activeDotIndex = currentIndex - 1;
  if (currentIndex === 0) activeDotIndex = posts.length - 1;
  if (currentIndex === extendedPosts.length - 1) activeDotIndex = 0;

  if (loading) return (
    <div className="overlay">
      <div className="spinner"></div>
      <p>Loading Overview page...</p>
    </div>
  );

  if (posts.length === 0) {
    return <div style={{ padding: '20px', textAlign: 'center' }}>No recent updates available.</div>;
  }

  return (
    <>
      <header className="top-header">
        <div className="welcome-text">
          <h2 style={{ display: '-webkit-box', WebkitLineClamp: '1', WebkitBoxOrient: 'vertical', overflow: 'hidden' }}>
            Welcome back, <span className="admin-highlight">{adminData?.StudName || 'Admin'}</span>
          </h2>
        </div>
      </header>

      <div className="card" style={{ height: 'stretch', display: 'flex', flexDirection: 'column', gap: '10px', overflow: 'hidden' }}>
        <h3 style={{ fontSize: '2rem', borderBottom: '2px solid var(--gold)', paddingBottom: '10px', color: '#002965', letterSpacing: '1px', textTransform: 'uppercase' }}>Latest Announcements and Tournaments</h3>
        
        <div className="carousel" style={{ position: 'relative', display: 'flex', alignItems: 'center', justifyContent: 'center', flex: 1, overflow: 'hidden' }}>
          
          {/* Back Button */}
          <button
            onClick={handlePrev}
            style={{
              position: 'absolute',
              left: '0',
              width: '80px',
              height: '40px',
              border: 'none',
              borderRadius: '12px',
              backgroundColor: '#002965',
              cursor: 'pointer',
              display: 'flex',
              alignItems: 'center',
              justifyContent: 'center',
              transition: 'background-color 0.3s ease, transform 0.2s ease, box-shadow 0.2s ease',
              fontFamily: 'var(--font-sans)',
              zIndex: 10,
              boxShadow: '0 4px 12px rgba(0,0,0,0.35)',
            }}
            onMouseEnter={(e) => {
              e.currentTarget.style.backgroundColor = '#FF5A00';
              e.currentTarget.style.boxShadow = '0 6px 18px rgba(0,0,0,0.5)';
            }}
            onMouseLeave={(e) => {
              e.currentTarget.style.backgroundColor = '#002965';
              e.currentTarget.style.boxShadow = '0 4px 12px rgba(0,0,0,0.35)';
            }}
            aria-label="Previous post"
          >
            <img src="src/assets/left.png" alt="" style={{ width: '22px', height: '22px' }} />
          </button>

          {/* Carousel Viewport */}
          <div style={{ 
            width: '100%',
            height: '100%',
            overflow: 'hidden', 
            position: 'relative',
            borderRadius: '16px',
            margin: '0 45px',
            border: '2px solid var(--gold)',
          }}>
            {/* Sliding Track */}
            <div 
              onTransitionEnd={handleTransitionEnd}
              style={{
                display: 'flex',
                height: '100%',
                width: `${extendedPosts.length * 100}%`,
                transform: `translateX(-${currentIndex * (100 / extendedPosts.length)}%)`,
                transition: isTransitioning ? 'transform 0.5s ease-in-out' : 'none'
            }}>
              {extendedPosts.map((post, index) => (
                <div key={index} style={{
                  width: `${100 / extendedPosts.length}%`,
                  height: '100%',
                  display: 'flex',
                  alignItems: 'center',
                  justifyContent: 'center',
                  textAlign: 'center'
                }}>
                  <div className="stat-item" style={{
                    margin: '0 auto',
                    width: '100%',
                    height: '100%',
                    padding: '30px 50px',
                    display: 'flex',
                    flexDirection: 'column',
                    justifyContent: 'center',
                    gap: '0',
                    boxSizing: 'border-box',
                    cursor: 'pointer',
                    background: 'linear-gradient(135deg, #d0d8e4 0%, #b8c5d6 100%)',
                    transition: 'filter 0.2s ease'
                  }} 
                  onClick={() => navigate(
                    post.type === 'announcement' ? '/announcements' : '/tournaments',
                    { state: { openId: post.id } }
                  )}>
                    <div style={{
                      display: 'inline-block',
                      alignSelf: 'flex-start',
                      background: post.type === 'announcement' ? '#002965' : '#FF5A00',
                      color: '#ffffff',
                      fontSize: '0.7rem',
                      fontWeight: '700',
                      letterSpacing: '2px',
                      textTransform: 'uppercase',
                      padding: '4px 12px',
                      borderRadius: '6px',
                      border: '2px solid var(--gold)'
                    }}>
                      {post.type === 'announcement' ? <><Megaphone size={15} strokeWidth={4} style={{ verticalAlign: 'middle', marginRight: '5px', position: 'relative', top: '-1px'}}/> Announcement</> : <><Trophy size={15} strokeWidth={4} style={{ verticalAlign: 'middle', marginRight: '5px', position: 'relative', top: '-1px'}}/> Tournament</>}
                    </div>
                    <div className="title" style={{ color: '#002965' }}>
                      {post.title}
                    </div>
                    <div className="date" style={{ color: 'var(--oak)', fontSize: '1.1rem', fontStyle: 'italic' }}>
                      {post.date.toLocaleDateString('en-US', { weekday: 'long', year: 'numeric', month: 'long', day: 'numeric' })}
                    </div>
                    <hr style={{ border: 'none', borderTop: '1.5px solid var(--gold)', margin: '5px 0' }} />
                    <div className="text">
                      {post.text}
                    </div>
                  </div>
                </div>
              ))}
            </div>
          </div>

          {/* Next Button */}
          <button
            onClick={handleNext}
            style={{
              position: 'absolute',
              right: '0px',
              width: '80px',
              height: '40px',
              border: 'none',
              borderRadius: '12px',
              backgroundColor: '#002965',
              cursor: 'pointer',
              display: 'flex',
              alignItems: 'center',
              justifyContent: 'center',
              transition: 'background-color 0.3s ease, box-shadow 0.2s ease',
              fontFamily: 'var(--font-sans)',
              zIndex: 10,
              boxShadow: '0 4px 12px rgba(0,0,0,0.35)',
            }}
            onMouseEnter={(e) => {
              e.currentTarget.style.backgroundColor = '#FF5A00';
              e.currentTarget.style.boxShadow = '0 6px 18px rgba(0,0,0,0.5)';
            }}
            onMouseLeave={(e) => {
              e.currentTarget.style.backgroundColor = '#002965';
              e.currentTarget.style.boxShadow = '0 4px 12px rgba(0,0,0,0.35)';
            }}
            aria-label="Next post"
          >
            <img src="src/assets/right.png" alt="" style={{ width: '22px', height: '22px' }} />
          </button>
        </div>

        {/* Carousel Indicators */}
        <div style={{
          display: 'flex',
          justifyContent: 'center',
          alignItems: 'center',
          gap: '10px',
          paddingTop: '6px',
          paddingBottom: '4px',
        }}>
          {posts.map((_, index) => (
            <button
              key={index}
              onClick={() => goToSlide(index)}
              style={{
                width: index === activeDotIndex ? '32px' : '12px',
                height: '12px',
                borderRadius: index === activeDotIndex ? '6px' : '50%',
                border: '2px solid var(--gold)',
                backgroundColor: index === activeDotIndex ? '#002965' : 'var(--antique-white)',
                cursor: 'pointer',
                transition: 'all 0.35s ease',
                padding: 0,
                boxShadow: index === activeDotIndex ? '0 2px 8px rgba(0,41,101,0.4)' : 'none',
              }}
              aria-label={`Go to post ${index + 1}`}
            />
          ))}
        </div>
      </div>
    </>
  );
};

export default Overview;
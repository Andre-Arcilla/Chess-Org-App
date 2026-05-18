import React, { useState, useEffect, useRef } from 'react';
import { useOutletContext } from 'react-router-dom';
import { supabase } from '../db';

const Overview = () => {
  const { adminData } = useOutletContext();  
  const [posts, setPosts] = useState([]);
  const [loading, setLoading] = useState(true);
  const [currentIndex, setCurrentIndex] = useState(1);
  const [isTransitioning, setIsTransitioning] = useState(true);
  const isAnimating = useRef(false);

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
          title: 'Recent Announcement: ' + (item.Title || 'Announcement not available.'),
          text: item.Text || 'Announcement details not available.',
          date: new Date(item.Date),
          type: 'announcement'
        }));

        const formattedTournaments = (tournamentsResult.data || []).map(item => ({
          title: 'Upcoming Tournament: ' + (item.Title || 'Tournament not available.'),
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
        <h3 style={{ fontSize: '2rem' }}>Latest Announcements and Tournaments</h3>
        
        <div className="carousel" style={{ position: 'relative', display: 'flex', alignItems: 'center', justifyContent: 'center', flex: 1, overflow: 'hidden' }}>
          
          {/* Back Button */}
          <button
            onClick={handlePrev}
            style={{
              position: 'absolute',
              left: '0',
              width: '75px',
              height: '50px',
              border: 'none',
              borderRadius: '10px',
              backgroundColor: '#002965',
              cursor: 'pointer',
              fontSize: '1.5rem',
              display: 'flex',
              alignItems: 'center',
              justifyContent: 'center',
              color: '#FF5A00',
              transition: 'background-color 0.3s ease, color 0.3s ease',
              fontWeight: 'bold',
              fontFamily: 'var(--font-sans)',
              zIndex: 10,
            }}
            onMouseEnter={(e) => {
              e.currentTarget.style.backgroundColor = '#FF5A00';
              e.currentTarget.style.color = '#002965';
            }}
            onMouseLeave={(e) => {
              e.currentTarget.style.backgroundColor = '#002965';
              e.currentTarget.style.color = '#FF5A00';
            }}
            aria-label="Previous post"
          >
            <img src="src/assets/left.png" alt="" />
          </button>

          {/* Carousel Viewport */}
          <div style={{ 
            width: '100%',
            height: '100%',
            overflow: 'hidden', 
            position: 'relative',
            borderRadius: '25px',
            margin: '30px'
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
                    padding: '25px 60px',
                    display: 'flex',
                    flexDirection: 'column',
                    justifyContent: 'center',
                    gap: '0',
                    boxSizing: 'border-box'
                  }}>
                    <div className="title">
                      {post.title}
                    </div>
                    <div className="date">
                      {post.date.toLocaleDateString()}
                    </div>
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
              width: '75px',
              height: '50px',
              border: 'none',
              borderRadius: '10px',
              backgroundColor: '#002965',
              cursor: 'pointer',
              fontSize: '1.5rem',
              display: 'flex',
              alignItems: 'center',
              justifyContent: 'center',
              color: '#FF5A00',
              transition: 'background-color 0.3s ease, color 0.3s ease',
              fontWeight: 'bold',
              fontFamily: 'var(--font-sans)',
              zIndex: 10
            }}
            onMouseEnter={(e) => {
              e.currentTarget.style.backgroundColor = '#FF5A00';
              e.currentTarget.style.color = '#002965';
            }}
            onMouseLeave={(e) => {
              e.currentTarget.style.backgroundColor = '#002965';
              e.currentTarget.style.color = '#FF5A00';
            }}
            aria-label="Next post"
          >
            <img src="src/assets/right.png" alt="" />
          </button>
        </div>

        {/* Carousel Indicators */}
        <div style={{
          display: 'flex',
          justifyContent: 'center',
          gap: '10px',
          marginTop: '10px'
        }}>
          {posts.map((_, index) => (
            <button
              key={index}
              onClick={() => goToSlide(index)}
              style={{
                width: '15px',
                height: '15px',
                borderRadius: '50%',
                border: '2px solid var(--gold)',
                backgroundColor: index === activeDotIndex ? 'var(--gold)' : 'var(--antique-white)',
                cursor: 'pointer',
                transition: 'background-color 0.3s ease',
                padding: 0
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
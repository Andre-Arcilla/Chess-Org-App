import React, { useState, useEffect, useRef } from 'react';
import { useOutletContext } from 'react-router-dom';

const Overview = () => {
  const { adminData } = useOutletContext();
  
  const stats = [
    { label: 'Registered Members', value: '124' },
    { label: 'Active Matches', value: '12' },
    { label: 'Pending Applications', value: '3' },
    { label: 'Completed Tournaments', value: '8' },
    { label: 'Average Rating', value: '1850' }
  ];

  // 1. Create an extended array with clones at both ends for the infinite effect
  const extendedStats = [stats[stats.length - 1], ...stats, stats[0]];

  // 2. Start at index 1, which is our first "real" stat
  const [currentIndex, setCurrentIndex] = useState(1);
  const [isTransitioning, setIsTransitioning] = useState(true);
  
  // NEW: Add a ref to track if an animation is currently happening
  const isAnimating = useRef(false);

  // Auto-play timer
  useEffect(() => {
    const interval = setInterval(() => {
      handleNext();
    }, 4000);

    // Clears the timer whenever the slide changes so manual clicks reset the clock
    return () => clearInterval(interval);
  }, [currentIndex]);

  const handleNext = () => {
    if (isAnimating.current) return; // Prevent spam clicking
    isAnimating.current = true;
    setIsTransitioning(true);
    setCurrentIndex((prev) => prev + 1);
  };

  const handlePrev = () => {
    if (isAnimating.current) return; // Prevent spam clicking
    isAnimating.current = true;
    setIsTransitioning(true);
    setCurrentIndex((prev) => prev - 1);
  };

  const goToSlide = (realIndex) => {
    const targetIndex = realIndex + 1;
    if (targetIndex === currentIndex) return; // Do nothing if clicking the active dot
    if (isAnimating.current) return; // Prevent spam clicking
    
    isAnimating.current = true;
    setIsTransitioning(true);
    setCurrentIndex(targetIndex);
  };

  // 3. The magic trick: jump instantly when we reach a clone
  const handleTransitionEnd = (e) => {
    // Prevent bubbled transition events from children triggering this
    if (e.target !== e.currentTarget) return; 
    
    // Release the animation lock when the sliding finishes
    isAnimating.current = false;

    if (currentIndex === 0) {
      // Reached the clone of the last item at the beginning -> jump to real last item
      setIsTransitioning(false);
      setCurrentIndex(stats.length);
    } else if (currentIndex === extendedStats.length - 1) {
      // Reached the clone of the first item at the end -> jump to real first item
      setIsTransitioning(false);
      setCurrentIndex(1);
    }
  };

  // Calculate which indicator dot should be active based on our current position
  let activeDotIndex = currentIndex - 1;
  if (currentIndex === 0) activeDotIndex = stats.length - 1;
  if (currentIndex === extendedStats.length - 1) activeDotIndex = 0;

  return (
    <>
      <header className="top-header">
        <div className="welcome-text">
          <h2 style={{ display: '-webkit-box', WebkitLineClamp: '1', WebkitBoxOrient: 'vertical', overflow: 'hidden' }}>
            Welcome back, <span className="admin-highlight">{adminData?.StudName || 'Admin'}</span>
          </h2>
        </div>
      </header>
      
      <div className="card" style={{ height: 'stretch', display: 'flex', flexDirection: 'column', gap: '10px' }}>
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
              zIndex: 10
            }}
            onMouseEnter={(e) => {
              e.target.style.backgroundColor = '#FF5A00';
              e.target.style.color = '#002965';
            }}
            onMouseLeave={(e) => {
              e.target.style.backgroundColor = '#002965';
              e.target.style.color = '#FF5A00';
            }}
            aria-label="Previous stat"
          >
            ‹
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
                width: `${extendedStats.length * 100}%`,
                transform: `translateX(-${currentIndex * (100 / extendedStats.length)}%)`,
                transition: isTransitioning ? 'transform 0.5s ease-in-out' : 'none'
            }}>
              {/* Map out extended stat items (including clones) */}
              {extendedStats.map((stat, index) => (
                <div key={index} style={{
                  width: `${100 / extendedStats.length}%`,
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
                    padding: '25px 60px' 
                  }}>
                    <span className="label">{stat.label}</span>
                    <span className="value">{stat.value}</span>
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
              zIndex: 10
            }}
            onMouseEnter={(e) => {
              e.target.style.backgroundColor = '#FF5A00';
              e.target.style.color = '#002965';
            }}
            onMouseLeave={(e) => {
              e.target.style.backgroundColor = '#002965';
              e.target.style.color = '#FF5A00';
            }}
            aria-label="Next stat"
          >
            ›
          </button>
        </div>

        {/* Carousel Indicators */}
        <div style={{
          display: 'flex',
          justifyContent: 'center',
          gap: '10px',
          marginTop: '10px'
        }}>
          {stats.map((_, index) => (
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
              aria-label={`Go to stat ${index + 1}`}
            />
          ))}
        </div>
      </div>
    </>
  );
};

export default Overview;
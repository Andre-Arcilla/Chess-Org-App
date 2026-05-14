import React, { useState, useEffect } from 'react';

const Overview = () => {
  const [currentSlide, setCurrentSlide] = useState(0);
  const [timerKey, setTimerKey] = useState(0);

  const stats = [
    { label: 'Registered Members', value: '124' },
    { label: 'Active Matches', value: '12' },
    { label: 'Pending Applications', value: '3' },
    { label: 'Completed Tournaments', value: '8' },
    { label: 'Average Rating', value: '1850' }
  ];

  useEffect(() => {
    const interval = setInterval(() => {
      setCurrentSlide((prev) => (prev + 1) % stats.length);
    }, 4000);

    return () => clearInterval(interval);
  }, [timerKey, stats.length]);

  const goToSlide = (index) => {
    setCurrentSlide(index);
    setTimerKey(prev => prev + 1);
  };

  return (
    <div className="card">
      <h3 style={{ fontSize: '2rem' }}>Club Statistics</h3>
      <p>Current overview of your elite chess organization.</p>
      
      <div style={{ position: 'relative', height: '30vh', display: 'flex', alignItems: 'center', justifyContent: 'center', flex: 1 }}>
        {/* Back Button */}
        <button
          onClick={() => goToSlide((currentSlide - 1 + stats.length) % stats.length)}
          style={{
            position: 'absolute',
            left: '5px',
            width: '50px',
            height: 'stretch',
            border: 'none',
            backgroundColor: 'var(--antique-white)',
            cursor: 'pointer',
            fontSize: '1.5rem',
            display: 'flex',
            alignItems: 'center',
            justifyContent: 'center',
            color: 'var(--mahogany)',
            transition: 'all 0.3s ease',
            fontWeight: 'bold',
            zIndex: 10
          }}
          onMouseEnter={(e) => {
            e.target.style.backgroundColor = 'var(--gold)';
            e.target.style.color = 'var(--antique-white)';
          }}
          onMouseLeave={(e) => {
            e.target.style.backgroundColor = 'var(--antique-white)';
            e.target.style.color = 'var(--mahogany)';
          }}
          aria-label="Previous stat"
        >
          ‹
        </button>

        {/* Carousel Container */}
        <div style={{ 
          width: '100%',
          height: '100%',
          display: 'flex',
          alignItems: 'center',
          justifyContent: 'center'
        }}>
          {/* Stat Item Display */}
          <div style={{
            flex: 1,
            textAlign: 'center',
            animation: 'fadeIn 0.5s ease-in-out',
            height: '100%',
            display: 'flex',
            alignItems: 'center',
            justifyContent: 'center'
          }}>
            <div className="stat-item" style={{
              margin: '0 auto',
              width: '100%',
              height: '100%'
            }}>
              <span className="label">{stats[currentSlide].label}</span>
              <span className="value">{stats[currentSlide].value}</span>
            </div>
          </div>
        </div>

        {/* Next Button */}
        <button
          onClick={() => goToSlide((currentSlide + 1) % stats.length)}
          style={{
            position: 'absolute',
            right: '0px',
            width: '50px',
            height: 'stretch',
            border: 'none',
            backgroundColor: 'var(--antique-white)',
            cursor: 'pointer',
            fontSize: '1.5rem',
            display: 'flex',
            alignItems: 'center',
            justifyContent: 'center',
            color: 'var(--mahogany)',
            transition: 'all 0.3s ease',
            fontWeight: 'bold',
            zIndex: 10
          }}
          onMouseEnter={(e) => {
            e.target.style.backgroundColor = 'var(--gold)';
            e.target.style.color = 'var(--antique-white)';
          }}
          onMouseLeave={(e) => {
            e.target.style.backgroundColor = 'var(--antique-white)';
            e.target.style.color = 'var(--mahogany)';
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
        marginTop: '20px'
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
              backgroundColor: index === currentSlide ? 'var(--gold)' : 'var(--antique-white)',
              cursor: 'pointer',
              transition: 'background-color 0.3s ease',
              padding: 0
            }}
            aria-label={`Go to stat ${index + 1}`}
          />
        ))}
      </div>

      <style>{`
        @keyframes fadeIn {
          from {
            opacity: 0;
          }
          to {
            opacity: 1;
          }
        }
      `}</style>
    </div>
  );
};

export default Overview;

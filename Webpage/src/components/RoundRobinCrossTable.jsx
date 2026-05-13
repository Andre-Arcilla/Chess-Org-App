import React from 'react';

const RoundRobinCrossTable = ({ participantCount, results, setResults, readOnly = false, participants = [] }) => {
  const handleResultChange = (row, col, value) => {
    if (readOnly) return;
    // Allow partial inputs like '0.' and '1/' so users can finish typing 0.5 or 1/2
    const allowed = ['0', '1', '0.5', '1/2', '', '0.', '1/'];
    if (!allowed.includes(value)) return;

    let normalizedValue = value;
    if (value === '1/2') normalizedValue = '0.5';

    const newResults = { ...results };
    
    if (normalizedValue === '') {
      delete newResults[`${row}-${col}`];
      delete newResults[`${col}-${row}`];
    } else {
      newResults[`${row}-${col}`] = normalizedValue;
      
      // Reciprocal logic only for COMPLETE inputs
      if (normalizedValue === '1') {
        newResults[`${col}-${row}`] = '0';
      } else if (normalizedValue === '0') {
        newResults[`${col}-${row}`] = '1';
      } else if (normalizedValue === '0.5') {
        newResults[`${col}-${row}`] = '0.5';
      }
    }
    
    setResults(newResults);
  };

  const getScore = (rowIndex) => {
    let score = 0;
    for (let i = 0; i < participantCount; i++) {
      const val = results[`${rowIndex}-${i}`];
      if (val === '1') score += 1;
      else if (val === '0.5' || val === '1/2') score += 0.5;
    }
    return score;
  };

  return (
    <>
      <div style={{ padding: '20px 20px 0 20px' }}>
        <h4 style={{ fontFamily: 'var(--font-serif)', color: 'var(--mahogany)', marginBottom: '15px' }}>Tournament Crosstable</h4>
      </div>
      <table style={{ width: '100%', borderCollapse: 'separate', borderSpacing: 0, backgroundColor: 'rgba(255,255,255,0.5)', borderLeft: 'none', borderRight: 'none', borderBottom: '1px solid var(--oak)' }}>
        <thead style={{ position: 'sticky', top: 0, zIndex: 10, backgroundColor: 'var(--mahogany)', color: 'var(--parchment)' }}>
          <tr style={{ backgroundColor: 'var(--mahogany)', color: 'var(--parchment)' }}>
            <th style={{ 
              padding: '8px', 
              border: '1px solid var(--oak)', 
              fontSize: '0.8rem', 
              width: '40px',
              position: 'sticky',
              left: 0,
              backgroundColor: 'var(--mahogany)',
              zIndex: 11
            }}>Rank</th>
            <th style={{ padding: '8px', border: '1px solid var(--oak)', fontSize: '0.8rem', width: '180px', minWidth: '180px' }}>Player</th>
            {Array.from({ length: participantCount }).map((_, i) => (
              <th key={i} style={{ padding: '8px', border: '1px solid var(--oak)', fontSize: '0.8rem', width: '40px', minWidth: '40px' }}>{i + 1}</th>
            ))}
            <th style={{ padding: '8px', border: '1px solid var(--oak)', fontSize: '0.8rem', width: '60px', minWidth: '60px' }}>Score</th>
          </tr>
        </thead>
        <tbody>
          {Array.from({ length: participantCount }).map((_, rowIndex) => (
            <tr key={rowIndex}>
              <td style={{ 
                padding: '8px', 
                border: '1px solid var(--oak)', 
                textAlign: 'center', 
                fontSize: '0.8rem', 
                backgroundColor: 'var(--antique-white)',
                position: 'sticky',
                left: 0,
                zIndex: 5
              }}>
                {rowIndex + 1}
              </td>
              <td style={{ 
                padding: '8px', 
                border: '1px solid var(--oak)', 
                fontWeight: 'bold', 
                fontSize: '0.8rem', 
                backgroundColor: 'var(--antique-white)', 
                width: '180px', 
                minWidth: '180px',
                maxWidth: '180px',
                overflow: 'hidden',
                textOverflow: 'ellipsis',
                whiteSpace: 'nowrap'
              }}>
                {participants[rowIndex]?.StudName || `Player #${rowIndex + 1}`}
              </td>
              {Array.from({ length: participantCount }).map((_, colIndex) => (
                <td 
                  key={colIndex} 
                  style={{ 
                    padding: '0', 
                    border: '1px solid var(--oak)', 
                    textAlign: 'center', 
                    fontSize: '0.8rem',
                    width: '40px',
                    height: '40px',
                    minWidth: '40px',
                    backgroundColor: rowIndex === colIndex ? 'var(--mahogany-light)' : 'transparent',
                    color: rowIndex === colIndex ? 'var(--parchment)' : 'inherit'
                  }}
                >
                  {rowIndex === colIndex ? '—' : (
                    readOnly ? (
                      <span style={{ 
                        width: '100%', 
                        height: '100%', 
                        display: 'flex', 
                        justifyContent: 'center', 
                        alignItems: 'center',
                        fontSize: '0.85rem' 
                      }}>
                        {results[`${rowIndex}-${colIndex}`] || ''}
                      </span>
                    ) : (
                      <input 
                        value={results[`${rowIndex}-${colIndex}`] || ''}
                        onChange={(e) => handleResultChange(rowIndex, colIndex, e.target.value)}
                        style={{
                          width: '100%',
                          height: '100%',
                          textAlign: 'center',
                          border: 'none',
                          background: 'transparent',
                          outline: 'none',
                          padding: 0,
                          color: 'inherit',
                          fontFamily: 'inherit',
                          fontSize: '0.85rem'
                        }}
                      />
                    )
                  )}
                </td>
              ))}
              <td style={{ padding: '8px', border: '1px solid var(--oak)', textAlign: 'center', fontWeight: 'bold', fontSize: '0.8rem' }}>
                {getScore(rowIndex)}
              </td>
            </tr>
          ))}
        </tbody>
      </table>
    </>
  );
};

export default RoundRobinCrossTable;

<?php
session_start();
if (!isset($_SESSION['logged_in']) || $_SESSION['role'] !== 'Coach') {
    header("Location: login.php");
    exit();
}
?>
<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>Chessistant - Coach Dashboard</title>
    <link rel="stylesheet" href="chessistant.css">
    <link rel="icon" href="data:image/svg+xml,<svg xmlns=%22http://www.w3.org/2000/svg%22 viewBox=%220 0 100 100%22><text y=%22.9em%22 font-size=%2290%22>♟️</text></svg>">
</head>

<body class="coach-theme">
    
    <nav class="navbar">
        <div class="nav-container">
            <div class="nav-brand">
                <span>♟️</span>
                <span>Chessistant</span>
                <span class="role-badge">COACH</span>
            </div>
            <ul class="nav-links">
                <li><a href="#" class="nav-link active" data-page="overview">Overview</a></li>
                <li><a href="#" class="nav-link" data-page="players">Players</a></li>
                <li><a href="#" class="nav-link" data-page="training">Training</a></li>
                <li><a href="#" class="nav-link" data-page="tournaments">Tournaments</a></li>
                <li><a href="#" class="nav-link" data-page="feedback">Game Reviews</a></li>

                <li><a href="logout.php" class="nav-link" style="background: rgba(231, 76, 60, 0.8); margin-left: 15px; border-radius: 6px; padding: 10px 20px; align-self: center;">Logout 🚪</a></li>
            </ul>
        </div>
    </nav>

    <div id="app">
        <div id="overview-page" class="page active">
            <div class="container">
                <div class="hero">
                    <h1>Welcome, Coach Clark Dela Torre</h1>
                    <p>Manage your team, assign training, and track player progress</p>
                </div>

                <div class="stats-grid">
                    <div class="stat-card">
                        <div class="stat-value">45</div>
                        <div class="stat-label">Total Players</div>
                    </div>
                    <div class="stat-card">
                        <div class="stat-value">1842</div>
                        <div class="stat-label">Avg Team Rating</div>
                    </div>
                    <div class="stat-card">
                        <div class="stat-value">89%</div>
                        <div class="stat-label">Practice Completion</div>
                    </div>
                    <div class="stat-card">
                        <div class="stat-value">12</div>
                        <div class="stat-label">Pending Reviews</div>
                    </div>
                </div>

                <div class="section">
                    <h2>Quick Actions</h2>
                    <div class="card-grid" style="margin-top: 20px;">
                        <div class="card">
                            <div class="card-header">📝 Assign Training</div>
                            <div class="card-body">
                                <p>Create custom training plans and assign lessons to players</p>
                            </div>
                            <div class="card-footer">
                                <button class="btn btn-coach" onclick="openModal('assignTraining')">Assign Now</button>
                            </div>
                        </div>
                        <div class="card">
                            <div class="card-header">🏆 Manage Tournaments</div>
                            <div class="card-body">
                                <p>Create, organize, and manage team tournaments</p>
                            </div>
                            <div class="card-footer">
                                <button class="btn btn-coach" onclick="switchPage('tournaments')">Go to Tournaments</button>
                            </div>
                        </div>
                        <div class="card">
                            <div class="card-header">💬 Review Games</div>
                            <div class="card-body">
                                <p>Provide feedback on player games with private coach notes</p>
                            </div>
                            <div class="card-footer">
                                <button class="btn btn-coach" onclick="switchPage('feedback')">Review Games</button>
                            </div>
                        </div>
                    </div>
                </div>

                <div class="section">
                    <div class="section-header">
                        <h2>Top Performers This Month</h2>
                        <button class="btn btn-outline" onclick="switchPage('players')">View All Players</button>
                    </div>
                    <div class="card-grid">
                        <div class="player-card">
                            <div class="player-header">
                                <div class="player-name">Carlos Mendoza</div>
                                <div class="player-rating">1940</div>
                            </div>
                            <div class="player-stats">
                                <div class="player-stat">
                                    <div class="player-stat-value">+45</div>
                                    <div class="player-stat-label">Rating Gain</div>
                                </div>
                                <div class="player-stat">
                                    <div class="player-stat-value">78%</div>
                                    <div class="player-stat-label">Win Rate</div>
                                </div>
                                <div class="player-stat">
                                    <div class="player-stat-value">24</div>
                                    <div class="player-stat-label">Games</div>
                                </div>
                            </div>
                            <button class="btn btn-coach" style="width: 100%;">View Profile</button>
                        </div>

                        <div class="player-card">
                            <div class="player-header">
                                <div class="player-name">John Dela Cruz</div>
                                <div class="player-rating">1850</div>
                            </div>
                            <div class="player-stats">
                                <div class="player-stat">
                                    <div class="player-stat-value">+32</div>
                                    <div class="player-stat-label">Rating Gain</div>
                                </div>
                                <div class="player-stat">
                                    <div class="player-stat-value">67%</div>
                                    <div class="player-stat-label">Win Rate</div>
                                </div>
                                <div class="player-stat">
                                    <div class="player-stat-value">19</div>
                                    <div class="player-stat-label">Games</div>
                                </div>
                            </div>
                            <button class="btn btn-coach" style="width: 100%;">View Profile</button>
                        </div>

                        <div class="player-card">
                            <div class="player-header">
                                <div class="player-name">Maria Santos</div>
                                <div class="player-rating">1720</div>
                            </div>
                            <div class="player-stats">
                                <div class="player-stat">
                                    <div class="player-stat-value">+28</div>
                                    <div class="player-stat-label">Rating Gain</div>
                                </div>
                                <div class="player-stat">
                                    <div class="player-stat-value">64%</div>
                                    <div class="player-stat-label">Win Rate</div>
                                </div>
                                <div class="player-stat">
                                    <div class="player-stat-value">17</div>
                                    <div class="player-stat-label">Games</div>
                                </div>
                            </div>
                            <button class="btn btn-coach" style="width: 100%;">View Profile</button>
                        </div>
                    </div>
                </div>

                <div class="section">
                    <h2>Recent Team Activity</h2>
                    <div class="alert alert-coach" style="margin-top: 20px;">
                        <span>🎯</span>
                        <span><strong>5 players</strong> completed their tactical training this week</span>
                    </div>
                    <div class="alert alert-info">
                        <span>🏆</span>
                        <span><strong>Weekly Blitz Tournament</strong> starts in 2 days - 12 players registered</span>
                    </div>
                </div>
            </div>
        </div>

        <div id="players-page" class="page" style="display: none;">
            <div class="container">
                <div class="hero">
                    <h1>Player Management</h1>
                    <p>View and track all team members' chess performance</p>
                </div>

                <div class="section">
                    <div class="section-header">
                        <h2>All Players (45)</h2>
                        <div>
                            <input type="text" placeholder="Search players..." style="padding: 10px; border: 2px solid #e9ecef; border-radius: 6px; margin-right: 10px;">
                            <select style="padding: 10px; border: 2px solid #e9ecef; border-radius: 6px;">
                                <option>All Ratings</option>
                                <option>2000+</option>
                                <option>1800-2000</option>
                                <option>1600-1800</option>
                                <option>Below 1600</option>
                            </select>
                        </div>
                    </div>

                    <div class="table-container">
                        <table>
                            <thead>
                                <tr>
                                    <th>Player Name</th>
                                    <th>Current Rating</th>
                                    <th>Win Rate</th>
                                    <th>Total Games</th>
                                    <th>This Month</th>
                                    <th>Training Progress</th>
                                    <th>Actions</th>
                                </tr>
                            </thead>
                            <tbody>
                                <tr>
                                    <td><strong>Carlos Mendoza</strong></td>
                                    <td><span style="font-weight: 700; color: var(--coach-purple);">1940</span></td>
                                    <td>78%</td>
                                    <td>156</td>
                                    <td style="color: var(--victory-green); font-weight: 600;">+45</td>
                                    <td>
                                        <div class="progress-bar" style="width: 150px;">
                                            <div class="progress-fill" style="width: 85%;"></div>
                                        </div>
                                        <small style="color: var(--text-secondary);">85%</small>
                                    </td>
                                    <td>
                                        <button class="btn btn-coach" style="padding: 8px 16px; font-size: 0.85rem;">View</button>
                                    </td>
                                </tr>
                                <tr>
                                    <td><strong>John Dela Cruz</strong></td>
                                    <td><span style="font-weight: 700; color: var(--coach-purple);">1850</span></td>
                                    <td>67%</td>
                                    <td>143</td>
                                    <td style="color: var(--victory-green); font-weight: 600;">+32</td>
                                    <td>
                                        <div class="progress-bar" style="width: 150px;">
                                            <div class="progress-fill" style="width: 72%;"></div>
                                        </div>
                                        <small style="color: var(--text-secondary);">72%</small>
                                    </td>
                                    <td>
                                        <button class="btn btn-coach" style="padding: 8px 16px; font-size: 0.85rem;">View</button>
                                    </td>
                                </tr>
                                <tr>
                                    <td><strong>Maria Santos</strong></td>
                                    <td><span style="font-weight: 700; color: var(--coach-purple);">1720</span></td>
                                    <td>64%</td>
                                    <td>128</td>
                                    <td style="color: var(--victory-green); font-weight: 600;">+28</td>
                                    <td>
                                        <div class="progress-bar" style="width: 150px;">
                                            <div class="progress-fill" style="width: 90%;"></div>
                                        </div>
                                        <small style="color: var(--text-secondary);">90%</small>
                                    </td>
                                    <td>
                                        <button class="btn btn-coach" style="padding: 8px 16px; font-size: 0.85rem;">View</button>
                                    </td>
                                </tr>
                                <tr>
                                    <td><strong>Pedro Reyes</strong></td>
                                    <td><span style="font-weight: 700; color: var(--coach-purple);">1680</span></td>
                                    <td>58%</td>
                                    <td>112</td>
                                    <td style="color: var(--stalemate-gold); font-weight: 600;">+12</td>
                                    <td>
                                        <div class="progress-bar" style="width: 150px;">
                                            <div class="progress-fill" style="width: 65%;"></div>
                                        </div>
                                        <small style="color: var(--text-secondary);">65%</small>
                                    </td>
                                    <td>
                                        <button class="btn btn-coach" style="padding: 8px 16px; font-size: 0.85rem;">View</button>
                                    </td>
                                </tr>
                                <tr>
                                    <td><strong>Anna Garcia</strong></td>
                                    <td><span style="font-weight: 700; color: var(--coach-purple);">1590</span></td>
                                    <td>61%</td>
                                    <td>98</td>
                                    <td style="color: var(--victory-green); font-weight: 600;">+18</td>
                                    <td>
                                        <div class="progress-bar" style="width: 150px;">
                                            <div class="progress-fill" style="width: 78%;"></div>
                                        </div>
                                        <small style="color: var(--text-secondary);">78%</small>
                                    </td>
                                    <td>
                                        <button class="btn btn-coach" style="padding: 8px 16px; font-size: 0.85rem;">View</button>
                                    </td>
                                </tr>
                            </tbody>
                        </table>
                    </div>
                </div>
            </div>
        </div>

        <div id="training-page" class="page" style="display: none;">
            <div class="container">
                <div class="hero">
                    <h1>Training Management</h1>
                    <p>Assign lessons, create practice plans, and track completion</p>
                </div>

                <div class="section">
                    <div class="section-header">
                        <h2>Training Programs</h2>
                        <button class="btn btn-coach" onclick="openModal('assignTraining')">+ Assign New Training</button>
                    </div>

                    <div class="tabs">
                        <div class="tab-buttons">
                            <button class="tab-btn active" data-tab="active-plans">Active Plans</button>
                            <button class="tab-btn" data-tab="templates">Templates</button>
                            <button class="tab-btn" data-tab="completion">Completion Stats</button>
                        </div>

                        <div class="tab-content active" id="active-plans">
                            <div class="card-grid">
                                <div class="card">
                                    <div class="card-header">Tactical Patterns - Week 1</div>
                                    <div class="card-body">
                                        <p><strong>Assigned to:</strong> 12 players</p>
                                        <p><strong>Deadline:</strong> Feb 20, 2026</p>
                                        <p><strong>Completion:</strong></p>
                                        <div class="progress-bar">
                                            <div class="progress-fill" style="width: 75%;"></div>
                                        </div>
                                        <small style="color: var(--text-secondary);">9/12 completed</small>
                                    </div>
                                    <div class="card-footer">
                                        <button class="btn btn-outline">View Details</button>
                                    </div>
                                </div>

                                <div class="card">
                                    <div class="card-header">Opening Repertoire</div>
                                    <div class="card-body">
                                        <p><strong>Assigned to:</strong> 8 players</p>
                                        <p><strong>Deadline:</strong> Feb 25, 2026</p>
                                        <p><strong>Completion:</strong></p>
                                        <div class="progress-bar">
                                            <div class="progress-fill" style="width: 50%;"></div>
                                        </div>
                                        <small style="color: var(--text-secondary);">4/8 completed</small>
                                    </div>
                                    <div class="card-footer">
                                        <button class="btn btn-outline">View Details</button>
                                    </div>
                                </div>

                                <div class="card">
                                    <div class="card-header">Endgame Mastery</div>
                                    <div class="card-body">
                                        <p><strong>Assigned to:</strong> 15 players</p>
                                        <p><strong>Deadline:</strong> Mar 1, 2026</p>
                                        <p><strong>Completion:</strong></p>
                                        <div class="progress-bar">
                                            <div class="progress-fill" style="width: 60%;"></div>
                                        </div>
                                        <small style="color: var(--text-secondary);">9/15 completed</small>
                                    </div>
                                    <div class="card-footer">
                                        <button class="btn btn-outline">View Details</button>
                                    </div>
                                </div>
                            </div>
                        </div>

                        <div class="tab-content" id="templates">
                            <p>Training templates library will be displayed here...</p>
                        </div>

                        <div class="tab-content" id="completion">
                            <p>Detailed completion statistics will be displayed here...</p>
                        </div>
                    </div>
                </div>
            </div>
        </div>

        <div id="tournaments-page" class="page" style="display: none;">
            <div class="container">
                <div class="hero">
                    <h1>Tournament Management</h1>
                    <p>Create, organize, and manage team tournaments</p>
                </div>

                <div class="section">
                    <div class="section-header">
                        <h2>Manage Tournaments</h2>
                        <button class="btn btn-success" onclick="openModal('createTournament')">+ Create Tournament</button>
                    </div>

                    <div class="card-grid">
                        <div class="card">
                            <div class="card-header" style="background: linear-gradient(135deg, var(--victory-green) 0%, #229954 100%);">
                                UMak Championship 2026
                            </div>
                            <div class="card-body">
                                <p><strong>Date:</strong> March 15-17, 2026</p>
                                <p><strong>Format:</strong> Swiss System, 7 rounds</p>
                                <p><strong>Participants:</strong> 24/32</p>
                                <p><strong>Status:</strong> <span class="badge badge-success">Registration Open</span></p>
                            </div>
                            <div class="card-footer">
                                <button class="btn btn-coach">Manage</button>
                                <button class="btn btn-outline" style="margin-left: 10px;">Edit</button>
                            </div>
                        </div>

                        <div class="card">
                            <div class="card-header" style="background: linear-gradient(135deg, var(--strategic-blue) 0%, #2980b9 100%);">
                                Weekly Blitz Tournament
                            </div>
                            <div class="card-body">
                                <p><strong>Date:</strong> Every Saturday, 2:00 PM</p>
                                <p><strong>Format:</strong> Round Robin</p>
                                <p><strong>Participants:</strong> 12/16</p>
                                <p><strong>Status:</strong> <span class="badge badge-warning">This Weekend</span></p>
                            </div>
                            <div class="card-footer">
                                <button class="btn btn-coach">Manage</button>
                                <button class="btn btn-outline" style="margin-left: 10px;">Edit</button>
                            </div>
                        </div>
                    </div>
                </div>
            </div>
        </div>

        <div id="feedback-page" class="page" style="display: none;">
            <div class="container">
                <div class="hero">
                    <h1>Game Reviews & Feedback</h1>
                    <p>Provide private coaching feedback on player games</p>
                </div>

                <div class="alert alert-coach">
                    <span>🔒</span>
                    <span><strong>Coach Private Feature:</strong> Your feedback is only visible to you and the player - admins cannot see these notes.</span>
                </div>

                <div class="section">
                    <div class="section-header">
                        <h2>Pending Reviews (12)</h2>
                        <select style="padding: 10px; border: 2px solid #e9ecef; border-radius: 6px;">
                            <option>All Players</option>
                            <option>High Priority</option>
                            <option>Recent Games</option>
                        </select>
                    </div>

                    <div class="table-container">
                        <table>
                            <thead>
                                <tr>
                                    <th>Player</th>
                                    <th>Opponent</th>
                                    <th>Result</th>
                                    <th>Date</th>
                                    <th>Opening</th>
                                    <th>Your Feedback</th>
                                    <th>Actions</th>
                                </tr>
                            </thead>
                            <tbody>
                                <tr>
                                    <td><strong>Carlos Mendoza</strong></td>
                                    <td>External Player</td>
                                    <td><span class="badge badge-success">Win</span></td>
                                    <td>Feb 12, 2026</td>
                                    <td>Sicilian Defense</td>
                                    <td><span class="badge badge-warning">Pending</span></td>
                                    <td>
                                        <button class="btn btn-coach" style="padding: 8px 16px; font-size: 0.85rem;" onclick="openModal('addFeedback')">Review</button>
                                    </td>
                                </tr>
                                <tr>
                                    <td><strong>John Dela Cruz</strong></td>
                                    <td>Maria Santos</td>
                                    <td><span class="badge badge-danger">Loss</span></td>
                                    <td>Feb 11, 2026</td>
                                    <td>Queen's Gambit</td>
                                    <td><span class="badge badge-coach">Reviewed</span></td>
                                    <td>
                                        <button class="btn btn-outline" style="padding: 8px 16px; font-size: 0.85rem;" onclick="openModal('viewFeedback')">View</button>
                                    </td>
                                </tr>
                                <tr>
                                    <td><strong>Maria Santos</strong></td>
                                    <td>Pedro Reyes</td>
                                    <td><span class="badge badge-success">Win</span></td>
                                    <td>Feb 10, 2026</td>
                                    <td>Ruy Lopez</td>
                                    <td><span class="badge badge-warning">Pending</span></td>
                                    <td>
                                        <button class="btn btn-coach" style="padding: 8px 16px; font-size: 0.85rem;" onclick="openModal('addFeedback')">Review</button>
                                    </td>
                                </tr>
                            </tbody>
                        </table>
                    </div>
                </div>

                <div class="section">
                    <h2>Recent Feedback Given</h2>
                    <div class="feedback-box">
                        <div class="feedback-header">
                            <span class="feedback-author">To: John Dela Cruz</span>
                            <span class="feedback-date">Feb 11, 2026</span>
                        </div>
                        <div style="margin: 10px 0; font-size: 0.9rem; color: var(--text-secondary);">
                            <strong>Game:</strong> vs Maria Santos (Loss) - Queen's Gambit
                        </div>
                        <div class="feedback-text">
                            <p><strong>Strengths:</strong> Good opening preparation up to move 10. Your positional understanding in the middlegame was solid.</p>
                            <p style="margin-top: 10px;"><strong>Areas for Improvement:</strong> On move 23, you missed a tactical opportunity with Nxe5. Also, watch your time management - you spent too much time in the opening and got into time trouble.</p>
                            <p style="margin-top: 10px;"><strong>Homework:</strong> Practice knight endgames and review the Tartakower variation of the Queen's Gambit.</p>
                        </div>
                    </div>

                    <div class="feedback-box">
                        <div class="feedback-header">
                            <span class="feedback-author">To: Carlos Mendoza</span>
                            <span class="feedback-date">Feb 9, 2026</span>
                        </div>
                        <div style="margin: 10px 0; font-size: 0.9rem; color: var(--text-secondary);">
                            <strong>Game:</strong> vs External Player (Win) - Sicilian Defense
                        </div>
                        <div class="feedback-text">
                            <p><strong>Excellent game!</strong> Your tactical vision on move 18 (Rxe4) was brilliant. The way you converted the endgame showed great technique.</p>
                            <p style="margin-top: 10px;"><strong>Minor point:</strong> Be careful with move 12. Your opponent could have punished you with d5. Study that position.</p>
                        </div>
                    </div>
                </div>
            </div>
        </div>
    </div>

    <div id="assignTraining" class="modal">
        <div class="modal-content">
            <div class="modal-header">
                <h3>Assign Training</h3>
                <button class="modal-close" onclick="closeModal('assignTraining')">×</button>
            </div>
            <div class="modal-body">
                <form>
                    <div class="form-group">
                        <label class="form-label">Training Module</label>
                        <select class="form-select">
                            <option>Tactical Patterns</option>
                            <option>Opening Repertoire</option>
                            <option>Endgame Mastery</option>
                            <option>Positional Play</option>
                        </select>
                    </div>
                    <div class="form-group">
                        <label class="form-label">Assign To</label>
                        <select class="form-select" multiple style="height: 120px;">
                            <option>All Players</option>
                            <option>Carlos Mendoza</option>
                            <option>John Dela Cruz</option>
                            <option>Maria Santos</option>
                            <option>Pedro Reyes</option>
                        </select>
                        <small style="color: var(--text-secondary);">Hold Ctrl/Cmd to select multiple</small>
                    </div>
                    <div class="form-group">
                        <label class="form-label">Deadline</label>
                        <input type="date" class="form-input">
                    </div>
                    <div class="form-group">
                        <label class="form-label">Instructions (Optional)</label>
                        <textarea class="form-textarea" placeholder="Add any specific instructions or focus areas..."></textarea>
                    </div>
                    <button type="submit" class="btn btn-coach" style="width: 100%;">Assign Training</button>
                </form>
            </div>
        </div>
    </div>

    <div id="createTournament" class="modal">
        <div class="modal-content">
            <div class="modal-header">
                <h3>Create New Tournament</h3>
                <button class="modal-close" onclick="closeModal('createTournament')">×</button>
            </div>
            <div class="modal-body">
                <form>
                    <div class="form-group">
                        <label class="form-label">Tournament Name</label>
                        <input type="text" class="form-input" placeholder="e.g., Spring Championship 2026">
                    </div>
                    <div class="form-group">
                        <label class="form-label">Format</label>
                        <select class="form-select">
                            <option>Swiss System</option>
                            <option>Round Robin</option>
                            <option>Single Elimination</option>
                            <option>Double Elimination</option>
                        </select>
                    </div>
                    <div class="form-group">
                        <label class="form-label">Start Date</label>
                        <input type="date" class="form-input">
                    </div>
                    <div class="form-group">
                        <label class="form-label">Time Control</label>
                        <input type="text" class="form-input" placeholder="e.g., 15+10">
                    </div>
                    <div class="form-group">
                        <label class="form-label">Max Participants</label>
                        <input type="number" class="form-input" placeholder="32">
                    </div>
                    <button type="submit" class="btn btn-success" style="width: 100%;">Create Tournament</button>
                </form>
            </div>
        </div>
    </div>

    <div id="addFeedback" class="modal">
        <div class="modal-content">
            <div class="modal-header">
                <h3>Add Game Feedback</h3>
                <button class="modal-close" onclick="closeModal('addFeedback')">×</button>
            </div>
            <div class="modal-body">
                <div class="alert alert-coach">
                    <span>🔒</span>
                    <span>This feedback is private between you and the player</span>
                </div>
                <form>
                    <div class="form-group">
                        <label class="form-label">Player Strengths</label>
                        <textarea class="form-textarea" placeholder="What did the player do well in this game?"></textarea>
                    </div>
                    <div class="form-group">
                        <label class="form-label">Areas for Improvement</label>
                        <textarea class="form-textarea" placeholder="What should the player work on?"></textarea>
                    </div>
                    <div class="form-group">
                        <label class="form-label">Specific Homework/Practice</label>
                        <textarea class="form-textarea" placeholder="What should they practice next?"></textarea>
                    </div>
                    <div class="form-group">
                        <label class="form-label">Overall Rating</label>
                        <select class="form-select">
                            <option>Excellent Performance</option>
                            <option>Good Performance</option>
                            <option>Needs Improvement</option>
                        </select>
                    </div>
                    <button type="submit" class="btn btn-coach" style="width: 100%;">Submit Feedback</button>
                </form>
            </div>
        </div>
    </div>

    <div id="viewFeedback" class="modal">
        <div class="modal-content">
            <div class="modal-header">
                <h3>Your Feedback - John Dela Cruz</h3>
                <button class="modal-close" onclick="closeModal('viewFeedback')">×</button>
            </div>
            <div class="modal-body">
                <div style="margin-bottom: 15px; padding: 10px; background: #f8f9fa; border-radius: 6px;">
                    <strong>Game:</strong> vs Maria Santos (Loss) - Queen's Gambit<br>
                    <strong>Date:</strong> Feb 11, 2026
                </div>
                <div class="feedback-box">
                    <h4 style="margin-bottom: 10px;">Strengths:</h4>
                    <p>Good opening preparation up to move 10. Your positional understanding in the middlegame was solid.</p>
                    
                    <h4 style="margin: 15px 0 10px 0;">Areas for Improvement:</h4>
                    <p>On move 23, you missed a tactical opportunity with Nxe5. Also, watch your time management - you spent too much time in the opening and got into time trouble.</p>
                    
                    <h4 style="margin: 15px 0 10px 0;">Homework:</h4>
                    <p>Practice knight endgames and review the Tartakower variation of the Queen's Gambit.</p>
                    
                    <div style="margin-top: 15px; padding-top: 15px; border-top: 1px solid #e9ecef;">
                        <strong>Overall Rating:</strong> <span class="badge badge-warning">Needs Improvement</span>
                    </div>
                </div>
                <button class="btn btn-outline" style="width: 100%; margin-top: 15px;" onclick="closeModal('viewFeedback')">Close</button>
            </div>
        </div>
    </div>

    <footer>
        <p><strong>Chessistant Coach Dashboard</strong></p>
        <p>UMak Chess Team | Coach: Clark Dela Torre</p>
    </footer>

<script>
// --- Page Navigation ---
    const navLinks = document.querySelectorAll('.nav-link');
    const pages = document.querySelectorAll('.page');

    navLinks.forEach(link => {
        link.addEventListener('click', (e) => {
            // THE FIX: Only prevent default and switch tabs IF it has a data-page attribute
            if (link.hasAttribute('data-page')) {
                e.preventDefault();
                const pageName = link.getAttribute('data-page');
                switchPage(pageName);
            }
            // If it DOESN'T have data-page (like our Logout button), it will normally navigate to the link!
        });
    });
    // We moved the highlight logic INSIDE this function
    function switchPage(pageName) {
        // Hide all pages
        pages.forEach(page => page.style.display = 'none');
        
        // Show target page
        document.getElementById(`${pageName}-page`).style.display = 'block';
        
        // Update the active highlight on the navbar
        navLinks.forEach(link => {
            link.classList.remove('active');
            if (link.getAttribute('data-page') === pageName) {
                link.classList.add('active');
            }
        });
        
        // Scroll back to top
        window.scrollTo(0, 0);
    }

    // Modal Functions
    function openModal(modalId) {
        document.getElementById(modalId).classList.add('active');
    }

    function closeModal(modalId) {
        document.getElementById(modalId).classList.remove('active');
    }

    document.querySelectorAll('.modal').forEach(modal => {
        modal.addEventListener('click', (e) => {
            if (e.target === modal) {
                modal.classList.remove('active');
            }
        });
    });

    // Form handling
    document.querySelectorAll('form').forEach(form => {
        form.addEventListener('submit', (e) => {
            e.preventDefault();
            alert('Submitted! (Demo)');
            const modal = form.closest('.modal');
            if (modal) modal.classList.remove('active');
        });
    });
</script>
</body>
</html>
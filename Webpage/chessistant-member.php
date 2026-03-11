<?php
session_start();
if (!isset($_SESSION['logged_in']) || $_SESSION['role'] !== 'Member') {
    header("Location: login.php");
    exit();
}
?>
<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>Chessistant - UMak Chess Team Platform</title>
    <link rel="stylesheet" href="chessistant.css">
    <link rel="icon" href="data:image/svg+xml,<svg xmlns=%22http://www.w3.org/2000/svg%22 viewBox=%220 0 100 100%22><text y=%22.9em%22 font-size=%2290%22>♟️</text></svg>">
</head>
<body>
    <nav class="navbar">
        <div class="nav-container">
            <div class="nav-brand">
                <span>♟️</span>
                <span>Chessistant</span>
            </div>
            <button class="mobile-menu-btn" id="mobileMenuBtn" style="display:none;">☰</button>
            <ul class="nav-links" id="navLinks">
                <li><a href="#" class="nav-link active" data-page="dashboard">Dashboard</a></li>
                <li><a href="#" class="nav-link" data-page="training">Training</a></li>
                <li><a href="#" class="nav-link" data-page="tournaments">Tournaments</a></li>
                <li><a href="#" class="nav-link" data-page="analytics">Analytics</a></li>
                <li><a href="#" class="nav-link" data-page="admin">Settings</a></li>

                <li><a href="logout.php" class="nav-link" style="background: rgba(231, 76, 60, 0.8); margin-left: 15px; border-radius: 6px; padding: 10px 20px; align-self: center;">Logout 🚪</a></li>
            </ul>
        </div>
    </nav>

    <div id="app">
        <div id="dashboard-page" class="page active">
            <div class="container">
                <div class="hero">
                    <h1>Welcome to Chessistant</h1>
                    <p>Your complete chess training platform for the UMak Chess Team</p>
                    <div style="display: flex; gap: 15px; justify-content: center; flex-wrap: wrap;">
                        <button class="btn btn-primary" onclick="openModal('dailyPuzzle')">Daily Puzzle</button>
                        <button class="btn btn-outline" style="border-color: white; color: white;" onclick="openModal('gameAnalysis')">Analyze Game</button>
                    </div>
                </div>

                <div class="stats-grid">
                    <div class="stat-card" style="border-top-color: var(--strategic-blue);">
                        <div class="stat-value" style="color: var(--strategic-blue);">1850</div>
                        <div class="stat-label">Current Rating</div>
                    </div>
                    <div class="stat-card" style="border-top-color: var(--victory-green);">
                        <div class="stat-value" style="color: var(--victory-green);">67%</div>
                        <div class="stat-label">Win Rate</div>
                    </div>
                    <div class="stat-card" style="border-top-color: var(--stalemate-gold);">
                        <div class="stat-value" style="color: var(--stalemate-gold);">143</div>
                        <div class="stat-label">Total Games</div>
                    </div>
                    <div class="stat-card" style="border-top-color: #9b59b6;">
                        <div class="stat-value" style="color: #9b59b6;">12</div>
                        <div class="stat-label">Current Streak</div>
                    </div>
                </div>

                <div class="section">
                    <h2>Recent Matches</h2>
                    <div class="table-container">
                        <table>
                            <thead>
                                <tr>
                                    <th>Opponent</th>
                                    <th>Result</th>
                                    <th>Date</th>
                                    <th>Opening</th>
                                    <th>Rating Change</th>
                                    <th>Actions</th>
                                </tr>
                            </thead>
                            <tbody>
                                <tr>
                                    <td>John Dela Cruz</td>
                                    <td><span class="badge badge-success">Win</span></td>
                                    <td>Feb 10, 2026</td>
                                    <td>Sicilian Defense</td>
                                    <td style="color: var(--victory-green); font-weight: 600;">+12</td>
                                    <td><button class="btn btn-primary" style="padding: 6px 12px; font-size: 0.85rem;" onclick="openModal('gameAnalysis')">Analyze</button></td>
                                </tr>
                                <tr>
                                    <td>Maria Santos</td>
                                    <td><span class="badge badge-danger">Loss</span></td>
                                    <td>Feb 8, 2026</td>
                                    <td>Queen's Gambit</td>
                                    <td style="color: var(--checkmate-red); font-weight: 600;">-8</td>
                                    <td><button class="btn btn-primary" style="padding: 6px 12px; font-size: 0.85rem;" onclick="openModal('gameAnalysis')">Analyze</button></td>
                                </tr>
                                <tr>
                                    <td>Pedro Reyes</td>
                                    <td><span class="badge badge-warning">Draw</span></td>
                                    <td>Feb 5, 2026</td>
                                    <td>Italian Game</td>
                                    <td style="color: var(--neutral-gray); font-weight: 600;">0</td>
                                    <td><button class="btn btn-primary" style="padding: 6px 12px; font-size: 0.85rem;" onclick="openModal('gameAnalysis')">Analyze</button></td>
                                </tr>
                                <tr>
                                    <td>Anna Garcia</td>
                                    <td><span class="badge badge-success">Win</span></td>
                                    <td>Feb 3, 2026</td>
                                    <td>Ruy Lopez</td>
                                    <td style="color: var(--victory-green); font-weight: 600;">+10</td>
                                    <td><button class="btn btn-primary" style="padding: 6px 12px; font-size: 0.85rem;" onclick="openModal('gameAnalysis')">Analyze</button></td>
                                </tr>
                            </tbody>
                        </table>
                    </div>
                </div>

                <div class="card-grid">
                    <div class="card">
                        <div class="card-header">📢 Latest Announcements</div>
                        <div class="card-body">
                            <div class="alert alert-info">
                                <span>📅</span>
                                <span><strong>UMak Championship 2026</strong> - Registration opens March 1st</span>
                            </div>
                            <div class="alert alert-success">
                                <span>🎯</span>
                                <span>New training module on tactical patterns now available!</span>
                            </div>
                        </div>
                    </div>

                    <div class="card">
                        <div class="card-header">🏆 Upcoming Tournaments</div>
                        <div class="card-body">
                            <div style="margin-bottom: 15px;">
                                <strong>Weekly Blitz Tournament</strong>
                                <p style="color: var(--text-secondary); font-size: 0.9rem;">Every Saturday, 2:00 PM</p>
                                <span class="badge badge-warning">Upcoming</span>
                            </div>
                            <div>
                                <strong>Monthly Rapid Championship</strong>
                                <p style="color: var(--text-secondary); font-size: 0.9rem;">March 20, 2026</p>
                                <span class="badge badge-primary" style="background: var(--strategic-blue);">Registration Open</span>
                            </div>
                        </div>
                        <div class="card-footer" style="padding: 15px 20px; background: #f8f9fa; border-top: 1px solid #e9ecef;">
                            <button class="btn btn-primary" onclick="switchPage('tournaments')">View All Tournaments</button>
                        </div>
                    </div>
                </div>
            </div>
        </div>

        <div id="training-page" class="page" style="display: none;">
            <div class="container">
                <div class="hero">
                    <h1>Chess Training Center</h1>
                    <p>Improve your game with puzzles, lessons, and AI analysis</p>
                </div>

                <div class="tabs">
                    <div class="tab-buttons">
                        <button class="tab-btn active" data-tab="puzzles">Daily Puzzles</button>
                        <button class="tab-btn" data-tab="lessons">Lessons</button>
                        <button class="tab-btn" data-tab="bot">Practice with Bot</button>
                        <button class="tab-btn" data-tab="pgn">Famous Games (PGN)</button>
                    </div>

                    <div class="tab-content active" id="puzzles-tab">
                        <h3>Today's Puzzle</h3>
                        <div class="chess-board"></div>
                        <div style="text-align: center; margin-top: 20px;">
                            <p style="font-size: 1.1rem; margin-bottom: 15px;"><strong>White to move and win</strong></p>
                            <button class="btn btn-primary">Show Hint</button>
                            <button class="btn btn-success">Submit Solution</button>
                        </div>
                    </div>

                    <div class="tab-content" id="lessons-tab">
                        <h3>Available Lessons</h3>
                        <div class="card-grid">
                            <div class="card">
                                <div class="card-header">🎯 Tactical Patterns</div>
                                <div class="card-body">
                                    <p>Master common tactical motifs: pins, forks, skewers, and discovered attacks.</p>
                                    <div class="progress-bar">
                                        <div class="progress-fill" style="width: 60%;"></div>
                                    </div>
                                    <small style="color: var(--text-secondary);">60% Complete</small>
                                </div>
                                <div style="padding: 15px 20px; border-top: 1px solid #e9ecef;">
                                    <button class="btn btn-primary">Continue</button>
                                </div>
                            </div>
                            <div class="card">
                                <div class="card-header">♔ Endgame Mastery</div>
                                <div class="card-body">
                                    <p>Learn essential endgame positions and techniques for winning or drawing.</p>
                                    <div class="progress-bar">
                                        <div class="progress-fill" style="width: 80%;"></div>
                                    </div>
                                    <small style="color: var(--text-secondary);">80% Complete</small>
                                </div>
                                <div style="padding: 15px 20px; border-top: 1px solid #e9ecef;">
                                    <button class="btn btn-primary">Continue</button>
                                </div>
                            </div>
                        </div>
                    </div>

                    <div class="tab-content" id="bot-tab">
                        <h3>Practice Against Stockfish</h3>
                        <div class="alert alert-info">
                            <span>ℹ️</span>
                            <span>Select difficulty level and start a game against our Stockfish 17.1 engine</span>
                        </div>
                        <div style="margin: 20px 0;">
                            <label class="form-label">Engine Strength</label>
                            <select class="form-select">
                                <option>Beginner (800)</option>
                                <option>Intermediate (1200)</option>
                                <option selected>Expert (2000)</option>
                                <option>Master (2400)</option>
                            </select>
                        </div>
                        <div class="chess-board"></div>
                        <div style="text-align: center; margin-top: 20px;">
                            <button class="btn btn-success">Start New Game</button>
                        </div>
                    </div>

                    <div class="tab-content" id="pgn-tab">
                        <h3>Famous Games Database</h3>
                        <div class="table-container">
                            <table>
                                <thead>
                                    <tr>
                                        <th>White</th>
                                        <th>Black</th>
                                        <th>Year</th>
                                        <th>Result</th>
                                        <th>Actions</th>
                                    </tr>
                                </thead>
                                <tbody>
                                    <tr>
                                        <td>Bobby Fischer</td>
                                        <td>Boris Spassky</td>
                                        <td>1972</td>
                                        <td>1-0</td>
                                        <td><button class="btn btn-primary" style="padding: 6px 12px; font-size: 0.85rem;">View</button></td>
                                    </tr>
                                    <tr>
                                        <td>Garry Kasparov</td>
                                        <td>Anatoly Karpov</td>
                                        <td>1985</td>
                                        <td>1-0</td>
                                        <td><button class="btn btn-primary" style="padding: 6px 12px; font-size: 0.85rem;">View</button></td>
                                    </tr>
                                </tbody>
                            </table>
                        </div>
                    </div>
                </div>
            </div>
        </div>

        <div id="tournaments-page" class="page" style="display: none;">
            <div class="container">
                <div class="hero">
                    <h1>Tournaments</h1>
                    <p>Compete, improve, and climb the rankings</p>
                </div>

                <div class="section">
                    <h2>Active Tournaments</h2>
                    <div class="card-grid">
                        <div class="card">
                            <div class="card-header" style="background: linear-gradient(135deg, var(--victory-green) 0%, #229954 100%);">
                                UMak Championship 2026
                            </div>
                            <div class="card-body">
                                <p><strong>Date:</strong> March 15-17, 2026</p>
                                <p><strong>Format:</strong> Swiss System, 7 rounds</p>
                                <p><strong>Time Control:</strong> 90+30</p>
                                <p><strong>Participants:</strong> 24/32</p>
                                <div style="margin-top: 15px;">
                                    <span class="badge badge-success">Registration Open</span>
                                </div>
                            </div>
                            <div style="padding: 15px 20px; border-top: 1px solid #e9ecef;">
                                <button class="btn btn-success">Register Now</button>
                            </div>
                        </div>

                        <div class="card">
                            <div class="card-header" style="background: linear-gradient(135deg, var(--strategic-blue) 0%, #2980b9 100%);">
                                Weekly Blitz Tournament
                            </div>
                            <div class="card-body">
                                <p><strong>Date:</strong> Every Saturday, 2:00 PM</p>
                                <p><strong>Format:</strong> Round Robin</p>
                                <p><strong>Time Control:</strong> 3+2</p>
                                <p><strong>Participants:</strong> 12/16</p>
                                <div style="margin-top: 15px;">
                                    <span class="badge badge-warning">This Weekend</span>
                                </div>
                            </div>
                            <div style="padding: 15px 20px; border-top: 1px solid #e9ecef;">
                                <button class="btn btn-primary">Register</button>
                            </div>
                        </div>
                    </div>
                </div>

                <div class="section">
                    <h2>Sample Tournament Bracket</h2>
                    <div style="overflow-x: auto; padding: 20px; background: #f8f9fa; border-radius: 8px;">
                        <div class="bracket">
                            <div class="bracket-round">
                                <h4 style="text-align: center; margin-bottom: 20px;">Semifinals</h4>
                                <div class="bracket-match">
                                    <div class="bracket-player winner">John Dela Cruz (1.5)</div>
                                    <div class="bracket-player">Maria Santos (0.5)</div>
                                </div>
                                <div class="bracket-match">
                                    <div class="bracket-player winner">Pedro Reyes (1.5)</div>
                                    <div class="bracket-player">Anna Garcia (0.5)</div>
                                </div>
                            </div>
                            <div class="bracket-round">
                                <h4 style="text-align: center; margin-bottom: 20px;">Finals</h4>
                                <div class="bracket-match">
                                    <div class="bracket-player">John Dela Cruz</div>
                                    <div class="bracket-player">Pedro Reyes</div>
                                </div>
                            </div>
                        </div>
                    </div>
                </div>
            </div>
        </div>

        <div id="analytics-page" class="page" style="display: none;">
            <div class="container">
                <div class="hero">
                    <h1>Performance Analytics</h1>
                    <p>Track your progress and identify areas for improvement</p>
                </div>

                <div class="section">
                    <h2>Rating Progress</h2>
                    <div style="background: #f8f9fa; padding: 30px; border-radius: 8px; text-align: center;">
                        <p style="color: var(--text-secondary); margin-bottom: 20px;">Rating Chart Visualization</p>
                        <div style="height: 200px; display: flex; align-items: flex-end; justify-content: space-around; gap: 10px;">
                            <div style="width: 50px; height: 60%; background: linear-gradient(to top, var(--strategic-blue), var(--victory-green)); border-radius: 4px;"></div>
                            <div style="width: 50px; height: 75%; background: linear-gradient(to top, var(--strategic-blue), var(--victory-green)); border-radius: 4px;"></div>
                            <div style="width: 50px; height: 55%; background: linear-gradient(to top, var(--strategic-blue), var(--victory-green)); border-radius: 4px;"></div>
                            <div style="width: 50px; height: 80%; background: linear-gradient(to top, var(--strategic-blue), var(--victory-green)); border-radius: 4px;"></div>
                            <div style="width: 50px; height: 85%; background: linear-gradient(to top, var(--strategic-blue), var(--victory-green)); border-radius: 4px;"></div>
                            <div style="width: 50px; height: 90%; background: linear-gradient(to top, var(--strategic-blue), var(--victory-green)); border-radius: 4px;"></div>
                        </div>
                    </div>
                </div>

                <div class="card-grid">
                    <div class="card">
                        <div class="card-header">📊 Opening Statistics</div>
                        <div class="card-body">
                            <div style="margin-bottom: 15px;">
                                <div style="display: flex; justify-content: space-between; margin-bottom: 5px;">
                                    <span>Sicilian Defense</span>
                                    <span><strong>72% Win Rate</strong></span>
                                </div>
                                <div class="progress-bar">
                                    <div class="progress-fill" style="width: 72%;"></div>
                                </div>
                            </div>
                            <div style="margin-bottom: 15px;">
                                <div style="display: flex; justify-content: space-between; margin-bottom: 5px;">
                                    <span>Queen's Gambit</span>
                                    <span><strong>65% Win Rate</strong></span>
                                </div>
                                <div class="progress-bar">
                                    <div class="progress-fill" style="width: 65%;"></div>
                                </div>
                            </div>
                        </div>
                    </div>

                    <div class="card">
                        <div class="card-header">⚠️ Common Mistakes</div>
                        <div class="card-body">
                            <div style="padding: 10px; background: #fff3cd; border-left: 4px solid var(--stalemate-gold); border-radius: 4px; margin-bottom: 10px;">
                                <strong>Blunders:</strong> 12 in last 20 games
                            </div>
                            <div style="padding: 10px; background: #fff3cd; border-left: 4px solid var(--stalemate-gold); border-radius: 4px; margin-bottom: 10px;">
                                <strong>Time Trouble:</strong> 8 games
                            </div>
                            <div style="padding: 10px; background: #d4edda; border-left: 4px solid var(--victory-green); border-radius: 4px;">
                                <strong>Good Moves:</strong> 87% accuracy
                            </div>
                        </div>
                    </div>
                </div>
            </div>
        </div>

        <div id="admin-page" class="page" style="display: none;">
            <div class="container">
                <div class="hero">
                    <h1>Member Settings</h1>
                    <p>Manage your account settings and preferences</p>
                </div>

                <div class="layout-with-sidebar">
                    <div class="sidebar">
                        <h3 style="margin-bottom: 15px;">Settings Menu</h3>
                        <div class="sidebar-item active">
                            <span>👤</span>
                            <span>Profile</span>
                        </div>
                        <div class="sidebar-item">
                            <span>🔒</span>
                            <span>Security</span>
                        </div>
                        <div class="sidebar-item">
                            <span>🔔</span>
                            <span>Notifications</span>
                        </div>
                        <div class="sidebar-item">
                            <span>🎨</span>
                            <span>Board Theme</span>
                        </div>
                    </div>

                    <div>
                        <div class="section">
                            <h2>Profile Information</h2>
                            <form>
                                <div class="form-group">
                                    <label class="form-label">Full Name</label>
                                    <input type="text" class="form-input" value="Your Name">
                                </div>
                                <div class="form-group">
                                    <label class="form-label">Email</label>
                                    <input type="email" class="form-input" value="you@umak.edu.ph" disabled>
                                    <small style="color: var(--text-secondary);">Email cannot be changed.</small>
                                </div>
                                <button type="submit" class="btn btn-primary">Update Profile</button>
                            </form>
                        </div>
                    </div>
                </div>
            </div>
        </div>
    </div>

    <div id="dailyPuzzle" class="modal">
        <div class="modal-content">
            <div class="modal-header">
                <h3>Today's Daily Puzzle</h3>
                <button class="modal-close" onclick="closeModal('dailyPuzzle')">×</button>
            </div>
            <div class="modal-body">
                <div class="chess-board" style="max-width: 400px;"></div>
                <p style="text-align: center; margin: 20px 0; font-weight: 600; font-size: 1.1rem;">White to move and win</p>
                <div class="alert alert-info">
                    <span>💡</span>
                    <span>Look for a tactical opportunity that wins material</span>
                </div>
                <div style="text-align: center; margin-top: 20px;">
                    <button class="btn btn-primary">Show Hint</button>
                    <button class="btn btn-success">Submit Solution</button>
                </div>
            </div>
        </div>
    </div>

    <div id="gameAnalysis" class="modal">
        <div class="modal-content">
            <div class="modal-header">
                <h3>Game Analysis</h3>
                <button class="modal-close" onclick="closeModal('gameAnalysis')">×</button>
            </div>
            <div class="modal-body">
                <div class="chess-board" style="max-width: 400px;"></div>
                <div style="margin-top: 20px;">
                    <h4>Engine Evaluation</h4>
                    <div style="background: #f8f9fa; padding: 15px; border-radius: 8px; margin: 10px 0;">
                        <div style="font-size: 1.5rem; font-weight: 700; color: var(--victory-green);">+0.5</div>
                        <p style="color: var(--text-secondary); margin-top: 5px;">White has a slight advantage</p>
                    </div>
                    <div style="background: white; padding: 15px; border: 1px solid #e9ecef; border-radius: 8px;">
                        <p><strong>Best Move:</strong> Nf3</p>
                        <p><strong>Opening:</strong> Ruy Lopez</p>
                    </div>
                </div>
            </div>
        </div>
    </div>

    <footer>
        <p><strong>Chessistant</strong> - UMak Chess Team Platform</p>
        <p>Developed by Group DOWND | 2026</p>
        <p style="font-size: 0.9rem; margin-top: 10px;">
            Project Manager: Kenichi Lei Calica | Front-end: Hans Dominic Arcilla | Back-end: Andre Viktor Arcilla
        </p>
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
        function switchPage(pageName) {
            pages.forEach(page => page.style.display = 'none');
            
            const targetPage = document.getElementById(`${pageName}-page`);
            if (targetPage) targetPage.style.display = 'block';
            
            navLinks.forEach(link => {
                link.classList.remove('active');
                if (link.getAttribute('data-page') === pageName) {
                    link.classList.add('active');
                }
            });
            window.scrollTo(0, 0);

            localStorage.setItem('activeMemberTab', pageName);
        }

        document.addEventListener('DOMContentLoaded', () => {
            const savedTab = localStorage.getItem('activeMemberTab');
            if (savedTab) {
                switchPage(savedTab);
            }
        });

        const tabButtons = document.querySelectorAll('.tab-btn');
        tabButtons.forEach(button => {
            button.addEventListener('click', () => {
                const tabName = button.getAttribute('data-tab');
                const parentTabs = button.closest('.tabs');
                
                parentTabs.querySelectorAll('.tab-btn').forEach(btn => btn.classList.remove('active'));
                button.classList.add('active');
                
                parentTabs.querySelectorAll('.tab-content').forEach(content => content.classList.remove('active'));
                parentTabs.querySelector(`#${tabName}-tab`).classList.add('active');
            });
        });

        // --- Modal Functions ---
        function openModal(modalId) { document.getElementById(modalId).classList.add('active'); }
        function closeModal(modalId) { document.getElementById(modalId).classList.remove('active'); }
        document.querySelectorAll('.modal').forEach(modal => {
            modal.addEventListener('click', (e) => {
                if (e.target === modal) modal.classList.remove('active');
            });
        });

        // --- Sidebar nav ---
        const sidebarItems = document.querySelectorAll('.sidebar-item');
        sidebarItems.forEach(item => {
            item.addEventListener('click', () => {
                sidebarItems.forEach(i => i.classList.remove('active'));
                item.classList.add('active');
            });
        });

        // --- Form Submission Prevention (WIP) ---
        document.querySelectorAll('form').forEach(form => {
            form.addEventListener('submit', (e) => {
                e.preventDefault();
                alert('Form submitted! (WIP - No actual submission handling yet)');
                const modal = form.closest('.modal');
                if (modal) modal.classList.remove('active');
            });
        });
    </script>
</body>
</html>
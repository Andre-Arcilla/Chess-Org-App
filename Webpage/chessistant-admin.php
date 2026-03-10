<?php
require 'db.php';

// Fetch all Members
$stmt = $pdo->prepare("SELECT StudName, Email, Role, Rating, StudNum, Date FROM Profiles");
$stmt->execute();
$members = $stmt->fetchAll();

// Fetch Tournaments (Joining with Profiles to get the Coach's Name)
$stmt = $pdo->prepare("
    SELECT t.TourID, t.Title, t.Date, t.Text, p.StudName AS AuthorName 
    FROM Tournaments t 
    LEFT JOIN Profiles p ON t.Author = p.StudNum 
    ORDER BY t.TourID DESC
");
$stmt->execute();
$tournaments = $stmt->fetchAll();

// Fetch Announcements
$stmt = $pdo->prepare("
    SELECT a.AnnID, a.Title, a.Text, a.Date, p.StudName AS AuthorName 
    FROM Announcements a 
    LEFT JOIN Profiles p ON a.Author = p.StudNum 
    ORDER BY a.AnnID DESC
");
$stmt->execute();
$announcements = $stmt->fetchAll();
?>
<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>Chessistant - Admin Dashboard</title>
    <link rel="stylesheet" href="chessistant.css">
</head>

<body class="admin-theme"> 

    <nav class="navbar">
        <div class="nav-container">
            <div class="nav-brand">
                <span>♟️</span>
                <span>Chessistant</span>
                <span class="role-badge">ADMIN</span>
            </div>
            <ul class="nav-links">
                <li><a href="#" class="nav-link active" data-page="dashboard">Dashboard</a></li>
                <li><a href="#" class="nav-link" data-page="members">Members</a></li>
                <li><a href="#" class="nav-link" data-page="tournaments">Tournaments</a></li>
                <li><a href="#" class="nav-link" data-page="announcements">Announcements</a></li>
                <li><a href="#" class="nav-link" data-page="settings">System Settings</a></li>
            </ul>
        </div>
    </nav>

    <div id="app">
        <div id="dashboard-page" class="page active">
            <div class="container">
                <div class="hero admin-theme">
                    <h1>Admin Control Panel</h1>
                    <p>Complete system administration and management</p>
                </div>

                <div class="stats-grid">
                    <div class="stat-card">
                        <div class="stat-value"><?= count($members) ?></div>
                        <div class="stat-label">Total Members</div>
                    </div>
                    <div class="stat-card">
                        <div class="stat-value"><?= count($tournaments) ?></div>
                        <div class="stat-label">Active Tournaments</div>
                    </div>
                    <div class="stat-card">
                        <div class="stat-value"><?= count($announcements) ?></div>
                        <div class="stat-label">Announcements</div>
                    </div>
                </div>

                <div class="section">
                    <h2>Admin Actions</h2>
                    <div class="card-grid" style="margin-top: 20px;">
                        <div class="card">
                            <div class="card-header">👥 Member Management</div>
                            <div class="card-body">
                                <p>Add, edit, delete members and assign roles (Admin/Coach/Member)</p>
                                <button class="btn btn-admin" style="width: 100%; margin-bottom: 5px;" onclick="switchPage('members')">Manage Members</button>
                            </div>
                        </div>
                        <div class="card">
                            <div class="card-header">📢 Announcements</div>
                            <div class="card-body">
                                <p>Create and manage team-wide announcements</p>
                                <button class="btn btn-admin" style="width: 100%; margin-bottom: 5px;" onclick="openModal('createAnnouncement')">Create Announcement</button>
                            </div>
                        </div>
                        <div class="card">
                            <div class="card-header">⚙️ System Settings</div>
                            <div class="card-body">
                                <p>Configure application settings, features, and permissions</p>
                                <button class="btn btn-admin" style="width: 100%; margin-bottom: 5px;" onclick="switchPage('settings')">Manage Settings</button>
                            </div>
                        </div>
                    </div>
                </div>
            </div>
        </div>

        <div id="members-page" class="page" style="display: none;">
            <div class="container">
                <div class="hero admin-theme">
                    <h1>Member Management</h1>
                    <p>Full CRUD control over all system users</p>
                </div>

                <div class="section">
                    <div class="section-header">
                        <h2>All Members (<?= count($members) ?>)</h2>
                        <button class="btn btn-success" onclick="openModal('addMember')">+ Add Member</button>
                    </div>

                    <div style="margin-bottom: 20px; display: flex; gap: 10px;">
                        <input type="text" id="searchMember" placeholder="Search by name or email..." style="flex: 1; padding: 10px; border: 2px solid #e9ecef; border-radius: 6px;">
                        <select id="filterRole" style="padding: 10px; border: 2px solid #e9ecef; border-radius: 6px;">
                            <option value="All Roles">All Roles</option>
                            <option value="Admin">Admin</option>
                            <option value="Coach">Coach</option>
                            <option value="Member">Member</option>
                        </select>
                    </div>

                    <div class="table-container">
                        <table>
                            <thead>
                                <tr>
                                    <th>Name</th>
                                    <th>Email</th>
                                    <th>Role</th>
                                    <th>Rating</th>
                                    <th>Status</th>
                                    <th>Joined Date</th>
                                    <th>Actions</th>
                                </tr>
                            </thead>
                            <tbody id="membersTableBody">
                                <?php foreach ($members as $member): ?>
                                    <tr>
                                        <td><strong><?= htmlspecialchars($member['StudName']) ?></strong></td>
                                        <td><?= htmlspecialchars($member['Email']) ?></td>
                                        
                                        <td class="role-cell">
                                            <?php if ($member['Role'] === 'Admin'): ?>
                                                <span class="badge badge-admin">Admin</span>
                                            <?php elseif ($member['Role'] === 'Coach'): ?>
                                                <span class="badge badge-coach">Coach</span>
                                            <?php else: ?>
                                                <span class="badge badge-member">Member</span>
                                            <?php endif; ?>
                                        </td>
                                        
                                        <td><?= htmlspecialchars($member['Rating']) ?></td>
                                        
                                        <td>
                                            <?php if ($member['Role'] === 'Disabled'): ?>
                                                <span class="badge badge-danger">Suspended</span>
                                            <?php else: ?>
                                                <span class="badge badge-success">Active</span>
                                            <?php endif; ?>
                                        </td>
                                        
                                        <td><?= date("M j, Y", strtotime($member['Date'])) ?></td>
                                        
                                        <td class="action-buttons">
                                            <button type="button" class="icon-btn" title="Edit" 
                                                    data-studnum="<?= htmlspecialchars($member['StudNum']) ?>" 
                                                    data-name="<?= htmlspecialchars($member['StudName']) ?>" 
                                                    data-email="<?= htmlspecialchars($member['Email']) ?>" 
                                                    data-rating="<?= htmlspecialchars($member['Rating']) ?>"
                                                    onclick="openEditModal(this)">✏️</button>
                                            
                                            <button type="button" class="icon-btn" title="Change Role" 
                                                    data-studnum="<?= htmlspecialchars($member['StudNum']) ?>" 
                                                    data-name="<?= htmlspecialchars($member['StudName']) ?>" 
                                                    data-role="<?= htmlspecialchars($member['Role']) ?>"
                                                    onclick="openRoleModal(this)">👑</button>
                                            
                                            <form action="admin_actions/delete_member.php" method="POST" style="display:inline;" onsubmit="return confirm('Are you sure you want to completely remove this member?');">
                                                <input type="hidden" name="studNum" value="<?= htmlspecialchars($member['StudNum']) ?>">
                                                <button type="submit" class="icon-btn" title="Delete">🗑️</button>
                                            </form>
                                        </td>
                                    </tr>
                                <?php endforeach; ?>
                            </tbody>
                        </table>
                    </div>
                </div>
            </div>
        </div>

        <div id="tournaments-page" class="page" style="display: none;">
            <div class="container">
                <div class="hero admin-theme">
                    <h1>Tournament Management</h1>
                    <p>View and manage all tournaments</p>
                </div>

                <div class="alert alert-info">
                    <span>ℹ️</span>
                    <span>Note: Tournament creation and management is handled by coaches. Admins have view-only access.</span>
                </div>

                <div class="section">
                    <h2>All Tournaments</h2>
                    <div class="card-grid" style="margin-top: 20px;">
                        <?php if (empty($tournaments)): ?>
                            <p style="color: var(--text-secondary);">No tournaments have been created yet.</p>
                        <?php else: ?>
                            <?php foreach ($tournaments as $tourn): ?>
                                <div class="card">
                                    <div class="card-header" style="background: linear-gradient(135deg, var(--strategic-blue) 0%, #2980b9 100%);">
                                        <?= htmlspecialchars($tourn['Title']) ?>
                                    </div>
                                    <div class="card-body">
                                        <p><strong>Created:</strong> <?= date("M j, Y", strtotime($tourn['Date'])) ?></p>
                                        <p><strong>Managed by:</strong> Coach <?= htmlspecialchars($tourn['AuthorName'] ?? 'Unknown') ?></p>
                                        <div style="margin-top: 15px; padding-top: 15px; border-top: 1px solid #e9ecef;">
                                            <p><?= nl2br(htmlspecialchars($tourn['Text'])) ?></p>
                                        </div>
                                    </div>
                                </div>
                            <?php endforeach; ?>
                        <?php endif; ?>
                    </div>
                </div>
            </div>
        </div>

        <div id="announcements-page" class="page" style="display: none;">
            <div class="container">
                <div class="hero admin-theme">
                    <h1>Announcements</h1>
                    <p>Create and manage team-wide announcements</p>
                </div>

                <div class="section">
                    <div class="section-header">
                        <h2>All Announcements (<?= count($announcements) ?>)</h2>
                        <button class="btn btn-admin" onclick="openModal('createAnnouncement')">+ Create Announcement</button>
                    </div>

                    <div class="table-container">
                        <table>
                            <thead>
                                <tr>
                                    <th>Title</th>
                                    <th>Content Preview</th>
                                    <th>Author</th>
                                    <th>Date Posted</th>
                                    <th>Actions</th>
                                </tr>
                            </thead>
                            <tbody>
                                <?php if (empty($announcements)): ?>
                                    <tr><td colspan="5" style="text-align:center;">No announcements found.</td></tr>
                                <?php else: ?>
                                    <?php foreach ($announcements as $ann): ?>
                                        <tr>
                                            <td><strong><?= htmlspecialchars($ann['Title']) ?></strong></td>
                                            <td><?= htmlspecialchars(substr($ann['Text'], 0, 50)) ?>...</td>
                                            <td><?= htmlspecialchars($ann['AuthorName'] ?? 'Unknown') ?></td>
                                            <td><?= date("M j, Y", strtotime($ann['Date'])) ?></td>
                                            <td class="action-buttons">
                                                <button type="button" class="icon-btn" title="Edit" 
                                                        data-id="<?= htmlspecialchars($ann['AnnID']) ?>"
                                                        data-title="<?= htmlspecialchars($ann['Title']) ?>"
                                                        data-text="<?= htmlspecialchars($ann['Text']) ?>"
                                                        onclick="openEditAnnModal(this)">✏️</button>
                                                
                                                <form action="admin_actions/delete_announcement.php" method="POST" style="display:inline;" onsubmit="return confirm('Delete this announcement permanently?');">
                                                    <input type="hidden" name="annID" value="<?= htmlspecialchars($ann['AnnID']) ?>">
                                                    <button type="submit" class="icon-btn" title="Delete">🗑️</button>
                                                </form>
                                            </td>
                                        </tr>
                                    <?php endforeach; ?>
                                <?php endif; ?>
                            </tbody>
                        </table>
                    </div>
                </div>
            </div>
        </div>

        <div id="settings-page" class="page" style="display: none;">
            <div class="container">
                <div class="hero admin-theme">
                    <h1>System Settings</h1>
                    <p>Configure application settings and permissions</p>
                </div>
                <div class="alert alert-danger">
                    <span>⚠️</span>
                    <span><strong>Admin Only:</strong> Changes here affect the entire system. Use caution.</span>
                </div>
                <div class="section">
                    <div class="settings-group">
                        <div class="settings-title">General Settings</div>
                        <div class="setting-item">
                            <div>
                                <strong>Allow New Registrations</strong>
                                <p style="font-size: 0.85rem; color: var(--text-secondary);">Enable public registration for new members</p>
                            </div>
                            <div class="toggle active" onclick="this.classList.toggle('active')"><div class="toggle-slider"></div></div>
                        </div>
                    </div>
                    <button class="btn btn-success" style="margin-top: 20px;">Save All Changes</button>
                </div>
            </div>
        </div>
    </div>

    <div id="addMember" class="modal">
        <div class="modal-content">
            <div class="modal-header">
                <h3>Add New Member</h3>
                <button type="button" class="modal-close" onclick="closeModal('addMember')">×</button>
            </div>
            <div class="modal-body">
                <form action="admin_actions/add_member.php" method="POST">
                    <div class="form-group">
                        <label class="form-label">Full Name</label>
                        <input type="text" name="studName" class="form-input" placeholder="Enter full name" required>
                    </div>
                    <div class="form-group">
                        <label class="form-label">Student Number</label>
                        <input type="text" name="studNum" class="form-input" placeholder="e.g., A12345678" required>
                    </div>
                    <div class="form-group">
                        <label class="form-label">Email</label>
                        <input type="email" name="email" class="form-input" placeholder="name@umak.edu.ph" required>
                    </div>
                    <div class="form-group">
                        <label class="form-label">Role</label>
                        <select name="role" class="form-select" required>
                            <option value="">Select Role</option>
                            <option value="Member">Member</option>
                            <option value="Coach">Coach</option>
                            <option value="Admin">Admin</option>
                        </select>
                    </div>
                    <div class="form-group">
                        <label class="form-label">Initial Rating</label>
                        <input type="number" name="rating" class="form-input" placeholder="1500" value="1500">
                    </div>
                    <div class="form-group">
                        <label class="form-label">Temporary Password</label>
                        <input type="password" name="password" class="form-input" required>
                    </div>
                    <button type="submit" class="btn btn-admin" style="width: 100%;">Add Member</button>
                </form>
            </div>
        </div>
    </div>

    <div id="editMember" class="modal">
        <div class="modal-content">
            <div class="modal-header">
                <h3>Edit Member</h3>
                <button type="button" class="modal-close" onclick="closeModal('editMember')">×</button>
            </div>
            <div class="modal-body">
                <form action="admin_actions/edit_member.php" method="POST">
                    <input type="hidden" name="studNum" id="edit_studNum">
                    <div class="form-group">
                        <label class="form-label">Full Name</label>
                        <input type="text" name="studName" id="edit_studName" class="form-input" required>
                    </div>
                    <div class="form-group">
                        <label class="form-label">Email</label>
                        <input type="email" name="email" id="edit_email" class="form-input" required>
                    </div>
                    <div class="form-group">
                        <label class="form-label">Rating</label>
                        <input type="number" name="rating" id="edit_rating" class="form-input" required>
                    </div>
                    <button type="submit" class="btn btn-admin" style="width: 100%;">Save Changes</button>
                </form>
            </div>
        </div>
    </div>

    <div id="changeRole" class="modal">
        <div class="modal-content">
            <div class="modal-header">
                <h3>Change Member Role</h3>
                <button type="button" class="modal-close" onclick="closeModal('changeRole')">×</button>
            </div>
            <div class="modal-body">
                <form action="admin_actions/change_role.php" method="POST">
                    <input type="hidden" name="studNum" id="role_studNum">
                    <div class="form-group">
                        <label class="form-label">Member Name</label>
                        <input type="text" id="role_studName" class="form-input" disabled>
                    </div>
                    <div class="form-group">
                        <label class="form-label">Current Role</label>
                        <input type="text" id="role_current" class="form-input" disabled>
                    </div>
                    <div class="form-group">
                        <label class="form-label">New Role</label>
                        <select name="newRole" class="form-select" required>
                            <option value="">Select New Role</option>
                            <option value="Member">Member</option>
                            <option value="Coach">Coach</option>
                            <option value="Admin">Admin</option>
                            <option value="Disabled">Disabled (Suspended)</option>
                        </select>
                    </div>
                    <button type="submit" class="btn btn-admin" style="width: 100%;">Change Role</button>
                </form>
            </div>
        </div>
    </div>

    <div id="createAnnouncement" class="modal">
        <div class="modal-content">
            <div class="modal-header">
                <h3>Create Announcement</h3>
                <button type="button" class="modal-close" onclick="closeModal('createAnnouncement')">×</button>
            </div>
            <div class="modal-body">
                <form action="admin_actions/add_announcement.php" method="POST">
                    <input type="hidden" name="authorId" value="A12345932"> 

                    <div class="form-group">
                        <label class="form-label">Title</label>
                        <input type="text" name="title" class="form-input" placeholder="Announcement title" required>
                    </div>
                    <div class="form-group">
                        <label class="form-label">Content</label>
                        <textarea name="text" class="form-textarea" placeholder="Write your announcement..." required></textarea>
                    </div>
                    <button type="submit" class="btn btn-admin" style="width: 100%;">Publish Announcement</button>
                </form>
            </div>
        </div>
    </div>

    <div id="editAnnouncement" class="modal">
        <div class="modal-content">
            <div class="modal-header">
                <h3>Edit Announcement</h3>
                <button type="button" class="modal-close" onclick="closeModal('editAnnouncement')">×</button>
            </div>
            <div class="modal-body">
                <form action="admin_actions/edit_announcement.php" method="POST">
                    <input type="hidden" name="lastEditorId" value="A12345932">
                    <input type="hidden" name="annID" id="edit_annID">

                    <div class="form-group">
                        <label class="form-label">Title</label>
                        <input type="text" name="title" id="edit_annTitle" class="form-input" required>
                    </div>
                    <div class="form-group">
                        <label class="form-label">Content</label>
                        <textarea name="text" id="edit_annText" class="form-textarea" required></textarea>
                    </div>
                    <button type="submit" class="btn btn-admin" style="width: 100%;">Save Changes</button>
                </form>
            </div>
        </div>
    </div>

    <footer>
        <p><strong>Chessistant Admin Dashboard</strong></p>
        <p>UMak Chess Team | System Administrator</p>
    </footer>

<script>
    // --- Page Navigation with Local Storage Tab Fix ---
    const navLinks = document.querySelectorAll('.nav-link');
    const pages = document.querySelectorAll('.page');

    navLinks.forEach(link => {
        link.addEventListener('click', (e) => {
            e.preventDefault();
            const pageName = link.getAttribute('data-page');
            switchPage(pageName);
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

        // Save the active tab to the browser's local storage
        localStorage.setItem('activeAdminTab', pageName);
    }

    // When the page reloads, return to the saved tab
    document.addEventListener('DOMContentLoaded', () => {
        const savedTab = localStorage.getItem('activeAdminTab');
        if (savedTab) {
            switchPage(savedTab);
        }
    });

    // --- Modal Controls ---
    function openModal(modalId) { document.getElementById(modalId).classList.add('active'); }
    function closeModal(modalId) { document.getElementById(modalId).classList.remove('active'); }
    document.querySelectorAll('.modal').forEach(modal => {
        modal.addEventListener('click', (e) => {
            if (e.target === modal) modal.classList.remove('active');
        });
    });

    // --- Member Modal Population ---
    function openEditModal(btn) {
        document.getElementById('edit_studNum').value = btn.dataset.studnum;
        document.getElementById('edit_studName').value = btn.dataset.name;
        document.getElementById('edit_email').value = btn.dataset.email;
        document.getElementById('edit_rating').value = btn.dataset.rating;
        openModal('editMember');
    }

    function openRoleModal(btn) {
        document.getElementById('role_studNum').value = btn.dataset.studnum;
        document.getElementById('role_studName').value = btn.dataset.name;
        document.getElementById('role_current').value = btn.dataset.role;
        openModal('changeRole');
    }

    // --- Announcement Modal Population ---
    function openEditAnnModal(btn) {
        document.getElementById('edit_annID').value = btn.dataset.id;
        document.getElementById('edit_annTitle').value = btn.dataset.title;
        document.getElementById('edit_annText').value = btn.dataset.text;
        openModal('editAnnouncement');
    }

    // --- Live Search & Filter Logic for Members ---
    const searchInput = document.getElementById('searchMember');
    const roleFilter = document.getElementById('filterRole');
    const tableBody = document.getElementById('membersTableBody');

    function filterTable() {
        if (!tableBody) return;
        const searchTerm = searchInput.value.toLowerCase();
        const selectedRole = roleFilter.value;
        const rows = tableBody.getElementsByTagName('tr');

        for (let row of rows) {
            const name = row.cells[0].textContent.toLowerCase();
            const email = row.cells[1].textContent.toLowerCase();
            const role = row.querySelector('.role-cell').textContent.trim();
            
            const matchesSearch = name.includes(searchTerm) || email.includes(searchTerm);
            const matchesRole = selectedRole === 'All Roles' || role === selectedRole;

            row.style.display = (matchesSearch && matchesRole) ? '' : 'none';
        }
    }

    if(searchInput) searchInput.addEventListener('input', filterTable);
    if(roleFilter) roleFilter.addEventListener('change', filterTable);
</script>
</body>
</html>
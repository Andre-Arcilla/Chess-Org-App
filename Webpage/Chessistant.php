<?php
session_start();
date_default_timezone_set('Asia/Manila');

// Redirect to login if not authenticated
if (!isset($_SESSION['admin_logged_in']) || $_SESSION['admin_logged_in'] !== true) {
    header("Location: login.php");
    exit();
}

// 1. Database Connection
try {
    $db = new PDO('sqlite:Hoshiyomi_ChessApp.db');
    $db->setAttribute(PDO::ATTR_ERRMODE, PDO::ERRMODE_EXCEPTION);
} catch (PDOException $e) {
    die("Database connection failed: " . $e->getMessage());
}

// 2. Logic Handlers
if ($_SERVER['REQUEST_METHOD'] == 'POST') {
    // Handle Member Approval
    if (isset($_POST['approve_user'])) {
        $stmt = $db->prepare("INSERT INTO Profiles (StudName, StudNum, Email, Password, Role) 
                              SELECT StudName, StudNum, Email, Password, 'Member' FROM Registrations WHERE RegID = ?");
        $stmt->execute([$_POST['reg_id']]);
        $db->prepare("DELETE FROM Registrations WHERE RegID = ?")->execute([$_POST['reg_id']]);
    }

    // Save/Update Tournament
    if (isset($_POST['save_tournament'])) {
        if (!empty($_POST['tour_id'])) {
            $stmt = $db->prepare("UPDATE Tournaments SET LastEditor = ?, Title = ?, Text = ?, LastModified = strftime('%s', 'now') WHERE TourID = ?");
            $stmt->execute([$_POST['author'], $_POST['title'], $_POST['text'], $_POST['tour_id']]);
        } else {
            $stmt = $db->prepare("INSERT INTO Tournaments (Author, LastEditor, Title, Text) VALUES (?, ?, ?, ?)");
            $stmt->execute([$_POST['author'], $_POST['author'], $_POST['title'], $_POST['text']]);
        }
    }

    // Save/Update Announcement
    if (isset($_POST['save_announcement'])) {
        if (!empty($_POST['ann_id'])) {
            $stmt = $db->prepare("UPDATE Announcements SET LastEditor = ?, Title = ?, Text = ?, LastModified = strftime('%s', 'now') WHERE AnnID = ?");
            $stmt->execute([$_POST['author'], $_POST['title'], $_POST['text'], $_POST['ann_id']]);
        } else {
            $stmt = $db->prepare("INSERT INTO Announcements (Author, LastEditor, Title, Text) VALUES (?, ?, ?, ?)");
            $stmt->execute([$_POST['author'], $_POST['author'], $_POST['title'], $_POST['text']]);
        }
    }
    
    // Deletion
    if (isset($_POST['delete_post'])) {
        $table = $_POST['post_type'] === 'tournament' ? 'Tournaments' : 'Announcements';
        $idCol = $_POST['post_type'] === 'tournament' ? 'TourID' : 'AnnID';
        $stmt = $db->prepare("DELETE FROM $table WHERE $idCol = ?");
        $stmt->execute([$_POST['post_id']]);
    }

    if (isset($_POST['reset_password'])) {
    $stmt = $db->prepare("UPDATE Profiles SET Password = '12345' WHERE UserID = ?");
    $stmt->execute([$_POST['user_id']]);
    echo "<script>alert('Password reset to 12345 for User ID: " . $_POST['user_id'] . "');</script>";
    }
}

$page = isset($_GET['page']) ? $_GET['page'] : 'home';
$mode = isset($_GET['mode']) ? $_GET['mode'] : 'view'; 
$search = isset($_GET['search']) ? $_GET['search'] : '';
?>

<!DOCTYPE html>
<html>
<head>
    <title>Chessistant Admin Panel</title>
    <link rel="stylesheet" href="style.css">
    <link rel="stylesheet" href="https://cdnjs.cloudflare.com/ajax/libs/font-awesome/6.0.0/css/all.min.css">
</head>
<body>

<div id="sidebar">
    <div class="admin-profile">
        <small style="color: #bdc3c7; font-size: 10px;">LOGGED IN AS:</small>
        <div class="admin-name"><?php echo htmlspecialchars($_SESSION['admin_name']); ?></div>
        <div class="admin-id"><?php echo htmlspecialchars($_SESSION['admin_stud_num'] ?? 'ID Not Found'); ?></div>
    </div>

    <h2>Chessistant</h2>
    <nav>
        <a href="?page=home"><i class="fas fa-home"></i> Home</a>
        <a href="?page=roster"><i class="fas fa-users"></i> Club Roster</a>
        <a href="?page=approvals"><i class="fas fa-check-circle"></i> Approvals</a>
        <a href="?page=tournament"><i class="fas fa-trophy"></i> Tournaments</a>
        <a href="?page=announcement"><i class="fas fa-bullhorn"></i> Announcements</a>
    </nav>
    
    <a href="logout.php" style="color: #ef4444; margin-top: 50px; border-top: 1px solid #34495e; padding-top: 20px;">
        <i class="fas fa-sign-out-alt"></i> Logout
    </a>
</div>

    <div id="content">
        <?php
        switch ($page) {
            case 'roster':
                echo "<h2>Club Roster</h2>";
                //searchbar function for roster member
                echo "<div class='card'>
                        <form method='GET' class='search-container'>
                            <input type='hidden' name='page' value='roster'>
                            <input type='text' name='search' placeholder='Search by Name, ID, or Role...' value='" . htmlspecialchars($search) . "'>
                            <button type='submit' class='btn-edit'>Search</button>
                            <a href='?page=roster'><button type='button' class='btn-cancel'>Clear</button></a>
                        </form>
                      </div>";               
                $queryStr = "SELECT * FROM Profiles";
                $params = [];
                if (!empty($search)) {
                    $queryStr .= " WHERE StudName LIKE ? OR StudNum LIKE ? OR Role LIKE ?";
                    $params = ["%$search%", "%$search%", "%$search%"];
                }
                $queryStr .= " ORDER BY StudName ASC";
                
                echo "<table>
                <tr>
                    <th>Name</th>
                    <th>Student #</th>
                    <th>Rating</th>
                    <th>Role</th>
                    <th>Last Active</th>
                    <th>Actions</th>
                </tr>";

                $stmt = $db->prepare($queryStr);
                $stmt->execute($params);
                $users = $stmt->fetchAll();

                if (!$users) {
                    echo "<tr><td colspan='4' style='text-align:center;'>No members found.</td></tr>";
                } else {

                foreach ($users as $u) {
                    // Convert Unix timestamp to readable date 
                    $lastActive = ($u['LastModified'] > 0) ? date("M d, Y h:i A", $u['LastModified']) : "Never";
                    
                    echo "<tr>
                            <td>{$u['StudName']}</td>
                            <td><code>{$u['StudNum']}</code></td>
                            <td>{$u['Rating']}</td>
                            <td>{$u['Role']}</td>
                            <td><small>$lastActive</small></td>
                            <td>
                                <form method='POST' style='display:inline;' onsubmit='return confirm(\"Reset password?\");'>
                                    <input type='hidden' name='user_id' value='{$u['UserID']}'>
                                    <button type='submit' name='reset_password' class='btn-delete' style='background:#f39c12; padding: 4px 8px; font-size: 11px;'>Reset Pass</button>
                                </form>
                            </td>
                        </tr>";
                }
                echo "</table>";
                }
                break;
            //Approvals
            case 'approvals':
                echo "<h2>Membership Approvals</h2>";
                
                // Fetch pending registrations
                $regs = $db->query("SELECT * FROM Registrations ORDER BY Date DESC")->fetchAll();
                
                if (!$regs) {
                    echo "<div class='card'><p style='color: #64748b; text-align: center;'>No pending registrations at the moment.</p></div>";
                } else {
                    foreach ($regs as $r) {
                        echo "<div class='card' style='display: flex; justify-content: space-between; align-items: center;'>
                                <div>
                                    <div style='font-weight: 600; font-size: 16px; color: #0f172a;'>" . htmlspecialchars($r['StudName']) . "</div>
                                    <div style='font-size: 13px; color: #64748b;'>Student ID: <code style='color: #3b82f6;'>" . htmlspecialchars($r['StudNum']) . "</code></div>
                                    <div style='font-size: 12px; color: #94a3b8;'>Applied on: " . htmlspecialchars($r['Date']) . "</div>
                                </div>
                                <form method='POST' onsubmit='return confirm(\"Approve this member?\");'>
                                    <input type='hidden' name='reg_id' value='{$r['RegID']}'>
                                    <button type='submit' name='approve_user' class='btn-save'>Approve Member</button>
                                </form>
                            </div>";
                    }
                }
                break;
            //Tournaments
            case 'tournament':
                renderManagementUI($db, 'tournament', 'TourID', 'Tournaments', 'save_tournament', $mode);
                break;
            //Announcements
            case 'announcement':
                renderManagementUI($db, 'announcement', 'AnnID', 'Announcements', 'save_announcement', $mode);
                break;

            default:
                echo "<h1>Welcome, Admin</h1><p>Select a management tool from the sidebar to begin.</p>";
                break;

            // Inside case Roster
            foreach ($users as $u) {
            echo "<tr>
                    <td>{$u['StudName']}</td>
                    <td><code>{$u['StudNum']}</code></td>
                    <td>{$u['Rating']}</td>
                    <td>{$u['Role']}</td>
                    <td>
                        <form method='POST' style='display:inline;' onsubmit='return confirm(\"Reset this user\'s password to 12345?\");'>
                            <input type='hidden' name='user_id' value='{$u['UserID']}'>
                            <button type='submit' name='reset_password' class='btn-delete' style='background:#f39c12;'>Reset Pass</button>
                        </form>
                    </td>
                </tr>";
}
        }

        function renderManagementUI($db, $pageName, $idCol, $tableName, $btnName, $currentMode) {
            $data = [$idCol => '', 'Title' => '', 'Text' => '', 'Author' => ''];
            if (isset($_GET['id'])) {
                $stmt = $db->prepare("SELECT * FROM $tableName WHERE $idCol = ?");
                $stmt->execute([$_GET['id']]);
                $data = $stmt->fetch(PDO::FETCH_ASSOC);
            }

            $isEditing = ($currentMode === 'edit' && !empty($data[$idCol]));
            $isViewing = (!empty($data[$idCol]) && $currentMode === 'view');

            echo "<h2>Manage " . ucfirst($pageName) . "s</h2>";
            echo "<div class='card " . ($isViewing ? 'view-only' : '') . "'>
                    <h3>" . ($data[$idCol] ? ($isEditing ? "Editing #{$data[$idCol]}" : "Viewing #{$data[$idCol]}") : "Create New Post") . "</h3>
                    <form method='POST' action='?page=$pageName'>
                        <input type='hidden' name='" . strtolower($idCol) . "' value='{$data[$idCol]}'>
                        <input type='text' name='author' placeholder='Admin ID' value='{$data['Author']}' " . ($isViewing ? 'readonly' : 'required') . ">
                        <input type='text' name='title' placeholder='Title' value='{$data['Title']}' " . ($isViewing ? 'readonly' : 'required') . ">
                        <textarea name='text' placeholder='Content Body' rows='4' " . ($isViewing ? 'readonly' : '') . ">{$data['Text']}</textarea>
                        
                        " . ($isViewing ? "
                            <a href='?page=$pageName&id={$data[$idCol]}&mode=edit'><button type='button' class='btn-edit'>Edit</button></a>
                            <a href='?page=$pageName'><button type='button' class='btn-cancel'>Back</button></a>
                        " : "
                            <button type='submit' name='$btnName' class='btn-save'>" . ($data[$idCol] ? "Update" : "Publish") . "</button>
                            <a href='?page=$pageName'><button type='button' class='btn-cancel'>Cancel</button></a>
                        ") . "
                    </form>
                  </div>";
            
            $posts = $db->query("SELECT * FROM $tableName ORDER BY Date DESC")->fetchAll();
            echo "<table><tr><th>Title</th><th>Date</th><th>Actions</th></tr>";
            foreach ($posts as $p) {
                echo "<tr>
                        <td><strong>{$p['Title']}</strong></td>
                        <td>{$p['Date']}</td>
                        <td>
                            <a href='?page=$pageName&id={$p[$idCol]}&mode=view'><button class='btn-action'>View</button></a>
                            <form method='POST' style='display:inline;'>
                                <input type='hidden' name='post_id' value='{$p[$idCol]}'>
                                <input type='hidden' name='post_type' value='$pageName'>
                                <button type='submit' name='delete_post' class='btn-delete' onclick='return confirm(\"Permanently delete this?\")'>Delete</button>
                            </form>
                        </td>
                      </tr>";
            }
            echo "</table>";
        }
        ?>
    </div>
</body>
</html>
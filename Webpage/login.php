<?php
session_start();

// Use the existing database connection we made!
require 'db.php'; 

// --- Remember Me Check ---
if (!isset($_SESSION['logged_in']) && isset($_COOKIE['chess_user'])) {
    $stmt = $pdo->prepare("SELECT * FROM Profiles WHERE StudNum = ? LIMIT 1");
    $stmt->execute([$_COOKIE['chess_user']]);
    $user = $stmt->fetch();
    
    if ($user && $user['Role'] !== 'Disabled') {
        $_SESSION['logged_in'] = true;
        $_SESSION['name'] = $user['StudName'];
        $_SESSION['stud_num'] = $user['StudNum'];
        $_SESSION['role'] = $user['Role'];
        
        // Auto-route based on role
        if ($user['Role'] === 'Admin') { header("Location: chessistant-admin.php"); exit(); }
        elseif ($user['Role'] === 'Coach') { header("Location: chessistant-coach.php"); exit(); }
        else { header("Location: chessistant-member.php"); exit(); }
    }
}

$error = "";

// --- Login Process ---
if ($_SERVER['REQUEST_METHOD'] == 'POST') {
    $studNum = trim($_POST['stud_num']);
    $password = $_POST['password'];

    $stmt = $pdo->prepare("SELECT * FROM Profiles WHERE StudNum = ? LIMIT 1");
    $stmt->execute([$studNum]);
    $user = $stmt->fetch();

    if ($user) {
        // 1. Check if account is suspended
        if ($user['Role'] === 'Disabled') {
            $error = "Your account has been suspended. Please contact an admin.";
        } 
        // 2. Verify password (checks both plain text and hashed passwords for flexibility)
        elseif ($password === $user['Password'] || password_verify($password, $user['Password'])) {
            
            // Save last login time
            $pdo->prepare("UPDATE Profiles SET LastModified = strftime('%s', 'now') WHERE StudNum = ?")->execute([$studNum]);

            // Set Session Variables
            $_SESSION['logged_in'] = true;
            $_SESSION['name'] = $user['StudName'];
            $_SESSION['stud_num'] = $user['StudNum'];
            $_SESSION['role'] = $user['Role'];

            // Set Cookie if "Remember Me" is checked
            if (isset($_POST['remember'])) {
                setcookie('chess_user', $user['StudNum'], time() + (86400 * 30), "/"); 
            }

            // 3. The Auto-Router
            if ($user['Role'] === 'Admin') { header("Location: chessistant-admin.php"); }
            elseif ($user['Role'] === 'Coach') { header("Location: chessistant-coach.php"); }
            else { header("Location: chessistant-member.php"); }
            exit();
            
        } else {
            $error = "Invalid Student Number or Password.";
        }
    } else {
        $error = "Invalid Student Number or Password.";
    }
}
?>

<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>Chessistant | Login</title>
    
    <link rel="stylesheet" href="chessistant.css">
    <link rel="icon" href="data:image/svg+xml,<svg xmlns=%22http://www.w3.org/2000/svg%22 viewBox=%220 0 100 100%22><text y=%22.9em%22 font-size=%2290%22>♟️</text></svg>">
    <link rel="stylesheet" href="chessistant.css">
    
    <style>
        /* Specific layout rules to center the login box nicely */
        body { display: flex; flex-direction: column; min-height: 100vh; }
        .login-container { flex: 1; display: flex; align-items: center; justify-content: center; padding: 20px; }
        .login-card { width: 100%; max-width: 400px; box-shadow: 0 10px 30px rgba(0,0,0,0.1); }
        .password-wrapper { position: relative; }
        .toggle-password { position: absolute; right: 15px; top: 50%; transform: translateY(-50%); cursor: pointer; color: var(--text-secondary); }
    </style>
</head>
<body>
    <nav class="navbar">
        <div class="nav-container" style="justify-content: center;">
            <div class="nav-brand">
                <span>♟️</span>
                <span>Chessistant</span>
            </div>
        </div>
    </nav>

    <div class="login-container">
        <div class="card login-card">
            <div class="card-header" style="text-align: center; background: var(--strategic-blue);">
                Sign In to Chessistant
            </div>
            <div class="card-body">
                
                <?php if($error): ?>
                    <div class="alert alert-danger" style="padding: 10px; font-size: 0.9rem;">
                        <span>⚠️</span> <?php echo htmlspecialchars($error); ?>
                    </div>
                <?php endif; ?>
                
                <form method="POST">
                    <div class="form-group">
                        <label class="form-label">Student Number</label>
                        <input type="text" name="stud_num" class="form-input" placeholder="e.g., A12345678" required>
                    </div>
                    
                    <div class="form-group">
                        <label class="form-label">Password</label>
                        <div class="password-wrapper">
                            <input type="password" name="password" id="passwordField" class="form-input" placeholder="Enter password" required>
                            <i class="fa-solid fa-eye toggle-password" id="toggleIcon"></i>
                        </div>
                    </div>

                    <div style="display: flex; justify-content: space-between; align-items: center; font-size: 0.9rem; margin-bottom: 20px;">
                        <label style="cursor: pointer;"><input type="checkbox" name="remember"> Remember Me</label>
                        <a href="#" style="color: var(--strategic-blue); text-decoration: none;" onclick="alert('Contact your Coach or Admin to reset your password.')">Forgot Password?</a>
                    </div>
                    
                    <button type="submit" class="btn btn-primary" style="width: 100%;">Login</button>
                </form>
            </div>
        </div>
    </div>

    <script>
        const passwordField = document.querySelector('#passwordField');
        const toggleIcon = document.querySelector('#toggleIcon');

        toggleIcon.addEventListener('click', function () {
            const type = passwordField.getAttribute('type') === 'password' ? 'text' : 'password';
            passwordField.setAttribute('type', type);
            this.classList.toggle('fa-eye');
            this.classList.toggle('fa-eye-slash');
        });
    </script>
</body>
</html>
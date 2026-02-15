<?php
session_start();

// 1. Database Connection
try {
    $db = new PDO('sqlite:Hoshiyomi_ChessApp.db');
    $db->setAttribute(PDO::ATTR_ERRMODE, PDO::ERRMODE_EXCEPTION);
} catch (PDOException $e) {
    die("Database connection failed: " . $e->getMessage());
}

// Remember me check
if (!isset($_SESSION['admin_logged_in']) && isset($_COOKIE['admin_user'])) {
    $stmt = $db->prepare("SELECT * FROM Profiles WHERE StudNum = ? LIMIT 1");
    $stmt->execute([$_COOKIE['admin_user']]);
    $user = $stmt->fetch(PDO::FETCH_ASSOC);
    
    if ($user && $user['Role'] === 'Admin') {
        $_SESSION['admin_logged_in'] = true;
        $_SESSION['admin_name'] = $user['StudName'];
        $_SESSION['admin_stud_num'] = $user['StudNum'];
        header("Location: Chessistant.php");
        exit();
    }
}

$error = "";

if ($_SERVER['REQUEST_METHOD'] == 'POST') {
    $studNum = $_POST['stud_num'];
    $password = $_POST['password'];

    $stmt = $db->prepare("SELECT * FROM Profiles WHERE StudNum = ? LIMIT 1");
    $stmt->execute([$studNum]);
    $user = $stmt->fetch(PDO::FETCH_ASSOC);

    if ($user) {
        if ($user['Role'] !== 'Admin') {
            $error = "Access Denied: Admin role required.";
        } elseif ($password === $user['Password']) {
            //saves last login time on the page
            $db->prepare("UPDATE Profiles SET LastModified = strftime('%s', 'now', 'localtime') WHERE StudNum = ?")->execute([$studNum]);

            $_SESSION['admin_logged_in'] = true;
            $_SESSION['admin_name'] = $user['StudName'];
            $_SESSION['admin_stud_num'] = $user['StudNum'];

            if (isset($_POST['remember'])) {
                setcookie('admin_user', $user['StudNum'], time() + (86400 * 30), "/"); 
            }

            header("Location: Chessistant.php");
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
<html>
<head>
    <title>Chessistant | Admin Login</title>
    <link rel="stylesheet" href="style.css">
    <link rel="stylesheet" href="https://cdnjs.cloudflare.com/ajax/libs/font-awesome/6.0.0/css/all.min.css">
</head>
<body class="login-body"> <div class="login-card">
        <h2>Admin Login</h2>
        
        <?php if($error) echo "<div class='error-text'>$error</div>"; ?>
        
        <form method="POST">
            <div class="input-group">
                <input type="text" name="stud_num" placeholder="Student Number" required>
            </div>
            
            <div class="input-group">
                <input type="password" name="password" id="passwordField" placeholder="Password" required>
                <i class="fa-solid fa-eye toggle-password" id="toggleIcon"></i>
            </div>

            <div class="options">
                <label><input type="checkbox" name="remember"> Remember Me</label>
                <a href="#" style="color:#3498db; text-decoration:none;" onclick="alert('Contact Head Admin to reset.')">Forgot Password?</a>
            </div>
            
            <button type="submit" class="login-btn">Login</button>
        </form>
    </div>

    <script>
        const passwordField = document.querySelector('#passwordField');
        const toggleIcon = document.querySelector('#toggleIcon');

        toggleIcon.addEventListener('click', function () {
            // Toggle the type attribute
            const type = passwordField.getAttribute('type') === 'password' ? 'text' : 'password';
            passwordField.setAttribute('type', type);
            
            // Toggle the eye icon
            this.classList.toggle('fa-eye');
            this.classList.toggle('fa-eye-slash');
        });
    </script>
</body>
</html>
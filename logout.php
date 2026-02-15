<?php
session_start();

// 1. Clear all session variables
$_SESSION = array();

// 2. Destroy the session
session_destroy();

// 3. COOKIE MONSTER (kill the cookie)
// One hour expiration on the COOOKIE
if (isset($_COOKIE['admin_user'])) {
    setcookie('admin_user', '', time() - 3600, '/'); 
}

// 4. Redirect to login
header("Location: login.php");
exit();
?>
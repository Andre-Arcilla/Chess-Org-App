<?php
session_start();

// 1. Clear all session variables
$_SESSION = array();

// 2. Destroy the session
session_destroy();

// 3. Clear the new global cookie
if (isset($_COOKIE['chess_user'])) {
    setcookie('chess_user', '', time() - 3600, '/'); 
}

// 4. Clear the old admin cookie (just in case it's lingering from testing)
if (isset($_COOKIE['admin_user'])) {
    setcookie('admin_user', '', time() - 3600, '/'); 
}

// 5. Redirect to the fresh login page
header("Location: login.php");
exit();
?>
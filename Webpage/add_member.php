<?php
// Require the database connection we made earlier
require 'db.php';

// Check if the form was actually submitted
if ($_SERVER['REQUEST_METHOD'] === 'POST') {
    
    // 1. Grab and clean the data from the form
    $studName = trim($_POST['studName']);
    $studNum = trim($_POST['studNum']);
    $email = trim($_POST['email']);
    $role = trim($_POST['role']);
    // Default to 1500 if the rating field is left blank
    $rating = !empty($_POST['rating']) ? (int)$_POST['rating'] : 1500; 
    $password = $_POST['password'];

    // 2. Hash the password for security
    // Never store plain text passwords in your database!
    $hashedPassword = password_hash($password, PASSWORD_DEFAULT);

    // 3. Prepare the SQL INSERT statement
    // Using named placeholders (:studName) prevents SQL injection attacks
    $sql = "INSERT INTO Profiles (StudName, StudNum, Email, Role, Rating, Password) 
            VALUES (:studName, :studNum, :email, :role, :rating, :password)";
    
    $stmt = $pdo->prepare($sql);

    // 4. Execute the query
    try {
        $stmt->execute([
            ':studName' => $studName,
            ':studNum'  => $studNum,
            ':email'    => $email,
            ':role'     => $role,
            ':rating'   => $rating,
            ':password' => $hashedPassword
        ]);

        // Success! Redirect them back to the admin page
        header("Location: chessistant-admin.php?success=MemberAdded");
        exit;

    } catch (PDOException $e) {
        // Error Code 23000 means a UNIQUE constraint failed (like a duplicate StudNum)
        if ($e->getCode() == 23000) {
            die("Error: A member with that Student Number already exists.");
        } else {
            die("Database Error: " . $e->getMessage());
        }
    }
} else {
    // If someone tries to visit add_member.php directly without submitting the form
    header("Location: chessistant-admin.php");
    exit;
}
?>
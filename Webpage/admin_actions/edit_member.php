<?php
require '../db.php';
if ($_SERVER['REQUEST_METHOD'] === 'POST') {
    $stmt = $pdo->prepare("UPDATE Profiles SET StudName = :name, Email = :email, Rating = :rating WHERE StudNum = :studNum");
    $stmt->execute([
        ':name' => trim($_POST['studName']),
        ':email' => trim($_POST['email']),
        ':rating' => (int)$_POST['rating'],
        ':studNum' => $_POST['studNum']
    ]);
    header("Location: ../chessistant-admin.php?success=MemberEdited");
    exit;
}
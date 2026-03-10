<?php
require 'db.php';
if ($_SERVER['REQUEST_METHOD'] === 'POST') {
    $stmt = $pdo->prepare("UPDATE Profiles SET Role = :role WHERE StudNum = :studNum");
    $stmt->execute([
        ':role' => $_POST['newRole'],
        ':studNum' => $_POST['studNum']
    ]);
    header("Location: chessistant-admin.php?success=RoleChanged");
    exit;
}
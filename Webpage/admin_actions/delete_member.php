<?php
require '../db.php';
if ($_SERVER['REQUEST_METHOD'] === 'POST') {
    $stmt = $pdo->prepare("DELETE FROM Profiles WHERE StudNum = :studNum");
    $stmt->execute([':studNum' => $_POST['studNum']]);
    header("Location: ../chessistant-admin.php?success=MemberDeleted");
    exit;
}
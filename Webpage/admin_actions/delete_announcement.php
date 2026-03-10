<?php
require '../db.php';
if ($_SERVER['REQUEST_METHOD'] === 'POST') {
    $stmt = $pdo->prepare("DELETE FROM Announcements WHERE AnnID = :annID");
    $stmt->execute([':annID' => $_POST['annID']]);
    header("Location: ../chessistant-admin.php?success=AnnouncementDeleted");
    exit;
}
<?php
require 'db.php';
if ($_SERVER['REQUEST_METHOD'] === 'POST') {
    $stmt = $pdo->prepare("UPDATE Announcements SET Title = :title, Text = :text, LastEditor = :lastEditor, LastModified = strftime('%s', 'now') WHERE AnnID = :annID");
    $stmt->execute([
        ':title' => trim($_POST['title']),
        ':text' => trim($_POST['text']),
        ':lastEditor' => $_POST['lastEditorId'],
        ':annID' => $_POST['annID']
    ]);
    header("Location: chessistant-admin.php?success=AnnouncementUpdated");
    exit;
}
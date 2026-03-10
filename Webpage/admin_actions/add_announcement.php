<?php
require '../db.php';
if ($_SERVER['REQUEST_METHOD'] === 'POST') {
    $stmt = $pdo->prepare("INSERT INTO Announcements (Author, LastEditor, Title, Text) VALUES (:author, :lastEditor, :title, :text)");
    $stmt->execute([
        ':author' => $_POST['authorId'],
        ':lastEditor' => $_POST['authorId'], // Initial creator is also the last editor
        ':title' => trim($_POST['title']),
        ':text' => trim($_POST['text'])
    ]);
    header("Location: ../chessistant-admin.php?success=AnnouncementCreated");
    exit;
}
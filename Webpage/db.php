<?php
// db.php

// Define the path to your SQLite file
$dbPath = __DIR__ . '/Hoshiyomi_ChessApp.db';

try {
    // Create a new PDO instance to connect to SQLite
    $pdo = new PDO("sqlite:" . $dbPath);
    
    // Turn on exceptions for errors so we can catch them easily
    $pdo->setAttribute(PDO::ATTR_ERRMODE, PDO::ERRMODE_EXCEPTION);
    
    // Set the default fetch mode to associative arrays (easier to work with)
    $pdo->setAttribute(PDO::ATTR_DEFAULT_FETCH_MODE, PDO::FETCH_ASSOC);

} catch (PDOException $e) {
    // If the connection fails, stop the script and show the error
    die("Database connection failed: " . $e->getMessage());
}
?>
using SQLite4Unity3d;
using System;
using System.IO;
using UnityEngine;

public class GenerateDatabase : MonoBehaviour
{
    public static GenerateDatabase Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            transform.SetParent(null);
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }

        SetupDatabase();
    }

    [Header("data")]
    [SerializeField] private string saveFileName = "Hoshiyomi_ChessOrg.db";
    [SerializeField] public SQLiteConnection database;
    [SerializeField] public ProfileModel currentUser;

    private void SetupDatabase()
    {
        ConnectDB();

        // ---------------------------------------------------------
        // 1. CREATE TABLES FIRST (Safe because of "IF NOT EXISTS")
        // ---------------------------------------------------------

        // Creates Profiles table
        database.Execute(@"
            CREATE TABLE IF NOT EXISTS Profiles (
                UserID  INTEGER NOT NULL DEFAULT 1 CHECK(UserID >= 0),
                StudName    TEXT NOT NULL DEFAULT 'John Smith' COLLATE NOCASE,
                StudNum TEXT NOT NULL UNIQUE COLLATE NOCASE,
                Email   TEXT NOT NULL DEFAULT 'no.mail@umak.edu.ph' COLLATE NOCASE,
                Password    TEXT NOT NULL DEFAULT '12345' COLLATE BINARY,
                Rating  INTEGER NOT NULL DEFAULT 100 CHECK(Rating >= 1),
                Puzzles TEXT NOT NULL DEFAULT '0/0',
                Role    TEXT NOT NULL DEFAULT 'Member' CHECK(Role IN ('Member', 'Coach', 'Admin', 'Disabled')) COLLATE NOCASE,
                Date    TEXT NOT NULL DEFAULT (strftime('%m/%d/%Y %H:%M:%S', 'now')) COLLATE NOCASE,
                LastModified    INTEGER NOT NULL DEFAULT (strftime('%s', 'now')) COLLATE NOCASE,
                PRIMARY KEY(UserID AUTOINCREMENT)
            );
        ");

        // Creates ChessGames table
        database.Execute(@"
            CREATE TABLE IF NOT EXISTS ChessGames (
                GameID  INTEGER NOT NULL DEFAULT 1 CHECK(GameID >= 0),
                GameNum INTEGER NOT NULL DEFAULT 1,
                StudNum TEXT NOT NULL,
                PlayerColor TEXT NOT NULL DEFAULT 'White' CHECK(PlayerColor IN ('White', 'Black')) COLLATE NOCASE,
                Date    TEXT NOT NULL DEFAULT (strftime('%m/%d/%Y %H:%M:%S', 'now')) COLLATE NOCASE,
                Result  TEXT NOT NULL DEFAULT 'Draw' CHECK(Result IN ('Win', 'Lose', 'Draw')) COLLATE NOCASE,
                PGN TEXT NOT NULL DEFAULT 'N/A' COLLATE NOCASE,
                Feedback    TEXT,
                LastModified    INTEGER NOT NULL DEFAULT (strftime('%s', 'now')) COLLATE NOCASE,
                FeedbackRead    INTEGER NOT NULL DEFAULT 0,
                PRIMARY KEY(GameID AUTOINCREMENT),
                FOREIGN KEY(StudNum) REFERENCES Profiles(StudNum) ON DELETE CASCADE ON UPDATE CASCADE
            );
        ");

        // Creates Announcements table
        database.Execute(@"
            CREATE TABLE IF NOT EXISTS Announcements (
                AnnID   INTEGER NOT NULL DEFAULT 1 CHECK(AnnID >= 0),
                Author  TEXT NOT NULL,
                LastEditor  TEXT NOT NULL,
                Title   TEXT NOT NULL DEFAULT 'Title' COLLATE NOCASE,
                Date    TEXT NOT NULL DEFAULT (strftime('%m/%d/%Y %H:%M:%S', 'now')) COLLATE NOCASE,
                Text    TEXT NOT NULL DEFAULT 'Text' COLLATE NOCASE,
                LastModified    INTEGER NOT NULL DEFAULT (strftime('%s', 'now')) COLLATE NOCASE,
                PRIMARY KEY(AnnID AUTOINCREMENT),
                FOREIGN KEY(Author) REFERENCES Profiles(StudNum) ON DELETE CASCADE ON UPDATE CASCADE,
                FOREIGN KEY(LastEditor) REFERENCES Profiles(StudNum) ON DELETE CASCADE ON UPDATE CASCADE
            );
        ");

        // Creates Tournaments table
        database.Execute(@"
            CREATE TABLE IF NOT EXISTS Tournaments (
                TourID  INTEGER NOT NULL DEFAULT 1 CHECK(TourID >= 0),
                Author  TEXT NOT NULL,
                LastEditor  TEXT NOT NULL,
                Title   TEXT NOT NULL DEFAULT 'Title' COLLATE NOCASE,
                Date    TEXT NOT NULL DEFAULT (strftime('%m/%d/%Y %H:%M:%S', 'now')) COLLATE NOCASE,
                Text    TEXT NOT NULL DEFAULT 'Text' COLLATE NOCASE,
                LastModified    INTEGER NOT NULL DEFAULT (strftime('%s', 'now')) COLLATE NOCASE,
                PRIMARY KEY(TourID AUTOINCREMENT),
                FOREIGN KEY(Author) REFERENCES Profiles(StudNum) ON DELETE CASCADE ON UPDATE CASCADE,
                FOREIGN KEY(LastEditor) REFERENCES Profiles(StudNum) ON DELETE CASCADE ON UPDATE CASCADE
            );
        ");

        // Creates Registrations table
        database.Execute(@"
            CREATE TABLE IF NOT EXISTS Registrations (
                RegID   INTEGER NOT NULL DEFAULT 1 CHECK(RegID >= 0),
                StudName    TEXT NOT NULL DEFAULT 'John Smith' COLLATE NOCASE,
                StudNum TEXT NOT NULL UNIQUE COLLATE NOCASE,
                Email   TEXT NOT NULL DEFAULT 'no.mail@umak.edu.ph' COLLATE NOCASE,
                Password    TEXT NOT NULL DEFAULT '12345' COLLATE BINARY,
                Date    TEXT NOT NULL DEFAULT (strftime('%m/%d/%Y %H:%M:%S', 'now')) COLLATE NOCASE,
                LastModified    INTEGER NOT NULL DEFAULT (strftime('%s', 'now')) COLLATE NOCASE,
                PRIMARY KEY(RegID AUTOINCREMENT)
            );
        ");

        // Creates OrgRoster table
        database.Execute(@"
            CREATE TABLE IF NOT EXISTS OrgRoster (
                MmbrID  INTEGER NOT NULL DEFAULT 1 CHECK(MmbrID >= 0),
                StudName    TEXT NOT NULL DEFAULT 'John Smith' COLLATE NOCASE,
                StudNum TEXT NOT NULL UNIQUE COLLATE NOCASE,
                LastModified    INTEGER NOT NULL DEFAULT (strftime('%s', 'now')) COLLATE NOCASE,
                PRIMARY KEY(MmbrID AUTOINCREMENT)
            );
        ");

        // ---------------------------------------------------------
        // 2. NOW CHECK COUNTS (Safe because tables exist)
        // ---------------------------------------------------------

        int profileCount = database.ExecuteScalar<int>("SELECT COUNT(*) FROM Profiles");
        int gameCount = database.ExecuteScalar<int>("SELECT COUNT(*) FROM ChessGames");

        // 3. Logic: If empty, reset and populate
        if (profileCount < 5 && gameCount < 10)
        {
            // NOTE: If DeleteTablesDebug() DROPS the tables (removes them entirely),
            // you must run the CREATE commands again right after this call!
            // If it only performs "DELETE FROM...", then you are fine.
            DeleteTablesDebug();

            // If DeleteTablesDebug() drops tables, uncomment the lines below to recreate them:
            // SetupDatabase(); // Recursive call to recreate, or just copy paste Create logic
            // return; 

            ProfileDebug();
            AnnouncementDebug();
            TournamentDebug();
            GameDebug();
            OrgDebug();
            RegDebug();
        }
    }

    public SQLiteConnection ConnectDB()
    {
        if (database == null)
        {
            string dbPath = Path.Combine(Application.persistentDataPath, saveFileName);
            database = new SQLiteConnection(dbPath);
            database.Execute("PRAGMA foreign_keys = ON;");
        }

        return database;
    }

    private void ProfileDebug()
    {
        if (database.Table<ProfileModel>().FirstOrDefault() == null)
        {
            string hash = HashScript.Hash("123");

            database.Execute(@$"
                INSERT INTO Profiles (StudName, StudNum, Email, Password, Rating, Puzzles, Role)
                VALUES ('John Softeng', 'A12346169', 'asd@umak.edu.ph', '{hash}', 100, 1, 'Admin');
            ");

            database.Execute(@$"
                INSERT INTO Profiles (StudName, StudNum, Email, Password, Rating, Puzzles, Role)
                VALUES ('Hugh G. Rektion', '222', 'asd@umak.edu.ph', '{hash}', 100, 1, 'Admin');
            ");

            database.Execute(@$"
                INSERT INTO Profiles (StudName, StudNum, Email, Password, Rating, Puzzles, Role)
                VALUES ('ahjid Fajram', '333', 'asd@umak.edu.ph', '{hash}', 100, 1, 'Admin');
            ");

            database.Execute(@$"
                INSERT INTO Profiles (StudName, StudNum, Email, Password, Rating, Puzzles, Role)
                VALUES ('Mhalac E. Taeteh', '444', 'asd@umak.edu.ph', '{hash}', 100, 1, 'Coach');
            ");

            database.Execute(@$"
                INSERT INTO Profiles (StudName, StudNum, Email, Password, Rating, Puzzles, Role)
                VALUES ('Fhuc Mae A. Noues', '555', 'asd@umak.edu.ph', '{hash}', 100, 1, 'Member');
            ");
        }
    }

    private void AnnouncementDebug()
    {
        if (database.Table<AnnouncementModel>().FirstOrDefault() == null)
        {
            database.Execute(@$"
                INSERT INTO Announcements (Author, LastEditor, Title, Date, Text) 
                VALUES ('A12346169', 'A12346169', 'I need sleep', '{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}', 'Lorem ipsum dolor sit amet, consectetur adipiscing elit. Vestibulum imperdiet, justo eget hendrerit aliquam, risus magna ultrices risus, eget laoreet lectus est non elit. Integer laoreet, risus ac varius interdum, metus eros commodo magna, ac facilisis ipsum arcu vitae justo.

Nulla facilisi. Donec sit amet eros a sem pulvinar tincidunt. Phasellus elementum est nec mi commodo, sed egestas est bibendum. Sed volutpat justo vel magna blandit, eget lacinia ex pretium.');
            ");

            database.Execute(@$"
                INSERT INTO Announcements (Author, LastEditor, Title, Date, Text) 
                VALUES ('A12346169', 'A12346169', 'I eated my shoe', '{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}', 'Quisque in arcu id nisl tincidunt ultricies. Ut commodo arcu nec justo finibus, id posuere urna gravida. Etiam at tincidunt magna, et ultricies augue.

Praesent interdum velit sit amet massa porttitor, vitae viverra ligula efficitur. Aliquam erat volutpat. Donec dignissim, enim nec facilisis pulvinar, augue sem sagittis erat, ac luctus libero mi et enim. Vestibulum venenatis justo vel est malesuada, vitae pretium urna aliquam. Nam pharetra lacinia arcu, sit amet dictum tortor fermentum id. Etiam volutpat, turpis ac fermentum posuere, neque elit mattis nulla, id interdum libero ante nec risus. Mauris non enim tellus. Sed condimentum est vitae ligula imperdiet, ac vulputate augue accumsan.
Curabitur suscipit eros et turpis consequat, ac pretium justo malesuada. Aenean euismod, est ut vulputate porttitor, libero justo dapibus justo, at aliquet sem tortor in est. Sed viverra erat at diam pulvinar, quis eleifend erat fermentum. Suspendisse varius lectus sit amet turpis fermentum luctus. Maecenas ut semper eros, nec tempus ante. Sed nec nisi id erat euismod mattis. Cras et orci a ante pulvinar blandit.');
            ");

            database.Execute(@$"
                INSERT INTO Announcements (Author, LastEditor, Title, Date, Text) 
                VALUES ('A12346169', 'A12346169', 'Give freedom, Give fire, Give me 1.0 or I cry', '{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}', 'Nulla facilisi. Donec sit amet eros a sem pulvinar tincidunt. Phasellus elementum est nec mi commodo, sed egestas est bibendum. Sed volutpat justo vel magna blandit, eget lacinia ex pretium.

Quisque in arcu id nisl tincidunt ultricies. Ut commodo arcu nec justo finibus, id posuere urna gravida. Etiam at tincidunt magna, et ultricies augue.');
            ");
        }
    }

    private void TournamentDebug()
    {
        if (database.Table<TournamentModel>().FirstOrDefault() == null)
        {
            database.Execute(@$"
                INSERT INTO Tournaments (Author, LastEditor, Title, Date, Text) 
                VALUES ('A12346169', 'A12346169', 'How to finish work faster', '{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}', 'Nulla facilisi. Donec sit amet eros a sem pulvinar tincidunt. Phasellus elementum est nec mi commodo, sed egestas est bibendum. Sed volutpat justo vel magna blandit, eget lacinia ex pretium.

Quisque in arcu id nisl tincidunt ultricies. Ut commodo arcu nec justo finibus, id posuere urna gravida. Etiam at tincidunt magna, et ultricies augue.');
            ");

            database.Execute(@$"
                INSERT INTO Tournaments (Author, LastEditor, Title, Date, Text) 
                VALUES ('A12346169', 'A12346169', 'ChatGPT Prompt Engineer', '{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}', 'Lorem ipsum dolor sit amet, consectetur adipiscing elit. Vestibulum imperdiet, justo eget hendrerit aliquam, risus magna ultrices risus, eget laoreet lectus est non elit. Integer laoreet, risus ac varius interdum, metus eros commodo magna, ac facilisis ipsum arcu vitae justo.

Nulla facilisi. Donec sit amet eros a sem pulvinar tincidunt. Phasellus elementum est nec mi commodo, sed egestas est bibendum. Sed volutpat justo vel magna blandit, eget lacinia ex pretium.');
            ");

            string text = "Glasses are really versatile. First, you can have glasses-wearing girls take them off and suddenly become beautiful, or have girls wearing glasses flashing those cute grins, or have girls stealing the protagonist's glasses and putting them on like, \"Haha, got your glasses!' That's just way too cute! Also, boys with glasses! I really like when their glasses have that suspicious looking gleam, and it's amazing how it can look really cool or just be a joke. I really like how it can fulfill all those abstract needs. Being able to switch up the styles and colors of glasses based on your mood is a lot of fun too! It's actually so much fun! You have those half rim glasses, or the thick frame glasses, everything! It's like you're enjoying all these kinds of glasses at a buffet. I really want Luna to try some on or Marine to try some on to replace her eyepatch. We really need glasses to become a thing in hololive and start selling them for HoloComi. Don't. You. Think. We. Really. Need. To. Officially. Give. Everyone. Glasses?".Replace("'", "''");
            database.Execute(@$"
                INSERT INTO Tournaments (Author, LastEditor, Title, Date, Text)
                VALUES ('A12346169', 'A12346169', 'kyaa~~', '{DateTime.Now:yyyy-MM-dd HH:mm:ss}', '{text}');
            ");
        }
    }

    private void GameDebug()
    {
        if (database.Table<GameModel>().FirstOrDefault() == null)
        {
            // --- Student A12346169 (Game 1: The Fool's Mate - Loss) ---
            database.Execute(@"
    INSERT INTO ChessGames (GameNum, StudNum, PlayerColor, PGN, Result, Feedback)
    VALUES (?, ?, ?, ?, ?, ?)",
                1,
                "A12346169",
                "White",
                @"[Event ""Casual Game""]
    [Site ""Unity Chess""]
    [Date ""2025.11.28""]
    [Round ""1""]
    [White ""Player 1""]
    [Black ""Player 2""]
    [Result ""0-1""]

    1. f3 e5 2. g4 Qh4# 0-1",
                "Lose",
                @"You moved your f-pawn too early! Never open the diagonal to your King like that. This is the fastest checkmate in chess. Let's review opening principles next session."
            );

            // --- Student A12346169 (Game 2: Scholar's Mate - Win) ---
            database.Execute(@"
    INSERT INTO ChessGames (GameNum, StudNum, PlayerColor, PGN, Result, Feedback)
    VALUES (?, ?, ?, ?, ?, ?)",
                2,
                "A12346169",
                "White",
                @"[Event ""Club Match""]
    [Site ""Unity Chess""]
    [Date ""2025.11.29""]
    [White ""Player 1""]
    [Black ""Player 2""]
    [Result ""1-0""]

    1. e4 e5 2. Qh5 Nc6 3. Bc4 Nf6 4. Qxf7# 1-0",
                "Win",
                @"Classic Scholar's Mate! It works on beginners, but be careful trying this against higher-rated players—if they defend f7, your Queen gets chased around."
            );

            // --- Student 222 (Game 1: Sicilian Defense - Win) ---
            database.Execute(@"
    INSERT INTO ChessGames (GameNum, StudNum, PlayerColor, PGN, Result, Feedback)
    VALUES (?, ?, ?, ?, ?, ?)",
                1,
                "222",
                "Black",
                @"[Event ""Tournament""]
    [Site ""Unity Chess""]
    [Result ""0-1""]

    1. e4 c5 2. Nf3 d6 3. d4 cxd4 4. Nxd4 Nf6 5. Nc3 a6 6. Bg5 e6 7. f4 Qb6 0-1",
                "Win",
                @"Excellent handling of the Sicilian Najdorf (Poisoned Pawn variation). You created dynamic counterplay on the queenside. Very aggressive!"
            );

            // --- Student 222 (Game 2: Blunder - Lose) ---
            database.Execute(@"
    INSERT INTO ChessGames (GameNum, StudNum, PlayerColor, PGN, Result, Feedback)
    VALUES (?, ?, ?, ?, ?, ?)",
                2,
                "222",
                "White",
                @"[Event ""Blitz""]
    [Result ""0-1""]

    1. d4 d5 2. c4 e6 3. Nc3 Nf6 4. Bg5 Be7 5. e3 O-O 6. Bd3?? dxc4 0-1",
                "Lose",
                @"You hung your Bishop on move 6! You were playing a solid Queen's Gambit until that slip. Always double-check loose pieces before moving."
            );

            // --- Student 333 (Game 1: Long Game - Draw) ---
            database.Execute(@"
    INSERT INTO ChessGames (GameNum, StudNum, PlayerColor, PGN, Result, Feedback)
    VALUES (?, ?, ?, ?, ?, ?)",
                1,
                "333",
                "White",
                @"[Event ""Marathon""]
    [Result ""1/2-1/2""]

    1. Nf3 d5 2. g3 Nf6 3. Bg2 e6 4. O-O Be7 5. d3 O-O 6. Nbd2 c5 7. e4 Nc6 1/2-1/2",
                "Draw",
                @"A bit passive in the middlegame. You played the King's Indian Attack correctly, but you needed to push for e5 earlier to fight for a win."
            );

            // --- Student 333 (Game 2: Fried Liver Attack - Win) ---
            database.Execute(@"
    INSERT INTO ChessGames (GameNum, StudNum, PlayerColor, PGN, Result, Feedback)
    VALUES (?, ?, ?, ?, ?, ?)",
                2,
                "333",
                "White",
                @"[Event ""Aggressive""]
    [Result ""1-0""]

    1. e4 e5 2. Nf3 Nc6 3. Bc4 Nf6 4. Ng5 d5 5. exd5 Nxd5 6. Nxf7 Kxf7 7. Qf3+ Ke6 1-0",
                "Win",
                @"The Fried Liver Attack! Risky, but you handled the initiative perfectly. The King hunt was executed with precision."
            );

            // --- Student 444 (Game 1: King's Gambit - Draw) ---
            database.Execute(@"
    INSERT INTO ChessGames (GameNum, StudNum, PlayerColor, PGN, Result, Feedback)
    VALUES (?, ?, ?, ?, ?, ?)",
                1,
                "444",
                "White",
                @"[Event ""Casual""]
    [Result ""1/2-1/2""]

    1. e4 e5 2. f4 exf4 3. Nf3 g5 4. Bc4 Bg7 5. O-O h6 6. d4 d6 1/2-1/2",
                "Draw",
                @"A spicy King's Gambit! The game got wild, but you held the position well. A draw here is a respectable result given the chaotic tactics."
            );

            // --- Student 444 (Game 2: Smothered Mate - Win) ---
            database.Execute(@"
    INSERT INTO ChessGames (GameNum, StudNum, PlayerColor, PGN, Result, Feedback)
    VALUES (?, ?, ?, ?, ?, ?)",
                2,
                "444",
                "Black",
                @"[Event ""Training""]
    [Result ""0-1""]

    1. e4 e5 2. Nf3 Nc6 3. Bc4 Nd4 4. Nxe5 Qg5 5. Nxf7 Qxg2 6. Rf1 Qxe4+ 7. Be2 Nf3# 0-1",
                "Win",
                @"AYOOOOOOOO! THAT SMOTHERED MATE WAS COLD! Sacrificing the Knight to trap the King? Absolutely brilliant tactical vision!"
            );

            // --- Student 555 (Game 1: Caro-Kann - Win) ---
            database.Execute(@"
    INSERT INTO ChessGames (GameNum, StudNum, PlayerColor, PGN, Result, Feedback)
    VALUES (?, ?, ?, ?, ?, ?)",
                1,
                "555",
                "Black",
                @"[Event ""Ranked""]
    [Result ""0-1""]

    1. e4 c6 2. d4 d5 3. Nc3 dxe4 4. Nxe4 Bf5 5. Ng3 Bg6 6. h4 h6 0-1",
                "Win",
                @"Textbook Caro-Kann Defense. Very solid structure. You didn't give them any weaknesses to attack. Good positional understanding."
            );

            // --- Student 555 (Game 2: Smothered Mate - Win) ---
            database.Execute(@"
    INSERT INTO ChessGames (GameNum, StudNum, PlayerColor, PGN, Result, Feedback)
    VALUES (?, ?, ?, ?, ?, ?)",
                2,
                "555",
                "Black",
                @"[Event ""Training""]
    [Result ""0-1""]

    1. e4 e5 2. Nf3 Nc6 3. Bc4 Nd4 4. Nxe5 Qg5 5. Nxf7 Qxg2 6. Rf1 Qxe4+ 7. Be2 Nf3# 0-1",
                "Win",
                @"AYOOOOOOOO! THAT SMOTHERED MATE WAS COLD! Sacrificing the Knight to trap the King? Absolutely brilliant tactical vision!"
            );
        }
    }

    private void OrgDebug()
    {
        if (database.Table<OrgMemberModel>().FirstOrDefault() == null)
        {
            database.Execute(@"
                INSERT INTO OrgRoster (StudName, StudNum)
                VALUES ('aaaaaaa', '999');
            ");

            database.Execute(@"
                INSERT INTO OrgRoster (StudName, StudNum)
                VALUES ('aaaaaaa', '888');
            ");

            database.Execute(@"
                INSERT INTO OrgRoster (StudName, StudNum)
                VALUES ('aaaaaaa', '777');
            ");
        }
    }

    private void RegDebug()
    {
        if (database.Table<RegisterModel>().FirstOrDefault() == null)
        {
            database.Execute(@"
                INSERT INTO Registrations (StudName, StudNum, Email, Password)
                VALUES ('John A', '123', 'asd@umak.edu.ph', '123');
            ");

            database.Execute(@"
                INSERT INTO Registrations (StudName, StudNum, Email, Password)
                VALUES ('John B', '234', 'asd@umak.edu.ph', '123');
            ");

            database.Execute(@"
                INSERT INTO Registrations (StudName, StudNum, Email, Password)
                VALUES ('John C', '345', 'asd@umak.edu.ph', '123');
            ");
        }
    }

    private void DeleteTablesDebug()
    {
        database.Execute(@"DELETE FROM Profiles");
        database.Execute(@"DELETE FROM ChessGames");
        database.Execute(@"DELETE FROM Announcements");
        database.Execute(@"DELETE FROM Tournaments");
        database.Execute(@"DELETE FROM Registrations");
        database.Execute(@"DELETE FROM OrgRoster");
        database.Execute(@"DELETE FROM sqlite_sequence WHERE name='Profiles'");
        database.Execute(@"DELETE FROM sqlite_sequence WHERE name='ChessGames'");
        database.Execute(@"DELETE FROM sqlite_sequence WHERE name='Announcements'");
        database.Execute(@"DELETE FROM sqlite_sequence WHERE name='Tournaments'");
    }
}
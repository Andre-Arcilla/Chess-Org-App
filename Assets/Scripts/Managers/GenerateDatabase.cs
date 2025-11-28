using SQLite4Unity3d;
using System;
using System.Collections;
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

        // Creates Profiles table
        database.Execute(@"
            CREATE TABLE IF NOT EXISTS Profiles (
	            UserID	INTEGER NOT NULL DEFAULT 1 CHECK(UserID >= 0),
	            StudName	TEXT NOT NULL DEFAULT 'John Smith' COLLATE NOCASE,
	            StudNum	TEXT NOT NULL UNIQUE COLLATE NOCASE,
	            Email	TEXT NOT NULL DEFAULT 'no.mail@umak.edu.ph' COLLATE NOCASE,
	            Password	TEXT NOT NULL DEFAULT '12345' COLLATE BINARY,
	            Rating	INTEGER NOT NULL DEFAULT 100 CHECK(Rating >= 1),
	            Puzzles	INTEGER NOT NULL DEFAULT 0 CHECK(Puzzles >= 0),
	            Role	TEXT NOT NULL DEFAULT 'Member' CHECK(Role IN ('Member', 'Coach', 'Admin', 'Disabled')) COLLATE NOCASE,
	            Date	TEXT NOT NULL DEFAULT (strftime('%m/%d/%Y %H:%M:%S', 'now')) COLLATE NOCASE,
	            LastModified	INTEGER NOT NULL DEFAULT (strftime('%s', 'now')) COLLATE NOCASE,
	            PRIMARY KEY(UserID AUTOINCREMENT)
            );
        ");

        // Creates ChessGames table
        database.Execute(@"
            CREATE TABLE IF NOT EXISTS ChessGames (
	            GameID	INTEGER NOT NULL DEFAULT 1 CHECK(GameID >= 0),
	            GameNum	INTEGER NOT NULL DEFAULT 1,
	            StudNum	TEXT NOT NULL,
	            PlayerColor	TEXT NOT NULL DEFAULT 'White' CHECK(PlayerColor IN ('White', 'Black')) COLLATE NOCASE,
	            Date	TEXT NOT NULL DEFAULT (strftime('%m/%d/%Y %H:%M:%S', 'now')) COLLATE NOCASE,
	            Moves	TEXT NOT NULL DEFAULT 'N/A' COLLATE NOCASE,
	            Result	TEXT NOT NULL DEFAULT 'Draw' CHECK(Result IN ('Win', 'Lose', 'Draw')) COLLATE NOCASE,
	            LastModified	INTEGER NOT NULL DEFAULT (strftime('%s', 'now')) COLLATE NOCASE,
	            PRIMARY KEY(GameID AUTOINCREMENT),
	            FOREIGN KEY(StudNum) REFERENCES Profiles(StudNum) ON DELETE CASCADE ON UPDATE CASCADE
            );
        ");

        // Creates Announcements table
        database.Execute(@"
            CREATE TABLE IF NOT EXISTS Announcements (
	            AnnID 	INTEGER NOT NULL DEFAULT 1 CHECK(AnnID >= 0),
	            Author	TEXT NOT NULL,
	            LastEditor	TEXT NOT NULL,
	            Title	TEXT NOT NULL DEFAULT 'Title' COLLATE NOCASE,
	            Date	TEXT NOT NULL DEFAULT (strftime('%m/%d/%Y %H:%M:%S', 'now')) COLLATE NOCASE,
	            Text	TEXT NOT NULL DEFAULT 'Text' COLLATE NOCASE,
	            IsEditing	INTEGER NOT NULL DEFAULT 0 CHECK(IsEditing == 0 || IsEditing == 1),
	            LastModified	INTEGER NOT NULL DEFAULT (strftime('%s', 'now')) COLLATE NOCASE,
	            PRIMARY KEY(AnnID AUTOINCREMENT),
	            FOREIGN KEY(Author) REFERENCES Profiles(StudNum) ON DELETE CASCADE ON UPDATE CASCADE,
	            FOREIGN KEY(LastEditor) REFERENCES Profiles(StudNum) ON DELETE CASCADE ON UPDATE CASCADE
            );
        ");

        // Creates Tournaments table
        database.Execute(@"
            CREATE TABLE IF NOT EXISTS Tournaments (
	            TourID	INTEGER NOT NULL DEFAULT 1 CHECK(TourID >= 0),
	            Author	TEXT NOT NULL,
	            LastEditor	TEXT NOT NULL,
	            Title	TEXT NOT NULL DEFAULT 'Title' COLLATE NOCASE,
	            Date	TEXT NOT NULL DEFAULT (strftime('%m/%d/%Y %H:%M:%S', 'now')) COLLATE NOCASE,
	            Text	TEXT NOT NULL DEFAULT 'Text' COLLATE NOCASE,
	            IsEditing	INTEGER NOT NULL DEFAULT 0 CHECK(IsEditing == 0 || IsEditing == 1),
	            LastModified	INTEGER NOT NULL DEFAULT (strftime('%s', 'now')) COLLATE NOCASE,
	            PRIMARY KEY(TourID AUTOINCREMENT),
	            FOREIGN KEY(Author) REFERENCES Profiles(StudNum) ON DELETE CASCADE ON UPDATE CASCADE,
	            FOREIGN KEY(LastEditor) REFERENCES Profiles(StudNum) ON DELETE CASCADE ON UPDATE CASCADE
            );
        ");

        // Creates Registrations table
        database.Execute(@"
            CREATE TABLE IF NOT EXISTS Registrations (
	            RegID	INTEGER NOT NULL DEFAULT 1 CHECK(RegID >= 0),
	            StudName	TEXT NOT NULL DEFAULT 'John Smith' COLLATE NOCASE,
	            StudNum	TEXT NOT NULL UNIQUE COLLATE NOCASE,
	            Email	TEXT NOT NULL DEFAULT 'no.mail@umak.edu.ph' COLLATE NOCASE,
	            Password	TEXT NOT NULL DEFAULT '12345' COLLATE BINARY,
	            Date	TEXT NOT NULL DEFAULT (strftime('%m/%d/%Y %H:%M:%S', 'now')) COLLATE NOCASE,
	            LastModified	INTEGER NOT NULL DEFAULT (strftime('%s', 'now')) COLLATE NOCASE,
	            PRIMARY KEY(RegID AUTOINCREMENT)
            );
        ");

        // Creates OrgRoster table
        database.Execute(@"
            CREATE TABLE IF NOT EXISTS OrgRoster (
	            MmbrID	INTEGER NOT NULL DEFAULT 1 CHECK(MmbrID >= 0),
	            StudName	TEXT NOT NULL DEFAULT 'John Smith' COLLATE NOCASE,
	            StudNum	TEXT NOT NULL UNIQUE COLLATE NOCASE,
	            LastModified	INTEGER NOT NULL DEFAULT (strftime('%s', 'now')) COLLATE NOCASE,
	            PRIMARY KEY(MmbrID AUTOINCREMENT)
            );
        ");

        // DEBUG TEST

        ProfileDebug();
        AnnouncementDebug();
        TournamentDebug();
        GameDebug();
        OrgDebug();
        RegDebug();
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

    // MOVE TO ITS OWN SCRIPT
    private void ProfileDebug()
    {
        if (database.Table<ProfileModel>().FirstOrDefault() == null)
        {
            database.Execute(@"
                INSERT INTO Profiles (StudName, StudNum, Email, Password, Rating, Puzzles, Role)
                VALUES ('John Softeng', 'A12346169', 'asd@umak.edu.ph', '123', 100, 1, 'Admin');
            ");

            database.Execute(@"
                INSERT INTO Profiles (StudName, StudNum, Email, Password, Rating, Puzzles, Role)
                VALUES ('Hugh G. Rektion', '222', 'asd@umak.edu.ph', '123', 100, 1, 'Admin');
            ");

            database.Execute(@"
                INSERT INTO Profiles (StudName, StudNum, Email, Password, Rating, Puzzles, Role)
                VALUES ('ahjid Fajram', '333', 'asd@umak.edu.ph', '123', 100, 1, 'Admin');
            ");

            database.Execute(@"
                INSERT INTO Profiles (StudName, StudNum, Email, Password, Rating, Puzzles, Role)
                VALUES ('Mhalac E. Taeteh', '444', 'asd@umak.edu.ph', '123', 100, 1, 'Admin');
            ");

            database.Execute(@"
                INSERT INTO Profiles (StudName, StudNum, Email, Password, Rating, Puzzles, Role)
                VALUES ('Fhuc Mae A. Noues', '555', 'asd@umak.edu.ph', '123', 100, 1, 'Admin');
            ");
        }
    }

    // MOVE TO ITS OWN SCRIPT
    private void AnnouncementDebug()
    {
        if (database.Table<AnnouncementModel>().FirstOrDefault() == null)
        {
            database.Execute(@$"
                INSERT INTO Announcements (Author, LastEditor, Title, Date, Text, IsEditing) 
                VALUES ('A12346169', 'A12346169', 'I need sleep', '{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}', 'Lorem ipsum dolor sit amet, consectetur adipiscing elit. Vestibulum imperdiet, justo eget hendrerit aliquam, risus magna ultrices risus, eget laoreet lectus est non elit. Integer laoreet, risus ac varius interdum, metus eros commodo magna, ac facilisis ipsum arcu vitae justo.

Nulla facilisi. Donec sit amet eros a sem pulvinar tincidunt. Phasellus elementum est nec mi commodo, sed egestas est bibendum. Sed volutpat justo vel magna blandit, eget lacinia ex pretium.', 0);
            ");

            database.Execute(@$"
                INSERT INTO Announcements (Author, LastEditor, Title, Date, Text, IsEditing) 
                VALUES ('A12346169', 'A12346169', 'I eated my shoe', '{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}', 'Quisque in arcu id nisl tincidunt ultricies. Ut commodo arcu nec justo finibus, id posuere urna gravida. Etiam at tincidunt magna, et ultricies augue.

Praesent interdum velit sit amet massa porttitor, vitae viverra ligula efficitur. Aliquam erat volutpat. Donec dignissim, enim nec facilisis pulvinar, augue sem sagittis erat, ac luctus libero mi et enim. Vestibulum venenatis justo vel est malesuada, vitae pretium urna aliquam. Nam pharetra lacinia arcu, sit amet dictum tortor fermentum id. Etiam volutpat, turpis ac fermentum posuere, neque elit mattis nulla, id interdum libero ante nec risus. Mauris non enim tellus. Sed condimentum est vitae ligula imperdiet, ac vulputate augue accumsan.
Curabitur suscipit eros et turpis consequat, ac pretium justo malesuada. Aenean euismod, est ut vulputate porttitor, libero justo dapibus justo, at aliquet sem tortor in est. Sed viverra erat at diam pulvinar, quis eleifend erat fermentum. Suspendisse varius lectus sit amet turpis fermentum luctus. Maecenas ut semper eros, nec tempus ante. Sed nec nisi id erat euismod mattis. Cras et orci a ante pulvinar blandit.', 0);
            ");

            database.Execute(@$"
                INSERT INTO Announcements (Author, LastEditor, Title, Date, Text, IsEditing) 
                VALUES ('A12346169', 'A12346169', 'Give freedom, Give fire, Give me 1.0 or I cry', '{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}', 'Nulla facilisi. Donec sit amet eros a sem pulvinar tincidunt. Phasellus elementum est nec mi commodo, sed egestas est bibendum. Sed volutpat justo vel magna blandit, eget lacinia ex pretium.

Quisque in arcu id nisl tincidunt ultricies. Ut commodo arcu nec justo finibus, id posuere urna gravida. Etiam at tincidunt magna, et ultricies augue.', 0);
            ");
        }
    }

    // MOVE TO ITS OWN SCRIPT
    private void TournamentDebug()
    {
        if (database.Table<TournamentModel>().FirstOrDefault() == null)
        {
            database.Execute(@$"
                INSERT INTO Tournaments (Author, LastEditor, Title, Date, Text, IsEditing) 
                VALUES ('A12346169', 'A12346169', 'How to finish work faster', '{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}', 'Nulla facilisi. Donec sit amet eros a sem pulvinar tincidunt. Phasellus elementum est nec mi commodo, sed egestas est bibendum. Sed volutpat justo vel magna blandit, eget lacinia ex pretium.

Quisque in arcu id nisl tincidunt ultricies. Ut commodo arcu nec justo finibus, id posuere urna gravida. Etiam at tincidunt magna, et ultricies augue.', 0);
            ");

            database.Execute(@$"
                INSERT INTO Tournaments (Author, LastEditor, Title, Date, Text, IsEditing) 
                VALUES ('A12346169', 'A12346169', 'ChatGPT Prompt Engineer', '{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}', 'Lorem ipsum dolor sit amet, consectetur adipiscing elit. Vestibulum imperdiet, justo eget hendrerit aliquam, risus magna ultrices risus, eget laoreet lectus est non elit. Integer laoreet, risus ac varius interdum, metus eros commodo magna, ac facilisis ipsum arcu vitae justo.

Nulla facilisi. Donec sit amet eros a sem pulvinar tincidunt. Phasellus elementum est nec mi commodo, sed egestas est bibendum. Sed volutpat justo vel magna blandit, eget lacinia ex pretium.', 0);
            ");

            string text = "Glasses are really versatile. First, you can have glasses-wearing girls take them off and suddenly become beautiful, or have girls wearing glasses flashing those cute grins, or have girls stealing the protagonist's glasses and putting them on like, \"Haha, got your glasses!' That's just way too cute! Also, boys with glasses! I really like when their glasses have that suspicious looking gleam, and it's amazing how it can look really cool or just be a joke. I really like how it can fulfill all those abstract needs. Being able to switch up the styles and colors of glasses based on your mood is a lot of fun too! It's actually so much fun! You have those half rim glasses, or the thick frame glasses, everything! It's like you're enjoying all these kinds of glasses at a buffet. I really want Luna to try some on or Marine to try some on to replace her eyepatch. We really need glasses to become a thing in hololive and start selling them for HoloComi. Don't. You. Think. We. Really. Need. To. Officially. Give. Everyone. Glasses?".Replace("'", "''");
            database.Execute(@$"
                INSERT INTO Tournaments (Author, LastEditor, Title, Date, Text, IsEditing)
                VALUES ('A12346169', 'A12346169', 'kyaa~~', '{DateTime.Now:yyyy-MM-dd HH:mm:ss}', '{text}', 0);
            ");
        }
    }

    private void GameDebug()
    {
        if (database.Table<GameModel>().FirstOrDefault() == null)
        {
            database.Execute(@"
                INSERT INTO ChessGames (GameNum, StudNum, PlayerColor, PGN, Result)
                VALUES ('1', 'A12346169', 'White', '[Event ""Casual Game""]
                [Site ""Unity Chess""]
                [Date ""2025.11.28""]
                [Round ""1""]
                [White ""Player 1""]
                [Black ""Player 2""]
                [Result ""*""]

                1. h4 g5 2. g3 gxh4 3. Bg2 hxg3 4. Rh2 gxh2 5. Bf1 ', 'Win');
            ");

            database.Execute(@"
                INSERT INTO ChessGames (GameNum, StudNum, PlayerColor, PGN, Result)
                VALUES ('2', 'A12346169', 'White', '[Event ""Casual Game""]
                [Site ""Unity Chess""]
                [Date ""2025.11.28""]
                [Round ""1""]
                [White ""Player 1""]
                [Black ""Player 2""]
                [Result ""*""]

                1. h4 g5 2. g3 gxh4 3. Bg2 hxg3 4. Rh2 gxh2 5. Bf1 ', 'Win');
            ");

            database.Execute(@"
                INSERT INTO ChessGames (GameNum, StudNum, PlayerColor, PGN, Result)
                VALUES ('3', 'A12346169', 'White', '[Event ""Casual Game""]
                [Site ""Unity Chess""]
                [Date ""2025.11.28""]
                [Round ""1""]
                [White ""Player 1""]
                [Black ""Player 2""]
                [Result ""*""]

                1. h4 g5 2. g3 gxh4 3. Bg2 hxg3 4. Rh2 gxh2 5. Bf1 ', 'Win');
            ");

            database.Execute(@"
                INSERT INTO ChessGames (GameNum, StudNum, PlayerColor, PGN, Result)
                VALUES ('4', 'A12346169', 'White', '[Event ""Casual Game""]
                [Site ""Unity Chess""]
                [Date ""2025.11.28""]
                [Round ""1""]
                [White ""Player 1""]
                [Black ""Player 2""]
                [Result ""*""]

                1. h4 g5 2. g3 gxh4 3. Bg2 hxg3 4. Rh2 gxh2 5. Bf1 ', 'Win');
            ");
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
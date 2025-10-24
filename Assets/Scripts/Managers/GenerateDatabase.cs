using SQLite4Unity3d;
using System.IO;
using System.Linq;
using TMPro;
using UnityEngine;

public class GenerateDatabase : MonoBehaviour
{
    [Header("data")]
    [SerializeField] private string saveFileName = "Hoshiyomi_ChessOrg.db";

    [SerializeField] private GameObject profilePage;
    [SerializeField] private TextMeshProUGUI studName;
    [SerializeField] private TextMeshProUGUI studNum;
    [SerializeField] private TextMeshProUGUI rating;
    [SerializeField] private TextMeshProUGUI puzzles;

    void Start()
    {
        // Connect to database using this filepath
        var dbPath = Path.Combine(Application.persistentDataPath, saveFileName);
        var database = new SQLiteConnection(dbPath);

        // Creates Profiles table
        database.Execute(@"
            CREATE TABLE IF NOT EXISTS Profiles (
                UserID INTEGER NOT NULL DEFAULT 1 CHECK(UserID >= 0),
                StudNum TEXT NOT NULL COLLATE NOCASE,
                Password TEXT NOT NULL DEFAULT '12345' COLLATE BINARY,
                Name TEXT NOT NULL DEFAULT 'John Smith' COLLATE NOCASE,
                College TEXT NOT NULL DEFAULT 'CCIS' COLLATE NOCASE,
                Rating INTEGER NOT NULL DEFAULT 100 CHECK(Rating >= 1),
                Puzzles INTEGER NOT NULL DEFAULT 0 CHECK(Puzzles >= 0),
                Role TEXT NOT NULL DEFAULT 'Member' CHECK(Role IN ('Member', 'Mod', 'Admin')) COLLATE NOCASE,
                PRIMARY KEY(UserID AUTOINCREMENT)
            );
        ");

        // Creates ChessGames table
        database.Execute(@"
            CREATE TABLE IF NOT EXISTS ChessGames (
	            GameID	INTEGER NOT NULL DEFAULT 1 CHECK(GameID >= 0),
	            StudNum	TEXT NOT NULL,
	            PlayerColor	TEXT NOT NULL DEFAULT 'White' CHECK(PlayerColor IN ('White', 'Black')) COLLATE NOCASE,
	            Date	TEXT NOT NULL DEFAULT '12/12/2000 12:00:00' COLLATE NOCASE,
	            Moves	TEXT NOT NULL DEFAULT 'N/A' COLLATE NOCASE,
	            Result	TEXT NOT NULL DEFAULT 'Draw' CHECK(Result IN ('Win', 'Lose', 'Draw')) COLLATE NOCASE,
	            PRIMARY KEY(GameID AUTOINCREMENT),
	            FOREIGN KEY(StudNum) REFERENCES Profiles(StudNum)
            );
        ");

        // Creates Announcements table
        database.Execute(@"
            CREATE TABLE IF NOT EXISTS Announcements (
	            AnnID	INTEGER NOT NULL DEFAULT 1 CHECK(AnnID >= 0),
	            Author	TEXT NOT NULL,
	            LastEditor	TEXT NOT NULL,
	            Title	TEXT NOT NULL DEFAULT 'Title' COLLATE NOCASE,
	            Date	TEXT NOT NULL DEFAULT '12/12/2000 12:00:00' COLLATE NOCASE,
	            Text	TEXT NOT NULL DEFAULT 'Text' COLLATE NOCASE,
	            IsEditing	INTEGER NOT NULL DEFAULT 0 CHECK(IsEditing == 0 || IsEditing == 1),
	            PRIMARY KEY(AnnID AUTOINCREMENT),
	            FOREIGN KEY(Author) REFERENCES Profiles(StudNum),
	            FOREIGN KEY(LastEditor) REFERENCES Profiles(StudNum)
            );
        ");

        // Creates Tournaments table
        database.Execute(@"
            CREATE TABLE IF NOT EXISTS Tournaments (
	            TourID	INTEGER NOT NULL DEFAULT 1 CHECK(TourID >= 0),
	            Author	TEXT NOT NULL,
	            LastEditor	TEXT NOT NULL,
	            Title	TEXT NOT NULL DEFAULT 'Title' COLLATE NOCASE,
	            Date	TEXT NOT NULL DEFAULT '12/12/2000 12:00:00' COLLATE NOCASE,
	            Text	TEXT NOT NULL DEFAULT 'Text' COLLATE NOCASE,
	            IsEditing	INTEGER NOT NULL DEFAULT 0 CHECK(IsEditing == 0 || IsEditing == 1),
	            PRIMARY KEY(TourID AUTOINCREMENT),
	            FOREIGN KEY(Author) REFERENCES Profiles(StudNum),
	            FOREIGN KEY(LastEditor) REFERENCES Profiles(StudNum)
            );
        ");

        // Creates Registrations table
        database.Execute(@"
            CREATE TABLE IF NOT EXISTS Registrations (
	            RegID	INTEGER NOT NULL DEFAULT 1 CHECK(RegID >= 0),
                StudNum TEXT NOT NULL UNIQUE COLLATE NOCASE,
	            Password	TEXT NOT NULL DEFAULT '12345' COLLATE BINARY,
	            Name	TEXT NOT NULL DEFAULT 'John Smith' COLLATE NOCASE,
	            College	TEXT NOT NULL DEFAULT 'CCIS' COLLATE NOCASE,
	            PRIMARY KEY(RegID AUTOINCREMENT)
            );
        ");

        // Creates OrgRoster table
        database.Execute(@"
            CREATE TABLE IF NOT EXISTS OrgRoster (
	            MmbrID	INTEGER NOT NULL DEFAULT 1 CHECK(MmbrID >= 0),
                StudNum TEXT NOT NULL UNIQUE COLLATE NOCASE,
	            Name	TEXT NOT NULL DEFAULT 'John Smith' COLLATE NOCASE,
	            PRIMARY KEY(MmbrID AUTOINCREMENT)
            );
        ");

        // DEBUG TEST
        database.Execute(@"
            INSERT INTO Profiles (StudNum, Password, Name, College, Rating, Puzzles, Role) 
            VALUES ('A12346169', '123', 'Andre Arcilla', 'CCIS', 410, 10, 'Member');
        ");

        int oldIndex = profilePage.transform.GetSiblingIndex();
        profilePage.transform.SetAsFirstSibling();
        profilePage.SetActive(true);
        Profiles profile = database.Table<Profiles>().FirstOrDefault();

        studName.text = profile.StudName;
        studNum.text = profile.Name;
        rating.text = profile.Rating.ToString();
        puzzles.text = profile.Puzzles.ToString();
        profilePage.SetActive(false);
        profilePage.transform.SetSiblingIndex(oldIndex);
    }
}

public class Profiles
{
    public int UserID { get; set; }

    public string Name { get; set; }

    public string Password { get; set; }

    public string StudName { get; set; }

    public string College { get; set; }

    public int Rating { get; set; }

    public int Puzzles { get; set; }

    public string Role { get; set; }
}

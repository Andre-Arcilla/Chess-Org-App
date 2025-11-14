using SQLite4Unity3d;
using System;

[Table("ChessGames")]
public class GameModel
{
    [PrimaryKey, AutoIncrement]
    public int GameID { get; set; }

    public int GameNum { get; set; }

    public string StudNum { get; set; }

    public string PlayerColor { get; set; }

    public DateTime Date { get; set; }

    public string Moves { get; set; }

    public string Result { get; set; }

    public long LastModified { get; set; }
}


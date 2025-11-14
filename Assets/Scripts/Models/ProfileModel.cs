using SQLite4Unity3d;
using System;

[Table("Profiles")]
public class ProfileModel
{
    [PrimaryKey, AutoIncrement]
    public int UserID { get; set; }

    public string StudName { get; set; }

    public string Email { get; set; }

    public string StudNum { get; set; }

    public string Password { get; set; }

    public int Rating { get; set; }

    public int Puzzles { get; set; }

    public string Role { get; set; }

    public DateTime Date { get; set; }

    public long LastModified { get; set; }
}

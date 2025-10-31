using SQLite4Unity3d;

[Table("Profiles")]
public class ProfileModel
{
    public int UserID { get; set; }

    public string StudName { get; set; }

    public string StudNum { get; set; }

    public string Password { get; set; }

    public string College { get; set; }

    public int Rating { get; set; }

    public int Puzzles { get; set; }

    public string Role { get; set; }
}

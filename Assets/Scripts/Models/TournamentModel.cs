using SQLite4Unity3d;

[Table("Tournaments")]
public class TournamentModel
{
    public int AnnID { get; set; }

    public string Author { get; set; }

    public string LastEditor { get; set; }

    public string Title { get; set; }

    public string Date { get; set; }

    public string Text { get; set; }

    public int IsEditing { get; set; }
}

using SQLite4Unity3d;
using System;

[Table("Announcements")]
public class AnnouncementModel
{
    [PrimaryKey, AutoIncrement]
    public int AnnID { get; set; }

    public string Author { get; set; }

    public string LastEditor { get; set; }

    public string Title { get; set; }

    public DateTime Date { get; set; }

    public string Text { get; set; }

    public long LastModified { get; set; }
}
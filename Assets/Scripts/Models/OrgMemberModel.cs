using SQLite4Unity3d;

[Table("OrgRoster")]
public class OrgMemberModel
{
    [PrimaryKey, AutoIncrement]
    public int MmbrID { get; set; }

    public string StudName { get; set; }

    public string StudNum { get; set; }

    public long LastModified { get; set; }
}

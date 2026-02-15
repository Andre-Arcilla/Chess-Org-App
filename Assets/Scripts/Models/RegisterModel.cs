using SQLite4Unity3d;
using System;

[Table("Registrations")]
public class RegisterModel
{
    [PrimaryKey, AutoIncrement]
    public int RegID { get; set; }

    public string StudName { get; set; }

    public string Email { get; set; }

    public string StudNum { get; set; }

    public string Password { get; set; }

    public DateTime Date { get; set; }

    public long LastModified { get; set; }
}
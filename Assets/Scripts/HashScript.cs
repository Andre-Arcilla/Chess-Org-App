using System;
using System.Security.Cryptography;
using System.Text;


public class HashScript
{
    private const string SALT = "SuiseiHoshimachi";

    public static string Hash(string password)
    {
        var bytes = new Rfc2898DeriveBytes(
            password,
            Encoding.UTF8.GetBytes(SALT),
            10000
        );

        return Convert.ToBase64String(bytes.GetBytes(32));
    }
}
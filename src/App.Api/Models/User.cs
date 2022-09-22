namespace App.Api.Models;

public class User
{
    public User(string username, byte[] passwordHash)
    {
        Username = username;
        PasswordHash = passwordHash;
        RefreshToken = null;
    }

    public string Username { get; }
    public byte[] PasswordHash { get; }
    public Token? RefreshToken { get; set; }
}
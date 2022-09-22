namespace App.Api.Models;

public class AuthenticationRequest 
{
    public AuthenticationRequest(string username, string password)
    {
        Username = username;
        Password = password;
    }

    public string Username { get; }
    public string Password { get; }
}
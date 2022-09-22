namespace App.Api.Models;

public class RegistrationRequest
{
    public RegistrationRequest(string username, string password)
    {
        Username = username;
        Password = password;
    }

    public string Username { get; }
    public string Password { get; }
}
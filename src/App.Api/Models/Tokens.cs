namespace App.Api.Models;

public class Tokens
{
    public Tokens(Token accessToken, Token refreshToken)
    {
        AccessToken = accessToken;
        RefreshToken = refreshToken; 
    }

    public Token AccessToken { get; }
    public Token RefreshToken { get; }
}
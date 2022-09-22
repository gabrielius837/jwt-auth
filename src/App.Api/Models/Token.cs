namespace App.Api.Models;

public class Token
{
    public Token(string value, long expires)
    {
        Value = value;
        Expires = expires; 
    }

    public string Value { get; }
    public long Expires { get; }
}
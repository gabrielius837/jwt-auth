namespace App.Api;

public static class AppSettingsExtensions
{
    public static TokenValidationParameters CreateTokenValidationParameters(this AppSettings settings)
    {
        return new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey
            (
                Encoding.UTF8.GetBytes(settings.SecretKey)
            ),
            ValidateIssuer = false,
            ValidateAudience = false,
            ClockSkew = TimeSpan.Zero
        };
    }
}
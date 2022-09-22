namespace App.Api.Models;

public record AppSettings
(
    string SecretKey,
    int AccessTokenLifetime,
    int RefreshTokenLifetime,
    // must be true in prod
    bool SecureCookie
);

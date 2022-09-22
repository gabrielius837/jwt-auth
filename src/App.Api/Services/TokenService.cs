namespace App.Api.Services;

public interface ITokenService
{
    Task<Tokens?> CreateTokens(AuthenticationRequest request);
    Task<Tokens?> RefreshTokens(string refreshToken);
    JwtSecurityToken? ReadAccessToken(string accessToken);
}

public class TokenService : ITokenService
{
    private readonly AppSettings _settings;
    private readonly IUserService _userService;

    public TokenService(AppSettings settings, IUserService userService)
    {
        _settings = settings;
        _userService = userService;
    }

    public static Token CreateAccessToken(string username, AppSettings settings)
    {
        var claims = new Claim[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, username),
            };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(settings.SecretKey));

        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha512);

        var expires = DateTime.UtcNow.AddSeconds(settings.AccessTokenLifetime);
        var sectoken = new JwtSecurityToken
        (
            claims: claims,
            expires: expires,
            signingCredentials: creds
        );

        var jwt = new JwtSecurityTokenHandler().WriteToken(sectoken);
        var timestamp = new DateTimeOffset(expires).ToUnixTimeSeconds();

        var token = new Token(jwt, timestamp);

        return token;
    }

    public async Task<Tokens?> CreateTokens(AuthenticationRequest request)
    {
        var user = await _userService.CreateRefreshToken(request);
        if (user is null || user.RefreshToken is null)
            return null;
        
        var accessToken = CreateAccessToken(user.Username, _settings);

        var tokens = new Tokens(accessToken, user.RefreshToken);
        return tokens;
    }

    public JwtSecurityToken? ReadAccessToken(string token)
    {
        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            tokenHandler.ValidateToken
            (
                token, 
                _settings.CreateTokenValidationParameters(),
                out SecurityToken validatedToken
            );

            var jwtToken = validatedToken as JwtSecurityToken;
            return jwtToken;
        }
        catch
        {
            // do nothing if jwt validation fails
            // sub is not attached to context so request won't have access to secure routes
            return null;
        }
    }

    public async Task<Tokens?> RefreshTokens(string refreshToken)
    {
        var user = await _userService.RefreshRefreshToken(refreshToken);
        if (user is null || user.RefreshToken is null)
            return null;
        
        var accessToken = CreateAccessToken(user.Username, _settings);

        var result = new Tokens(accessToken, user.RefreshToken);
        return result;
    }
}
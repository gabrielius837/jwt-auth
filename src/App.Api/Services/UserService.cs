namespace App.Api.Services;

public interface IUserService
{
    Task<User?> Register(RegistrationRequest request);
    Task<User?> CreateRefreshToken(AuthenticationRequest request);
    Task<User?> RefreshRefreshToken(string refreshToken);
    Task<bool> RevokeToken(string refreshToken);
}

public class UserService : IUserService
{
    private static readonly IDictionary<string, User> _userCache = new Dictionary<string, User>();
    private readonly AppSettings _settings;

    public UserService(AppSettings settings)
    {
        _settings = settings;
    }

    private static bool Exists(string username)
    {
        return _userCache.ContainsKey(username);
    }

    public Task<User?> Register(RegistrationRequest request)
    {
        if (Exists(request.Username))
            return Task.FromResult<User?>(null);

        var key = Encoding.UTF8.GetBytes(_settings.SecretKey);
        var hash = CreatePasswordHash(request.Password, key);
        var entity = new User(request.Username, hash);
        _userCache[request.Username] = entity;
        return Task.FromResult<User?>(entity);
    }

    private static User? ValidateCredentials(AuthenticationRequest request, AppSettings settings)
    {
        var exists = _userCache.ContainsKey(request.Username);
        if (!exists)
            return null;
        
        var existingUser = _userCache[request.Username];
        var key = Encoding.UTF8.GetBytes(settings.SecretKey);
        var valid = VerifyPassword(request.Password, existingUser.PasswordHash, key);
        return valid ? existingUser : null;
    }

    public Task<User?> CreateRefreshToken(AuthenticationRequest request)
    {
        var user = ValidateCredentials(request, _settings);
        if (user is null)
            return Task.FromResult<User?>(null);
        
        var updatedUser = CreateRefreshToken(user, _settings);
        return Task.FromResult<User?>(updatedUser);
    }

    private static User CreateRefreshToken(User user, AppSettings settings)
    {
        var bytes = RandomNumberGenerator.GetBytes(64);
        var token = Convert.ToBase64String(bytes);
        var timestamp = DateTimeOffset.UtcNow.AddSeconds(settings.RefreshTokenLifetime).ToUnixTimeSeconds();
        var result = new Token(token, timestamp);
        user.RefreshToken = result;
        return user;
    }

    public static byte[] CreatePasswordHash(string password, byte[] key)
    {
        using var hmac = new HMACSHA512(key);
        return hmac.ComputeHash(Encoding.UTF8.GetBytes(password));
    }

    private static bool VerifyPassword(string password, byte[] hash, byte[] key)
    {
        using var hmac = new HMACSHA512(key);
        var computedHash = hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(password));
        return computedHash.SequenceEqual(hash);
    }

    public Task<User?> RefreshRefreshToken(string refreshToken)
    {
        var user = FindUser(refreshToken);
        if (user is null)
            return Task.FromResult<User?>(null);

        var updatedUser = CreateRefreshToken(user, _settings);
        return Task.FromResult<User?>(updatedUser);
    }

    private static User? FindUser(string refreshToken)
    {
        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var user = _userCache.FirstOrDefault(user => 
            user.Value.RefreshToken != null
            && user.Value.RefreshToken.Value == refreshToken
            && user.Value.RefreshToken.Expires > now).Value;
        return user;
    }

    public Task<bool> RevokeToken(string refreshToken)
    {
        var user = FindUser(refreshToken);
        if (user is null)
            return Task.FromResult(false);
        
        var token = user.RefreshToken;
        user.RefreshToken = null;
        var result = token is not null ? true : false;
        return Task.FromResult(result);
    }
}
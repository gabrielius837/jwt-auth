namespace App.UnitTests;

public class UserService_Tests
{
    const string KEY = "a70c18675573a15b6e257c15616d134f94ec437988871489bacf2ac18775311f";
    const string PASSWORD = "pest";

    [Fact]
    public async Task Register_ShouldSucceed_ForTheFirstTime()
    {
        // arrange
        var settings = new AppSettings(KEY, 5, 10, false);
        const string USERNAME = "test";
        var registrationReq = new RegistrationRequest(USERNAME, PASSWORD);
        var userService = new UserService(settings);

        // act
        var user = await userService.Register(registrationReq);
        var hash = UserService.CreatePasswordHash(PASSWORD, Encoding.UTF8.GetBytes(settings.SecretKey));

        // assert
        Assert.NotNull(user);
        Assert.Equal(user?.Username, USERNAME);
        Assert.Equal(user?.PasswordHash, hash);
    }

    [Fact]
    public async Task Register_ShouldFail_ForTheSecondTime()
    {
        // arrange
        const string USERNAME = "fest";
        var settings = new AppSettings(KEY, 5, 10, false);
        var registrationReq = new RegistrationRequest(USERNAME, PASSWORD);
        var userService = new UserService(settings);

        // act
        var user = await userService.Register(registrationReq);
        var userAgain = await userService.Register(registrationReq);

        // assert
        Assert.NotNull(user);
        Assert.Null(userAgain);
    }

    [Fact]
    public async Task CreateRefreshToken_ShouldFail_WhenUserDoesNotExist()
    {
        // arrange
        const string USERNAME = "pest";
        var settings = new AppSettings(KEY, 5, 10, false);
        var authReq = new AuthenticationRequest(USERNAME, PASSWORD);
        var userService = new UserService(settings);

        // act
        var user = await userService.CreateRefreshToken(authReq);

        // assert
        Assert.Null(user);
    }

    [Fact]
    public async Task CreateRefreshToken_ShouldSucceed_WhenUserExists()
    {
        // arrange
        const string USERNAME = "crest";
        const int LIFETIME = 10;
        var settings = new AppSettings(KEY, 5, LIFETIME, false);
        var authReq = new AuthenticationRequest(USERNAME, PASSWORD);
        var regReq = new RegistrationRequest(USERNAME, PASSWORD);
        var userService = new UserService(settings);
        var reg = await userService.Register(regReq);

        // act
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var user = await userService.CreateRefreshToken(authReq);
        var expires = user?.RefreshToken?.Expires;

        // assert
        Assert.NotNull(user);
        Assert.Equal(reg, user);
        Assert.Equal(reg?.Username, user?.Username);
        Assert.Equal(reg?.PasswordHash, user?.PasswordHash);
        // adjusting for possible second shift
        Assert.True
        (
            timestamp + LIFETIME == expires ||
            timestamp + LIFETIME + 1 == expires
        );
    }

    [Fact]
    public async Task RefreshRefreshToken_ShouldFail_WhenUserDoesNotExist()
    {
        // arrange
        var settings = new AppSettings(KEY, 5, 10, false);
        const string TOKEN = "token";
        var userService = new UserService(settings);

        // act
        var user = await userService.RefreshRefreshToken(TOKEN);

        // assert
        Assert.Null(user);
    }

    [Fact]
    public async Task RefreshRefreshToken_ShouldFail_WhenTokenIsExpired()
    {
        // arrange
        const string USERNAME = "fresh";
        var settings = new AppSettings(KEY, 5, -1, false);
        var userService = new UserService(settings);
        var regReq = new RegistrationRequest(USERNAME, PASSWORD);
        var authReq = new AuthenticationRequest(USERNAME, PASSWORD);
        var user = await userService.Register(regReq);
        await userService.CreateRefreshToken(authReq);
        if (user is null || user.RefreshToken is null)
            throw new ArgumentNullException("Failed to initialize state");

        // act
        var result = await userService.RefreshRefreshToken(user.RefreshToken.Value);
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        // assert
        Assert.Null(result);
        Assert.True(timestamp > user.RefreshToken.Expires);
    }

    [Fact]
    public async Task RefreshRefreshToken_ShouldSucceed_WhenEverythingIsOk()
    {
        // arrange
        const string USERNAME = "new";
        const int LIFETIME = 10;
        var settings = new AppSettings(KEY, 5, LIFETIME, false);
        var userService = new UserService(settings);
        var regReq = new RegistrationRequest(USERNAME, PASSWORD);
        var authReq = new AuthenticationRequest(USERNAME, PASSWORD);
        var user = await userService.Register(regReq);
        await userService.CreateRefreshToken(authReq);
        if (user is null || user.RefreshToken is null)
            throw new ArgumentNullException("Failed to initialize state");

        // act
        var oldToken = user.RefreshToken;
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var result = await userService.RefreshRefreshToken(user.RefreshToken.Value);
        var expires = result?.RefreshToken?.Expires;

        // assert
        Assert.NotNull(result);
        Assert.Equal(result, user);
        Assert.Equal(result?.Username, user.Username);
        Assert.Equal(result?.PasswordHash, user.PasswordHash);
        Assert.NotNull(result?.RefreshToken);
        Assert.NotEqual(oldToken, result?.RefreshToken);
        Assert.NotEqual(oldToken.Value, result?.RefreshToken?.Value);
        // adjusting for possible second shift
        Assert.True
        (
            timestamp + LIFETIME == expires ||
            timestamp + LIFETIME + 1 == expires
        );
    }

    [Fact]
    public async Task RevokeToken_ShouldFail_WhenCantFindUser()
    {
        // arrange
        const string TOKEN = "token";
        const int LIFETIME = 10;
        var settings = new AppSettings(KEY, 5, LIFETIME, false);
        var userService = new UserService(settings);

        // act
        var result = await userService.RevokeToken(TOKEN);

        // assert
        Assert.False(result);
    }

    [Fact]
    public async Task RevokeToken_ShouldFail_WhenUserExistsButNoValidToken()
    {
        // arrange
        const string USERNAME = "valid";
        const string TOKEN = "token";
        const int LIFETIME = 10;
        var settings = new AppSettings(KEY, 5, LIFETIME, false);
        var userService = new UserService(settings);
        var regReq = new RegistrationRequest(USERNAME, PASSWORD);
        var authReq = new AuthenticationRequest(USERNAME, PASSWORD);
        var user = await userService.Register(regReq);
        await userService.CreateRefreshToken(authReq);
        if (user is null || user.RefreshToken is null)
            throw new ArgumentNullException("Failed to initialize state");

        // act
        var result = await userService.RevokeToken(TOKEN);

        // assert
        Assert.False(result);
    }

    [Fact]
    public async Task RevokeToken_ShouldSucceed_WhenEverythingIsOk()
    {
        // arrange
        const string USERNAME = "candid";
        const int LIFETIME = 10;
        var settings = new AppSettings(KEY, 5, LIFETIME, false);
        var userService = new UserService(settings);
        var regReq = new RegistrationRequest(USERNAME, PASSWORD);
        var authReq = new AuthenticationRequest(USERNAME, PASSWORD);
        var user = await userService.Register(regReq);
        await userService.CreateRefreshToken(authReq);
        if (user is null || user.RefreshToken is null)
            throw new ArgumentNullException("Failed to initialize state");

        // act
        var result = await userService.RevokeToken(user.RefreshToken.Value);

        // assert
        Assert.True(result);
    }
}
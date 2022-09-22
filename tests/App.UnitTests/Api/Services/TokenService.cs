namespace App.UnitTests;

public class TokenService_Tests
{
    const string KEY = "a70c18675573a15b6e257c15616d134f94ec437988871489bacf2ac18775311f";
    const int ACCESS_LIFETIME = 300;
    const int REFRESH_LIFETIME = 25200;
    const bool SECURE = false;
    const string USERNAME = "test_test";
    const string PASSWORD = "pass";
    const string TOKEN = "token";

    [Fact]
    public async Task CreateTokens_ShouldReturnNull_WhenNoUsersReturned()
    {
        // arrange
        var settings = new AppSettings(KEY, ACCESS_LIFETIME, REFRESH_LIFETIME, SECURE);
        var userService = new Mock<IUserService>();
        User? user = null;
        var req = new AuthenticationRequest(USERNAME, PASSWORD);
        userService.Setup(x => x.CreateRefreshToken(req))
            .ReturnsAsync(user);
        var service = new TokenService(settings, userService.Object);

        // act
        var result = await service.CreateTokens(req);

        // assert
        Assert.Null(result);
    }

    [Fact]
    public async Task CreateTokens_ShouldReturnNull_WhenRefreshTokenIsMissing()
    {
        // arrange
        var settings = new AppSettings(KEY, ACCESS_LIFETIME, REFRESH_LIFETIME, SECURE);
        var userService = new Mock<IUserService>();
        User? user = new User(USERNAME, Encoding.UTF8.GetBytes(PASSWORD));
        var req = new AuthenticationRequest(USERNAME, PASSWORD);
        userService.Setup(x => x.CreateRefreshToken(req))
            .ReturnsAsync(user);
        var service = new TokenService(settings, userService.Object);

        // act
        var result = await service.CreateTokens(req);

        // assert
        Assert.Null(result);
    }

    [Fact]
    public async Task CreateTokens_ShouldReturnUser_WhenEverythingIsOk()
    {
        // arrange
        var settings = new AppSettings(KEY, ACCESS_LIFETIME, REFRESH_LIFETIME, SECURE);
        var userService = new Mock<IUserService>();
        User? user = new User(USERNAME, Encoding.UTF8.GetBytes(PASSWORD));
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        user.RefreshToken = new Token(TOKEN, timestamp);
        var req = new AuthenticationRequest(USERNAME, PASSWORD);
        userService.Setup(x => x.CreateRefreshToken(req))
            .ReturnsAsync(user);
        var service = new TokenService(settings, userService.Object);

        // act
        var result = await service.CreateTokens(req);

        // assert
        Assert.NotNull(result);
        Assert.Equal(user.RefreshToken, result?.RefreshToken);
        Assert.Equal(result?.RefreshToken.Value, TOKEN);
        Assert.Equal(result?.RefreshToken.Expires, timestamp);
    }

    [Fact]
    public async Task RefreshTokens_ShouldReturnNull_WhenNoUsersReturned()
    {
        // arrange
        var settings = new AppSettings(KEY, ACCESS_LIFETIME, REFRESH_LIFETIME, SECURE);
        var userService = new Mock<IUserService>();
        User? user = null;
        userService.Setup(x => x.RefreshRefreshToken(TOKEN))
            .ReturnsAsync(user);
        var service = new TokenService(settings, userService.Object);

        // act
        var result = await service.RefreshTokens(TOKEN);

        // assert
        Assert.Null(result);
    }

    [Fact]
    public async Task RefreshTokens_ShouldReturnNull_WhenRefreshTokenIsMissing()
    {
        // arrange
        var settings = new AppSettings(KEY, ACCESS_LIFETIME, REFRESH_LIFETIME, SECURE);
        var userService = new Mock<IUserService>();
        User? user = new User(USERNAME, Encoding.UTF8.GetBytes(PASSWORD));
        userService.Setup(x => x.RefreshRefreshToken(TOKEN))
            .ReturnsAsync(user);
        var service = new TokenService(settings, userService.Object);

        // act
        var result = await service.RefreshTokens(TOKEN);

        // assert
        Assert.Null(result);
    }

    [Fact]
    public async Task RefreshTokens_ShouldReturnUser_WhenEverythingIsOk()
    {
        // arrange
        var settings = new AppSettings(KEY, ACCESS_LIFETIME, REFRESH_LIFETIME, SECURE);
        var userService = new Mock<IUserService>();
        User? user = new User(USERNAME, Encoding.UTF8.GetBytes(PASSWORD));
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        user.RefreshToken = new Token(TOKEN, timestamp);
        userService.Setup(x => x.RefreshRefreshToken(TOKEN))
            .ReturnsAsync(user);
        var service = new TokenService(settings, userService.Object);

        // act
        var result = await service.RefreshTokens(TOKEN);

        // assert
        Assert.NotNull(result);
        Assert.Equal(user.RefreshToken, result?.RefreshToken);
        Assert.Equal(result?.RefreshToken.Value, TOKEN);
        Assert.Equal(result?.RefreshToken.Expires, timestamp);
    }

    [Fact]
    public void ReadAccessToken_ReturnNull_WhenTokenIsExpired()
    {
        // arrange
        var settings = new AppSettings(KEY, -1, REFRESH_LIFETIME, SECURE);
        var userService = new Mock<IUserService>();
        var service = new TokenService(settings, userService.Object);
        var token = TokenService.CreateAccessToken(USERNAME, settings);

        // act
        var result = service.ReadAccessToken(token.Value);

        // assert
        Assert.Null(result);
    }

    [Fact]
    public void ReadToken_ShouldReturnToken_WhenEverythingIsOkay()
    {
        // arrange
        var settings = new AppSettings(KEY, ACCESS_LIFETIME, REFRESH_LIFETIME, SECURE);
        var userService = new Mock<IUserService>();
        var service = new TokenService(settings, userService.Object);
        var token = TokenService.CreateAccessToken(USERNAME, settings);

        // act
        var result = service.ReadAccessToken(token.Value);
        var claim = result?.Claims.FirstOrDefault(claim => 
            claim.Type == JwtRegisteredClaimNames.Sub && claim.Value == USERNAME
        );
        var datetime = DateTimeOffset.FromUnixTimeSeconds(token.Expires).UtcDateTime;
        
        // assert
        Assert.NotNull(result);
        Assert.NotNull(claim);
        Assert.Equal(datetime, result?.ValidTo);
    }

}
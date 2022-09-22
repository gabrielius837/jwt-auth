
namespace App.UnitTests;

public class AuthController_Tests
{
    const string USERNAME = "test";
    const string PASSWORD = "pest";
    [Fact]
    public async Task Register_ShouldProduce200Response_WhenRegistrationIsSuccesful()
    {
        // arrange
        var request = new RegistrationRequest(USERNAME, PASSWORD);
        var userService = new Mock<IUserService>();
        var tokenService = new Mock<ITokenService>();
        var wrapper = new Mock<IHttpContextAccessor>();
        User? user = new User(USERNAME, Encoding.UTF8.GetBytes(PASSWORD));
        userService.Setup(x => x.Register(request)).ReturnsAsync(user);
        var settings = new AppSettings(new string('a', 64), 300, 25200, false);
        var logger = new NullLogger<AuthController>();
        var controller = new AuthController(wrapper.Object, settings, userService.Object, tokenService.Object, logger);

        // act
        var result = await controller.Register(request);

        // assert
        Assert.IsType<OkResult>(result);
    }

    [Fact]
    public async Task Register_ShouldProduce409Response_WhenRegistrationFails()
    {
        // arrange
        var request = new RegistrationRequest(USERNAME, PASSWORD);
        var userService = new Mock<IUserService>();
        var tokenService = new Mock<ITokenService>();
        var wrapper = new Mock<IHttpContextAccessor>();
        User? user = null;
        userService.Setup(x => x.Register(request)).ReturnsAsync(user);
        var settings = new AppSettings(new string('a', 64), 300, 25200, false);
        var logger = new NullLogger<AuthController>();
        var controller = new AuthController(wrapper.Object, settings, userService.Object, tokenService.Object, logger);

        // act
        var result = await controller.Register(request);

        // assert
        Assert.IsType<ConflictResult>(result);
    }

    [Fact]
    public async Task Authenticate_ShouldProduce401Response_WhenNoTokens()
    {
        // arrange
        var flag = false;
        var request = new AuthenticationRequest(USERNAME, PASSWORD);
        var userService = new Mock<IUserService>();
        var tokenService = new Mock<ITokenService>();
        Tokens? tokens = null;
        tokenService.Setup(x => x.CreateTokens(request))
            .ReturnsAsync(tokens)
            .Callback(() => flag = true);
        var wrapper = new Mock<IHttpContextAccessor>();
        var context = new Mock<HttpContext>();
        wrapper.SetupProperty(x => x.HttpContext, context.Object);
        var settings = new AppSettings(new string('a', 64), 300, 25200, false);
        var logger = new NullLogger<AuthController>();
        var controller = new AuthController(wrapper.Object, settings, userService.Object, tokenService.Object, logger);

        // act
        var result = await controller.Authenticate(request);

        // assert
        Assert.IsType<UnauthorizedResult>(result);
        Assert.True(flag);
    }

    [Fact]
    public async Task Authenticate_ShouldProduce200Response_WhenEverythingIsOk()
    {
        // arrange
        var flag = false;
        var request = new AuthenticationRequest(USERNAME, PASSWORD);
        var userService = new Mock<IUserService>();
        var tokenService = new Mock<ITokenService>();
        Tokens? tokens = new Tokens
        (
            new Token("access", 0),
            new Token("resfresh", 1)
        );
        tokenService.Setup(x => x.CreateTokens(request))
            .ReturnsAsync(tokens);
        var wrapper = new Mock<IHttpContextAccessor>();
        var context = new Mock<HttpContext>();
        var response = new Mock<HttpResponse>();
        var resCookies = new Mock<IResponseCookies>();
        resCookies.Setup(x => x.Append("Bearer", tokens.RefreshToken.Value, It.IsAny<CookieOptions>()))
            .Callback(() => flag = true);
        response.SetupGet(x => x.Cookies)
            .Returns(resCookies.Object);
        context.SetupGet(x => x.Response)
            .Returns(response.Object);
        wrapper.SetupProperty(x => x.HttpContext, context.Object);
        var settings = new AppSettings(new string('a', 64), 300, 25200, false);
        var logger = new NullLogger<AuthController>();
        var controller = new AuthController(wrapper.Object, settings, userService.Object, tokenService.Object, logger);

        // act
        var result = await controller.Authenticate(request) as OkObjectResult;

        // assert
        Assert.IsType<OkObjectResult>(result);
        Assert.Equal(result?.Value, tokens.AccessToken);
        Assert.True(flag);
    }

    [Fact]
    public async Task Refresh_ShouldProduce401Response_WhenCookieIsMissing()
    {
        // arrange
        var flag = false;
        var userService = new Mock<IUserService>();
        var tokenService = new Mock<ITokenService>();
        var wrapper = new Mock<IHttpContextAccessor>();
        var context = new Mock<HttpContext>();
        var request = new Mock<HttpRequest>();
        var cookies = new Mock<IRequestCookieCollection>();
        string? cookie = null;
        cookies.Setup(x => x["Bearer"])
            .Returns(cookie)
            .Callback(() => flag = true);
        request.SetupGet(x => x.Cookies)
            .Returns(cookies.Object);
        context.SetupGet(x => x.Request)
            .Returns(request.Object);
        wrapper.SetupGet(x => x.HttpContext)
            .Returns(context.Object);
        var settings = new AppSettings(new string('a', 64), 300, 25200, false);
        var logger = new NullLogger<AuthController>();
        var controller = new AuthController(wrapper.Object, settings, userService.Object, tokenService.Object, logger);

        // act
        var result = await controller.Refresh();

        // assert
        Assert.IsType<UnauthorizedResult>(result);
        Assert.True(flag);
    }

    [Fact]
    public async Task Refresh_ShouldProduce401Response_WhenTokensWasNotCreated()
    {
        // arrange
        var flag = false;
        var userService = new Mock<IUserService>();
        var tokenService = new Mock<ITokenService>();
        var wrapper = new Mock<IHttpContextAccessor>();
        var context = new Mock<HttpContext>();
        var request = new Mock<HttpRequest>();
        var cookies = new Mock<IRequestCookieCollection>();
        string cookie = "cookie";
        cookies.Setup(x => x["Bearer"])
            .Returns(cookie);
        request.SetupGet(x => x.Cookies)
            .Returns(cookies.Object);
        context.SetupGet(x => x.Request)
            .Returns(request.Object);
        wrapper.SetupGet(x => x.HttpContext)
            .Returns(context.Object);
        Tokens? tokens = null;
        tokenService.Setup(x => x.RefreshTokens(cookie))
            .ReturnsAsync(tokens)
            .Callback(() => flag = true);
        var settings = new AppSettings(new string('a', 64), 300, 25200, false);
        var logger = new NullLogger<AuthController>();
        var controller = new AuthController(wrapper.Object, settings, userService.Object, tokenService.Object, logger);

        // act
        var result = await controller.Refresh();

        // assert
        Assert.IsType<UnauthorizedResult>(result);
        Assert.True(flag);
    }

    [Fact]
    public async Task Refresh_ShouldProduce200Response_WhenEverythingIsOkay()
    {
        // arrange
        var flag = false;
        var userService = new Mock<IUserService>();
        var tokenService = new Mock<ITokenService>();
        var wrapper = new Mock<IHttpContextAccessor>();
        var context = new Mock<HttpContext>();
        var request = new Mock<HttpRequest>();
        var response = new Mock<HttpResponse>();
        var reqCookies = new Mock<IRequestCookieCollection>();
        var resCookies = new Mock<IResponseCookies>();
        string cookie = "cookie";
        reqCookies.Setup(x => x["Bearer"])
            .Returns(cookie);
        resCookies.Setup(x => x.Append("Bearer", "bbb", It.IsAny<CookieOptions>()))
            .Callback(() => flag = true);
        request.SetupGet(x => x.Cookies)
            .Returns(reqCookies.Object);
        response.SetupGet(x => x.Cookies)
            .Returns(resCookies.Object);
        context.SetupGet(x => x.Request)
            .Returns(request.Object);
        context.SetupGet(x => x.Response)
            .Returns(response.Object);
        wrapper.SetupGet(x => x.HttpContext)
            .Returns(context.Object);
        Tokens? tokens = new Tokens
        (
            new Token("aaa", 11),
            new Token("bbb", 22)
        );
        tokenService.Setup(x => x.RefreshTokens(cookie))
            .ReturnsAsync(tokens);
        var settings = new AppSettings(new string('a', 64), 300, 25200, false);
        var logger = new NullLogger<AuthController>();
        var controller = new AuthController(wrapper.Object, settings, userService.Object, tokenService.Object, logger);

        // act
        var result = await controller.Refresh();
        var obj = result as OkObjectResult;

        // assert
        Assert.IsType<OkObjectResult>(result);
        Assert.Equal(obj?.Value, tokens.AccessToken);
        Assert.True(flag);
    }

    [Fact]
    public async Task Revoke_ShouldProduce401_WhenCookieIsMissing()
    {
        // arrange
        var flag = false;
        var userService = new Mock<IUserService>();
        var tokenService = new Mock<ITokenService>();
        var wrapper = new Mock<IHttpContextAccessor>();
        var context = new Mock<HttpContext>();
        var request = new Mock<HttpRequest>();
        var cookies = new Mock<IRequestCookieCollection>();
        string? cookie = null;
        cookies.Setup(x => x["Bearer"])
            .Returns(cookie)
            .Callback(() => flag = true);
        request.SetupGet(x => x.Cookies)
            .Returns(cookies.Object);
        context.SetupGet(x => x.Request)
            .Returns(request.Object);
        wrapper.SetupGet(x => x.HttpContext)
            .Returns(context.Object);
        var settings = new AppSettings(new string('a', 64), 300, 25200, false);
        var logger = new NullLogger<AuthController>();
        var controller = new AuthController(wrapper.Object, settings, userService.Object, tokenService.Object, logger);

        // act
        var result = await controller.Revoke();

        // assert
        Assert.IsType<UnauthorizedResult>(result);
        Assert.True(flag);
    }

    [Fact]
    public async Task Revoke_ShouldProduce404_WhenUserHasNoRefreshToken()
    {
        // arrange
        var flag = false;
        var userService = new Mock<IUserService>();
        var tokenService = new Mock<ITokenService>();
        var wrapper = new Mock<IHttpContextAccessor>();
        var context = new Mock<HttpContext>();
        var request = new Mock<HttpRequest>();
        var cookies = new Mock<IRequestCookieCollection>();
        string? cookie = "cookie";
        cookies.Setup(x => x["Bearer"])
            .Returns(cookie);
        request.SetupGet(x => x.Cookies)
            .Returns(cookies.Object);
        context.SetupGet(x => x.Request)
            .Returns(request.Object);
        wrapper.SetupGet(x => x.HttpContext)
            .Returns(context.Object);
        userService.Setup(x => x.RevokeToken(cookie))
            .ReturnsAsync(false)
            .Callback(() => flag = true);
        var settings = new AppSettings(new string('a', 64), 300, 25200, false);
        var logger = new NullLogger<AuthController>();
        var controller = new AuthController(wrapper.Object, settings, userService.Object, tokenService.Object, logger);

        // act
        var result = await controller.Revoke();

        // assert
        Assert.IsType<NotFoundResult>(result);
        Assert.True(flag);
    }

    [Fact]
    public async Task Revoke_ShouldProduce200_WhenEverythingIsOk()
    {
        // arrange
        var flag = false;
        var userService = new Mock<IUserService>();
        var tokenService = new Mock<ITokenService>();
        var wrapper = new Mock<IHttpContextAccessor>();
        var context = new Mock<HttpContext>();
        var request = new Mock<HttpRequest>();
        var cookies = new Mock<IRequestCookieCollection>();
        string? cookie = "cookie";
        cookies.Setup(x => x["Bearer"])
            .Returns(cookie);
        request.SetupGet(x => x.Cookies)
            .Returns(cookies.Object);
        context.SetupGet(x => x.Request)
            .Returns(request.Object);
        wrapper.SetupGet(x => x.HttpContext)
            .Returns(context.Object);
        userService.Setup(x => x.RevokeToken(cookie))
            .ReturnsAsync(true)
            .Callback(() => flag = true);
        var settings = new AppSettings(new string('a', 64), 300, 25200, false);
        var logger = new NullLogger<AuthController>();
        var controller = new AuthController(wrapper.Object, settings, userService.Object, tokenService.Object, logger);

        // act
        var result = await controller.Revoke();

        // assert
        Assert.IsType<OkResult>(result);
        Assert.True(flag);
    }
}
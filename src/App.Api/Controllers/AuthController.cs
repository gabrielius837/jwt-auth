
namespace App.Api.Controllers;

[ApiController]
[Route("auth")]
public class AuthController : ControllerBase
{
    const string BEARER = "Bearer";

    private readonly IHttpContextAccessor _accessor;
    private readonly AppSettings _settings;
    private readonly IUserService _userService;
    private readonly ITokenService _tokenService;
    private readonly ILogger<AuthController> _logger;

    public AuthController
    (
        IHttpContextAccessor accessor,
        AppSettings settings,
        IUserService userService,
        ITokenService tokenService,
        ILogger<AuthController> logger
    )
    {
        _accessor = accessor;
        _settings = settings;
        _userService = userService;
        _tokenService = tokenService;
        _logger = logger;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register(RegistrationRequest request)
    {
        var result = await _userService.Register(request);

        if (result is null)
        {
            _logger.LogInformation("Could not register {user}", request.Username);
            return Conflict();
        }

        _logger.LogInformation("{user} has been registered successfully", request.Username);
        return Ok();
    }

    [HttpPost]
    public async Task<IActionResult> Authenticate(AuthenticationRequest request)
    {
        var context = _accessor.HttpContext ?? throw new Exception("Context is missing");

        var tokens = await _tokenService.CreateTokens(request);
        if (tokens is null)
        {
            _logger.LogInformation("Could not authenticate {user}", request.Username);
            return Unauthorized();
        }

        var opt = new CookieOptions()
        {
            Expires = DateTimeOffset.FromUnixTimeSeconds(tokens.RefreshToken.Expires),
            Secure = _settings.SecureCookie,
            HttpOnly = true,
            IsEssential = true
        };
        context.Response.Cookies.Append(BEARER, tokens.RefreshToken.Value, opt);

        _logger.LogInformation("{user} has been authenticated successfully", request.Username);
        return Ok(tokens.AccessToken);
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh()
    {
        var context = _accessor.HttpContext ?? throw new Exception("Context is missing");

        var cookie = context.Request.Cookies[BEARER];
        if (cookie is null)
        {
            _logger.LogInformation("refresh token as not found in cookie");
            return Unauthorized();
        }

        var tokens = await _tokenService.RefreshTokens(cookie);
        if (tokens is null)
        {
            _logger.LogInformation("Could not refresh tokens");
            return Unauthorized();
        }

        var opt = new CookieOptions()
        {
            Expires = DateTimeOffset.FromUnixTimeSeconds(tokens.RefreshToken.Expires),
            Secure = _settings.SecureCookie,
            HttpOnly = true,
            IsEssential = true
        };
        context.Response.Cookies.Append(BEARER, tokens.RefreshToken.Value, opt);

        _logger.LogInformation("Tokens successfully refreshed");
        return Ok(tokens.AccessToken);
    }

    // Only relevant in junction with UseAuthentication middleware
    [Authorize]
    [HttpGet("check")]
    public IActionResult Check()
    {
        return Ok();
    }

    [HttpPost("revoke")]
    public async Task<IActionResult> Revoke()
    {
        var context = _accessor.HttpContext ?? throw new Exception("Context is missing");

        var cookie = context.Request.Cookies[BEARER];
        if (cookie is null)
        {
            _logger.LogInformation("refresh token as not found in cookie");
            return Unauthorized();
        }
        
        var result = await _userService.RevokeToken(cookie);
        return result ? Ok() : NotFound();
    }
}
namespace App.Api.Controllers;

[Authorize]
[ApiController]
public abstract class AuthorizedController: ControllerBase
{
    protected AuthorizedController() {}
}
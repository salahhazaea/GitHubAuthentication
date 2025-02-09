using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;

namespace GitHubOAuthApp.Controllers;

public class AuthController : ControllerBase
{
    [HttpGet("/Login")]
    public IResult Login()
    {
         
        return Results.Challenge(new AuthenticationProperties
                                {
                                    RedirectUri = "/",
                                }, ["GitHub"]);

    }
}


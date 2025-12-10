using Microsoft.AspNetCore.Mvc;

namespace PersonalMedia.Blazor.Controllers;

[Route("auth")]
public class AuthController : Controller
{
    private readonly IConfiguration _configuration;

    public AuthController(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    [HttpPost("login")]
    public IActionResult Login([FromForm] string code)
    {
        var validCode = _configuration["AppSettings:AccessCode"] ?? "1234";

        if (code == validCode)
        {
            HttpContext.Session.SetString("Authenticated", "true");
            return Redirect("/app");
        }

        return Redirect("/?error=invalid");
    }

    [HttpGet("logout")]
    public IActionResult Logout()
    {
        HttpContext.Session.Clear();
        return Redirect("/");
    }
}

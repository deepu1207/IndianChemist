using IndianChemist.Models;
using IndianChemist.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
namespace IndianChemist.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController(AuthService auth) : ControllerBase
{
    [HttpPost("register"), EnableRateLimiting("auth")]
    public Task<IActionResult> Register([FromBody] Register model) => auth.RegisterAsync(model, HttpContext);
    [HttpGet("verify")]
    public IActionResult Verify([FromQuery] string token) => auth.Verify(token);
    [HttpPost("login"), EnableRateLimiting("auth")]
    public IActionResult Login([FromBody] Login model) => auth.Login(model, HttpContext);
    [HttpPost("refresh")]
    public IActionResult Refresh() => auth.Refresh(HttpContext);
    [HttpPost("logout")]
    public IActionResult Logout() => auth.Logout(HttpContext);
}

using Application.DTOs.Auth;
using Application.ServiceInterfaces;
using Application.WrapperClass;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Todo.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[EnableRateLimiting("auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly IWebHostEnvironment _env;

    public AuthController(IAuthService authService, IWebHostEnvironment env)
    {
        _authService = authService;
        _env = env;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var result = await _authService.RegisterAsync(dto);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var result = await _authService.LoginAsync(dto);
        if (!result.Success)
            return BadRequest(result);

        // H1: Set token in HttpOnly cookie so it is inaccessible to JavaScript
        Response.Cookies.Append("ica_auth", result.Data!.Token, new CookieOptions
        {
            HttpOnly = true,
            Secure = !_env.IsDevelopment(), // true in production (HTTPS required)
            SameSite = SameSiteMode.Strict,
            Expires = result.Data.ExpiresAt
        });

        return Ok(result);
    }

    [HttpPost("logout")]
    public IActionResult Logout()
    {
        Response.Cookies.Delete("ica_auth");
        return Ok(ApiResponse<object?>.Ok(null, "Logged out."));
    }
}

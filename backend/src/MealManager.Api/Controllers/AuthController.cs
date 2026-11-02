using MealManager.Application.Features.Auth;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MealManager.Api.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController(ISender sender, IWebHostEnvironment environment) : ControllerBase
{
    public const string RefreshCookieName = "bmm_refresh";

    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<IActionResult> Register(RegisterCommand command)
    {
        var result = await sender.Send(command);
        SetRefreshCookie(result.RefreshToken);
        return StatusCode(201, new { result.AccessToken, result.User });
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login(LoginCommand command)
    {
        var result = await sender.Send(command);
        SetRefreshCookie(result.RefreshToken);
        return Ok(new { result.AccessToken, result.User });
    }

    [HttpPost("otp/request")]
    [AllowAnonymous]
    public async Task<ActionResult<OtpRequestResult>> RequestOtp(RequestOtpCommand command) =>
        Ok(await sender.Send(command));

    [HttpPost("otp/verify")]
    [AllowAnonymous]
    public async Task<IActionResult> VerifyOtp(VerifyOtpCommand command)
    {
        var result = await sender.Send(command);
        SetRefreshCookie(result.RefreshToken);
        return Ok(new { result.AccessToken, result.User });
    }

    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<IActionResult> Refresh()
    {
        var result = await sender.Send(new RefreshTokenCommand(Request.Cookies[RefreshCookieName]));
        SetRefreshCookie(result.RefreshToken);
        return Ok(new { result.AccessToken, result.User });
    }

    [HttpPost("logout")]
    [AllowAnonymous]
    public async Task<IActionResult> Logout()
    {
        await sender.Send(new LogoutCommand(Request.Cookies[RefreshCookieName]));
        Response.Cookies.Delete(RefreshCookieName, new CookieOptions { Path = "/api/auth" });
        return NoContent();
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<ActionResult<AuthUserDto>> Me() =>
        Ok(await sender.Send(new GetCurrentUserQuery()));

    // HttpOnly cookie is the only channel for the refresh token (skill bmm-app-router-multitenancy).
    private void SetRefreshCookie(string refreshToken) =>
        Response.Cookies.Append(RefreshCookieName, refreshToken, new CookieOptions
        {
            HttpOnly = true,
            Secure = !environment.IsDevelopment(),
            SameSite = SameSiteMode.Lax,
            Path = "/api/auth",
            MaxAge = TimeSpan.FromDays(7),
            IsEssential = true,
        });
}

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SubscriptionBilling.Application.DTOs.Auth;
using SubscriptionBilling.Application.Interfaces;

namespace SubscriptionBilling.API.Controllers;

public class AuthController : BaseApiController
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("register")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        var response = await _authService.RegisterAsync(request, ClientIpAddress);
        SetRefreshTokenCookie(response.RefreshToken, response.ExpiresAtUtc);
        return Ok(response);
    }

    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var response = await _authService.LoginAsync(request, ClientIpAddress);
        SetRefreshTokenCookie(response.RefreshToken, response.ExpiresAtUtc);
        return Ok(response);
    }

    [HttpPost("refresh-token")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest? request)
    {
        var token = request?.RefreshToken ?? Request.Cookies["refreshToken"];
        if (string.IsNullOrWhiteSpace(token))
        {
            return BadRequest(new { Message = "Refresh token is required." });
        }

        var response = await _authService.RefreshTokenAsync(token, ClientIpAddress);
        SetRefreshTokenCookie(response.RefreshToken, response.ExpiresAtUtc);
        return Ok(response);
    }

    [HttpPost("logout")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> Logout()
    {
        var token = Request.Cookies["refreshToken"];
        if (!string.IsNullOrEmpty(token))
        {
            await _authService.RevokeTokenAsync(token, ClientIpAddress);
        }

        Response.Cookies.Delete("refreshToken");
        return Ok(new { Message = "Logged out successfully." });
    }

    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(typeof(UserProfileDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetCurrentUser()
    {
        var profile = await _authService.GetCurrentUserProfileAsync(CurrentUserId);
        return Ok(profile);
    }

    private void SetRefreshTokenCookie(string token, DateTime expiresAtUtc)
    {
        var cookieOptions = new CookieOptions
        {
            HttpOnly = true,
            Expires = expiresAtUtc,
            SameSite = SameSiteMode.Lax,
            Secure = Request.IsHttps
        };

        Response.Cookies.Append("refreshToken", token, cookieOptions);
    }
}

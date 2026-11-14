using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;

namespace SubscriptionBilling.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public abstract class BaseApiController : ControllerBase
{
    protected int CurrentUserId
    {
        get
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(claim, out var id) ? id : 0;
        }
    }

    protected string CurrentUserEmail => User.FindFirst(ClaimTypes.Email)?.Value ?? string.Empty;

    protected string? ClientIpAddress =>
        HttpContext.Request.Headers.TryGetValue("X-Forwarded-For", out var ip)
            ? ip.ToString().Split(',').FirstOrDefault()?.Trim()
            : HttpContext.Connection.RemoteIpAddress?.ToString();
}

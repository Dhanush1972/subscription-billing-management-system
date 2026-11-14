using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SubscriptionBilling.Application.Common;
using SubscriptionBilling.Application.DTOs.Users;
using SubscriptionBilling.Application.Interfaces;

namespace SubscriptionBilling.API.Controllers;

[Authorize(Roles = "Admin")]
public class UsersController : BaseApiController
{
    private readonly IAuthService _authService;

    public UsersController(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<UserManagementDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllUsers([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
    {
        var result = await _authService.GetAllUsersAsync(pageNumber, pageSize);
        return Ok(result);
    }

    [HttpPatch("{id:int}/status")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateUserStatus(int id, [FromBody] UpdateUserStatusRequest request)
    {
        await _authService.UpdateUserStatusAsync(id, request.IsActive, CurrentUserId);
        return Ok(new { Message = $"User status updated to {(request.IsActive ? "Active" : "Inactive")}." });
    }
}

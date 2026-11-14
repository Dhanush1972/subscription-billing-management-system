using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SubscriptionBilling.Application.DTOs.Plans;
using SubscriptionBilling.Application.Interfaces;

namespace SubscriptionBilling.API.Controllers;

[Route("api/subscription-plans")]
[Route("api/plans")]
public class PlansController : BaseApiController
{
    private readonly ISubscriptionPlanService _planService;

    public PlansController(ISubscriptionPlanService planService)
    {
        _planService = planService;
    }

    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(typeof(IReadOnlyList<SubscriptionPlanDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetActivePlans()
    {
        var plans = await _planService.GetActivePlansAsync();
        return Ok(plans);
    }

    [HttpGet("admin")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(IReadOnlyList<SubscriptionPlanDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllPlansForAdmin()
    {
        var plans = await _planService.GetAllPlansForAdminAsync();
        return Ok(plans);
    }

    [HttpGet("{id:int}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(SubscriptionPlanDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPlanById(int id)
    {
        var plan = await _planService.GetPlanByIdAsync(id);
        return Ok(plan);
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(SubscriptionPlanDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreatePlan([FromBody] CreatePlanRequest request)
    {
        var created = await _planService.CreatePlanAsync(request, CurrentUserId);
        return CreatedAtAction(nameof(GetPlanById), new { id = created.Id }, created);
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(SubscriptionPlanDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdatePlan(int id, [FromBody] UpdatePlanRequest request)
    {
        var updated = await _planService.UpdatePlanAsync(id, request, CurrentUserId);
        return Ok(updated);
    }

    [HttpPatch("{id:int}/status")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ToggleStatus(int id, [FromBody] bool isActive)
    {
        await _planService.TogglePlanStatusAsync(id, isActive, CurrentUserId);
        return Ok(new { Message = $"Plan status updated to {(isActive ? "Active" : "Inactive")}." });
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeletePlan(int id)
    {
        await _planService.DeletePlanAsync(id, CurrentUserId);
        return Ok(new { Message = "Plan deleted or deactivated successfully." });
    }
}

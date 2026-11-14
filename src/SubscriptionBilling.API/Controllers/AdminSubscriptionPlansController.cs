using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SubscriptionBilling.Application.DTOs.Plans;
using SubscriptionBilling.Application.Interfaces;

namespace SubscriptionBilling.API.Controllers;

[Route("api/admin/subscription-plans")]
[Authorize(Roles = "Admin")]
public class AdminSubscriptionPlansController : BaseApiController
{
    private readonly ISubscriptionPlanService _planService;

    public AdminSubscriptionPlansController(ISubscriptionPlanService planService)
    {
        _planService = planService;
    }

    [HttpPost]
    [ProducesResponseType(typeof(SubscriptionPlanDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreatePlan([FromBody] CreatePlanRequest request)
    {
        var created = await _planService.CreatePlanAsync(request, CurrentUserId);
        return CreatedAtAction(nameof(PlansController.GetPlanById), "Plans", new { id = created.Id }, created);
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(SubscriptionPlanDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdatePlan(int id, [FromBody] UpdatePlanRequest request)
    {
        var updated = await _planService.UpdatePlanAsync(id, request, CurrentUserId);
        return Ok(updated);
    }

    [HttpPatch("{id:int}/status")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ToggleStatus(int id, [FromBody] bool isActive)
    {
        await _planService.TogglePlanStatusAsync(id, isActive, CurrentUserId);
        return Ok(new { Message = $"Plan status updated to {(isActive ? "Active" : "Inactive")}." });
    }
}

using FoodEmolite.Application.DTOs.Revenue;
using FoodEmolite.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FoodEmolite.API.Controllers;

[ApiController]
[Authorize]
[Route("api/revenue")]
public class RevenueController : BaseApiController
{
    private readonly IRevenueService _revenueService;

    public RevenueController(IRevenueService revenueService)
    {
        _revenueService = revenueService;
    }

    [HttpGet("admin")]
    public async Task<IActionResult> GetAdminRevenue([FromQuery] RevenueQueryDto request)
    {
        var result = await _revenueService.GetAdminRevenueAsync(request);
        return Ok(result);
    }

    [HttpGet("agent")]
    public async Task<IActionResult> GetAgentRevenue([FromQuery] RevenueQueryDto request)
    {
        var result = await _revenueService.GetAgentRevenueAsync(
            CurrentUserId!.Value,
            request);

        return Ok(result);
    }
}
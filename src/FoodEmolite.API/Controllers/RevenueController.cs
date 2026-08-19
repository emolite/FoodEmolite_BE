using FoodEmolite.Application.DTOs.Revenue;
using FoodEmolite.Application.Interfaces;
using FoodEmolite.Shared.Entities;
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

    [HttpGet("agent/top-products")]
    public async Task<IActionResult> GetAgentTopProducts([FromQuery] RevenueQueryDto request, [FromQuery] int top = 10)
    {
        var result = await _revenueService.GetAgentTopSellingProductsAsync(CurrentUserId!.Value, request, top);
        return Ok(result);
    }

    [HttpPost("agent/products/search")]
    public async Task<IActionResult> SearchAgentProductRevenue([FromBody] BaseSearchRequest<ProductRevenueSearchRequest> request)
    {
        var result = await _revenueService.GetAgentProductRevenueAsync(CurrentUserId!.Value, request);
        return Ok(result);
    }

    [HttpGet("admin/top-products")]
    public async Task<IActionResult> GetAdminTopProducts([FromQuery] RevenueQueryDto request, [FromQuery] string? storeRefCode, [FromQuery] int top = 10)
    {
        var result = await _revenueService.GetAdminTopSellingProductsAsync(request, storeRefCode, top);
        return Ok(result);
    }

    [HttpPost("admin/products/search")]
    public async Task<IActionResult> SearchAdminProductRevenue([FromBody] BaseSearchRequest<ProductRevenueSearchRequest> request)
    {
        var result = await _revenueService.GetAdminProductRevenueAsync(request);
        return Ok(result);
    }
}
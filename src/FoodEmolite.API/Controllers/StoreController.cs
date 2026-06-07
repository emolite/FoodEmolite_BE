using FoodEmolite.Application.DTOs.Store;
using FoodEmolite.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FoodEmolite.API.Controllers;

[ApiController]
[Authorize]
[Route("api/stores")]
public class StoreController : BaseApiController
{
    private readonly IStoreService _storeService;

    public StoreController(IStoreService storeService)
    {
        _storeService = storeService;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromForm] CreateStoreRequestDto request)
    {
        var result = await _storeService.CreateAsync(
            CurrentUserId!.Value,
            request);

        return Ok(result);
    }

    [HttpPut("{refCode}")]
    public async Task<IActionResult> Update(
        string refCode,
        [FromForm] UpdateStoreRequestDto request)
    {
        var result = await _storeService.UpdateAsync(
            refCode,
            CurrentUserId!.Value,
            request);

        return Ok(result);
    }

    [HttpDelete("{refCode}")]
    public async Task<IActionResult> Delete(string refCode)
    {
        var result = await _storeService.DeleteAsync(
            refCode,
            CurrentUserId!.Value);

        return Ok(result);
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        var result = await _storeService.GetAllAsync(page, pageSize);
        return Ok(result);
    }

    [HttpGet("owner/{ownerRefCode}")]
    public async Task<IActionResult> GetByOwnerRefCode(
        string ownerRefCode,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        var result = await _storeService.GetByOwnerRefCodeAsync(
            ownerRefCode,
            page,
            pageSize);

        return Ok(result);
    }

    [HttpGet("{refCode}")]
    public async Task<IActionResult> GetDetail(string refCode)
    {
        var result = await _storeService.GetDetailAsync(refCode);
        return Ok(result);
    }
}
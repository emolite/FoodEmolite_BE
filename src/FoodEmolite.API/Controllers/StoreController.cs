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

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(
        long id,
        [FromForm] UpdateStoreRequestDto request)
    {
        var result = await _storeService.UpdateAsync(
            id,
            CurrentUserId!.Value,
            request);

        return Ok(result);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(long id)
    {
        var result = await _storeService.DeleteAsync(
            id,
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

    [HttpGet("{id}")]
    public async Task<IActionResult> GetDetail(long id)
    {
        var result = await _storeService.GetDetailAsync(id);
        return Ok(result);
    }
}
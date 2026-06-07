using FoodEmolite.Application.DTOs.StoreFood;
using FoodEmolite.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FoodEmolite.API.Controllers;

[ApiController]
[Authorize]
[Route("api/store-foods")]
public class StoreFoodController : BaseApiController
{
    private readonly IStoreFoodService _storeFoodService;

    public StoreFoodController(IStoreFoodService storeFoodService)
    {
        _storeFoodService = storeFoodService;
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateStoreFoodRequestDto request)
    {
        var result = await _storeFoodService.CreateAsync(request);
        return Ok(result);
    }

    [HttpPut("{refCode}")]
    public async Task<IActionResult> Update(string refCode, UpdateStoreFoodRequestDto request)
    {
        var result = await _storeFoodService.UpdateAsync(refCode, request);
        return Ok(result);
    }

    [HttpDelete("{refCode}")]
    public async Task<IActionResult> Delete(string refCode)
    {
        var result = await _storeFoodService.DeleteAsync(refCode);
        return Ok(result);
    }

    [HttpGet("{refCode}")]
    public async Task<IActionResult> GetDetail(string refCode)
    {
        var result = await _storeFoodService.GetDetailAsync(refCode);
        return Ok(result);
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        var result = await _storeFoodService.GetAllAsync(page, pageSize);
        return Ok(result);
    }

    [HttpGet("store/{storeRefCode}")]
    public async Task<IActionResult> GetByStoreRefCode(
        string storeRefCode,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        var result = await _storeFoodService.GetByStoreRefCodeAsync(
            storeRefCode,
            page,
            pageSize);

        return Ok(result);
    }
}
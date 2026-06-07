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
        var result = await _storeFoodService.CreateAsync(CurrentUserRefCode, request);
        return Ok(result);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(long id, UpdateStoreFoodRequestDto request)
    {
        var result = await _storeFoodService.UpdateAsync(id, request);
        return Ok(result);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(long id)
    {
        var result = await _storeFoodService.DeleteAsync(id);
        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetDetail(long id)
    {
        var result = await _storeFoodService.GetDetailAsync(id);
        return Ok(result);
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] int page     = 1, [FromQuery] int pageSize = 10)
    {
        var result = await _storeFoodService.GetAllAsync(page, pageSize);
        return Ok(result);
    }

    [HttpGet("store/{storeRefCode}")]
    public async Task<IActionResult> GetByStoreRefCode(string storeRefCode, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        var result = await _storeFoodService.GetByStoreRefCodeAsync(storeRefCode, page, pageSize);

        return Ok(result);
    }
}
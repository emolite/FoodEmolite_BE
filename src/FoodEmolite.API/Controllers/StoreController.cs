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

    [AllowAnonymous]
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        var result = await _storeService.GetAllAsync(page, pageSize);
        return Ok(result);
    }

    [AllowAnonymous]
    [HttpGet("ref/{refCode}")]
    public async Task<IActionResult> GetByRefCode(string refCode)
    {
        var result = await _storeService.GetByRefCodeAsync(refCode);
        return Ok(result);
    }
}
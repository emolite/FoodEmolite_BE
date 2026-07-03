using FoodEmolite.Application.DTOs.StoreFoodCategories;
using FoodEmolite.Application.Interfaces;
using FoodEmolite.Shared.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FoodEmolite.API.Controllers;

[ApiController]
[Route("api/store-food-categories")]
public class StoreFoodCategoriesController : BaseApiController
{
    private readonly IStoreFoodCategoriesService _service;

    public StoreFoodCategoriesController(IStoreFoodCategoriesService service)
    {
        _service = service;
    }

    [AllowAnonymous]
    [HttpGet("{storeRefCode}")]
    public async Task<IActionResult> GetByStoreRefCode(
    string storeRefCode)
    {
        var result = await _service.GetByStoreRefCodeAsync(storeRefCode);
        return Ok(result);
    }

    [HttpPost("search")]
    public async Task<IActionResult> Search([FromBody]BaseSearchRequest<StoreFoodCategorySearchRequest> request)
    {
        var result = await _service.SearchAsync(CurrentUserId!.Value, request);
        return Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateStoreFoodCategoryRequest request)
    {
        var result = await _service.CreateAsync( CurrentUserId!.Value, CurrentUserRefCode, request);
        return Ok(result);
    }

    [HttpPut]
    public async Task<IActionResult> Update([FromBody] UpdateStoreFoodCategoryRequest request)
    {
        var result = await _service.UpdateAsync(CurrentUserId!.Value, request);

        return Ok(result);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(long id)
    {
        var result = await _service.DeleteAsync(CurrentUserId!.Value, id);

        return Ok(result);
    }
}
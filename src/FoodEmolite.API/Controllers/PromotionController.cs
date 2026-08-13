using FoodEmolite.Application.DTOs.Promotion;
using FoodEmolite.Application.Interfaces;
using FoodEmolite.Shared.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FoodEmolite.API.Controllers;

[ApiController]
[Authorize]
[Route("api/promotions")]
public class PromotionController : BaseApiController
{
    private readonly IPromotionService _promotionService;

    public PromotionController(IPromotionService promotionService)
    {
        _promotionService = promotionService;
    }

    [HttpPost("search")]
    public async Task<IActionResult> Search([FromBody] BaseSearchRequest<PromotionSearchRequest> request)
    {
        var result = await _promotionService.GetByStoreRefCodeAsync(CurrentUserId!.Value, request);

        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetDetail(long id)
    {
        var result = await _promotionService.GetDetailAsync(CurrentUserId!.Value, id);

        return Ok(result);
    }

    [AllowAnonymous]
    [HttpGet("store/{storeRefCode}/active")]
    public async Task<IActionResult> GetActiveByStore(string storeRefCode)
    {
        var result = await _promotionService.GetActiveByStoreRefCodeAsync(storeRefCode);

        return Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreatePromotionRequestDto request)
    {
        var result = await _promotionService.CreateAsync(CurrentUserId!.Value, CurrentUserRefCode!, request);

        return Ok(result);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(long id, [FromBody] CreatePromotionRequestDto request)
    {
        var result = await _promotionService.UpdateAsync(CurrentUserId!.Value, id, request);

        return Ok(result);
    }

    [HttpPut("{id}/pause")]
    public async Task<IActionResult> Pause(long id)
    {
        var result = await _promotionService.PauseAsync(CurrentUserId!.Value, id);

        return Ok(result);
    }

    [HttpPut("{id}/resume")]
    public async Task<IActionResult> Resume(long id)
    {
        var result = await _promotionService.ResumeAsync(CurrentUserId!.Value, id);

        return Ok(result);
    }

    [HttpPut("{id}/cancel")]
    public async Task<IActionResult> Cancel(long id)
    {
        var result = await _promotionService.CancelAsync(CurrentUserId!.Value, id);

        return Ok(result);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(long id)
    {
        var result = await _promotionService.DeleteAsync(CurrentUserId!.Value, id);

        return Ok(result);
    }
}

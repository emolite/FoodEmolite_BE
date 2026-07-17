using FoodEmolite.Application.DTOs.Order;
using FoodEmolite.Application.Interfaces;
using FoodEmolite.Shared.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FoodEmolite.API.Controllers;

[ApiController]
[Authorize]
[Route("api/orders")]
public class OrderController : BaseApiController
{
    private readonly IOrderService _orderService;

    public OrderController(IOrderService orderService)
    {
        _orderService = orderService;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateOrderRequestDto request)
    {
        var result = await _orderService.CreateAsync(CurrentUserId!.Value, CurrentUserRefCode , request);

        return Ok(result);
    }

    [AllowAnonymous]
    [HttpPost("guest")]
    public async Task<IActionResult> CreateGuest([FromBody] CreateGuestOrderRequestDto request)
    {
        var result = await _orderService.CreateGuestAsync(request);

        return Ok(result);
    }

    [HttpGet("my")]
    public async Task<IActionResult> GetMyOrders(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        var result = await _orderService.GetMyOrdersAsync(
            CurrentUserId!.Value,
            page,
            pageSize);

        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetDetail(long id)
    {
        var result = await _orderService.GetDetailAsync(
            id,
            CurrentUserId!.Value);

        return Ok(result);
    }

    [HttpPost("store/search")]
    public async Task<IActionResult> GetByStoreRefCode([FromBody] BaseSearchRequest<OrderSearchRequest> request)
    {
        var result = await _orderService.GetByStoreRefCodeAsync(request);
        return Ok(result);
    }

    [HttpPut("{id}/status")]
    public async Task<IActionResult> UpdateStatus(long id, [FromBody] UpdateOrderStatusRequestDto request)
    {
        var result = await _orderService.UpdateStatusAsync(
            id,
            CurrentUserId!.Value, CurrentUserRefCode,
            request);

        return Ok(result);
    }

    [HttpPut("{id}/payment-status")]
    public async Task<IActionResult> UpdatePaymentStatus(long id, [FromBody] UpdatePaymentStatusRequestDto request)
    {

        var response = await _orderService.UpdatePaymentStatusAsync(
            id,
            CurrentUserId!.Value,
            CurrentUserRefCode,
            request
        );

        return Ok(response);
    }

    [HttpPut("{id}/cancel")]
    public async Task<IActionResult> CancelOrders(long id)
    {
        var result = await _orderService.CancelAsync(id, CurrentUserId.Value, CurrentUserRefCode);
        return Ok(result);
    }

    [HttpPost("print")]
    public async Task<IActionResult> PrintOrders([FromBody] PrintOrdersRequestDto request)
    {
        var result = await _orderService.PrintOrdersAsync(CurrentUserId!.Value, request);

        if (!result.IsSuccess) return Ok(result);

        return File(
            result.Data!,
            "application/pdf",
            $"orders-{DateTime.Now:yyyyMMddHHmmss}.pdf"
        );
    }

    [AllowAnonymous]
    [HttpGet("{orderCode}/payment-status")]
    public async Task<IActionResult> GetPaymentStatus(string orderCode)
    {
        var result = await _orderService.GetPaymentStatusAsync(orderCode);
        return Ok(result);
    }

    [AllowAnonymous]
    [HttpGet("pending-order")]
    public async Task<IActionResult> CheckPendingOrder([FromQuery] string deviceId)
    {
        var result = await _orderService.CheckPendingOrderAsync(deviceId);

        return Ok(result);
    }
}
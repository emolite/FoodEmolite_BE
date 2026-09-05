using FoodEmolite.Application.DTOs.Auth;
using FoodEmolite.Application.DTOs.Customer;
using FoodEmolite.Application.DTOs.Revenue;
using FoodEmolite.Application.DTOs.Store;
using FoodEmolite.Application.Interfaces;
using FoodEmolite.Shared.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FoodEmolite.API.Controllers;

/// <summary>
/// Toàn bộ API quản trị (cửa hàng, tài khoản, doanh thu, khách hàng, món ăn) gom về 1 chỗ,
/// tách khỏi các controller dùng chung với agent/user. KHÔNG yêu cầu đăng nhập — theo yêu cầu,
/// dự định dùng cho 1 hệ thống/công cụ nội bộ khác gọi trực tiếp, không qua FE admin (đã bỏ).
/// Vì không có JWT nên không có CurrentUserId/CurrentUserRefCode — các field audit (CreatedBy...)
/// dùng giá trị mặc định, không mang ý nghĩa "người gọi API".
/// </summary>
[ApiController]
[AllowAnonymous]
[Route("api/admin")]
public class AdminController : ControllerBase
{
    private readonly IStoreService _storeService;
    private readonly IProfileService _profileService;
    private readonly IAuthService _authService;
    private readonly IRevenueService _revenueService;
    private readonly ICustomerService _customerService;
    private readonly IStoreFoodService _storeFoodService;

    public AdminController(
        IStoreService storeService,
        IProfileService profileService,
        IAuthService authService,
        IRevenueService revenueService,
        ICustomerService customerService,
        IStoreFoodService storeFoodService)
    {
        _storeService = storeService;
        _profileService = profileService;
        _authService = authService;
        _revenueService = revenueService;
        _customerService = customerService;
        _storeFoodService = storeFoodService;
    }

    // ===================== Stores =====================

    [HttpPost("stores")]
    public async Task<IActionResult> CreateStore([FromForm] CreateStoreRequestDto request)
    {
        var result = await _storeService.CreateAsync(0, request);
        return Ok(result);
    }

    [HttpPut("stores/{id}")]
    public async Task<IActionResult> UpdateStore(long id, [FromForm] UpdateStoreRequestDto request)
    {
        var result = await _storeService.UpdateAsync(id, 0, request);
        return Ok(result);
    }

    [HttpDelete("stores/{id}")]
    public async Task<IActionResult> DeleteStore(long id)
    {
        var result = await _storeService.DeleteAsync(id, 0);
        return Ok(result);
    }

    [HttpGet("stores")]
    public async Task<IActionResult> GetAllStores([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        var result = await _storeService.GetAllAsync(page, pageSize);
        return Ok(result);
    }

    [HttpGet("stores/{id}")]
    public async Task<IActionResult> GetStoreDetail(long id)
    {
        var result = await _storeService.GetDetailAsync(id);
        return Ok(result);
    }

    [HttpGet("stores/owner/{ownerRefCode}")]
    public async Task<IActionResult> GetStoresByOwner(
        string ownerRefCode,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        var result = await _storeService.GetByOwnerRefCodeAsync(ownerRefCode, page, pageSize);
        return Ok(result);
    }

    // ===================== Accounts =====================

    [HttpGet("users")]
    public async Task<IActionResult> GetAllUsers([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        var result = await _profileService.GetAllAccountProfilesAsync(page, pageSize);
        return Ok(result);
    }

    [HttpGet("agents")]
    public async Task<IActionResult> GetAllAgents([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        var result = await _profileService.GetAllAgentProfilesAsync(page, pageSize);
        return Ok(result);
    }

    [HttpPost("agents")]
    public async Task<IActionResult> CreateAgent(RegisterRequest request)
    {
        var result = await _authService.CreateAgent(request, Guid.NewGuid().ToString().ToUpper());
        return Ok(result);
    }

    // ===================== Revenue =====================

    [HttpGet("revenue")]
    public async Task<IActionResult> GetRevenue([FromQuery] RevenueQueryDto request)
    {
        var result = await _revenueService.GetAdminRevenueAsync(request);
        return Ok(result);
    }

    [HttpGet("revenue/top-products")]
    public async Task<IActionResult> GetTopProducts(
        [FromQuery] RevenueQueryDto request,
        [FromQuery] string? storeRefCode,
        [FromQuery] int top = 10)
    {
        var result = await _revenueService.GetAdminTopSellingProductsAsync(request, storeRefCode, top);
        return Ok(result);
    }

    [HttpPost("revenue/products/search")]
    public async Task<IActionResult> SearchProductRevenue([FromBody] BaseSearchRequest<ProductRevenueSearchRequest> request)
    {
        var result = await _revenueService.GetAdminProductRevenueAsync(request);
        return Ok(result);
    }

    // ===================== Customers =====================

    [HttpPost("customers/search")]
    public async Task<IActionResult> SearchCustomers([FromBody] BaseSearchRequest<CustomerSearchRequest> request)
    {
        var result = await _customerService.GetAdminCustomersAsync(request);
        return Ok(result);
    }

    // ===================== Store foods =====================

    [HttpGet("store-foods")]
    public async Task<IActionResult> GetAllStoreFoods([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        var result = await _storeFoodService.GetAllAsync(page, pageSize);
        return Ok(result);
    }
}

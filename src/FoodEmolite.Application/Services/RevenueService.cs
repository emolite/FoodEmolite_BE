using FoodEmolite.Application.DTOs.Revenue;
using FoodEmolite.Application.ExternalService.Interfaces;
using FoodEmolite.Application.Interfaces;
using FoodEmolite.Domain.Entities;
using FoodEmolite.Domain.Interfaces;
using FoodEmolite.Shared.Entities;
using FoodEmolite.Shared.Responses;
using Microsoft.EntityFrameworkCore;

namespace FoodEmolite.Application.Services;

public class RevenueService : IRevenueService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICloudinaryService _cloudinaryService;

    public RevenueService(IUnitOfWork unitOfWork, ICloudinaryService cloudinaryService)
    {
        _unitOfWork = unitOfWork;
        _cloudinaryService = cloudinaryService;
    }

    public async Task<BaseResponse<AdminRevenueResponseDto>> GetAdminRevenueAsync(RevenueQueryDto request)
    {
        var repoAccount = _unitOfWork.GetRepository<Account>();
        var repoOrder = _unitOfWork.GetRepository<Order>();

        var fromDate = request.FromDate?.Date;
        var toDate = request.ToDate?.Date.AddDays(1).AddTicks(-1);
        var groupBy = request.GroupBy?.ToLower() == "month" ? "month" : "day";

        var orderQuery = repoOrder
            .Query()
            .AsNoTracking();

        if (fromDate.HasValue)
        {
            orderQuery = orderQuery.Where(x => x.CreatedAt >= fromDate.Value);
        }

        if (toDate.HasValue)
        {
            orderQuery = orderQuery.Where(x => x.CreatedAt <= toDate.Value);
        }

        var paidOrderQuery = orderQuery
            .Where(x => x.PaymentStatus == "PAID");

        var totalAgents = await repoAccount
            .Query()
            .AsNoTracking()
            .CountAsync(x =>
                !x.IsDeleted &&
                x.Role == "Agent");

        var totalUsers = await repoAccount
            .Query()
            .AsNoTracking()
            .CountAsync(x =>
                !x.IsDeleted &&
                x.Role == "User");

        var totalOrders = await orderQuery.CountAsync();

        var totalRevenue = await paidOrderQuery
            .SumAsync(x => (decimal?)x.TotalAmount) ?? 0;

        var paidOrders = await paidOrderQuery
            .Select(x => new
            {
                x.CreatedAt,
                x.TotalAmount,
                x.OrderStatus
            })
            .ToListAsync();

        var lineChart = BuildLineChart(
            paidOrders.Select(x => new RevenueRawItem
            {
                CreatedAt = x.CreatedAt,
                Amount = x.TotalAmount
            }).ToList(),
            groupBy);

        var pieChart = paidOrders
            .GroupBy(x => x.OrderStatus)
            .Select(g => new RevenuePieChartDto
            {
                Label = g.Key,
                Value = g.Sum(x => x.TotalAmount)
            })
            .ToList();

        return BaseResponse<AdminRevenueResponseDto>.Success(
            new AdminRevenueResponseDto
            {
                TotalAgents = totalAgents,
                TotalUsers = totalUsers,
                TotalOrders = totalOrders,
                TotalRevenue = totalRevenue,
                LineChart = lineChart,
                PieChart = pieChart
            });
    }

    public async Task<BaseResponse<AgentRevenueResponseDto>> GetAgentRevenueAsync(long currentUserId, RevenueQueryDto request)
    {
        var repoStore = _unitOfWork.GetRepository<Store>();
        var repoOrder = _unitOfWork.GetRepository<Order>();

        var fromDate = request.FromDate?.Date;
        var toDate = request.ToDate?.Date.AddDays(1).AddTicks(-1);
        var groupBy = request.GroupBy?.ToLower() == "month" ? "month" : "day";

        var store = await repoStore.FirstOrDefaultAsync(x =>
            x.OwnerAccountId == currentUserId &&
            !x.IsDeleted);

        if (store is null)
        {
            return BaseResponse<AgentRevenueResponseDto>.Fail("Store not found");
        }

        var orderQuery = repoOrder
            .Query()
            .AsNoTracking()
            .Where(x =>
                x.StoreRefCode == store.RefCode);

        if (fromDate.HasValue)
        {
            orderQuery = orderQuery.Where(x => x.CreatedAt >= fromDate.Value);
        }

        if (toDate.HasValue)
        {
            orderQuery = orderQuery.Where(x => x.CreatedAt <= toDate.Value);
        }

        var allOrders = await orderQuery
            .Select(x => new
            {
                x.OrderStatus,
                x.TotalAmount
            })
            .ToListAsync();

        var paidOrderQuery = orderQuery
            .Where(x => x.PaymentStatus == "PAID");

        var totalOrders = await orderQuery.CountAsync();

        var totalRevenue = await paidOrderQuery
            .SumAsync(x => (decimal?)x.TotalAmount) ?? 0;

        var paidOrders = await paidOrderQuery
            .Select(x => new
            {
                x.CreatedAt,
                x.TotalAmount,
                x.OrderStatus
            })
            .ToListAsync();

        var lineChart = BuildLineChart(
            paidOrders.Select(x => new RevenueRawItem
            {
                CreatedAt = x.CreatedAt,
                Amount = x.TotalAmount
            }).ToList(),
            groupBy);

        var pieChart = allOrders
            .GroupBy(x => x.OrderStatus)
            .Select(g => new RevenuePieChartDto
            {
                Label = g.Key,
                Value = g.Sum(x => x.TotalAmount)
            })
            .ToList();

        return BaseResponse<AgentRevenueResponseDto>.Success(
            new AgentRevenueResponseDto
            {
                TotalOrders = totalOrders,
                TotalRevenue = totalRevenue,
                LineChart = lineChart,
                PieChart = pieChart
            });
    }

    public async Task<BaseResponse<List<TopSellingProductDto>>> GetAgentTopSellingProductsAsync(long currentUserId, RevenueQueryDto request, int top = 10)
    {
        var repoStore = _unitOfWork.GetRepository<Store>();

        var store = await repoStore.FirstOrDefaultAsync(x =>
            x.OwnerAccountId == currentUserId &&
            !x.IsDeleted);

        if (store is null)
            return BaseResponse<List<TopSellingProductDto>>.Fail("Store not found");

        var fromDate = request.FromDate?.Date;
        var toDate = request.ToDate?.Date.AddDays(1).AddTicks(-1);

        var products = await BuildProductRevenueAsync(store.RefCode, fromDate, toDate);

        var topProducts = products
            .OrderByDescending(x => x.QuantitySold)
            .Take(top <= 0 ? 10 : top)
            .ToList();

        return BaseResponse<List<TopSellingProductDto>>.Success(topProducts);
    }

    public async Task<BaseTableResponse<TopSellingProductDto>> GetAgentProductRevenueAsync(long currentUserId, BaseSearchRequest<ProductRevenueSearchRequest> request)
    {
        var repoStore = _unitOfWork.GetRepository<Store>();

        var store = await repoStore.FirstOrDefaultAsync(x =>
            x.OwnerAccountId == currentUserId &&
            !x.IsDeleted);

        if (store is null)
        {
            return new BaseTableResponse<TopSellingProductDto>
            {
                Items = [],
                Page = request.Page,
                PageSize = request.PageSize,
                TotalRecords = 0
            };
        }

        var search = request.SearchParams;
        var fromDate = search?.FromDate?.Date;
        var toDate = search?.ToDate?.Date.AddDays(1).AddTicks(-1);

        var products = await BuildProductRevenueAsync(store.RefCode, fromDate, toDate);

        IEnumerable<TopSellingProductDto> filtered = products;

        if (!string.IsNullOrWhiteSpace(search?.Keyword))
        {
            var keyword = search.Keyword.Trim().ToLower();
            filtered = filtered.Where(x => x.FoodName.ToLower().Contains(keyword));
        }

        filtered = request.SortBy?.ToLower() switch
        {
            "quantitysold" => request.Asc
                ? filtered.OrderBy(x => x.QuantitySold)
                : filtered.OrderByDescending(x => x.QuantitySold),

            "foodname" => request.Asc
                ? filtered.OrderBy(x => x.FoodName)
                : filtered.OrderByDescending(x => x.FoodName),

            _ => request.Asc
                ? filtered.OrderBy(x => x.Revenue)
                : filtered.OrderByDescending(x => x.Revenue)
        };

        var materialized = filtered.ToList();
        var totalRecords = materialized.Count;

        var pageItems = materialized
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToList();

        return new BaseTableResponse<TopSellingProductDto>
        {
            Items = pageItems,
            Page = request.Page,
            PageSize = request.PageSize,
            TotalRecords = totalRecords
        };
    }

    public async Task<BaseResponse<List<TopSellingProductDto>>> GetAdminTopSellingProductsAsync(RevenueQueryDto request, string? storeRefCode, int top = 10)
    {
        var fromDate = request.FromDate?.Date;
        var toDate = request.ToDate?.Date.AddDays(1).AddTicks(-1);

        var products = await BuildProductRevenueAsync(storeRefCode, fromDate, toDate);

        var topProducts = products
            .OrderByDescending(x => x.QuantitySold)
            .Take(top <= 0 ? 10 : top)
            .ToList();

        return BaseResponse<List<TopSellingProductDto>>.Success(topProducts);
    }

    public async Task<BaseTableResponse<TopSellingProductDto>> GetAdminProductRevenueAsync(BaseSearchRequest<ProductRevenueSearchRequest> request)
    {
        var search = request.SearchParams;
        var fromDate = search?.FromDate?.Date;
        var toDate = search?.ToDate?.Date.AddDays(1).AddTicks(-1);

        var products = await BuildProductRevenueAsync(search?.StoreRefCode, fromDate, toDate);

        IEnumerable<TopSellingProductDto> filtered = products;

        if (!string.IsNullOrWhiteSpace(search?.Keyword))
        {
            var keyword = search.Keyword.Trim().ToLower();
            filtered = filtered.Where(x => x.FoodName.ToLower().Contains(keyword));
        }

        filtered = request.SortBy?.ToLower() switch
        {
            "quantitysold" => request.Asc
                ? filtered.OrderBy(x => x.QuantitySold)
                : filtered.OrderByDescending(x => x.QuantitySold),

            "foodname" => request.Asc
                ? filtered.OrderBy(x => x.FoodName)
                : filtered.OrderByDescending(x => x.FoodName),

            "storename" => request.Asc
                ? filtered.OrderBy(x => x.StoreName)
                : filtered.OrderByDescending(x => x.StoreName),

            _ => request.Asc
                ? filtered.OrderBy(x => x.Revenue)
                : filtered.OrderByDescending(x => x.Revenue)
        };

        var materialized = filtered.ToList();
        var totalRecords = materialized.Count;

        var pageItems = materialized
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToList();

        return new BaseTableResponse<TopSellingProductDto>
        {
            Items = pageItems,
            Page = request.Page,
            PageSize = request.PageSize,
            TotalRecords = totalRecords
        };
    }

    /// <summary>
    /// Gộp OrderItem theo món cho 1 cửa hàng trong khoảng ngày, chỉ tính đơn PaymentStatus = PAID.
    /// Dùng chung cho top-selling-products và bảng doanh thu theo sản phẩm.
    /// </summary>
    private async Task<List<TopSellingProductDto>> BuildProductRevenueAsync(string? storeRefCode, DateTime? fromDate, DateTime? toDate)
    {
        var repoOrder = _unitOfWork.GetRepository<Order>();
        var repoOrderItem = _unitOfWork.GetRepository<OrderItem>();
        var repoStoreFood = _unitOfWork.GetRepository<StoreFood>();
        var repoStore = _unitOfWork.GetRepository<Store>();

        var orderQuery = repoOrder
            .Query()
            .AsNoTracking()
            .Where(x => x.PaymentStatus == "PAID");

        if (!string.IsNullOrWhiteSpace(storeRefCode))
        {
            orderQuery = orderQuery.Where(x => x.StoreRefCode == storeRefCode);
        }

        if (fromDate.HasValue)
        {
            orderQuery = orderQuery.Where(x => x.CreatedAt >= fromDate.Value);
        }

        if (toDate.HasValue)
        {
            orderQuery = orderQuery.Where(x => x.CreatedAt <= toDate.Value);
        }

        var items = await (
            from oi in repoOrderItem.Query().AsNoTracking()
            join o in orderQuery on oi.OrderId equals o.Id
            join f in repoStoreFood.Query().AsNoTracking() on oi.StoreFoodId equals f.Id
            join s in repoStore.Query().AsNoTracking() on o.StoreRefCode equals s.RefCode
            select new
            {
                oi.StoreFoodId,
                f.FoodName,
                f.ThumbnailUrl,
                oi.Quantity,
                oi.TotalPrice,
                StoreRefCode = o.StoreRefCode,
                s.StoreName
            }
        ).ToListAsync();

        return items
            .GroupBy(x => new { x.StoreFoodId, x.FoodName, x.ThumbnailUrl, x.StoreRefCode, x.StoreName })
            .Select(g => new TopSellingProductDto
            {
                StoreFoodId = g.Key.StoreFoodId,
                FoodName = g.Key.FoodName,
                ThumbnailUrl = !string.IsNullOrWhiteSpace(g.Key.ThumbnailUrl)
                    ? _cloudinaryService.BuildImageUrl(g.Key.ThumbnailUrl)
                    : null,
                QuantitySold = g.Sum(x => x.Quantity),
                Revenue = g.Sum(x => x.TotalPrice),
                StoreRefCode = g.Key.StoreRefCode,
                StoreName = g.Key.StoreName
            })
            .ToList();
    }

    private static List<RevenueLineChartDto> BuildLineChart(
        List<RevenueRawItem> items, 
        string groupBy)
    {
        if (groupBy == "month")
        {
            return items
                .GroupBy(x => new
                {
                    x.CreatedAt.Year,
                    x.CreatedAt.Month
                })
                .OrderBy(g => g.Key.Year)
                .ThenBy(g => g.Key.Month)
                .Select(g => new RevenueLineChartDto
                {
                    Label = $"{g.Key.Month:00}/{g.Key.Year}",
                    Revenue = g.Sum(x => x.Amount),
                    OrderCount = g.Count()
                })
                .ToList();
        }

        return items
            .GroupBy(x => x.CreatedAt.Date)
            .OrderBy(g => g.Key)
            .Select(g => new RevenueLineChartDto
            {
                Label = g.Key.ToString("dd/MM/yyyy"),
                Revenue = g.Sum(x => x.Amount),
                OrderCount = g.Count()
            })
            .ToList();
    }

    private class RevenueRawItem
    {
        public DateTime CreatedAt { get; set; }
        public decimal Amount { get; set; }
    }
}
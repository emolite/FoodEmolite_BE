using FoodEmolite.Application.DTOs.Revenue;
using FoodEmolite.Application.Interfaces;
using FoodEmolite.Domain.Entities;
using FoodEmolite.Domain.Interfaces;
using FoodEmolite.Shared.Responses;
using Microsoft.EntityFrameworkCore;

namespace FoodEmolite.Application.Services;

public class RevenueService : IRevenueService
{
    private readonly IUnitOfWork _unitOfWork;

    public RevenueService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
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

        var pieChart = paidOrders
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
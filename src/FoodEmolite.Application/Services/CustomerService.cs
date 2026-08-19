using FoodEmolite.Application.DTOs.Customer;
using FoodEmolite.Application.ExternalService.Interfaces;
using FoodEmolite.Application.Interfaces;
using FoodEmolite.Domain.Entities;
using FoodEmolite.Domain.Interfaces;
using FoodEmolite.Shared.Entities;
using FoodEmolite.Shared.Responses;
using Microsoft.EntityFrameworkCore;

namespace FoodEmolite.Application.Services;

public class CustomerService : ICustomerService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICloudinaryService _cloudinaryService;

    public CustomerService(IUnitOfWork unitOfWork, ICloudinaryService cloudinaryService)
    {
        _unitOfWork = unitOfWork;
        _cloudinaryService = cloudinaryService;
    }

    public async Task<BaseTableResponse<CustomerListItemDto>> GetAgentCustomersAsync(long currentUserId, BaseSearchRequest<CustomerSearchRequest> request)
    {
        var repoStore = _unitOfWork.GetRepository<Store>();

        var store = await repoStore.FirstOrDefaultAsync(x =>
            x.OwnerAccountId == currentUserId &&
            !x.IsDeleted);

        if (store is null)
        {
            return new BaseTableResponse<CustomerListItemDto>
            {
                Items = [],
                Page = request.Page,
                PageSize = request.PageSize,
                TotalRecords = 0
            };
        }

        var customers = await BuildCustomersAsync(store.RefCode);

        return FilterSortPaginate(customers, request);
    }

    public async Task<BaseTableResponse<CustomerListItemDto>> GetAdminCustomersAsync(BaseSearchRequest<CustomerSearchRequest> request)
    {
        var customers = await BuildCustomersAsync(request.SearchParams?.StoreRefCode);

        return FilterSortPaginate(customers, request);
    }

    /// <summary>
    /// Gộp khách hàng (tài khoản đã đăng ký + khách vãng lai) theo từng cửa hàng, dựa trên lịch sử đơn hàng.
    /// </summary>
    private async Task<List<CustomerListItemDto>> BuildCustomersAsync(string? storeRefCode)
    {
        var repoOrder = _unitOfWork.GetRepository<Order>();
        var repoStore = _unitOfWork.GetRepository<Store>();
        var repoAccount = _unitOfWork.GetRepository<Account>();
        var repoProfile = _unitOfWork.GetRepository<AccountProfile>();
        var repoCustomer = _unitOfWork.GetRepository<Customer>();

        var orderQuery = repoOrder.Query().AsNoTracking();

        if (!string.IsNullOrWhiteSpace(storeRefCode))
        {
            orderQuery = orderQuery.Where(x => x.StoreRefCode == storeRefCode);
        }

        var orders = await (
            from o in orderQuery
            join s in repoStore.Query().AsNoTracking() on o.StoreRefCode equals s.RefCode
            select new
            {
                o.CustomerAccountId,
                o.CustomerId,
                StoreRefCode = o.StoreRefCode,
                s.StoreName,
                o.TotalAmount,
                o.PaymentStatus,
                o.CreatedAt
            }
        ).ToListAsync();

        var result = new List<CustomerListItemDto>();

        // Khách đã đăng ký tài khoản
        var registeredGroups = orders
            .Where(x => x.CustomerAccountId.HasValue)
            .GroupBy(x => new { AccountId = x.CustomerAccountId!.Value, x.StoreRefCode, x.StoreName })
            .ToList();

        if (registeredGroups.Count > 0)
        {
            var accountIds = registeredGroups.Select(g => g.Key.AccountId).Distinct().ToList();

            var accounts = await repoAccount.Query().AsNoTracking()
                .Where(x => accountIds.Contains(x.Id))
                .ToListAsync();

            var profiles = await repoProfile.Query().AsNoTracking()
                .Where(x => accountIds.Contains(x.AccountId))
                .ToListAsync();

            foreach (var group in registeredGroups)
            {
                var account = accounts.FirstOrDefault(x => x.Id == group.Key.AccountId);
                var profile = profiles.FirstOrDefault(x => x.AccountId == group.Key.AccountId);

                result.Add(new CustomerListItemDto
                {
                    RefCode = account?.RefCode ?? string.Empty,
                    CustomerName = !string.IsNullOrWhiteSpace(profile?.FullName) ? profile!.FullName : (account?.Username ?? "Khách hàng"),
                    PhoneNumber = profile?.PhoneNumber,
                    Email = account?.Email,
                    AvatarUrl = !string.IsNullOrWhiteSpace(profile?.AvatarUrl)
                        ? _cloudinaryService.BuildImageUrl(profile!.AvatarUrl!)
                        : null,
                    IsGuest = false,
                    TotalOrders = group.Count(),
                    TotalSpent = group.Where(x => x.PaymentStatus == "PAID").Sum(x => x.TotalAmount),
                    LastOrderAt = group.Max(x => x.CreatedAt),
                    StoreRefCode = group.Key.StoreRefCode,
                    StoreName = group.Key.StoreName
                });
            }
        }

        // Khách vãng lai
        var guestGroups = orders
            .Where(x => !x.CustomerAccountId.HasValue && x.CustomerId.HasValue)
            .GroupBy(x => new { CustomerId = x.CustomerId!.Value, x.StoreRefCode, x.StoreName })
            .ToList();

        if (guestGroups.Count > 0)
        {
            var customerIds = guestGroups.Select(g => g.Key.CustomerId).Distinct().ToList();

            var customers = await repoCustomer.Query().AsNoTracking()
                .Where(x => customerIds.Contains(x.Id))
                .ToListAsync();

            foreach (var group in guestGroups)
            {
                var customer = customers.FirstOrDefault(x => x.Id == group.Key.CustomerId);

                result.Add(new CustomerListItemDto
                {
                    RefCode = customer?.RefCode ?? string.Empty,
                    CustomerName = customer?.CustomerName ?? "Khách vãng lai",
                    PhoneNumber = null,
                    Email = null,
                    AvatarUrl = null,
                    IsGuest = true,
                    TotalOrders = group.Count(),
                    TotalSpent = group.Where(x => x.PaymentStatus == "PAID").Sum(x => x.TotalAmount),
                    LastOrderAt = group.Max(x => x.CreatedAt),
                    StoreRefCode = group.Key.StoreRefCode,
                    StoreName = group.Key.StoreName
                });
            }
        }

        return result;
    }

    private static BaseTableResponse<CustomerListItemDto> FilterSortPaginate(
        List<CustomerListItemDto> customers,
        BaseSearchRequest<CustomerSearchRequest> request)
    {
        IEnumerable<CustomerListItemDto> filtered = customers;

        var keyword = request.SearchParams?.Keyword?.Trim();

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var lowerKeyword = keyword.ToLower();

            filtered = filtered.Where(x =>
                x.CustomerName.ToLower().Contains(lowerKeyword) ||
                (x.PhoneNumber != null && x.PhoneNumber.Contains(lowerKeyword)) ||
                (x.Email != null && x.Email.ToLower().Contains(lowerKeyword)));
        }

        filtered = request.SortBy?.ToLower() switch
        {
            "totalorders" => request.Asc
                ? filtered.OrderBy(x => x.TotalOrders)
                : filtered.OrderByDescending(x => x.TotalOrders),

            "customername" => request.Asc
                ? filtered.OrderBy(x => x.CustomerName)
                : filtered.OrderByDescending(x => x.CustomerName),

            "lastorderat" => request.Asc
                ? filtered.OrderBy(x => x.LastOrderAt)
                : filtered.OrderByDescending(x => x.LastOrderAt),

            _ => request.Asc
                ? filtered.OrderBy(x => x.TotalSpent)
                : filtered.OrderByDescending(x => x.TotalSpent)
        };

        var materialized = filtered.ToList();
        var totalRecords = materialized.Count;

        var pageItems = materialized
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToList();

        return new BaseTableResponse<CustomerListItemDto>
        {
            Items = pageItems,
            Page = request.Page,
            PageSize = request.PageSize,
            TotalRecords = totalRecords
        };
    }
}

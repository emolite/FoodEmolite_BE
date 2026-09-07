using FoodEmolite.Shared.Common;
using FoodEmolite.Application.DTOs.Order;
using FoodEmolite.Application.DTOs.Print;
using FoodEmolite.Application.DTOs.Promotion;
using FoodEmolite.Application.DTOs.Realtime;
using FoodEmolite.Application.Interfaces;
using FoodEmolite.Domain.Entities;
using FoodEmolite.Domain.Interfaces;
using FoodEmolite.Shared.Entities;
using FoodEmolite.Shared.Responses;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace FoodEmolite.Application.Services;

public class OrderService : IOrderService
{
    private readonly IUnitOfWork _unitOfWork;
    private static readonly HttpClient _httpClient = new HttpClient();
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IRealtimeNotificationService _realtimeNotificationService;
    private readonly IPromotionService _promotionService;
    private readonly IActivityLogService _activityLogService;

    public OrderService(
        IUnitOfWork unitOfWork,
        IHttpContextAccessor httpContextAccessor,
        IRealtimeNotificationService realtimeNotificationService,
        IPromotionService promotionService,
        IActivityLogService activityLogService)
    {
        _unitOfWork = unitOfWork;
        _httpContextAccessor = httpContextAccessor;
        _realtimeNotificationService = realtimeNotificationService;
        _promotionService = promotionService;
        _activityLogService = activityLogService;
    }

    public async Task<BaseResponse<CreateOrderResponseDto>> CreateAsync(long currentUserId, string refCode, CreateOrderRequestDto request)
    {
        var repoStore = _unitOfWork.GetRepository<Store>();
        var repoFood = _unitOfWork.GetRepository<StoreFood>();
        var repoOrder = _unitOfWork.GetRepository<Order>();
        var repoOrderItem = _unitOfWork.GetRepository<OrderItem>();
        var repoOrderItemOption = _unitOfWork.GetRepository<OrderItemOption>();
        var repoOrderHistory = _unitOfWork.GetRepository<OrderHistory>();

        if (request.Items == null || !request.Items.Any())
            return BaseResponse<CreateOrderResponseDto>.Fail("Order item is required");

        if (request.Items.Any(x => x.Quantity <= 0))
            return BaseResponse<CreateOrderResponseDto>.Fail("Quantity must be greater than 0");

        var store = await repoStore.FirstOrDefaultAsync(x =>
            x.RefCode == request.StoreRefCode &&
            x.IsActive &&
            !x.IsDeleted);

        if (store is null)
            return BaseResponse<CreateOrderResponseDto>.Fail("Store not found");

        var storeFoodIds = request.Items
            .Select(x => x.StoreFoodId)
            .Distinct()
            .ToList();

        var foods = await repoFood
            .Query()
            .Where(x =>
                storeFoodIds.Contains(x.Id) &&
                x.StoreRefCode == request.StoreRefCode &&
                x.IsAvailable &&
                !x.IsDeleted)
            .ToListAsync();

        if (foods.Count != storeFoodIds.Count)
            return BaseResponse<CreateOrderResponseDto>.Fail("Food not found");

        var requiredQuantities = request.Items.GroupBy(x => x.StoreFoodId).ToDictionary(g => g.Key, g => g.Sum(x => x.Quantity));

        foreach (var food in foods)
        {
            var requiredQty = requiredQuantities[food.Id];

            if (food.Quantity < requiredQty)
                return BaseResponse<CreateOrderResponseDto>.Fail($"Món \"{food.FoodName}\" không đủ số lượng");
        }

        var (promotionContext, promoCodeError) = await LoadPromotionContextAsync(request.StoreRefCode, request.PromoCode);

        if (promotionContext is null)
            return BaseResponse<CreateOrderResponseDto>.Fail(promoCodeError ?? "Không tải được thông tin khuyến mãi");

        var (storeWideDiscountOverrides, storeWideDiscountError) = await ValidateSelectedStoreWideDiscountsAsync(
            promotionContext, request.SelectedStoreWideDiscounts, foods, currentUserId, null);

        if (storeWideDiscountError != null)
            return BaseResponse<CreateOrderResponseDto>.Fail(storeWideDiscountError);

        promotionContext.StoreWideDiscountOverrides = storeWideDiscountOverrides;

        decimal totalAmount = 0;

        foreach (var item in request.Items)
        {
            var food = foods.First(x => x.Id == item.StoreFoodId);
            var optionAmount = item.Options?.Sum(x => x.AdditionalPrice) ?? 0;

            totalAmount += ComputeLineTotal(food, item.Quantity, optionAmount, promotionContext);
        }

        var totalQuantity = request.Items.Sum(x => x.Quantity);

        var (selectedGifts, giftError) = ValidateSelectedGifts(promotionContext, request.SelectedGifts, totalAmount, totalQuantity);

        if (giftError != null)
            return BaseResponse<CreateOrderResponseDto>.Fail(giftError);

        var giftFoodIds = selectedGifts.Select(x => x.StoreFoodId).Distinct().ToList();

        var giftFoods = await repoFood
            .Query()
            .Where(x => giftFoodIds.Contains(x.Id) && x.StoreRefCode == request.StoreRefCode && x.IsAvailable && !x.IsDeleted)
            .ToListAsync();

        if (giftFoods.Count != giftFoodIds.Count)
            return BaseResponse<CreateOrderResponseDto>.Fail("Có món quà tặng không khả dụng");

        foreach (var gift in selectedGifts)
        {
            var giftFood = giftFoods.First(x => x.Id == gift.StoreFoodId);

            if (giftFood.Quantity < gift.Quantity)
                return BaseResponse<CreateOrderResponseDto>.Fail($"Món quà tặng \"{giftFood.FoodName}\" không đủ số lượng");
        }

        foreach (var food in foods)
        {
            food.Quantity -= requiredQuantities[food.Id];
            repoFood.Update(food);
        }

        foreach (var gift in selectedGifts)
        {
            var giftFood = giftFoods.First(x => x.Id == gift.StoreFoodId);
            giftFood.Quantity -= gift.Quantity;
            repoFood.Update(giftFood);
        }

        await _unitOfWork.SaveChangesAsync();

        var order = new Order
        {
            OrderCode = GenerateOrderCode(),
            RefCode = refCode,
            CustomerAccountId = currentUserId,
            StoreRefCode = request.StoreRefCode,
            TotalAmount = totalAmount,
            OrderStatus = "PENDING",
            PaymentStatus = totalAmount <= 0 ? "PAID" : "UNPAID",
            Note = request.Note,
            CreatedAt = DateTimeHelper.VnNow,
            CreatedBy = currentUserId
        };

        await repoOrder.AddAsync(order);
        await _unitOfWork.SaveChangesAsync();

        foreach (var item in request.Items)
        {
            var food = foods.First(x => x.Id == item.StoreFoodId);

            await AddOrderItemsForLineAsync(
                repoOrderItem, repoOrderItemOption, refCode, order.Id, food, item.Quantity, item.Options, promotionContext, currentUserId);
        }

        foreach (var gift in selectedGifts)
        {
            var giftFood = giftFoods.First(x => x.Id == gift.StoreFoodId);

            await repoOrderItem.AddAsync(new OrderItem
            {
                RefCode = refCode,
                OrderId = order.Id,
                StoreFoodId = giftFood.Id,
                Quantity = gift.Quantity,
                UnitPrice = 0,
                TotalPrice = 0,
                CreatedAt = DateTimeHelper.VnNow,
                CreatedBy = currentUserId
            });

            await _unitOfWork.SaveChangesAsync();
        }

        await repoOrderHistory.AddAsync(new OrderHistory
        {
            RefCode = refCode,
            OrderId = order.Id,
            OldStatus = null,
            NewStatus = order.OrderStatus,
            ChangedNote = order.Note,
            CreatedAt = DateTimeHelper.VnNow,
            CreatedBy = currentUserId
        });

        await _unitOfWork.SaveChangesAsync();

        await BroadcastFoodQuantitiesAsync(request.StoreRefCode, foods.Concat(giftFoods));

        await SavePromotionRedemptionsAsync(request.SelectedStoreWideDiscounts, promotionContext, order.Id, currentUserId, null);

        var customerDisplayName = await GetCustomerDisplayNameAsync(currentUserId);

        await BroadcastNewOrderAsync(order, customerDisplayName);

        await _activityLogService.LogAsync(
            "Customer",
            currentUserId,
            customerDisplayName,
            "CREATE_ORDER",
            $"Tạo đơn hàng \"{order.OrderCode}\" tại cửa hàng \"{store.StoreName}\", tổng tiền {order.TotalAmount:N0}đ");

        return BaseResponse<CreateOrderResponseDto>.Success(
            new CreateOrderResponseDto
            {
                OrderId = order.Id,
                OrderCode = order.OrderCode,
                PaymentStatus = order.PaymentStatus,
                TotalAmount = order.TotalAmount
            });
    }

    public async Task<BaseResponse<CreateOrderResponseDto>> CreateGuestAsync(CreateGuestOrderRequestDto request)
    {
        var repoStore = _unitOfWork.GetRepository<Store>();
        var repoFood = _unitOfWork.GetRepository<StoreFood>();
        var repoOrder = _unitOfWork.GetRepository<Order>();
        var repoOrderItem = _unitOfWork.GetRepository<OrderItem>();
        var repoOrderItemOption = _unitOfWork.GetRepository<OrderItemOption>();
        var repoOrderHistory = _unitOfWork.GetRepository<OrderHistory>();
        var repoCustomer = _unitOfWork.GetRepository<Customer>();

        if (string.IsNullOrWhiteSpace(request.CustomerName))
            return BaseResponse<CreateOrderResponseDto>.Fail("Customer name is required");

        if (request.Items == null || !request.Items.Any())
            return BaseResponse<CreateOrderResponseDto>.Fail("Order item is required");

        if (request.Items.Any(x => x.Quantity <= 0))
            return BaseResponse<CreateOrderResponseDto>.Fail("Quantity must be greater than 0");

        var store = await repoStore.FirstOrDefaultAsync(x =>
            x.RefCode == request.StoreRefCode &&
            x.IsActive &&
            !x.IsDeleted);

        if (store is null)
            return BaseResponse<CreateOrderResponseDto>.Fail("Store not found");

        var storeFoodIds = request.Items
            .Select(x => x.StoreFoodId)
            .Distinct()
            .ToList();

        var foods = await repoFood
            .Query()
            .Where(x =>
                storeFoodIds.Contains(x.Id) &&
                x.StoreRefCode == request.StoreRefCode &&
                x.IsAvailable &&
                !x.IsDeleted)
            .ToListAsync();

        if (foods.Count != storeFoodIds.Count)
            return BaseResponse<CreateOrderResponseDto>.Fail("Food not found");

        var requiredQuantities = request.Items.GroupBy(x => x.StoreFoodId).ToDictionary(g => g.Key, g => g.Sum(x => x.Quantity));

        foreach (var food in foods)
        {
            var requiredQty = requiredQuantities[food.Id];

            if (food.Quantity < requiredQty)
                return BaseResponse<CreateOrderResponseDto>.Fail($"Món \"{food.FoodName}\" không đủ số lượng");
        }

        // Lấy trước Customer (nếu có) theo deviceId để dùng kiểm tra đã dùng khuyến mãi
        // "áp dụng toàn bộ sản phẩm" (ApplyToAllProducts) hay chưa — khách vãng lai mới (customer =
        // null) coi như chưa từng dùng, sẽ được tạo record Customer bên dưới sau khi đơn được duyệt.
        Customer customer = null;

        if (!string.IsNullOrWhiteSpace(request.DeviceId))
        {
            customer = await repoCustomer.FirstOrDefaultAsync(x => x.DeviceId == request.DeviceId);
        }

        var (promotionContext, promoCodeError) = await LoadPromotionContextAsync(request.StoreRefCode, request.PromoCode);

        if (promotionContext is null)
            return BaseResponse<CreateOrderResponseDto>.Fail(promoCodeError ?? "Không tải được thông tin khuyến mãi");

        var (storeWideDiscountOverrides, storeWideDiscountError) = await ValidateSelectedStoreWideDiscountsAsync(
            promotionContext, request.SelectedStoreWideDiscounts, foods, null, customer?.Id);

        if (storeWideDiscountError != null)
            return BaseResponse<CreateOrderResponseDto>.Fail(storeWideDiscountError);

        promotionContext.StoreWideDiscountOverrides = storeWideDiscountOverrides;

        decimal totalAmount = 0;

        foreach (var item in request.Items)
        {
            var food = foods.First(x => x.Id == item.StoreFoodId);
            var optionAmount = item.Options?.Sum(x => x.AdditionalPrice) ?? 0;

            totalAmount += ComputeLineTotal(food, item.Quantity, optionAmount, promotionContext);
        }

        var totalQuantity = request.Items.Sum(x => x.Quantity);

        var (selectedGifts, giftError) = ValidateSelectedGifts(promotionContext, request.SelectedGifts, totalAmount, totalQuantity);

        if (giftError != null)
            return BaseResponse<CreateOrderResponseDto>.Fail(giftError);

        var giftFoodIds = selectedGifts.Select(x => x.StoreFoodId).Distinct().ToList();

        var giftFoods = await repoFood
            .Query()
            .Where(x => giftFoodIds.Contains(x.Id) && x.StoreRefCode == request.StoreRefCode && x.IsAvailable && !x.IsDeleted)
            .ToListAsync();

        if (giftFoods.Count != giftFoodIds.Count)
            return BaseResponse<CreateOrderResponseDto>.Fail("Có món quà tặng không khả dụng");

        foreach (var gift in selectedGifts)
        {
            var giftFood = giftFoods.First(x => x.Id == gift.StoreFoodId);

            if (giftFood.Quantity < gift.Quantity)
                return BaseResponse<CreateOrderResponseDto>.Fail($"Món quà tặng \"{giftFood.FoodName}\" không đủ số lượng");
        }

        foreach (var food in foods)
        {
            food.Quantity -= requiredQuantities[food.Id];
            repoFood.Update(food);
        }

        foreach (var gift in selectedGifts)
        {
            var giftFood = giftFoods.First(x => x.Id == gift.StoreFoodId);
            giftFood.Quantity -= gift.Quantity;
            repoFood.Update(giftFood);
        }

        await _unitOfWork.SaveChangesAsync();

        if (customer is null)
        {
            customer = new Customer
            {
                RefCode = Guid.NewGuid().ToString().ToUpper(),
                CustomerCode = GenerateCustomerCode(),
                CustomerName = request.CustomerName.Trim(),
                DeviceId = request.DeviceId,
                CreatedAt = DateTimeHelper.VnNow
            };

            await repoCustomer.AddAsync(customer);
            await _unitOfWork.SaveChangesAsync();
        }
        else
        {
            customer.CustomerName = request.CustomerName.Trim();

            repoCustomer.Update(customer);
            await _unitOfWork.SaveChangesAsync();
        }

        var refCode = customer.RefCode;

        var order = new Order
        {
            OrderCode = GenerateOrderCode(),
            RefCode = refCode,
            CustomerAccountId = null,
            CustomerId = customer.Id,
            StoreRefCode = request.StoreRefCode,
            TotalAmount = totalAmount,
            OrderStatus = "PENDING",
            PaymentStatus = totalAmount <= 0 ? "PAID" : "UNPAID",
            Note = request.Note,
            CreatedAt = DateTimeHelper.VnNow,
            CreatedBy = null,
            IpAddress = GetClientIp()
        };

        await repoOrder.AddAsync(order);
        await _unitOfWork.SaveChangesAsync();

        foreach (var item in request.Items)
        {
            var food = foods.First(x => x.Id == item.StoreFoodId);

            await AddOrderItemsForLineAsync(
                repoOrderItem, repoOrderItemOption, refCode, order.Id, food, item.Quantity, item.Options, promotionContext, null);
        }

        foreach (var gift in selectedGifts)
        {
            var giftFood = giftFoods.First(x => x.Id == gift.StoreFoodId);

            await repoOrderItem.AddAsync(new OrderItem
            {
                RefCode = refCode,
                OrderId = order.Id,
                StoreFoodId = giftFood.Id,
                Quantity = gift.Quantity,
                UnitPrice = 0,
                TotalPrice = 0,
                CreatedAt = DateTimeHelper.VnNow,
                CreatedBy = null
            });

            await _unitOfWork.SaveChangesAsync();
        }

        await repoOrderHistory.AddAsync(new OrderHistory
        {
            RefCode = refCode,
            OrderId = order.Id,
            OldStatus = null,
            NewStatus = order.OrderStatus,
            ChangedNote = order.Note,
            CreatedAt = DateTimeHelper.VnNow,
            CreatedBy = null
        });

        await _unitOfWork.SaveChangesAsync();

        await BroadcastFoodQuantitiesAsync(request.StoreRefCode, foods.Concat(giftFoods));

        await SavePromotionRedemptionsAsync(request.SelectedStoreWideDiscounts, promotionContext, order.Id, null, customer.Id);

        await BroadcastNewOrderAsync(order, customer.CustomerName);

        await _activityLogService.LogAsync(
            "Guest",
            customer.Id,
            customer.CustomerName,
            "CREATE_ORDER",
            $"Khách vãng lai tạo đơn hàng \"{order.OrderCode}\" tại cửa hàng \"{store.StoreName}\", tổng tiền {order.TotalAmount:N0}đ");

        return BaseResponse<CreateOrderResponseDto>.Success(
            new CreateOrderResponseDto
            {
                OrderId = order.Id,
                OrderCode = order.OrderCode,
                PaymentStatus = order.PaymentStatus,
                TotalAmount = order.TotalAmount
            });
    }

    public async Task<BaseTableResponse<OrderResponseDto>> GetMyOrdersAsync(long currentUserId, int page, int pageSize)
    {
        var repoOrder = _unitOfWork.GetRepository<Order>();
        var repoOrderItem = _unitOfWork.GetRepository<OrderItem>();
        var repoFood = _unitOfWork.GetRepository<StoreFood>();
        var repoAccount = _unitOfWork.GetRepository<Account>();
        var repoAccountProfile = _unitOfWork.GetRepository<AccountProfile>();

        page = page <= 0 ? 1 : page;
        pageSize = pageSize <= 0 ? 10 : pageSize;

        var query = repoOrder
            .Query()
            .AsNoTracking()
            .Where(x => x.CustomerAccountId == currentUserId);

        var totalRecords = await query.CountAsync();

        var items = await (
             from order in query

             join account in repoAccount.Query().AsNoTracking()
                 on order.CustomerAccountId equals account.Id

             join profile in repoAccountProfile.Query().AsNoTracking()
                 on account.Id equals profile.AccountId into profileGroup

             from profile in profileGroup.DefaultIfEmpty()

             orderby order.Id descending

             select new OrderResponseDto
             {
                 Id = order.Id,
                 OrderCode = order.OrderCode,
                 RefCode = order.RefCode,
                 CustomerAccountId = (long)order.CustomerAccountId,

                 CustomerName =
                     profile != null && !string.IsNullOrEmpty(profile.FullName)
                         ? profile.FullName
                         : account.Username,

                 StoreRefCode = order.StoreRefCode,
                 TotalAmount = order.TotalAmount,
                 OrderStatus = order.OrderStatus,
                 PaymentStatus = order.PaymentStatus,
                 Note = order.Note,
                 CreatedAt = order.CreatedAt,
                 Items = new List<OrderItemResponseDto>()
             })
             .Skip((page - 1) * pageSize)
             .Take(pageSize)
             .ToListAsync();

        var orderIds = items
            .Select(x => x.Id)
            .ToList();

        var orderItems = await (
            from orderItem in repoOrderItem.Query().AsNoTracking()
            join food in repoFood.Query().AsNoTracking()
                on orderItem.StoreFoodId equals food.Id
            where orderIds.Contains(orderItem.OrderId)
            select new OrderItemResponseDto
            {
                Id = orderItem.Id,
                OrderId = orderItem.OrderId,
                StoreFoodId = orderItem.StoreFoodId,
                FoodName = food.FoodName,
                Quantity = orderItem.Quantity,
                UnitPrice = orderItem.UnitPrice,
                TotalPrice = orderItem.TotalPrice
            })
            .ToListAsync();
        await FillOrderItemOptionsAsync(orderItems);
        foreach (var order in items)
        {
            order.Items = orderItems
                .Where(x => x.OrderId == order.Id)
                .ToList();
        }

        return new BaseTableResponse<OrderResponseDto>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalRecords = totalRecords
        };
    }

    public async Task<BaseResponse<OrderResponseDto>> GetDetailAsync(long id, long currentUserId)
    {
        var repoOrder = _unitOfWork.GetRepository<Order>();
        var repoOrderItem = _unitOfWork.GetRepository<OrderItem>();
        var repoFood = _unitOfWork.GetRepository<StoreFood>();

        var order = await repoOrder.FirstOrDefaultAsync(x =>
            x.Id == id &&
            x.CustomerAccountId == currentUserId);

        if (order is null)
            return BaseResponse<OrderResponseDto>.Fail("Order not found");

        var items = await (
            from orderItem in repoOrderItem.Query().AsNoTracking()
            join food in repoFood.Query().AsNoTracking()
                on orderItem.StoreFoodId equals food.Id
            where orderItem.OrderId == order.Id
            select new OrderItemResponseDto
            {
                Id = orderItem.Id,
                OrderId = orderItem.OrderId,
                StoreFoodId = orderItem.StoreFoodId,
                FoodName = food.FoodName,
                Quantity = orderItem.Quantity,
                UnitPrice = orderItem.UnitPrice,
                TotalPrice = orderItem.TotalPrice
            })
            .ToListAsync();
        await FillOrderItemOptionsAsync(items);
        return BaseResponse<OrderResponseDto>.Success(new OrderResponseDto
        {
            Id = order.Id,
            OrderCode = order.OrderCode,
            RefCode = order.RefCode,
            CustomerAccountId = (long)order.CustomerAccountId,
            StoreRefCode = order.StoreRefCode,
            TotalAmount = order.TotalAmount,
            OrderStatus = order.OrderStatus,
            PaymentStatus = order.PaymentStatus,
            Note = order.Note,
            CreatedAt = order.CreatedAt,
            Items = items
        });
    }

    public async Task<BaseTableResponse<OrderResponseDto>> GetByStoreRefCodeAsync(BaseSearchRequest<OrderSearchRequest> request)
    {
        var repoOrder = _unitOfWork.GetRepository<Order>();
        var repoOrderItem = _unitOfWork.GetRepository<OrderItem>();
        var repoFood = _unitOfWork.GetRepository<StoreFood>();
        var repoAccount = _unitOfWork.GetRepository<Account>();
        var repoAccountProfile = _unitOfWork.GetRepository<AccountProfile>();
        var repoCustomer = _unitOfWork.GetRepository<Customer>();

        request.Page = request.Page <= 0 ? 1 : request.Page;
        request.PageSize = request.PageSize <= 0 ? 10 : request.PageSize;

        var search = request.SearchParams;

        var query = repoOrder
            .Query()
            .AsNoTracking()
            .Where(x =>
                search != null &&
                !string.IsNullOrWhiteSpace(search.StoreRefCode) &&
                x.StoreRefCode == search.StoreRefCode);

        if (!string.IsNullOrWhiteSpace(search?.OrderStatus))
        {
            query = query.Where(x => x.OrderStatus == search.OrderStatus);
        }

        if (!string.IsNullOrWhiteSpace(search?.PaymentStatus))
        {
            query = query.Where(x => x.PaymentStatus == search.PaymentStatus);
        }

        if (search?.FromDate != null)
        {
            var fromDate = search.FromDate.Value.Date;
            query = query.Where(x => x.CreatedAt >= fromDate);
        }

        if (search?.ToDate != null)
        {
            var toDate = search.ToDate.Value.Date.AddDays(1);
            query = query.Where(x => x.CreatedAt < toDate);
        }

        var projectedQuery =
            from order in query

            join account in repoAccount.Query().AsNoTracking()
                on order.CustomerAccountId equals account.Id into accountGroup
            from account in accountGroup.DefaultIfEmpty()

            join profile in repoAccountProfile.Query().AsNoTracking()
                on account.Id equals profile.AccountId into profileGroup
            from profile in profileGroup.DefaultIfEmpty()

            join customer in repoCustomer.Query().AsNoTracking()
                on order.CustomerId equals customer.Id into customerGroup
            from customer in customerGroup.DefaultIfEmpty()

            select new OrderResponseDto
            {
                Id = order.Id,
                OrderCode = order.OrderCode,
                RefCode = order.RefCode,

                CustomerAccountId = (long)order.CustomerAccountId,

                CustomerName =
                    account != null
                        ? (
                            profile != null && !string.IsNullOrEmpty(profile.FullName)
                                ? profile.FullName
                                : account.Username
                        )
                        : (
                            customer != null
                                ? customer.CustomerName
                                : "Khách vãng lai"
                        ),

                StoreRefCode = order.StoreRefCode,
                TotalAmount = order.TotalAmount,
                OrderStatus = order.OrderStatus,
                PaymentStatus = order.PaymentStatus,
                Note = order.Note,
                CreatedAt = order.CreatedAt,
                Items = new List<OrderItemResponseDto>()
            };

        if (!string.IsNullOrWhiteSpace(search?.Keyword))
        {
            var keyword = search.Keyword.Trim().ToLower();

            projectedQuery = projectedQuery.Where(x =>
                x.OrderCode.ToLower().Contains(keyword) ||
                x.RefCode.ToLower().Contains(keyword) ||
                x.CustomerName.ToLower().Contains(keyword) ||
                (x.Note != null && x.Note.ToLower().Contains(keyword))
            );
        }
        var totalRecords = await query.CountAsync();
        projectedQuery = request.SortBy switch
        {
            "totalAmount" => request.Asc
                ? projectedQuery.OrderBy(x => x.TotalAmount)
                : projectedQuery.OrderByDescending(x => x.TotalAmount),

            "createdAt" => request.Asc
                ? projectedQuery.OrderBy(x => x.CreatedAt)
                : projectedQuery.OrderByDescending(x => x.CreatedAt),

            "orderCode" => request.Asc
                ? projectedQuery.OrderBy(x => x.OrderCode)
                : projectedQuery.OrderByDescending(x => x.OrderCode),

            _ => projectedQuery.OrderByDescending(x => x.Id)
        };

        var items = await projectedQuery
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync();

        var orderIds = items
            .Select(x => x.Id)
            .ToList();

        var orderItems = await (
            from orderItem in repoOrderItem.Query().AsNoTracking()
            join food in repoFood.Query().AsNoTracking()
                on orderItem.StoreFoodId equals food.Id
            where orderIds.Contains(orderItem.OrderId)
            select new OrderItemResponseDto
            {
                Id = orderItem.Id,
                OrderId = orderItem.OrderId,
                StoreFoodId = orderItem.StoreFoodId,
                FoodName = food.FoodName,
                Quantity = orderItem.Quantity,
                UnitPrice = orderItem.UnitPrice,
                TotalPrice = orderItem.TotalPrice
            })
            .ToListAsync();

        await FillOrderItemOptionsAsync(orderItems);

        foreach (var order in items)
        {
            order.Items = orderItems
                .Where(x => x.OrderId == order.Id)
                .ToList();
        }

        return new BaseTableResponse<OrderResponseDto>
        {
            Items = items,
            Page = request.Page,
            PageSize = request.PageSize,
            TotalRecords = totalRecords
        };
    }

    public async Task<BaseTableResponse<OrderResponseDto>> GetAllForAdminAsync(BaseSearchRequest<OrderSearchRequest> request)
    {
        var repoOrder = _unitOfWork.GetRepository<Order>();
        var repoOrderItem = _unitOfWork.GetRepository<OrderItem>();
        var repoFood = _unitOfWork.GetRepository<StoreFood>();
        var repoAccount = _unitOfWork.GetRepository<Account>();
        var repoAccountProfile = _unitOfWork.GetRepository<AccountProfile>();
        var repoCustomer = _unitOfWork.GetRepository<Customer>();

        request.Page = request.Page <= 0 ? 1 : request.Page;
        request.PageSize = request.PageSize <= 0 ? 10 : request.PageSize;

        var search = request.SearchParams;

        var query = repoOrder
            .Query()
            .AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search?.StoreRefCode))
        {
            query = query.Where(x => x.StoreRefCode == search.StoreRefCode);
        }

        if (!string.IsNullOrWhiteSpace(search?.OrderStatus))
        {
            query = query.Where(x => x.OrderStatus == search.OrderStatus);
        }

        if (!string.IsNullOrWhiteSpace(search?.PaymentStatus))
        {
            query = query.Where(x => x.PaymentStatus == search.PaymentStatus);
        }

        if (search?.FromDate != null)
        {
            var fromDate = search.FromDate.Value.Date;
            query = query.Where(x => x.CreatedAt >= fromDate);
        }

        if (search?.ToDate != null)
        {
            var toDate = search.ToDate.Value.Date.AddDays(1);
            query = query.Where(x => x.CreatedAt < toDate);
        }

        var projectedQuery =
            from order in query

            join account in repoAccount.Query().AsNoTracking()
                on order.CustomerAccountId equals account.Id into accountGroup
            from account in accountGroup.DefaultIfEmpty()

            join profile in repoAccountProfile.Query().AsNoTracking()
                on account.Id equals profile.AccountId into profileGroup
            from profile in profileGroup.DefaultIfEmpty()

            join customer in repoCustomer.Query().AsNoTracking()
                on order.CustomerId equals customer.Id into customerGroup
            from customer in customerGroup.DefaultIfEmpty()

            select new OrderResponseDto
            {
                Id = order.Id,
                OrderCode = order.OrderCode,
                RefCode = order.RefCode,

                CustomerAccountId = (long)order.CustomerAccountId,

                CustomerName =
                    account != null
                        ? (
                            profile != null && !string.IsNullOrEmpty(profile.FullName)
                                ? profile.FullName
                                : account.Username
                        )
                        : (
                            customer != null
                                ? customer.CustomerName
                                : "Khách vãng lai"
                        ),

                StoreRefCode = order.StoreRefCode,
                TotalAmount = order.TotalAmount,
                OrderStatus = order.OrderStatus,
                PaymentStatus = order.PaymentStatus,
                Note = order.Note,
                CreatedAt = order.CreatedAt,
                Items = new List<OrderItemResponseDto>()
            };

        if (!string.IsNullOrWhiteSpace(search?.Keyword))
        {
            var keyword = search.Keyword.Trim().ToLower();

            projectedQuery = projectedQuery.Where(x =>
                x.OrderCode.ToLower().Contains(keyword) ||
                x.RefCode.ToLower().Contains(keyword) ||
                x.CustomerName.ToLower().Contains(keyword) ||
                (x.Note != null && x.Note.ToLower().Contains(keyword))
            );
        }

        var totalRecords = await projectedQuery.CountAsync();

        projectedQuery = request.SortBy switch
        {
            "totalAmount" => request.Asc
                ? projectedQuery.OrderBy(x => x.TotalAmount)
                : projectedQuery.OrderByDescending(x => x.TotalAmount),

            "createdAt" => request.Asc
                ? projectedQuery.OrderBy(x => x.CreatedAt)
                : projectedQuery.OrderByDescending(x => x.CreatedAt),

            "orderCode" => request.Asc
                ? projectedQuery.OrderBy(x => x.OrderCode)
                : projectedQuery.OrderByDescending(x => x.OrderCode),

            _ => projectedQuery.OrderByDescending(x => x.Id)
        };

        var items = await projectedQuery
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync();

        var orderIds = items
            .Select(x => x.Id)
            .ToList();

        var orderItems = await (
            from orderItem in repoOrderItem.Query().AsNoTracking()
            join food in repoFood.Query().AsNoTracking()
                on orderItem.StoreFoodId equals food.Id
            where orderIds.Contains(orderItem.OrderId)
            select new OrderItemResponseDto
            {
                Id = orderItem.Id,
                OrderId = orderItem.OrderId,
                StoreFoodId = orderItem.StoreFoodId,
                FoodName = food.FoodName,
                Quantity = orderItem.Quantity,
                UnitPrice = orderItem.UnitPrice,
                TotalPrice = orderItem.TotalPrice
            })
            .ToListAsync();

        await FillOrderItemOptionsAsync(orderItems);

        foreach (var order in items)
        {
            order.Items = orderItems
                .Where(x => x.OrderId == order.Id)
                .ToList();
        }

        return new BaseTableResponse<OrderResponseDto>
        {
            Items = items,
            Page = request.Page,
            PageSize = request.PageSize,
            TotalRecords = totalRecords
        };
    }

    public async Task<BaseResponse<string>> UpdateStatusAsync(long id, long currentUserId, string refCode, UpdateOrderStatusRequestDto request)
    {
        var repoOrder = _unitOfWork.GetRepository<Order>();
        var repoOrderHistory = _unitOfWork.GetRepository<OrderHistory>();

        var order = await repoOrder.FirstOrDefaultAsync(x =>
            x.Id == id);

        if (order is null)
            return BaseResponse<string>.Fail("Order not found");

        var oldStatus = order.OrderStatus;

        order.OrderStatus = request.NewStatus;
        order.UpdatedAt = DateTimeHelper.VnNow;
        order.UpdatedBy = currentUserId;

        repoOrder.Update(order);

        await repoOrderHistory.AddAsync(new OrderHistory
        {
            RefCode = refCode,
            OrderId = order.Id,
            OldStatus = oldStatus,
            NewStatus = request.NewStatus,
            ChangedNote = request.ChangedNote,
            CreatedAt = DateTimeHelper.VnNow,
            CreatedBy = currentUserId
        });

        await _unitOfWork.SaveChangesAsync();

        return BaseResponse<string>.Success("Update order status successfully");
    }

    public async Task<BaseResponse<string>> UpdatePaymentStatusAsync(long id, long currentUserId, string refCode, UpdatePaymentStatusRequestDto request)
    {
        var repoOrder = _unitOfWork.GetRepository<Order>();
        var repoOrderHistory = _unitOfWork.GetRepository<OrderHistory>();

        var order = await repoOrder.FirstOrDefaultAsync(x => x.Id == id);

        if (order is null)
            return BaseResponse<string>.Fail("Order not found");

        if (request.NewStatus != "PAID" && request.NewStatus != "UNPAID")
            return BaseResponse<string>.Fail("Invalid payment status");

        if (order.PaymentStatus == request.NewStatus)
            return BaseResponse<string>.Fail("Payment status is already updated");

        var oldStatus = order.PaymentStatus;

        order.PaymentStatus = request.NewStatus;
        order.UpdatedAt = DateTimeHelper.VnNow;
        order.UpdatedBy = currentUserId;

        repoOrder.Update(order);

        await repoOrderHistory.AddAsync(new OrderHistory
        {
            RefCode = refCode,
            OrderId = order.Id,
            OldStatus = oldStatus,
            NewStatus = request.NewStatus,
            ChangedNote = request.ChangedNote,
            CreatedAt = DateTimeHelper.VnNow,
            CreatedBy = currentUserId
        });

        await _unitOfWork.SaveChangesAsync();

        if (request.NewStatus == "PAID")
        {
            var confirmerName = await GetCustomerDisplayNameAsync(currentUserId);

            await _activityLogService.LogAsync(
                "Agent",
                currentUserId,
                confirmerName,
                "CONFIRM_PAYMENT",
                $"Xác nhận thanh toán đơn hàng \"{order.OrderCode}\", tổng tiền {order.TotalAmount:N0}đ");
        }

        return BaseResponse<string>.Success("Update payment status successfully");
    }

    public async Task<BaseResponse<string>> CancelAsync(long id, long currentUserId, string refCode)
    {
        var repoOrder = _unitOfWork.GetRepository<Order>();
        var repoOrderHistory = _unitOfWork.GetRepository<OrderHistory>();

        var order = await repoOrder.FirstOrDefaultAsync(x => x.Id == id);

        if (order is null)
            return BaseResponse<string>.Fail("Order not found");

        if (order.OrderStatus == "CANCELLED")
            return BaseResponse<string>.Fail("Order is already cancelled");

        if (order.OrderStatus == "COMPLETED")
            return BaseResponse<string>.Fail("Cannot cancel a completed order");

        var oldStatus = order.OrderStatus;

        order.OrderStatus = "CANCELLED";
        order.UpdatedAt = DateTimeHelper.VnNow;
        order.UpdatedBy = currentUserId;

        repoOrder.Update(order);

        await repoOrderHistory.AddAsync(new OrderHistory
        {
            RefCode = refCode,
            OrderId = order.Id,
            OldStatus = oldStatus,
            NewStatus = "CANCELLED",
            ChangedNote = null,
            CreatedAt = DateTimeHelper.VnNow,
            CreatedBy = currentUserId
        });

        await _unitOfWork.SaveChangesAsync();

        return BaseResponse<string>.Success("Cancel order successfully");
    }

    public async Task<BaseResponse<byte[]>> PrintOrdersAsync(long currentUserId, PrintOrdersRequestDto request)
    {
        if (request.OrderIds == null || !request.OrderIds.Any())
            return BaseResponse<byte[]>.Fail("Vui lòng chọn đơn hàng để in");

        var orderIds = request.OrderIds
            .Distinct()
            .ToList();

        var repoOrder = _unitOfWork.GetRepository<Order>();
        var repoAccount = _unitOfWork.GetRepository<Account>();
        var repoProfile = _unitOfWork.GetRepository<AccountProfile>();
        var repoCustomer = _unitOfWork.GetRepository<Customer>();
        var repoOrderItem = _unitOfWork.GetRepository<OrderItem>();
        var repoOrderItemOption = _unitOfWork.GetRepository<OrderItemOption>();
        var repoStoreFood = _unitOfWork.GetRepository<StoreFood>();

        var orders = await repoOrder
            .Query()
            .AsNoTracking()
            .Where(x => orderIds.Contains(x.Id))
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync();

        if (!orders.Any())
            return BaseResponse<byte[]>.Fail("Không tìm thấy đơn hàng");

        var foundOrderIds = orders
            .Select(x => x.Id)
            .ToList();

        var customerAccountIds = orders
            .Where(x => x.CustomerAccountId.HasValue)
            .Select(x => x.CustomerAccountId!.Value)
            .Distinct()
            .ToList();

        var guestCustomerIds = orders
            .Where(x => x.CustomerId.HasValue)
            .Select(x => x.CustomerId!.Value)
            .Distinct()
            .ToList();

        var accounts = await repoAccount
            .Query()
            .AsNoTracking()
            .Where(x => customerAccountIds.Contains(x.Id))
            .ToListAsync();

        var profiles = await repoProfile
            .Query()
            .AsNoTracking()
            .Where(x => customerAccountIds.Contains(x.AccountId))
            .ToListAsync();

        var customers = await repoCustomer
            .Query()
            .AsNoTracking()
            .Where(x => guestCustomerIds.Contains(x.Id))
            .ToListAsync();

        var orderItems = await repoOrderItem
            .Query()
            .AsNoTracking()
            .Where(x => foundOrderIds.Contains(x.OrderId))
            .ToListAsync();

        var storeFoodIds = orderItems
            .Select(x => x.StoreFoodId)
            .Distinct()
            .ToList();

        var storeFoods = await repoStoreFood
            .Query()
            .AsNoTracking()
            .Where(x => storeFoodIds.Contains(x.Id))
            .ToListAsync();

        var orderItemIds = orderItems
            .Select(x => x.Id)
            .ToList();

        var orderItemOptions = await repoOrderItemOption
            .Query()
            .AsNoTracking()
            .Where(x => orderItemIds.Contains(x.OrderItemId))
            .ToListAsync();

        var models = orders.Select(order =>
        {
            var account = order.CustomerAccountId.HasValue
                ? accounts.FirstOrDefault(x => x.Id == order.CustomerAccountId.Value)
                : null;

            var profile = order.CustomerAccountId.HasValue
                ? profiles.FirstOrDefault(x => x.AccountId == order.CustomerAccountId.Value)
                : null;

            var customer = order.CustomerId.HasValue
                ? customers.FirstOrDefault(x => x.Id == order.CustomerId.Value)
                : null;

            var items = orderItems
                .Where(x => x.OrderId == order.Id)
                .Select(item =>
                {
                    var food = storeFoods.FirstOrDefault(x => x.Id == item.StoreFoodId);

                    var options = orderItemOptions
                        .Where(x => x.OrderItemId == item.Id)
                        .OrderBy(x => x.OptionGroupName)
                        .ThenBy(x => x.OptionName)
                        .Select(option => new PrintOrderItemOptionViewModel
                        {
                            GroupName = option.OptionGroupName,
                            OptionName = option.OptionName,
                            AdditionalPrice = option.AdditionalPrice
                        })
                        .ToList();

                    return new PrintOrderItemViewModel
                    {
                        FoodName = food?.FoodName ?? "Không rõ món",
                        Quantity = item.Quantity,
                        UnitPrice = item.UnitPrice,
                        TotalPrice = item.TotalPrice,
                        Options = options
                    };
                })
                .ToList();

            return new PrintOrderViewModel
            {
                OrderCode = order.OrderCode,
                CustomerName =
                    profile?.FullName
                    ?? account?.Username
                    ?? customer?.CustomerName
                    ?? "Người lạ",
                CustomerPhone = profile?.PhoneNumber,
                CustomerAddress = profile?.Address,
                OrderStatus = order.OrderStatus,
                PaymentStatus = order.PaymentStatus,
                Note = order.Note,
                TotalAmount = order.TotalAmount,
                CreatedAt = order.CreatedAt,
                Items = items
            };
        }).ToList();

        var pdfBytes = await BuildOrdersPdfAsync(models);

        return BaseResponse<byte[]>.Success(pdfBytes);
    }

    public async Task<BaseResponse<string>> GetPaymentStatusAsync(string orderCode)
    {
        var repoOrder = _unitOfWork.GetRepository<Order>();
        var repoTransaction = _unitOfWork.GetRepository<PaymentTransaction>();

        var order = await repoOrder.FirstOrDefaultAsync(x => x.OrderCode == orderCode);

        if (order is null) return BaseResponse<string>.Fail("Order not found");

        if (order.PaymentStatus != "PAID")
        {
            var paidTransaction = await repoTransaction.FirstOrDefaultAsync(x =>
                x.OrderId == order.Id &&
                x.IsProcessed &&
                !x.IsDeleted);

            if (paidTransaction != null)
            {
                order.PaymentStatus = "PAID";
                await _unitOfWork.SaveChangesAsync();
            }
        }

        return BaseResponse<string>.Success(order.PaymentStatus);
    }

    public async Task<BaseResponse<string?>> CheckPendingOrderAsync(string deviceId)
    {
        var repoOrder = _unitOfWork.GetRepository<Order>();
        var repoCustomer = _unitOfWork.GetRepository<Customer>();

        var order = await (
            from o in repoOrder.Query()
            join c in repoCustomer.Query()
                on o.CustomerId equals c.Id
            where c.DeviceId == deviceId
                && o.PaymentStatus == "UNPAID"
                && !o.IsDelete
            orderby o.CreatedAt descending
            select o
        ).FirstOrDefaultAsync();

        if (order is null)
        {
            return BaseResponse<string?>.Success(null);
        }

        return BaseResponse<string?>.Success(order.OrderCode);
    }

    // Plain class (not a record): AddAutoServices reflects over every class in this assembly and
    // registers each interface it implements as a DI service — a record's auto-generated
    // IEquatable<T> would get swept up and registered, and since it has no parameterless
    // constructor DI fails to validate it at startup.
    private sealed class PromotionPricingContext
    {
        public PromotionPricingContext(
            Dictionary<long, decimal> fixedPriceMap,
            Dictionary<long, PromotionDiscountItemResponseDto> discountMap,
            List<PromotionResponseDto> giftPromotions,
            List<PromotionResponseDto> storeWideDiscountPromotions)
        {
            FixedPriceMap = fixedPriceMap;
            DiscountMap = discountMap;
            GiftPromotions = giftPromotions;
            StoreWideDiscountPromotions = storeWideDiscountPromotions;
        }

        public Dictionary<long, decimal> FixedPriceMap { get; }
        public Dictionary<long, PromotionDiscountItemResponseDto> DiscountMap { get; }
        public List<PromotionResponseDto> GiftPromotions { get; }

        // PRODUCT_DISCOUNT dạng ApplyToAllProducts = true (khách tự chọn 1 món nhận giảm giá)
        public List<PromotionResponseDto> StoreWideDiscountPromotions { get; }

        // StoreFoodId -> giá đã giảm, được điền bởi ValidateSelectedStoreWideDiscountsAsync
        // trước khi tính giá từng dòng đơn hàng.
        public Dictionary<long, decimal> StoreWideDiscountOverrides { get; set; } = new();
    }

    /// <summary>
    /// Nạp các promotion đang ACTIVE của store, chỉ giữ lại những promotion KHÔNG cần mã
    /// (tự động áp dụng) hoặc promotion có promotion_code khớp đúng với <paramref name="promoCode"/>
    /// khách nhập. Nếu khách nhập mã nhưng không khớp promotion nào đang active, trả về lỗi.
    /// </summary>
    private async Task<(PromotionPricingContext? Context, string? Error)> LoadPromotionContextAsync(string storeRefCode, string? promoCode)
    {
        var response = await _promotionService.GetActiveByStoreRefCodeAsync(storeRefCode);
        var allPromotions = response.Data ?? new List<PromotionResponseDto>();

        var normalizedCode = string.IsNullOrWhiteSpace(promoCode) ? null : promoCode.Trim();

        if (normalizedCode != null &&
            !allPromotions.Any(p => string.Equals(p.PromotionCode, normalizedCode, StringComparison.OrdinalIgnoreCase)))
        {
            return (null, "Mã giảm giá không hợp lệ hoặc đã hết hạn");
        }

        var promotions = allPromotions
            .Where(p => p.PromotionCode is null || string.Equals(p.PromotionCode, normalizedCode, StringComparison.OrdinalIgnoreCase))
            .ToList();

        var fixedPriceMap = promotions
            .Where(p => p.PromotionType == "FIXED_PRICE")
            .SelectMany(p => p.FixedPriceItems)
            .GroupBy(i => i.StoreFoodId)
            .ToDictionary(g => g.Key, g => g.First().FixedPrice);

        var discountMap = promotions
            .Where(p => p.PromotionType == "PRODUCT_DISCOUNT")
            .SelectMany(p => p.DiscountItems)
            .GroupBy(i => i.StoreFoodId)
            .ToDictionary(g => g.Key, g => g.First());

        var giftPromotions = promotions
            .Where(p => p.PromotionType == "BUY_X_GET_Y")
            .ToList();

        var storeWideDiscountPromotions = promotions
            .Where(p => p.PromotionType == "PRODUCT_DISCOUNT" && p.ApplyToAllProducts)
            .ToList();

        return (new PromotionPricingContext(fixedPriceMap, discountMap, giftPromotions, storeWideDiscountPromotions), null);
    }

    /// <summary>
    /// Giá của món theo các promotion FIXED_PRICE / PRODUCT_DISCOUNT (chọn sẵn danh sách món) —
    /// KHÔNG bao gồm giảm giá "toàn bộ sản phẩm" (ApplyToAllProducts), vì loại đó chỉ giảm cho đúng
    /// 1 đơn vị của món khách chọn (xem <see cref="ComputeLineTotal"/>), không áp cho cả dòng.
    /// </summary>
    private static decimal GetBasePrice(StoreFood food, PromotionPricingContext context)
    {
        if (context.FixedPriceMap.TryGetValue(food.Id, out var fixedPrice))
            return fixedPrice;

        if (context.DiscountMap.TryGetValue(food.Id, out var discount))
        {
            if (discount.DiscountType == "PERCENT")
            {
                var discountAmount = food.Price * discount.DiscountValue / 100m;

                if (discount.MaxDiscountAmount.HasValue)
                    discountAmount = Math.Min(discountAmount, discount.MaxDiscountAmount.Value);

                return Math.Max(food.Price - discountAmount, 0);
            }

            return Math.Max(food.Price - discount.DiscountValue, 0);
        }

        return food.Price;
    }

    /// <summary>
    /// Tổng tiền của 1 dòng đơn hàng (1 món x số lượng). Nếu món này là món khách chọn để nhận giảm
    /// giá "toàn bộ sản phẩm", CHỈ 1 đơn vị được tính giá đã giảm — phần còn lại (nếu số lượng > 1)
    /// vẫn tính giá bình thường, vì chương trình chỉ cho giảm đúng 1 sản phẩm/lần dùng.
    /// </summary>
    private static decimal ComputeLineTotal(StoreFood food, int quantity, decimal optionAmount, PromotionPricingContext context)
    {
        var basePrice = GetBasePrice(food, context);

        if (context.StoreWideDiscountOverrides.TryGetValue(food.Id, out var discountedUnitPrice))
            return basePrice * (quantity - 1) + discountedUnitPrice + optionAmount * quantity;

        return (basePrice + optionAmount) * quantity;
    }

    /// <summary>
    /// Kiểm tra các quà tặng khách chọn (Mua X tặng Y): mỗi chương trình chỉ được chọn 1 quà,
    /// chương trình/quà phải còn hiệu lực, và điều kiện "mua X" (tính trên tổng giỏ hàng) phải đạt.
    /// </summary>
    private static (List<(long StoreFoodId, int Quantity)> Gifts, string? Error) ValidateSelectedGifts(
        PromotionPricingContext context,
        List<SelectedGiftRequestDto>? selectedGifts,
        decimal subtotal,
        int totalQuantity)
    {
        var gifts = new List<(long StoreFoodId, int Quantity)>();

        if (selectedGifts is null || selectedGifts.Count == 0)
            return (gifts, null);

        if (selectedGifts.Select(x => x.PromotionId).Distinct().Count() != selectedGifts.Count)
            return (gifts, "Chỉ được chọn 1 quà tặng cho mỗi chương trình");

        foreach (var selected in selectedGifts)
        {
            var promotion = context.GiftPromotions.FirstOrDefault(p => p.Id == selected.PromotionId);

            if (promotion is null)
                return (gifts, "Chương trình quà tặng không còn hiệu lực");

            var gift = promotion.GiftItems.FirstOrDefault(g => g.StoreFoodId == selected.StoreFoodId);

            if (gift is null)
                return (gifts, "Quà tặng không hợp lệ");

            var conditionMet = promotion.ConditionType switch
            {
                "MIN_ORDER_AMOUNT" => subtotal >= (promotion.ConditionMinAmount ?? decimal.MaxValue),
                "MIN_QUANTITY" => totalQuantity >= (promotion.ConditionMinQuantity ?? int.MaxValue),
                _ => false
            };

            if (!conditionMet)
                return (gifts, $"Chưa đủ điều kiện để nhận quà từ chương trình \"{promotion.Name}\"");

            gifts.Add((gift.StoreFoodId, gift.GiftQuantity));
        }

        return (gifts, null);
    }

    /// <summary>
    /// Validate món khách chọn để nhận giảm giá cho các promotion PRODUCT_DISCOUNT dạng
    /// "áp dụng toàn bộ sản phẩm" (ApplyToAllProducts): món phải nằm trong đơn hàng, promotion còn
    /// hiệu lực, và khách (theo tài khoản đăng nhập hoặc theo Customer/deviceId với khách vãng lai)
    /// chưa từng dùng promotion này trước đó — mỗi khách chỉ được dùng 1 lần.
    /// </summary>
    private async Task<(Dictionary<long, decimal> Overrides, string? Error)> ValidateSelectedStoreWideDiscountsAsync(
        PromotionPricingContext context,
        List<SelectedStoreWideDiscountRequestDto>? selected,
        List<StoreFood> foods,
        long? customerAccountId,
        long? customerId)
    {
        var overrides = new Dictionary<long, decimal>();

        if (selected is null || selected.Count == 0)
            return (overrides, null);

        if (selected.Select(x => x.PromotionId).Distinct().Count() != selected.Count)
            return (overrides, "Chỉ được chọn 1 sản phẩm cho mỗi chương trình giảm giá");

        // Chương trình chỉ xét cho khách đã tồn tại trong hệ thống (đã đăng nhập, hoặc khách vãng lai
        // đã có record Customer từ trước — tức đã từng đặt hàng). Khách vãng lai hoàn toàn mới (chưa
        // từng có Customer nào ứng với deviceId) không đủ điều kiện — nếu không, ai cũng chỉ cần xoá
        // deviceId/dùng thiết bị khác để "reset" lượt dùng, phá vỡ giới hạn 1 lần/khách.
        if (customerAccountId is null && customerId is null)
            return (overrides, "Chương trình chỉ áp dụng cho khách hàng đã từng đặt hàng hoặc đã đăng nhập");

        var repoRedemption = _unitOfWork.GetRepository<PromotionRedemption>();

        foreach (var item in selected)
        {
            var promotion = context.StoreWideDiscountPromotions.FirstOrDefault(p => p.Id == item.PromotionId);

            if (promotion is null)
                return (overrides, "Chương trình giảm giá không còn hiệu lực");

            var food = foods.FirstOrDefault(f => f.Id == item.StoreFoodId);

            if (food is null)
                return (overrides, "Sản phẩm được chọn giảm giá phải nằm trong đơn hàng");

            var alreadyRedeemed = await repoRedemption.AnyAsync(x =>
                x.PromotionId == promotion.Id &&
                ((customerAccountId != null && x.CustomerAccountId == customerAccountId) ||
                 (customerId != null && x.CustomerId == customerId)));

            if (alreadyRedeemed)
                return (overrides, $"Bạn đã sử dụng chương trình \"{promotion.Name}\" rồi");

            var discountAmount = promotion.DiscountType == "PERCENT"
                ? food.Price * (promotion.DiscountValue ?? 0) / 100m
                : (promotion.DiscountValue ?? 0);

            if (promotion.DiscountType == "PERCENT" && promotion.MaxDiscountAmount.HasValue)
                discountAmount = Math.Min(discountAmount, promotion.MaxDiscountAmount.Value);

            overrides[food.Id] = Math.Max(food.Price - discountAmount, 0);
        }

        return (overrides, null);
    }

    private async Task SavePromotionRedemptionsAsync(
        List<SelectedStoreWideDiscountRequestDto>? selected,
        PromotionPricingContext context,
        long orderId,
        long? customerAccountId,
        long? customerId)
    {
        if (selected is null || selected.Count == 0)
            return;

        var repoRedemption = _unitOfWork.GetRepository<PromotionRedemption>();
        var now = DateTimeHelper.VnNow;

        foreach (var item in selected)
        {
            if (!context.StoreWideDiscountOverrides.ContainsKey(item.StoreFoodId))
                continue;

            await repoRedemption.AddAsync(new PromotionRedemption
            {
                PromotionId = item.PromotionId,
                StoreFoodId = item.StoreFoodId,
                OrderId = orderId,
                CustomerAccountId = customerAccountId,
                CustomerId = customerId,
                CreatedAt = now,
                CreatedBy = customerAccountId
            });
        }

        await _unitOfWork.SaveChangesAsync();
    }

    /// <summary>
    /// Tạo OrderItem cho 1 dòng đơn hàng. Nếu món này được chọn nhận giảm giá "toàn bộ sản phẩm" và
    /// số lượng > 1, tách thành 2 dòng OrderItem: 1 dòng số lượng 1 ở giá đã giảm, phần còn lại ở giá
    /// bình thường — vì chương trình chỉ giảm đúng 1 sản phẩm, không nhân theo số lượng.
    /// </summary>
    private async Task AddOrderItemsForLineAsync(
        IRepository<OrderItem> repoOrderItem,
        IRepository<OrderItemOption> repoOrderItemOption,
        string refCode,
        long orderId,
        StoreFood food,
        int quantity,
        List<CreateOrderItemOptionRequestDto>? options,
        PromotionPricingContext context,
        long? currentUserId)
    {
        var optionAmount = options?.Sum(x => x.AdditionalPrice) ?? 0;
        var basePrice = GetBasePrice(food, context);

        if (context.StoreWideDiscountOverrides.TryGetValue(food.Id, out var discountedUnitPrice))
        {
            var discountedLineUnitPrice = discountedUnitPrice + optionAmount;

            await AddSingleOrderItemAsync(
                repoOrderItem, repoOrderItemOption, refCode, orderId, food.Id,
                1, discountedLineUnitPrice, discountedLineUnitPrice, options, currentUserId);

            if (quantity > 1)
            {
                var remainingQuantity = quantity - 1;
                var remainingUnitPrice = basePrice + optionAmount;

                await AddSingleOrderItemAsync(
                    repoOrderItem, repoOrderItemOption, refCode, orderId, food.Id,
                    remainingQuantity, remainingUnitPrice, remainingUnitPrice * remainingQuantity, options, currentUserId);
            }
        }
        else
        {
            var unitPrice = basePrice + optionAmount;

            await AddSingleOrderItemAsync(
                repoOrderItem, repoOrderItemOption, refCode, orderId, food.Id,
                quantity, unitPrice, unitPrice * quantity, options, currentUserId);
        }
    }

    private async Task AddSingleOrderItemAsync(
        IRepository<OrderItem> repoOrderItem,
        IRepository<OrderItemOption> repoOrderItemOption,
        string refCode,
        long orderId,
        long storeFoodId,
        int quantity,
        decimal unitPrice,
        decimal totalPrice,
        List<CreateOrderItemOptionRequestDto>? options,
        long? currentUserId)
    {
        var orderItem = new OrderItem
        {
            RefCode = refCode,
            OrderId = orderId,
            StoreFoodId = storeFoodId,
            Quantity = quantity,
            UnitPrice = unitPrice,
            TotalPrice = totalPrice,
            CreatedAt = DateTimeHelper.VnNow,
            CreatedBy = currentUserId
        };

        await repoOrderItem.AddAsync(orderItem);
        await _unitOfWork.SaveChangesAsync();

        if (options != null && options.Any())
        {
            foreach (var option in options)
            {
                await repoOrderItemOption.AddAsync(new OrderItemOption
                {
                    RefCode = refCode,
                    OrderItemId = orderItem.Id,
                    OptionGroupId = option.OptionGroupId,
                    OptionGroupName = option.OptionGroupName,
                    OptionId = option.OptionId,
                    OptionName = option.OptionName,
                    AdditionalPrice = option.AdditionalPrice,
                    CreatedAt = DateTimeHelper.VnNow,
                    CreatedBy = currentUserId
                });
            }
        }
    }

    private async Task BroadcastNewOrderAsync(Order order, string customerName)
    {
        await _realtimeNotificationService.NotifyNewOrderAsync(new NewOrderNotificationDto
        {
            OrderId = order.Id,
            OrderCode = order.OrderCode,
            StoreRefCode = order.StoreRefCode,
            CustomerName = customerName,
            TotalAmount = order.TotalAmount,
            CreatedAt = order.CreatedAt
        });
    }

    private async Task BroadcastFoodQuantitiesAsync(string storeRefCode, IEnumerable<StoreFood> foods)
    {
        foreach (var food in foods)
        {
            await _realtimeNotificationService.NotifyFoodQuantityChangedAsync(new FoodQuantityChangedDto
            {
                StoreFoodId = food.Id,
                StoreRefCode = storeRefCode,
                Quantity = food.Quantity
            });
        }
    }

    private async Task<string> GetCustomerDisplayNameAsync(long currentUserId)
    {
        var repoAccount = _unitOfWork.GetRepository<Account>();
        var repoAccountProfile = _unitOfWork.GetRepository<AccountProfile>();

        var account = await repoAccount.FirstOrDefaultAsync(x => x.Id == currentUserId);
        var profile = await repoAccountProfile.FirstOrDefaultAsync(x => x.AccountId == currentUserId);

        return !string.IsNullOrEmpty(profile?.FullName)
            ? profile.FullName
            : account?.Username ?? "Khách hàng";
    }

    private async Task FillOrderItemOptionsAsync(List<OrderItemResponseDto> orderItems)
    {
        if (orderItems == null || !orderItems.Any())
            return;

        var repoOrderItemOption = _unitOfWork.GetRepository<OrderItemOption>();

        var orderItemIds = orderItems
            .Select(x => x.Id)
            .ToList();

        var options = await repoOrderItemOption
            .Query()
            .AsNoTracking()
            .Where(x => orderItemIds.Contains(x.OrderItemId))
            .Select(x => new OrderItemOptionResponseDto
            {
                Id = x.Id,
                OrderItemId = x.OrderItemId,
                OptionGroupId = x.OptionGroupId,
                OptionGroupName = x.OptionGroupName,
                OptionId = x.OptionId,
                OptionName = x.OptionName,
                AdditionalPrice = x.AdditionalPrice
            })
            .ToListAsync();

        foreach (var item in orderItems)
        {
            item.Options = options
                .Where(x => x.OrderItemId == item.Id)
                .ToList();
        }
    }

    public static string GenerateOrderCode()
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";

        var randomPart = new string(
            Enumerable.Range(0, 4)
                .Select(_ => chars[Random.Shared.Next(chars.Length)])
                .ToArray());

        return $"EMF{DateTimeHelper.VnNow:yyMMdd-HHmmss}-{randomPart}";
    }

    private string FormatCurrency(decimal value)
    {
        return $"{value:N0}đ";
    }

    private async Task<byte[]?> TryDownloadImageAsync(string? imageUrl)
    {
        if (string.IsNullOrWhiteSpace(imageUrl))
            return null;

        try
        {
            return await _httpClient.GetByteArrayAsync(imageUrl);
        }
        catch
        {
            return null;
        }
    }

    private async Task<byte[]> BuildOrdersPdfAsync(List<PrintOrderViewModel> orders)
    {
        QuestPDF.Settings.License = LicenseType.Community;

        var orderedOrders = orders
            .OrderBy(x => x.CreatedAt)
            .ToList();

        var grandTotal = orderedOrders.Sum(x => x.TotalAmount);
        var totalOrders = orderedOrders.Count;
        var totalItems = orderedOrders.Sum(x => x.Items.Sum(i => i.Quantity));

        var summaryItems = orderedOrders
            .SelectMany(x => x.Items)
            .GroupBy(x => x.FoodName)
            .Select(x => new
            {
                FoodName = x.Key,
                TotalQuantity = x.Sum(i => i.Quantity)
            })
            .OrderBy(x => x.FoodName)
            .ToList();

        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(28);
                page.DefaultTextStyle(x => x.FontSize(9));

                page.Header().Column(header =>
                {
                    header.Item().AlignCenter().Text("DANH SÁCH ĐƠN HÀNG")
                        .Bold()
                        .FontSize(16);

                    header.Item().AlignCenter()
                        .Text($"Ngày in: {DateTimeHelper.VnNow:dd/MM/yyyy HH:mm}")
                        .FontSize(9)
                        .FontColor(Colors.Grey.Darken1);
                });

                page.Content().PaddingTop(18).Column(column =>
                {
                    //-----------------------------------
                    // Tổng hợp
                    //-----------------------------------

                    column.Item()
                        .Text("TỔNG HỢP MÓN")
                        .Bold()
                        .FontSize(13);

                    column.Item().PaddingTop(8);

                    foreach (var summary in summaryItems)
                    {
                        column.Item()
                            .PaddingBottom(3)
                            .Text($"{summary.TotalQuantity}x  {summary.FoodName}")
                            .SemiBold()
                            .FontSize(10);
                    }

                    column.Item().PaddingVertical(14);

                    //-----------------------------------
                    // Chi tiết
                    //-----------------------------------

                    column.Item()
                        .Text("CHI TIẾT ĐƠN")
                        .Bold()
                        .FontSize(13);

                    column.Item().PaddingTop(10);

                    foreach (var order in orderedOrders)
                    {
                        column.Item().PaddingBottom(12);

                        // Tên khách
                        column.Item()
                            .Text(order.CustomerName)
                            .Bold()
                            .FontSize(12);

                        if (!string.IsNullOrWhiteSpace(order.Note))
                        {
                            column.Item()
                                .PaddingTop(2)
                                .PaddingBottom(4)
                                .Text($"Ghi chú: {order.Note}")
                                .FontSize(10)
                                .FontColor(Colors.Grey.Darken2);
                        }

                        foreach (var item in order.Items)
                        {
                            column.Item().PaddingTop(5).Column(itemColumn =>
                            {
                                itemColumn.Item().Row(row =>
                                {
                                    row.ConstantItem(28)
                                        .Text($"{item.Quantity}x")
                                        .SemiBold();

                                    row.RelativeItem()
                                        .Text(item.FoodName)
                                        .SemiBold()
                                        .FontSize(10);

                                    row.ConstantItem(80)
                                        .AlignRight()
                                        .Text(FormatCurrency(item.UnitPrice))
                                        .FontSize(10);
                                });

                                if (item.Options?.Any() == true)
                                {
                                    foreach (var option in item.Options)
                                    {
                                        var price = option.AdditionalPrice > 0
                                            ? $" (+{FormatCurrency(option.AdditionalPrice)})"
                                            : "";

                                        itemColumn.Item()
                                            .PaddingLeft(28)
                                            .PaddingTop(1)
                                            .Text($"{option.GroupName}: {option.OptionName}{price}")
                                            .FontSize(10)
                                            .FontColor(Colors.Grey.Darken1);
                                    }
                                }
                            });
                        }

                        column.Item().PaddingBottom(8);
                    }
                });

                page.Footer().Column(footer =>
                {
                    footer.Item().PaddingTop(8);

                    footer.Item().Row(row =>
                    {
                        row.RelativeItem()
                            .Text("TỔNG CỘNG")
                            .SemiBold()
                            .FontSize(11);

                        row.ConstantItem(120)
                            .AlignRight()
                            .Text(FormatCurrency(grandTotal))
                            .Bold()
                            .FontSize(15);
                    });

                    footer.Item()
                        .PaddingTop(4)
                        .Text($"{totalOrders} đơn • {totalItems} món")
                        .AlignRight()
                        .FontSize(9)
                        .FontColor(Colors.Grey.Darken1);
                });
            });
        }).GeneratePdf();
    }

    //private string GetOptionKey(List<PrintOrderItemOptionViewModel>? options)
    //{
    //    if (options == null || !options.Any())
    //        return "";

    //    return string.Join(" | ", options
    //        .OrderBy(x => x.GroupName)
    //        .ThenBy(x => x.OptionName)
    //        .Select(x => $"{x.GroupName}:{x.OptionName}"));
    //}

    //private string GetOptionDisplay(List<PrintOrderItemOptionViewModel>? options)
    //{
    //    if (options == null || !options.Any())
    //        return "không option";

    //    return string.Join(", ", options
    //        .OrderBy(x => x.GroupName)
    //        .ThenBy(x => x.OptionName)
    //        .Select(x => x.OptionName));
    //}

    private static string GenerateCustomerCode()
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        var random = new Random();

        var suffix = new string(
            Enumerable.Range(0, 20)
                .Select(_ => chars[random.Next(chars.Length)])
                .ToArray()
        );

        return $"CUS-{suffix}";
    }

    private string? GetClientIp()
    {
        var context = _httpContextAccessor.HttpContext;

        var ip = context?
            .Request
            .Headers["X-Forwarded-For"]
            .FirstOrDefault();

        if (!string.IsNullOrWhiteSpace(ip))
            return ip.Split(',')[0].Trim();

        return context?
            .Connection
            .RemoteIpAddress?
            .ToString();
    }
}
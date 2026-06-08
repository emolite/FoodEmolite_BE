using FoodEmolite.Application.DTOs.Order;
using FoodEmolite.Application.Interfaces;
using FoodEmolite.Domain.Entities;
using FoodEmolite.Domain.Interfaces;
using FoodEmolite.Shared.Responses;
using Microsoft.EntityFrameworkCore;

namespace FoodEmolite.Application.Services;

public class OrderService : IOrderService
{
    private readonly IUnitOfWork _unitOfWork;

    public OrderService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<BaseResponse<string>> CreateAsync(long currentUserId, string refCode, CreateOrderRequestDto request)
    {
        var repoStore = _unitOfWork.GetRepository<Store>();
        var repoFood = _unitOfWork.GetRepository<StoreFood>();
        var repoOrder = _unitOfWork.GetRepository<Order>();
        var repoOrderItem = _unitOfWork.GetRepository<OrderItem>();
        var repoOrderHistory = _unitOfWork.GetRepository<OrderHistory>();

        if (request.Items == null || !request.Items.Any())
            return BaseResponse<string>.Fail("Order item is required");

        if (request.Items.Any(x => x.Quantity <= 0))
            return BaseResponse<string>.Fail("Quantity must be greater than 0");

        var store = await repoStore.FirstOrDefaultAsync(x =>
            x.RefCode == request.StoreRefCode &&
            x.IsActive &&
            !x.IsDeleted);

        if (store is null)
            return BaseResponse<string>.Fail("Store not found");

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
            return BaseResponse<string>.Fail("Food not found");

        var orderItems = new List<OrderItem>();
        decimal totalAmount = 0;

        foreach (var item in request.Items)
        {
            var food = foods.First(x => x.Id == item.StoreFoodId);

            var totalPrice = food.Price * item.Quantity;

            totalAmount += totalPrice;

            orderItems.Add(new OrderItem
            {
                RefCode = refCode,
                StoreFoodId = food.Id,
                Quantity = item.Quantity,
                UnitPrice = food.Price,
                TotalPrice = totalPrice,
                CreatedAt = DateTime.Now,
                CreatedBy = currentUserId
            });
        }

        var order = new Order
        {
            RefCode = refCode,
            CustomerAccountId = currentUserId,
            StoreRefCode = request.StoreRefCode,
            TotalAmount = totalAmount,
            OrderStatus = "PENDING",
            PaymentStatus = "UNPAID",
            Note = request.Note,
            CreatedAt = DateTime.Now,
            CreatedBy = currentUserId
        };

        await repoOrder.AddAsync(order);
        await _unitOfWork.SaveChangesAsync();

        foreach (var item in orderItems)
        {
            item.OrderId = order.Id;
            await repoOrderItem.AddAsync(item);
        }

        await repoOrderHistory.AddAsync(new OrderHistory
        {
            RefCode = refCode,
            OrderId = order.Id,
            OldStatus = null,
            NewStatus = order.OrderStatus,
            ChangedNote = order.Note,
            CreatedAt = DateTime.Now,
            CreatedBy = currentUserId
        });

        await _unitOfWork.SaveChangesAsync();

        return BaseResponse<string>.Success("Create order successfully");
    }

    public async Task<BaseTableResponse<OrderResponseDto>> GetMyOrdersAsync(long currentUserId, int page, int pageSize)
    {
        var repoOrder = _unitOfWork.GetRepository<Order>();
        var repoOrderItem = _unitOfWork.GetRepository<OrderItem>();
        var repoFood = _unitOfWork.GetRepository<StoreFood>();

        page = page <= 0 ? 1 : page;
        pageSize = pageSize <= 0 ? 10 : pageSize;

        var query = repoOrder
            .Query()
            .AsNoTracking()
            .Where(x => x.CustomerAccountId == currentUserId);

        var totalRecords = await query.CountAsync();

        var items = await query
            .OrderByDescending(x => x.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new OrderResponseDto
            {
                Id = x.Id,
                RefCode = x.RefCode,
                CustomerAccountId = x.CustomerAccountId,
                StoreRefCode = x.StoreRefCode,
                TotalAmount = x.TotalAmount,
                OrderStatus = x.OrderStatus,
                PaymentStatus = x.PaymentStatus,
                Note = x.Note,
                CreatedAt = x.CreatedAt,
                Items = new List<OrderItemResponseDto>()
            })
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

        return BaseResponse<OrderResponseDto>.Success(new OrderResponseDto
        {
            Id = order.Id,
            RefCode = order.RefCode,
            CustomerAccountId = order.CustomerAccountId,
            StoreRefCode = order.StoreRefCode,
            TotalAmount = order.TotalAmount,
            OrderStatus = order.OrderStatus,
            PaymentStatus = order.PaymentStatus,
            Note = order.Note,
            CreatedAt = order.CreatedAt,
            Items = items
        });
    }

    public async Task<BaseTableResponse<OrderResponseDto>> GetByStoreRefCodeAsync(string storeRefCode, int page, int pageSize)
    {
        var repoOrder = _unitOfWork.GetRepository<Order>();
        var repoOrderItem = _unitOfWork.GetRepository<OrderItem>();
        var repoFood = _unitOfWork.GetRepository<StoreFood>();

        page = page <= 0 ? 1 : page;
        pageSize = pageSize <= 0 ? 10 : pageSize;

        var query = repoOrder
            .Query()
            .AsNoTracking()
            .Where(x => x.StoreRefCode == storeRefCode);

        var totalRecords = await query.CountAsync();

        var items = await query
            .OrderByDescending(x => x.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new OrderResponseDto
            {
                Id = x.Id,
                RefCode = x.RefCode,
                CustomerAccountId = x.CustomerAccountId,
                StoreRefCode = x.StoreRefCode,
                TotalAmount = x.TotalAmount,
                OrderStatus = x.OrderStatus,
                PaymentStatus = x.PaymentStatus,
                Note = x.Note,
                CreatedAt = x.CreatedAt,
                Items = new List<OrderItemResponseDto>()
            })
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
        order.UpdatedAt = DateTime.Now;
        order.UpdatedBy = currentUserId;

        repoOrder.Update(order);

        await repoOrderHistory.AddAsync(new OrderHistory
        {
            RefCode = refCode,
            OrderId = order.Id,
            OldStatus = oldStatus,
            NewStatus = request.NewStatus,
            ChangedNote = request.ChangedNote,
            CreatedAt = DateTime.Now,
            CreatedBy = currentUserId
        });

        await _unitOfWork.SaveChangesAsync();

        return BaseResponse<string>.Success("Update order status successfully");
    }
}
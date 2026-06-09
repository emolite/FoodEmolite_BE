using FoodEmolite.Application.DTOs.Order;
using FoodEmolite.Application.DTOs.Print;
using FoodEmolite.Application.Interfaces;
using FoodEmolite.Domain.Entities;
using FoodEmolite.Domain.Interfaces;
using FoodEmolite.Shared.Responses;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

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
        var repoOrderItemOption = _unitOfWork.GetRepository<OrderItemOption>();
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

        decimal totalAmount = 0;

        foreach (var item in request.Items)
        {
            var food = foods.First(x => x.Id == item.StoreFoodId);

            var optionAmount = item.Options?.Sum(x => x.AdditionalPrice) ?? 0;
            var unitPrice = food.Price + optionAmount;
            var totalPrice = unitPrice * item.Quantity;

            totalAmount += totalPrice;
        }

        var order = new Order
        {
            OrderCode = GenerateOrderCode(),
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

        foreach (var item in request.Items)
        {
            var food = foods.First(x => x.Id == item.StoreFoodId);

            var optionAmount = item.Options?.Sum(x => x.AdditionalPrice) ?? 0;
            var unitPrice = food.Price + optionAmount;
            var totalPrice = unitPrice * item.Quantity;

            var orderItem = new OrderItem
            {
                RefCode = refCode,
                OrderId = order.Id,
                StoreFoodId = food.Id,
                Quantity = item.Quantity,
                UnitPrice = unitPrice,
                TotalPrice = totalPrice,
                CreatedAt = DateTime.Now,
                CreatedBy = currentUserId
            };

            await repoOrderItem.AddAsync(orderItem);
            await _unitOfWork.SaveChangesAsync();

            if (item.Options != null && item.Options.Any())
            {
                foreach (var option in item.Options)
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
                        CreatedAt = DateTime.Now,
                        CreatedBy = currentUserId
                    });
                }
            }
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
                OrderCode = x.OrderCode,
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
                OrderCode = x.OrderCode,
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

        return BaseResponse<string>.Success("Update payment status successfully");
    }

    public async Task<BaseResponse<byte[]>> PrintOrdersAsync(
        long currentUserId,
        PrintOrdersRequestDto request)
    {
        if (request.OrderIds == null || !request.OrderIds.Any())
            return BaseResponse<byte[]>.Fail("Vui lòng chọn đơn hàng để in");

        var orderIds = request.OrderIds
            .Distinct()
            .ToList();

        var repoOrder = _unitOfWork.GetRepository<Order>();
        var repoAccount = _unitOfWork.GetRepository<Account>();
        var repoProfile = _unitOfWork.GetRepository<AccountProfile>();
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

        var customerIds = orders
            .Select(x => x.CustomerAccountId)
            .Distinct()
            .ToList();

        var accounts = await repoAccount
            .Query()
            .AsNoTracking()
            .Where(x => customerIds.Contains(x.Id))
            .ToListAsync();

        var profiles = await repoProfile
            .Query()
            .AsNoTracking()
            .Where(x => customerIds.Contains(x.AccountId))
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
            var account = accounts.FirstOrDefault(x => x.Id == order.CustomerAccountId);
            var profile = profiles.FirstOrDefault(x => x.AccountId == order.CustomerAccountId);

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
                CustomerName = profile?.FullName ?? account?.Username ?? "Không rõ người đặt",
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

        var pdfBytes = BuildOrdersPdf(models);

        return BaseResponse<byte[]>.Success(pdfBytes);
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

        return $"EMF{DateTime.Now:yyMMdd-HHmmss}-{randomPart}";
    }

    private string FormatCurrency(decimal value)
    {
        return $"{value:N0}đ";
    }

    private string GetOrderStatusText(string status)
    {
        return status switch
        {
            "PENDING" => "Chờ xác nhận",
            "CONFIRMED" => "Đã xác nhận",
            _ => status
        };
    }

    private string GetPaymentStatusText(string status)
    {
        return status switch
        {
            "UNPAID" => "Chưa thanh toán",
            "PAID" => "Đã thanh toán",
            _ => status
        };
    }

    private byte[] BuildOrdersPdf(List<PrintOrderViewModel> orders)
    {
        QuestPDF.Settings.License = LicenseType.Community;

        var groups = orders
            .GroupBy(x => new
            {
                x.CustomerName,
                x.CustomerPhone,
                x.CustomerAddress
            })
            .Select(group => new
            {
                CustomerName = group.Key.CustomerName,
                CustomerPhone = group.Key.CustomerPhone,
                CustomerAddress = group.Key.CustomerAddress,
                Orders = group.OrderBy(x => x.CreatedAt).ToList(),
                Items = group
                    .SelectMany(order => order.Items.Select(item => new
                    {
                        OrderCreatedAt = order.CreatedAt,
                        Item = item
                    }))
                    .ToList()
            })
            .ToList();

        var grandTotal = orders.Sum(x => x.TotalAmount);
        var totalOrders = orders.Count;
        var totalItems = orders.Sum(x => x.Items.Sum(i => i.Quantity));

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

                    header.Item().AlignCenter().Text($"Ngày in: {DateTime.Now:dd/MM/yyyy HH:mm}")
                        .FontSize(9)
                        .FontColor(Colors.Grey.Darken1);

                    header.Item().PaddingTop(8).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);
                });

                page.Content().PaddingTop(14).Column(column =>
                {
                    for (var groupIndex = 0; groupIndex < groups.Count; groupIndex++)
                    {
                        var group = groups[groupIndex];

                        if (groupIndex > 0)
                        {
                            column.Item()
                                .PaddingVertical(10)
                                .LineHorizontal(1)
                                .LineColor(Colors.Grey.Lighten2);
                        }

                        column.Item().Row(row =>
                        {
                            row.RelativeItem().Column(customer =>
                            {
                                customer.Item().Text(group.CustomerName)
                                    .Bold()
                                    .FontSize(12);

                                if (!string.IsNullOrWhiteSpace(group.CustomerPhone))
                                {
                                    customer.Item().Text($"SĐT: {group.CustomerPhone}")
                                        .FontSize(8)
                                        .FontColor(Colors.Grey.Darken1);
                                }

                                var firstOrderTime = group.Orders.Min(x => x.CreatedAt);

                                customer.Item().Text($"Đặt lúc: {firstOrderTime:dd/MM/yyyy HH:mm}")
                                    .FontSize(8)
                                    .FontColor(Colors.Grey.Darken1);
                            });

                            var customerTotal = group.Orders.Sum(x => x.TotalAmount);

                            row.ConstantItem(110).AlignRight().Text(FormatCurrency(customerTotal))
                                .Bold()
                                .FontSize(11);
                        });

                        column.Item().PaddingTop(8);

                        foreach (var rowItem in group.Items)
                        {
                            var item = rowItem.Item;

                            column.Item().PaddingBottom(7).Row(row =>
                            {
                                row.RelativeItem().Column(itemColumn =>
                                {
                                    itemColumn.Item().Text(item.FoodName)
                                        .FontSize(10)
                                        .SemiBold();

                                    if (item.Options != null && item.Options.Any())
                                    {
                                        foreach (var option in item.Options)
                                        {
                                            itemColumn.Item()
                                                .PaddingLeft(8)
                                                .Text($"+ {option.GroupName}: {option.OptionName} ({FormatCurrency(option.AdditionalPrice)})")
                                                .FontSize(8)
                                                .FontColor(Colors.Grey.Darken1);
                                        }
                                    }
                                });

                                row.ConstantItem(45).AlignCenter().Text($"x{item.Quantity}")
                                    .FontSize(10);

                                row.ConstantItem(90).AlignRight().Text(FormatCurrency(item.TotalPrice))
                                    .FontSize(10);
                            });
                        }
                    }
                });

                page.Footer().Column(footer =>
                {
                    footer.Item().LineHorizontal(1).LineColor(Colors.Grey.Lighten2);

                    footer.Item().PaddingTop(10).Row(row =>
                    {
                        row.RelativeItem().AlignRight().Text("TỔNG CỘNG")
                            .FontSize(11)
                            .SemiBold();

                        row.ConstantItem(120).AlignRight().Text(FormatCurrency(grandTotal))
                            .Bold()
                            .FontSize(15);
                    });

                    footer.Item().PaddingTop(4).AlignRight().Text($"Số đơn: {totalOrders}   |   Số món: {totalItems}")
                        .FontSize(9)
                        .FontColor(Colors.Grey.Darken1);
                });
            });
        }).GeneratePdf();
    }
}
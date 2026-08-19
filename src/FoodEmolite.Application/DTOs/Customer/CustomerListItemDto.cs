namespace FoodEmolite.Application.DTOs.Customer;

/// <summary>
/// 1 khách hàng đã từng đặt đơn tại (các) cửa hàng — gộp cả tài khoản đã đăng ký (Role=User)
/// lẫn khách vãng lai (Customer/DeviceId), kèm số liệu mua hàng.
/// </summary>
public class CustomerListItemDto
{
    public string RefCode { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public string? Email { get; set; }
    public string? AvatarUrl { get; set; }

    /// <summary>true = khách vãng lai (không có tài khoản), false = khách đã đăng ký tài khoản.</summary>
    public bool IsGuest { get; set; }

    public int TotalOrders { get; set; }
    public decimal TotalSpent { get; set; }
    public DateTime? LastOrderAt { get; set; }

    public string StoreRefCode { get; set; } = string.Empty;
    public string StoreName { get; set; } = string.Empty;
}

public class CustomerSearchRequest
{
    public string? Keyword { get; set; }

    /// <summary>Chỉ dùng cho admin — lọc theo 1 cửa hàng cụ thể. Null = tất cả cửa hàng.</summary>
    public string? StoreRefCode { get; set; }
}

namespace FoodEmolite.Application.DTOs.Revenue;

/// <summary>
/// Doanh thu gộp theo 1 món (dùng cho cả "Top sản phẩm bán chạy" và bảng "Doanh thu theo sản phẩm").
/// Chỉ tính trên các đơn đã thanh toán (PAID), khớp định nghĩa doanh thu đang dùng ở dashboard.
/// </summary>
public class TopSellingProductDto
{
    public long StoreFoodId { get; set; }
    public string FoodName { get; set; } = string.Empty;
    public string? ThumbnailUrl { get; set; }
    public int QuantitySold { get; set; }
    public decimal Revenue { get; set; }

    /// <summary>Chỉ có ý nghĩa ở màn admin (xem nhiều cửa hàng cùng lúc).</summary>
    public string StoreRefCode { get; set; } = string.Empty;
    public string StoreName { get; set; } = string.Empty;
}

public class ProductRevenueSearchRequest
{
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public string? Keyword { get; set; }

    /// <summary>Chỉ dùng cho admin — lọc theo 1 cửa hàng cụ thể. Null = tất cả cửa hàng.</summary>
    public string? StoreRefCode { get; set; }
}

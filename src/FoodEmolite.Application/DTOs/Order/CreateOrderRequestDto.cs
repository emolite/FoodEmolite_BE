namespace FoodEmolite.Application.DTOs.Order
{
    public class CreateOrderRequestDto
    {
        public string StoreRefCode { get; set; }

        public string? Note { get; set; }

        public List<CreateOrderItemRequestDto> Items { get; set; } = new();

        public List<SelectedGiftRequestDto> SelectedGifts { get; set; } = new();

        // Mã khuyến mãi khách nhập (không bắt buộc) — chỉ những promotion có promotion_code
        // mới cần mã này để được áp dụng; promotion không có mã sẽ tự áp dụng như bình thường.
        public string? PromoCode { get; set; }
    }

    public class SelectedGiftRequestDto
    {
        public long PromotionId { get; set; }

        public long StoreFoodId { get; set; }
    }

    public class CreateOrderItemRequestDto
    {
        public long StoreFoodId { get; set; }

        public int Quantity { get; set; }

        public List<CreateOrderItemOptionRequestDto> Options { get; set; } = new();
    }

    public class CreateOrderItemOptionRequestDto
    {
        public long OptionGroupId { get; set; }

        public string OptionGroupName { get; set; }

        public long OptionId { get; set; }

        public string OptionName { get; set; }

        public decimal AdditionalPrice { get; set; }
    }
}
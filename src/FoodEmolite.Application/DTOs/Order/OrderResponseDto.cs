namespace FoodEmolite.Application.DTOs.Order
{
    public class OrderResponseDto
    {
        public long Id { get; set; }

        public string OrderCode { get; set; }

        public string RefCode { get; set; }

        public long CustomerAccountId { get; set; }

        public string StoreRefCode { get; set; }

        public decimal TotalAmount { get; set; }

        public string OrderStatus { get; set; }

        public string PaymentStatus { get; set; }

        public string? Note { get; set; }

        public DateTime CreatedAt { get; set; }

        public List<OrderItemResponseDto> Items { get; set; } = new();
    }

    public class OrderItemResponseDto
    {
        public long Id { get; set; }

        public long OrderId { get; set; }

        public long StoreFoodId { get; set; }

        public string FoodName { get; set; }

        public int Quantity { get; set; }

        public decimal UnitPrice { get; set; }

        public decimal TotalPrice { get; set; }

        public List<OrderItemOptionResponseDto> Options { get; set; } = new();
    }

    public class OrderItemOptionResponseDto
    {
        public long Id { get; set; }

        public long OrderItemId { get; set; }

        public long? OptionGroupId { get; set; }

        public string OptionGroupName { get; set; }

        public long? OptionId { get; set; }

        public string OptionName { get; set; }

        public decimal AdditionalPrice { get; set; }
    }
}
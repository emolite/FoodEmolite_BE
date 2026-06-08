using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FoodEmolite.Application.DTOs.Order
{
    public class OrderResponseDto
    {
        public long Id { get; set; }

        public string RefCode { get; set; }

        public long CustomerAccountId { get; set; }

        public string StoreRefCode { get; set; }

        public decimal TotalAmount { get; set; }

        public string OrderStatus { get; set; }

        public string PaymentStatus { get; set; }

        public string? Note { get; set; }

        public DateTime CreatedAt { get; set; }

        public List<OrderItemResponseDto> Items { get; set; }
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
    }
}

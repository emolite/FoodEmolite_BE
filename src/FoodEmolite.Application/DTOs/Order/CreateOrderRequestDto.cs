using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FoodEmolite.Application.DTOs.Order
{
    public class CreateOrderRequestDto
    {
        public string StoreRefCode { get; set; }

        public string? Note { get; set; }

        public List<CreateOrderItemRequestDto> Items { get; set; }
    }

    public class CreateOrderItemRequestDto
    {
        public long StoreFoodId { get; set; }

        public int Quantity { get; set; }
    }
}

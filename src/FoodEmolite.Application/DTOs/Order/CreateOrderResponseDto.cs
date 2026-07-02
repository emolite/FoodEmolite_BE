using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FoodEmolite.Application.DTOs.Order
{
    public class CreateOrderResponseDto
    {
        public long OrderId { get; set; }
        public string OrderCode { get; set; }
        public string PaymentStatus { get; set; }
        public decimal TotalAmount { get; set; }
    }
}

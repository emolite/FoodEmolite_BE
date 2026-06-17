using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FoodEmolite.Application.DTOs.Order
{
    public class OrderSearchRequest
    {
        public string? Keyword { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public string? StoreRefCode { get; set; }
        public string? OrderStatus { get; set; }
        public string? PaymentStatus { get; set; }
    }
}

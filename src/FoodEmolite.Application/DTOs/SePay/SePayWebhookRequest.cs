using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FoodEmolite.Application.DTOs.SePay
{
    public class SePayWebhookRequest
    {
        public string TransactionId { get; set; }
        public string Content { get; set; }
        public string AccountNumber { get; set; }
        public long Amount { get; set; }
        public string? Description { get; set; }
    }
}

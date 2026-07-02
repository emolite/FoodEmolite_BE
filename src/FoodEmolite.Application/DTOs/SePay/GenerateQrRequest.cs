using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FoodEmolite.Application.DTOs.SePay
{
    public class GenerateQrRequest
    {
        public string BankBin { get; set; } = default!;
        public string AccountNo { get; set; } = default!;
        public decimal Amount { get; set; }
        public string OrderCode { get; set; } = default!;
    }
}

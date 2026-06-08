using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FoodEmolite.Application.DTOs.Profile
{
    public class StorePaymentInfoResponseDto
    {
        public long StoreId { get; set; }

        public string StoreRefCode { get; set; }

        public string StoreName { get; set; }

        public long BankAccountId { get; set; }

        public string BankName { get; set; }

        public string BankCode { get; set; }

        public string AccountNumber { get; set; }

        public string AccountHolderName { get; set; }

        public decimal Amount { get; set; }

        public string VietQrUrl { get; set; }
    }
}

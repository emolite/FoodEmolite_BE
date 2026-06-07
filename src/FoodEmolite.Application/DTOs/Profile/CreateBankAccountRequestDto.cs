using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FoodEmolite.Application.DTOs.Profile
{
    public class CreateBankAccountRequestDto
    {
        public string BankName { get; set; }

        public string? BankCode { get; set; }

        public string AccountNumber { get; set; }

        public string AccountHolderName { get; set; }

        public bool IsDefault { get; set; }
    }
}

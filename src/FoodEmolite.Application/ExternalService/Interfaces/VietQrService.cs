using FoodEmolite.Application.DTOs.SePay;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FoodEmolite.Application.ExternalService.Interfaces
{
    public class VietQrService : IVietQrService
    {
        public string Generate(GenerateQrRequest request)
        {
            var content = Uri.EscapeDataString(request.OrderCode);

            return $"https://img.vietqr.io/image/{request.BankBin}-{request.AccountNo}-compact.png?amount={request.Amount}&addInfo={content}";
        }
    }
}

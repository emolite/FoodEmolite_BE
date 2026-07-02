using FoodEmolite.Application.DTOs.SePay;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FoodEmolite.Application.ExternalService.Interfaces
{
    public interface IVietQrService
    {
        string Generate(GenerateQrRequest request);
    }
}

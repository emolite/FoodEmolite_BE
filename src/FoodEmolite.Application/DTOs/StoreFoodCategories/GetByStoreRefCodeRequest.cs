using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FoodEmolite.Application.DTOs.StoreFoodCategories
{
    public class GetByStoreRefCodeRequest
    {
        public string StoreRefCode { get; set; } = null!;
    }
}

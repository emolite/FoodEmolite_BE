using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FoodEmolite.Application.DTOs.StoreFood
{
    public class GetStoreFoodsRequest
    {
        public string StoreRefCode { get; set; } = null!;
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public long? StoreFoodCategoryId { get; set; }
    }
}

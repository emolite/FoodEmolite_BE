using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FoodEmolite.Application.DTOs.StoreFoodCategories
{
    public class CreateStoreFoodCategoryRequest
    {
        public string CategoryName { get; set; }

        public string? Description { get; set; }
    }
}

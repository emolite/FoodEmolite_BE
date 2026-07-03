using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FoodEmolite.Application.DTOs.StoreFoodCategories
{
    public class StoreFoodCategoryResponseDto
    {
        public long Id { get; set; }

        public string RefCode { get; set; }

        public string CategoryName { get; set; }

        public string? Description { get; set; }

        public DateTime CreatedAt { get; set; }
    }
}

using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FoodEmolite.Application.DTOs.StoreFood
{
    public class UpdateStoreFoodRequestDto
    {
        public string FoodName { get; set; }

        public IFormFile? ThumbnailFile { get; set; }

        public string? Description { get; set; }

        public decimal Price { get; set; }

        public int Quantity { get; set; }

        public long StoreFoodCategoryId { get; set; }

        public bool IsAvailable { get; set; }

        public List<StoreFoodOptionGroupRequestDto>? OptionGroups { get; set; }
    }
}

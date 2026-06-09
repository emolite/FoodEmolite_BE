using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FoodEmolite.Application.DTOs.StoreFood
{
    public class CreateStoreFoodRequestDto
    {
        public string StoreRefCode { get; set; }

        public string FoodName { get; set; }

        public IFormFile? ThumbnailFile { get; set; }

        public string? Description { get; set; }

        public decimal Price { get; set; }

        public int Quantity { get; set; }

        public List<StoreFoodOptionGroupRequestDto>? OptionGroups { get; set; }
    }

    public class StoreFoodOptionGroupRequestDto
    {
        public string GroupName { get; set; }
        public bool IsRequired { get; set; }
        public int MinSelect { get; set; }
        public int MaxSelect { get; set; }
        public int SortOrder { get; set; }
        public List<StoreFoodOptionRequestDto> Options { get; set; } = new();
    }

    public class StoreFoodOptionRequestDto
    {
        public string OptionName { get; set; }
        public decimal AdditionalPrice { get; set; }
        public bool IsAvailable { get; set; } = true;
        public int SortOrder { get; set; }
    }
}

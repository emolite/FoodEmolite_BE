using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FoodEmolite.Application.DTOs.StoreFood
{
    public class StoreFoodResponseDto
    {
        public long Id { get; set; }

        public string RefCode { get; set; }

        public string StoreRefCode { get; set; }

        public string StoreName { get; set; }

        public string FoodName { get; set; }

        public string? ThumbnailUrl { get; set; }

        public string? Description { get; set; }

        public decimal Price { get; set; }

        public int Quantity { get; set; }

        public bool IsAvailable { get; set; }
        public List<StoreFoodOptionGroupResponseDto> OptionGroups { get; set; } = new();
    }

    public class StoreFoodOptionGroupResponseDto
    {
        public long Id { get; set; }
        public string RefCode { get; set; }
        public string GroupName { get; set; }
        public bool IsRequired { get; set; }
        public int MinSelect { get; set; }
        public int MaxSelect { get; set; }
        public int SortOrder { get; set; }
        public List<StoreFoodOptionResponseDto> Options { get; set; } = new();
    }

    public class StoreFoodOptionResponseDto
    {
        public long Id { get; set; }
        public string RefCode { get; set; }
        public string OptionName { get; set; }
        public decimal AdditionalPrice { get; set; }
        public bool IsAvailable { get; set; }
        public int SortOrder { get; set; }
    }
}

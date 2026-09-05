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

        /// <summary>Chỉ được điền khi lấy danh sách toàn hệ thống (admin), null ở các API theo store.</summary>
        public string? StoreRefCode { get; set; }

        public string? StoreName { get; set; }
    }
}

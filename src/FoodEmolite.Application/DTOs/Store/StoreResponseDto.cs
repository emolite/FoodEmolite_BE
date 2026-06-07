using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FoodEmolite.Application.DTOs.Store
{
    public class StoreResponseDto
    {
        public long Id { get; set; }

        public string RefCode { get; set; }

        public long? OwnerAccountId { get; set; }

        public string StoreName { get; set; }

        public string? ThumbnailUrl { get; set; }

        public string? PhoneNumber { get; set; }

        public string? Address { get; set; }

        public string? Description { get; set; }

        public bool IsActive { get; set; }
        public DateTime? CreatedAt { get; set; }
    }
}

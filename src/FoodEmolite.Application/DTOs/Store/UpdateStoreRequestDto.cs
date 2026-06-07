using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FoodEmolite.Application.DTOs.Store
{
    public class UpdateStoreRequestDto
    {
        public string StoreName { get; set; }

        public IFormFile? ThumbnailFile { get; set; }

        public string? ThumbnailFileRefCode { get; set; }

        public string? PhoneNumber { get; set; }

        public string? Address { get; set; }

        public string? Description { get; set; }

        public bool IsActive { get; set; }
    }
}

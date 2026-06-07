using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FoodEmolite.Application.DTOs.Profile
{
    public class UpdateAccountProfileRequestDto
    {
        public string FullName { get; set; }

        public string? PhoneNumber { get; set; }

        public string? Gender { get; set; }

        public DateOnly? DateOfBirth { get; set; }

        public string? Address { get; set; }

        public IFormFile? AvatarUrl { get; set; }
    }
}

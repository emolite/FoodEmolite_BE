using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FoodEmolite.Application.DTOs.Profile
{
    public class UserProfileResponseDto
    {
        public AccountDto Account { get; set; } = null!;
        public AccountProfileDto? Profile { get; set; }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FoodEmolite.Application.DTOs.Profile
{
    public class GuestProfileResponseDto
    {
        public long CustomerId { get; set; }

        public string CustomerName { get; set; } = string.Empty;
    }
}

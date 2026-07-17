using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FoodEmolite.Application.DTOs.Order
{
    public class CreateGuestOrderRequestDto : CreateOrderRequestDto
    {
        public string CustomerName { get; set; } = null!;

        public string? DeviceId { get; set; }
    }
}

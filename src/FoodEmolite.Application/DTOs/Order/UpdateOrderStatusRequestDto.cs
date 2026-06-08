using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FoodEmolite.Application.DTOs.Order
{
    public class UpdateOrderStatusRequestDto
    {
        public string NewStatus { get; set; }

        public string? ChangedNote { get; set; }
    }
}

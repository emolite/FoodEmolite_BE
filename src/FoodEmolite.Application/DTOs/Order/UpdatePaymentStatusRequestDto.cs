using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FoodEmolite.Application.DTOs.Order
{
    public class UpdatePaymentStatusRequestDto
    {
        public string NewStatus { get; set; } = string.Empty;
        public string? ChangedNote { get; set; }
    }
}

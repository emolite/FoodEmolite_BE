using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FoodEmolite.Application.DTOs.Order
{
    public class PrintOrdersRequestDto
    {
        public List<long> OrderIds { get; set; } = new();
    }
}

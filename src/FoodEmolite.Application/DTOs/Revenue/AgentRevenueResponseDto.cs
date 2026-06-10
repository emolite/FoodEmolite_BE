using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FoodEmolite.Application.DTOs.Revenue
{
    public class AgentRevenueResponseDto
    {
        public int TotalOrders { get; set; }
        public decimal TotalRevenue { get; set; }

        public List<RevenueLineChartDto> LineChart { get; set; } = [];
        public List<RevenuePieChartDto> PieChart { get; set; } = [];
    }
}

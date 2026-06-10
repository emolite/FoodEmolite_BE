using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FoodEmolite.Application.DTOs.Revenue
{
    public class AdminRevenueResponseDto
    {
        public int TotalAgents { get; set; }
        public int TotalUsers { get; set; }
        public int TotalOrders { get; set; }
        public decimal TotalRevenue { get; set; }

        public List<RevenueLineChartDto> LineChart { get; set; } = [];
        public List<RevenuePieChartDto> PieChart { get; set; } = [];
    }

    public class RevenueLineChartDto
    {
        public string Label { get; set; } = string.Empty;
        public decimal Revenue { get; set; }
        public int OrderCount { get; set; }
    }

    public class RevenuePieChartDto
    {
        public string Label { get; set; } = string.Empty;
        public decimal Value { get; set; }
    }
}

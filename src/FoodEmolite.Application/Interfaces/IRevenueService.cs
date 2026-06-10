using FoodEmolite.Application.DTOs.Revenue;
using FoodEmolite.Shared.Responses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FoodEmolite.Application.Interfaces
{
    public interface IRevenueService
    {
        Task<BaseResponse<AdminRevenueResponseDto>> GetAdminRevenueAsync(RevenueQueryDto request);

        Task<BaseResponse<AgentRevenueResponseDto>> GetAgentRevenueAsync(long currentUserId, RevenueQueryDto request);
    }
}

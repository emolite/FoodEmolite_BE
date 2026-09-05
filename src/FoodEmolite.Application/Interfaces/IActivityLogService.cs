using FoodEmolite.Application.DTOs.ActivityLog;
using FoodEmolite.Shared.Entities;
using FoodEmolite.Shared.Responses;

namespace FoodEmolite.Application.Interfaces;

public interface IActivityLogService
{
    Task LogAsync(string actorType, long? actorId, string? actorName, string action, string description);

    Task<BaseTableResponse<ActivityLogResponseDto>> SearchAsync(BaseSearchRequest<ActivityLogSearchRequest> request);
}

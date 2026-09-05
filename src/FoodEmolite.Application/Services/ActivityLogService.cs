using FoodEmolite.Shared.Common;
using FoodEmolite.Application.DTOs.ActivityLog;
using FoodEmolite.Application.Interfaces;
using FoodEmolite.Domain.Entities;
using FoodEmolite.Domain.Interfaces;
using FoodEmolite.Shared.Entities;
using FoodEmolite.Shared.Responses;
using Microsoft.EntityFrameworkCore;

namespace FoodEmolite.Application.Services;

public class ActivityLogService : IActivityLogService
{
    private readonly IUnitOfWork _unitOfWork;

    public ActivityLogService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task LogAsync(string actorType, long? actorId, string? actorName, string action, string description)
    {
        var repo = _unitOfWork.GetRepository<ActivityLog>();

        await repo.AddAsync(new ActivityLog
        {
            ActorType = actorType,
            ActorId = actorId,
            ActorName = actorName,
            Action = action,
            Description = description,
            CreatedAt = DateTimeHelper.VnNow
        });

        await _unitOfWork.SaveChangesAsync();
    }

    public async Task<BaseTableResponse<ActivityLogResponseDto>> SearchAsync(BaseSearchRequest<ActivityLogSearchRequest> request)
    {
        var repo = _unitOfWork.GetRepository<ActivityLog>();

        request.Page = request.Page <= 0 ? 1 : request.Page;
        request.PageSize = request.PageSize <= 0 ? 10 : request.PageSize;

        var search = request.SearchParams;

        var query = repo.Query().AsNoTracking();

        var trimmedKeyword = search?.Keyword?.Trim();

        if (!string.IsNullOrWhiteSpace(trimmedKeyword))
        {
            query = query.Where(x =>
                x.Description.Contains(trimmedKeyword) ||
                (x.ActorName != null && x.ActorName.Contains(trimmedKeyword)));
        }

        if (!string.IsNullOrWhiteSpace(search?.Action))
        {
            query = query.Where(x => x.Action == search.Action);
        }

        if (search?.FromDate != null)
        {
            var fromDate = search.FromDate.Value.Date;
            query = query.Where(x => x.CreatedAt >= fromDate);
        }

        if (search?.ToDate != null)
        {
            var toDate = search.ToDate.Value.Date.AddDays(1);
            query = query.Where(x => x.CreatedAt < toDate);
        }

        var totalRecords = await query.CountAsync();

        query = request.Asc
            ? query.OrderBy(x => x.CreatedAt)
            : query.OrderByDescending(x => x.CreatedAt);

        var items = await query
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(x => new ActivityLogResponseDto
            {
                Id = x.Id,
                ActorType = x.ActorType,
                ActorId = x.ActorId,
                ActorName = x.ActorName,
                Action = x.Action,
                Description = x.Description,
                CreatedAt = x.CreatedAt
            })
            .ToListAsync();

        return new BaseTableResponse<ActivityLogResponseDto>
        {
            Items = items,
            Page = request.Page,
            PageSize = request.PageSize,
            TotalRecords = totalRecords
        };
    }
}

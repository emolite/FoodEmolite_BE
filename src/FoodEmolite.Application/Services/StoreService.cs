using FoodEmolite.Shared.Common;
using FoodEmolite.Application.DTOs.Store;
using FoodEmolite.Application.ExternalService.Interfaces;
using FoodEmolite.Application.Interfaces;
using FoodEmolite.Domain.Entities;
using FoodEmolite.Domain.Interfaces;
using FoodEmolite.Shared.Responses;
using Microsoft.EntityFrameworkCore;

namespace FoodEmolite.Application.Services;

public class StoreService : IStoreService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICloudinaryService _cloudinaryService;
    private readonly IActivityLogService _activityLogService;

    public StoreService(
        IUnitOfWork unitOfWork,
        ICloudinaryService cloudinaryService,
        IActivityLogService activityLogService)
    {
        _unitOfWork = unitOfWork;
        _cloudinaryService = cloudinaryService;
        _activityLogService = activityLogService;
    }

    public async Task<BaseResponse<string>> CreateAsync(long currentUserId, CreateStoreRequestDto request)
    {
        var repoStore = _unitOfWork.GetRepository<Store>();

        string? thumbnailFileRefCode = null;

        if (request.ThumbnailFile != null && request.ThumbnailFile.Length > 0)
        {
            var uploadResult = await _cloudinaryService.UploadStoreImageAsync(
                request.ThumbnailFile);

            if (!uploadResult.IsSuccess)
                return BaseResponse<string>.Fail(uploadResult.Message);

            thumbnailFileRefCode = uploadResult.Data;
        }

        var store = new Store
        {
            RefCode = Guid.NewGuid().ToString().ToUpper(),
            StoreName = request.StoreName,
            OwnerAccountId = request.OwnerAccountId,
            ThumbnailUrl = thumbnailFileRefCode,
            PhoneNumber = request.PhoneNumber,
            Address = request.Address,
            Description = request.Description,
            IsActive = true,
            IsDeleted = false,
            CreatedAt = DateTimeHelper.VnNow,
            CreatedBy = currentUserId
        };

        await repoStore.AddAsync(store);
        await _unitOfWork.SaveChangesAsync();

        await _activityLogService.LogAsync(
            currentUserId > 0 ? "Agent" : "System",
            currentUserId > 0 ? currentUserId : null,
            null,
            "CREATE_STORE",
            $"Tạo cửa hàng \"{store.StoreName}\"");

        return BaseResponse<string>.Success("Create store successfully");
    }

    public async Task<BaseResponse<string>> UpdateAsync(long id, long currentUserId, UpdateStoreRequestDto request)
    {
        var repoStore = _unitOfWork.GetRepository<Store>();

        var store = await repoStore.FirstOrDefaultAsync(x =>
            x.Id == id &&
            !x.IsDeleted);

        if (store is null)
            return BaseResponse<string>.Fail("Store not found");

        if (request.ThumbnailFile != null && request.ThumbnailFile.Length > 0)
        {
            var uploadResult = await _cloudinaryService.UploadStoreImageAsync(
                request.ThumbnailFile);

            if (!uploadResult.IsSuccess)
                return BaseResponse<string>.Fail(uploadResult.Message);

            store.ThumbnailUrl = uploadResult.Data;
        }

        store.StoreName = request.StoreName;
        store.PhoneNumber = request.PhoneNumber;
        store.Address = request.Address;
        store.Description = request.Description;
        store.IsActive = request.IsActive;
        store.UpdatedAt = DateTimeHelper.VnNow;
        store.UpdatedBy = currentUserId;

        repoStore.Update(store);
        await _unitOfWork.SaveChangesAsync();

        return BaseResponse<string>.Success("Update store successfully");
    }

    public async Task<BaseResponse<string>> DeleteAsync(long id, long currentUserId)
    {
        var repoStore = _unitOfWork.GetRepository<Store>();

        var store = await repoStore.FirstOrDefaultAsync(x =>
            x.Id == id );

        if (store is null)
            return BaseResponse<string>.Fail("Store not found");

        store.IsDeleted = true;
        store.UpdatedAt = DateTimeHelper.VnNow;
        store.UpdatedBy = currentUserId;

        repoStore.Update(store);
        await _unitOfWork.SaveChangesAsync();

        return BaseResponse<string>.Success("Delete store successfully");
    }

    public async Task<BaseTableResponse<StoreResponseDto>> GetAllAsync(int page, int pageSize, string? keyword = null, bool? isActive = null)
    {
        var repoStore = _unitOfWork.GetRepository<Store>();

        page = page <= 0 ? 1 : page;
        pageSize = pageSize <= 0 ? 10 : pageSize;

        var query = repoStore
            .Query()
            .AsNoTracking()
            .Where(x => !x.IsDeleted);

        var trimmedKeyword = keyword?.Trim();

        if (!string.IsNullOrWhiteSpace(trimmedKeyword))
        {
            query = query.Where(x => x.StoreName.Contains(trimmedKeyword));
        }

        if (isActive.HasValue)
        {
            query = query.Where(x => x.IsActive == isActive.Value);
        }

        var totalRecords = await query.CountAsync();

        var items = await query
            .OrderByDescending(x => x.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new StoreResponseDto
            {
                Id = x.Id,
                RefCode = x.RefCode,
                OwnerAccountId = x.OwnerAccountId,
                StoreName = x.StoreName,
                ThumbnailUrl = !string.IsNullOrWhiteSpace(x.ThumbnailUrl)
                    ? _cloudinaryService.BuildImageUrl(x.ThumbnailUrl)
                    : null,
                PhoneNumber = x.PhoneNumber,
                Address = x.Address,
                Description = x.Description,
                CreatedAt = x.CreatedAt,
                IsActive = x.IsActive
            })
            .ToListAsync();

        return new BaseTableResponse<StoreResponseDto>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalRecords = totalRecords
        };
    }

    public async Task<BaseTableResponse<StoreResponseDto>> GetByOwnerRefCodeAsync(string ownerRefCode, int page, int pageSize)
    {
        var repoAccount = _unitOfWork.GetRepository<Account>();
        var repoStore = _unitOfWork.GetRepository<Store>();

        page = page <= 0 ? 1 : page;
        pageSize = pageSize <= 0 ? 10 : pageSize;

        var account = await repoAccount.FirstOrDefaultAsync(x =>
            x.RefCode == ownerRefCode);

        if (account is null)
        {
            return new BaseTableResponse<StoreResponseDto>
            {
                Items = new List<StoreResponseDto>(),
                Page = page,
                PageSize = pageSize,
                TotalRecords = 0
            };
        }

        var query = repoStore
            .Query()
            .AsNoTracking()
            .Where(x =>
                x.OwnerAccountId == account.Id &&
                !x.IsDeleted);

        var totalRecords = await query.CountAsync();

        var items = await query
            .OrderByDescending(x => x.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new StoreResponseDto
            {
                Id = x.Id,
                RefCode = x.RefCode,
                OwnerAccountId = x.OwnerAccountId,
                StoreName = x.StoreName,
                ThumbnailUrl = !string.IsNullOrWhiteSpace(x.ThumbnailUrl)
                    ? _cloudinaryService.BuildImageUrl(x.ThumbnailUrl)
                    : null,
                PhoneNumber = x.PhoneNumber,
                Address = x.Address,
                Description = x.Description,
                IsActive = x.IsActive
            })
            .ToListAsync();

        return new BaseTableResponse<StoreResponseDto>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalRecords = totalRecords
        };
    }

    public async Task<BaseResponse<StoreResponseDto>> GetDetailAsync(long id)
    {
        var repoStore = _unitOfWork.GetRepository<Store>();

        var store = await repoStore.FirstOrDefaultAsync(x =>
            x.Id == id);

        if (store is null)
            return BaseResponse<StoreResponseDto>.Fail("Store not found");

        return BaseResponse<StoreResponseDto>.Success(new StoreResponseDto
        {
            Id = store.Id,
            RefCode = store.RefCode,
            OwnerAccountId = store.OwnerAccountId,
            StoreName = store.StoreName,
            ThumbnailUrl = !string.IsNullOrWhiteSpace(store.ThumbnailUrl)
                ? _cloudinaryService.BuildImageUrl(store.ThumbnailUrl)
                : null,
            PhoneNumber = store.PhoneNumber,
            Address = store.Address,
            Description = store.Description,
            IsActive = store.IsActive,
            CreatedAt = store.CreatedAt
        });
    }

    public async Task<BaseResponse<StoreResponseDto>> GetByRefCodeAsync(string refCode)
    {
        var repoStore = _unitOfWork.GetRepository<Store>();

        var store = await repoStore.FirstOrDefaultAsync(x =>
            x.RefCode == refCode &&
            !x.IsDeleted &&
            x.IsActive);

        if (store is null)
            return BaseResponse<StoreResponseDto>.Fail("Store not found");

        return BaseResponse<StoreResponseDto>.Success(new StoreResponseDto
        {
            Id = store.Id,
            RefCode = store.RefCode,
            OwnerAccountId = store.OwnerAccountId,
            StoreName = store.StoreName,
            ThumbnailUrl = !string.IsNullOrWhiteSpace(store.ThumbnailUrl)
                ? _cloudinaryService.BuildImageUrl(store.ThumbnailUrl)
                : null,
            PhoneNumber = store.PhoneNumber,
            Address = store.Address,
            Description = store.Description,
            IsActive = store.IsActive,
            CreatedAt = store.CreatedAt
        });
    }
}
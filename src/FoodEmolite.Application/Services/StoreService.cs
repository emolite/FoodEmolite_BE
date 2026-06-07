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

    public StoreService(
        IUnitOfWork unitOfWork,
        ICloudinaryService cloudinaryService)
    {
        _unitOfWork = unitOfWork;
        _cloudinaryService = cloudinaryService;
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
            ThumbnailUrl = thumbnailFileRefCode,
            PhoneNumber = request.PhoneNumber,
            Address = request.Address,
            Description = request.Description,
            IsActive = true,
            IsDeleted = false,
            CreatedAt = DateTime.Now,
            CreatedBy = currentUserId
        };

        await repoStore.AddAsync(store);
        await _unitOfWork.SaveChangesAsync();

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
        store.UpdatedAt = DateTime.Now;
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
        store.UpdatedAt = DateTime.Now;
        store.UpdatedBy = currentUserId;

        repoStore.Update(store);
        await _unitOfWork.SaveChangesAsync();

        return BaseResponse<string>.Success("Delete store successfully");
    }

    public async Task<BaseTableResponse<StoreResponseDto>> GetAllAsync(int page, int pageSize)
    {
        var repoStore = _unitOfWork.GetRepository<Store>();

        page = page <= 0 ? 1 : page;
        pageSize = pageSize <= 0 ? 10 : pageSize;

        var query = repoStore
            .Query()
            .AsNoTracking()
            .Where(x => !x.IsDeleted);

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
                ThumbnailUrl = x.ThumbnailUrl,
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
}
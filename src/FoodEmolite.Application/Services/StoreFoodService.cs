using FoodEmolite.Application.DTOs.StoreFood;
using FoodEmolite.Application.ExternalService.Interfaces;
using FoodEmolite.Application.Interfaces;
using FoodEmolite.Domain.Entities;
using FoodEmolite.Domain.Interfaces;
using FoodEmolite.Shared.Responses;
using Microsoft.EntityFrameworkCore;

namespace FoodEmolite.Application.Services;

public class StoreFoodService : IStoreFoodService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICloudinaryService _cloudinaryService;

    public StoreFoodService(
        IUnitOfWork unitOfWork,
        ICloudinaryService cloudinaryService)
    {
        _unitOfWork = unitOfWork;
        _cloudinaryService = cloudinaryService;
    }

    public async Task<BaseResponse<string>> CreateAsync(string refCode,CreateStoreFoodRequestDto request)
    {
        var repoStore = _unitOfWork.GetRepository<Store>();
        var repoStoreFood = _unitOfWork.GetRepository<StoreFood>();

        var store = await repoStore.FirstOrDefaultAsync(x =>
            x.RefCode == request.StoreRefCode &&
            !x.IsDeleted);

        if (store is null)
            return BaseResponse<string>.Fail("Store not found");

        string? thumbnailFileRefCode = null;

        if (request.ThumbnailFile != null && request.ThumbnailFile.Length > 0)
        {
            var uploadResult = await _cloudinaryService.UploadProductImageAsync(
                request.ThumbnailFile);

            if (!uploadResult.IsSuccess)
                return BaseResponse<string>.Fail(uploadResult.Message);

            thumbnailFileRefCode = uploadResult.Data;
        }

        var storeFood = new StoreFood
        {
            RefCode = refCode,
            StoreRefCode = request.StoreRefCode,
            FoodName = request.FoodName,
            ThumbnailUrl = thumbnailFileRefCode,
            Description = request.Description,
            Price = request.Price,
            Quantity = request.Quantity,
            IsAvailable = true,
            IsDeleted = false,
            CreatedAt = DateTime.Now
        };

        await repoStoreFood.AddAsync(storeFood);
        await _unitOfWork.SaveChangesAsync();

        return BaseResponse<string>.Success("Create store food successfully");
    }

    public async Task<BaseResponse<string>> UpdateAsync(long id, UpdateStoreFoodRequestDto request)
    {
        var repoStoreFood = _unitOfWork.GetRepository<StoreFood>();

        var storeFood = await repoStoreFood.FirstOrDefaultAsync(x =>
            x.Id == id &&
            !x.IsDeleted);

        if (storeFood is null)
            return BaseResponse<string>.Fail("Store food not found");

        if (request.ThumbnailFile != null && request.ThumbnailFile.Length > 0)
        {
            var uploadResult = await _cloudinaryService.UploadProductImageAsync(
                request.ThumbnailFile);

            if (!uploadResult.IsSuccess)
                return BaseResponse<string>.Fail(uploadResult.Message);

            storeFood.ThumbnailUrl = uploadResult.Data;
        }

        storeFood.FoodName = request.FoodName;
        storeFood.Description = request.Description;
        storeFood.Price = request.Price;
        storeFood.Quantity = request.Quantity;
        storeFood.IsAvailable = request.IsAvailable;

        repoStoreFood.Update(storeFood);
        await _unitOfWork.SaveChangesAsync();

        return BaseResponse<string>.Success("Update store food successfully");
    }

    public async Task<BaseResponse<string>> DeleteAsync(long id)
    {
        var repoStoreFood = _unitOfWork.GetRepository<StoreFood>();

        var storeFood = await repoStoreFood.FirstOrDefaultAsync(x =>
            x.Id == id &&
            !x.IsDeleted);

        if (storeFood is null)
            return BaseResponse<string>.Fail("Store food not found");

        storeFood.IsDeleted = true;

        repoStoreFood.Update(storeFood);
        await _unitOfWork.SaveChangesAsync();

        return BaseResponse<string>.Success("Delete store food successfully");
    }

    public async Task<BaseTableResponse<StoreFoodResponseDto>> GetAllAsync(int page, int pageSize)
    {
        var repoStoreFood = _unitOfWork.GetRepository<StoreFood>();
        var repoStore = _unitOfWork.GetRepository<Store>();

        page = page <= 0 ? 1 : page;
        pageSize = pageSize <= 0 ? 10 : pageSize;

        var query =
            from storeFood in repoStoreFood.Query().AsNoTracking()
            join store in repoStore.Query().AsNoTracking()
                on storeFood.StoreRefCode equals store.RefCode
            where !storeFood.IsDeleted
            select new StoreFoodResponseDto
            {
                Id = storeFood.Id,
                RefCode = storeFood.RefCode,
                StoreRefCode = storeFood.StoreRefCode,
                StoreName = store.StoreName,
                FoodName = storeFood.FoodName,
                ThumbnailUrl = !string.IsNullOrWhiteSpace(storeFood.ThumbnailUrl)
                    ? _cloudinaryService.BuildImageUrl(storeFood.ThumbnailUrl)
                    : null,
                Description = storeFood.Description,
                Price = storeFood.Price,
                Quantity = storeFood.Quantity,
                IsAvailable = storeFood.IsAvailable
            };

        var totalRecords = await query.CountAsync();

        var items = await query
            .OrderByDescending(x => x.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new BaseTableResponse<StoreFoodResponseDto>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalRecords = totalRecords
        };
    }

    public async Task<BaseTableResponse<StoreFoodResponseDto>> GetByStoreRefCodeAsync(
        string storeRefCode,
        int page,
        int pageSize)
    {
        var repoStoreFood = _unitOfWork.GetRepository<StoreFood>();

        page = page <= 0 ? 1 : page;
        pageSize = pageSize <= 0 ? 10 : pageSize;

        var query = repoStoreFood
            .Query()
            .AsNoTracking()
            .Where(x =>
                x.StoreRefCode == storeRefCode &&
                !x.IsDeleted);

        var totalRecords = await query.CountAsync();

        var items = await query
            .OrderByDescending(x => x.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new StoreFoodResponseDto
            {
                Id = x.Id,
                RefCode = x.RefCode,
                StoreRefCode = x.StoreRefCode,
                FoodName = x.FoodName,
                ThumbnailUrl = !string.IsNullOrWhiteSpace(x.ThumbnailUrl)
                    ? _cloudinaryService.BuildImageUrl(x.ThumbnailUrl)
                    : null,
                Description = x.Description,
                Price = x.Price,
                Quantity = x.Quantity,
                IsAvailable = x.IsAvailable
            })
            .ToListAsync();

        return new BaseTableResponse<StoreFoodResponseDto>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalRecords = totalRecords
        };
    }

    public async Task<BaseResponse<StoreFoodResponseDto>> GetDetailAsync(long id)
    {
        var repoStoreFood = _unitOfWork.GetRepository<StoreFood>();

        var storeFood = await repoStoreFood.FirstOrDefaultAsync(x =>
            x.Id == id &&
            !x.IsDeleted);

        if (storeFood is null)
            return BaseResponse<StoreFoodResponseDto>.Fail("Store food not found");

        return BaseResponse<StoreFoodResponseDto>.Success(new StoreFoodResponseDto
        {
            Id = storeFood.Id,
            RefCode = storeFood.RefCode,
            StoreRefCode = storeFood.StoreRefCode,
            FoodName = storeFood.FoodName,
            ThumbnailUrl = !string.IsNullOrWhiteSpace(storeFood.ThumbnailUrl)
                ? _cloudinaryService.BuildImageUrl(storeFood.ThumbnailUrl)
                : null,
            Description = storeFood.Description,
            Price = storeFood.Price,
            Quantity = storeFood.Quantity,
            IsAvailable = storeFood.IsAvailable
        });
    }
}
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

    public async Task<BaseResponse<string>> CreateAsync(string refCode, CreateStoreFoodRequestDto request)
    {
        var repoStore = _unitOfWork.GetRepository<Store>();
        var repoStoreFood = _unitOfWork.GetRepository<StoreFood>();
        var repoOptionGroup = _unitOfWork.GetRepository<StoreFoodOptionGroup>();
        var repoOption = _unitOfWork.GetRepository<StoreFoodOption>();

        var store = await repoStore.FirstOrDefaultAsync(x =>
            x.RefCode == request.StoreRefCode &&
            !x.IsDeleted);

        if (store is null)
            return BaseResponse<string>.Fail("Store not found");

        string? thumbnailFileRefCode = null;

        if (request.ThumbnailFile != null && request.ThumbnailFile.Length > 0)
        {
            var uploadResult = await _cloudinaryService.UploadProductImageAsync(request.ThumbnailFile);

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

        if (request.OptionGroups != null && request.OptionGroups.Any())
        {
            foreach (var groupRequest in request.OptionGroups)
            {
                var optionGroup = new StoreFoodOptionGroup
                {
                    RefCode = refCode,
                    StoreFoodId = storeFood.Id,
                    GroupName = groupRequest.GroupName,
                    IsRequired = groupRequest.IsRequired,
                    MinSelect = groupRequest.MinSelect,
                    MaxSelect = groupRequest.MaxSelect,
                    SortOrder = groupRequest.SortOrder,
                    IsDeleted = false,
                    CreatedAt = DateTime.Now
                };

                await repoOptionGroup.AddAsync(optionGroup);
                await _unitOfWork.SaveChangesAsync();

                foreach (var optionRequest in groupRequest.Options)
                {
                    var option = new StoreFoodOption
                    {
                        RefCode = refCode,
                        OptionGroupId = optionGroup.Id,
                        OptionName = optionRequest.OptionName,
                        AdditionalPrice = optionRequest.AdditionalPrice,
                        IsAvailable = optionRequest.IsAvailable,
                        SortOrder = optionRequest.SortOrder,
                        IsDeleted = false,
                        CreatedAt = DateTime.Now
                    };

                    await repoOption.AddAsync(option);
                }
            }

            await _unitOfWork.SaveChangesAsync();
        }

        return BaseResponse<string>.Success("Create store food successfully");
    }

    public async Task<BaseResponse<string>> UpdateAsync(string refCode, long id, UpdateStoreFoodRequestDto request)
    {
        var repoStoreFood = _unitOfWork.GetRepository<StoreFood>();
        var repoOptionGroup = _unitOfWork.GetRepository<StoreFoodOptionGroup>();
        var repoOption = _unitOfWork.GetRepository<StoreFoodOption>();

        var storeFood = await repoStoreFood.FirstOrDefaultAsync(x =>
            x.Id == id &&
            !x.IsDeleted);

        if (storeFood is null)
            return BaseResponse<string>.Fail("Store food not found");

        if (request.ThumbnailFile != null && request.ThumbnailFile.Length > 0)
        {
            var uploadResult = await _cloudinaryService.UploadProductImageAsync(request.ThumbnailFile);

            if (!uploadResult.IsSuccess)
                return BaseResponse<string>.Fail(uploadResult.Message);

            storeFood.ThumbnailUrl = uploadResult.Data;
        }

        storeFood.FoodName = request.FoodName;
        storeFood.Description = request.Description;
        storeFood.Price = request.Price;
        storeFood.Quantity = request.Quantity;
        storeFood.IsAvailable = request.IsAvailable;
        storeFood.UpdatedAt = DateTime.Now;

        repoStoreFood.Update(storeFood);

        if (request.OptionGroups != null && request.OptionGroups.Any())
        {
            var requestGroupIds = request.OptionGroups
                .Where(x => x.Id.HasValue)
                .Select(x => x.Id!.Value)
                .Distinct()
                .ToList();

            var oldGroups = await repoOptionGroup.Query()
                .Where(x =>
                    x.StoreFoodId == storeFood.Id &&
                    requestGroupIds.Contains(x.Id))
                .ToListAsync();

            var oldGroupIds = oldGroups
                .Select(x => x.Id)
                .ToList();

            var requestOptionIds = request.OptionGroups
                .SelectMany(x => x.Options ?? new List<StoreFoodOptionRequestDto>())
                .Where(x => x.Id.HasValue)
                .Select(x => x.Id!.Value)
                .Distinct()
                .ToList();

            var oldOptions = await repoOption.Query()
                .Where(x =>
                    oldGroupIds.Contains(x.OptionGroupId) ||
                    requestOptionIds.Contains(x.Id))
                .ToListAsync();

            foreach (var groupRequest in request.OptionGroups)
            {
                StoreFoodOptionGroup? optionGroup = null;

                if (groupRequest.Id.HasValue)
                {
                    optionGroup = oldGroups.FirstOrDefault(x => x.Id == groupRequest.Id.Value);

                    if (optionGroup is null)
                        continue;

                    optionGroup.GroupName = groupRequest.GroupName;
                    optionGroup.IsRequired = groupRequest.IsRequired;
                    optionGroup.MinSelect = groupRequest.MinSelect;
                    optionGroup.MaxSelect = groupRequest.MaxSelect;
                    optionGroup.SortOrder = groupRequest.SortOrder;
                    optionGroup.IsDeleted = groupRequest.IsDeleted;
                    optionGroup.UpdatedAt = DateTime.Now;

                    repoOptionGroup.Update(optionGroup);

                    if (groupRequest.IsDeleted)
                    {
                        var optionsOfDeletedGroup = oldOptions
                            .Where(x => x.OptionGroupId == optionGroup.Id)
                            .ToList();

                        foreach (var option in optionsOfDeletedGroup)
                        {
                            option.IsDeleted = true;
                            option.UpdatedAt = DateTime.Now;
                            repoOption.Update(option);
                        }

                        continue;
                    }
                }
                else
                {
                    if (groupRequest.IsDeleted)
                        continue;

                    optionGroup = new StoreFoodOptionGroup
                    {
                        RefCode = refCode,
                        StoreFoodId = storeFood.Id,
                        GroupName = groupRequest.GroupName,
                        IsRequired = groupRequest.IsRequired,
                        MinSelect = groupRequest.MinSelect,
                        MaxSelect = groupRequest.MaxSelect,
                        SortOrder = groupRequest.SortOrder,
                        IsDeleted = false,
                        CreatedAt = DateTime.Now
                    };

                    await repoOptionGroup.AddAsync(optionGroup);
                    await _unitOfWork.SaveChangesAsync();
                }

                if (groupRequest.Options == null || !groupRequest.Options.Any())
                    continue;

                foreach (var optionRequest in groupRequest.Options)
                {
                    if (optionRequest.Id.HasValue)
                    {
                        var option = oldOptions.FirstOrDefault(x => x.Id == optionRequest.Id.Value);

                        if (option is null)
                            continue;

                        option.OptionName = optionRequest.OptionName;
                        option.AdditionalPrice = optionRequest.AdditionalPrice;
                        option.IsAvailable = optionRequest.IsAvailable;
                        option.SortOrder = optionRequest.SortOrder;
                        option.IsDeleted = optionRequest.IsDeleted;
                        option.UpdatedAt = DateTime.Now;

                        repoOption.Update(option);
                    }
                    else
                    {
                        if (optionRequest.IsDeleted)
                            continue;

                        var option = new StoreFoodOption
                        {
                            RefCode = refCode,
                            OptionGroupId = optionGroup.Id,
                            OptionName = optionRequest.OptionName,
                            AdditionalPrice = optionRequest.AdditionalPrice,
                            IsAvailable = optionRequest.IsAvailable,
                            SortOrder = optionRequest.SortOrder,
                            IsDeleted = false,
                            CreatedAt = DateTime.Now
                        };

                        await repoOption.AddAsync(option);
                    }
                }
            }
        }

        await _unitOfWork.SaveChangesAsync();

        return BaseResponse<string>.Success("Update store food successfully");
    }

    public async Task<BaseResponse<string>> DeleteAsync(long id)
    {
        var repoStoreFood = _unitOfWork.GetRepository<StoreFood>();
        var repoOptionGroup = _unitOfWork.GetRepository<StoreFoodOptionGroup>();
        var repoOption = _unitOfWork.GetRepository<StoreFoodOption>();

        var storeFood = await repoStoreFood.FirstOrDefaultAsync(x =>
            x.Id == id &&
            !x.IsDeleted);

        if (storeFood is null)
            return BaseResponse<string>.Fail("Store food not found");

        storeFood.IsDeleted = true;
        storeFood.UpdatedAt = DateTime.Now;

        var groups = await repoOptionGroup.Query()
            .Where(x => x.StoreFoodId == storeFood.Id && !x.IsDeleted)
            .ToListAsync();

        var groupIds = groups.Select(x => x.Id).ToList();

        var options = await repoOption.Query()
            .Where(x => groupIds.Contains(x.OptionGroupId) && !x.IsDeleted)
            .ToListAsync();

        foreach (var option in options)
        {
            option.IsDeleted = true;
            option.UpdatedAt = DateTime.Now;
            repoOption.Update(option);
        }

        foreach (var group in groups)
        {
            group.IsDeleted = true;
            group.UpdatedAt = DateTime.Now;
            repoOptionGroup.Update(group);
        }

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

    public async Task<BaseTableResponse<StoreFoodResponseDto>> GetByStoreRefCodeAsync(string storeRefCode, int page, int pageSize)
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
        var repoOptionGroup = _unitOfWork.GetRepository<StoreFoodOptionGroup>();
        var repoOption = _unitOfWork.GetRepository<StoreFoodOption>();

        var storeFood = await repoStoreFood.FirstOrDefaultAsync(x =>
            x.Id == id &&
            !x.IsDeleted);

        if (storeFood is null)
            return BaseResponse<StoreFoodResponseDto>.Fail("Store food not found");

        var optionGroups = await repoOptionGroup.Query()
            .AsNoTracking()
            .Where(x => x.StoreFoodId == storeFood.Id && !x.IsDeleted)
            .OrderBy(x => x.SortOrder)
            .ToListAsync();

        var groupIds = optionGroups.Select(x => x.Id).ToList();

        var options = await repoOption.Query()
            .AsNoTracking()
            .Where(x => groupIds.Contains(x.OptionGroupId) && !x.IsDeleted)
            .OrderBy(x => x.SortOrder)
            .ToListAsync();

        var response = new StoreFoodResponseDto
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
            IsAvailable = storeFood.IsAvailable,
            OptionGroups = optionGroups.Select(group => new StoreFoodOptionGroupResponseDto
            {
                Id = group.Id,
                RefCode = group.RefCode,
                GroupName = group.GroupName,
                IsRequired = group.IsRequired,
                MinSelect = group.MinSelect,
                MaxSelect = group.MaxSelect,
                SortOrder = group.SortOrder,
                Options = options
                    .Where(option => option.OptionGroupId == group.Id)
                    .Select(option => new StoreFoodOptionResponseDto
                    {
                        Id = option.Id,
                        RefCode = option.RefCode,
                        OptionName = option.OptionName,
                        AdditionalPrice = option.AdditionalPrice,
                        IsAvailable = option.IsAvailable,
                        SortOrder = option.SortOrder
                    })
                    .ToList()
            }).ToList()
        };

        return BaseResponse<StoreFoodResponseDto>.Success(response);
    }
}
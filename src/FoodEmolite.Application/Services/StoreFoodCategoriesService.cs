using FoodEmolite.Application.DTOs.StoreFoodCategories;
using FoodEmolite.Application.Interfaces;
using FoodEmolite.Domain.Entities;
using FoodEmolite.Domain.Interfaces;
using FoodEmolite.Shared.Entities;
using FoodEmolite.Shared.Responses;
using Microsoft.EntityFrameworkCore;

namespace FoodEmolite.Application.Services;

public class StoreFoodCategoriesService : IStoreFoodCategoriesService
{
    private readonly IUnitOfWork _unitOfWork;

    public StoreFoodCategoriesService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<BaseResponse<List<StoreFoodCategoryResponseDto>>> GetByStoreRefCodeAsync(string storeRefCode)
    {
        var repoCategory = _unitOfWork.GetRepository<StoreFoodCategories>();

        var items = await repoCategory
            .Query()
            .AsNoTracking()
            .Where(x =>
                x.StoreRefCode == storeRefCode &&
                !x.IsDelete)
            .OrderBy(x => x.CategoryName)
            .Select(x => new StoreFoodCategoryResponseDto
            {
                Id = x.Id,
                RefCode = x.RefCode,
                CategoryName = x.CategoryName,
                Description = x.Description,
                CreatedAt = x.CreatedAt
            })
            .ToListAsync();

        return BaseResponse<List<StoreFoodCategoryResponseDto>>.Success(items);
    }
    public async Task<BaseTableResponse<StoreFoodCategoryResponseDto>> SearchAsync(long currentUserId, BaseSearchRequest<StoreFoodCategorySearchRequest> request)
    {
        var repoStore = _unitOfWork.GetRepository<Store>();
        var repoCategory = _unitOfWork.GetRepository<StoreFoodCategories>();

        var store =
            await repoStore.FirstOrDefaultAsync(x =>
                x.OwnerAccountId == currentUserId &&
                !x.IsDeleted);

        if (store is null)
        {
            return new BaseTableResponse<StoreFoodCategoryResponseDto>
            {
                Items = [],
                Page = request.Page,
                PageSize = request.PageSize,
                TotalRecords = 0
            };
        }

        var query = repoCategory
            .Query()
            .AsNoTracking()
            .Where(x =>
                x.StoreRefCode == store.RefCode &&
                !x.IsDelete);

        var keyword =
            request.SearchParams?.Keyword?.Trim();

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            query = query.Where(x =>
                x.CategoryName.Contains(keyword));
        }

        query = request.SortBy?.ToLower() switch
        {
            "categoryname" => request.Asc
                ? query.OrderBy(x => x.CategoryName)
                : query.OrderByDescending(x => x.CategoryName),

            "createdat" => request.Asc
                ? query.OrderBy(x => x.CreatedAt)
                : query.OrderByDescending(x => x.CreatedAt),

            _ => query.OrderByDescending(x => x.CreatedAt)
        };

        var totalRecords =
            await query.CountAsync();

        var items = await query
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(x => new StoreFoodCategoryResponseDto
            {
                Id = x.Id,
                RefCode = x.RefCode,
                CategoryName = x.CategoryName,
                Description = x.Description,
                CreatedAt = x.CreatedAt
            })
            .ToListAsync();

        return new BaseTableResponse<StoreFoodCategoryResponseDto>
        {
            Items = items,
            Page = request.Page,
            PageSize = request.PageSize,
            TotalRecords = totalRecords
        };
    }

    public async Task<BaseResponse<string>> CreateAsync(long currentUserId, string refCode, CreateStoreFoodCategoryRequest request)
    {
        var repoStore = _unitOfWork.GetRepository<Store>();
        var repoCategory = _unitOfWork.GetRepository<StoreFoodCategories>();

        var store =
            await repoStore.FirstOrDefaultAsync(x =>
                x.OwnerAccountId == currentUserId &&
                !x.IsDeleted);

        if (store is null)
        {
            return BaseResponse<string>.Fail("Store not found");
        }

        var existed =
            await repoCategory.AnyAsync(x =>
                x.StoreRefCode == store.RefCode &&
                x.CategoryName == request.CategoryName &&
                !x.IsDelete);

        if (existed)
        {
            return BaseResponse<string>
                .Fail("Category already exists");
        }

        var entity = new StoreFoodCategories
            {
                RefCode = refCode,
                StoreRefCode = store.RefCode,
                CategoryName = request.CategoryName,
                Description = request.Description,
                IsDelete = false,
                CreatedAt = DateTime.Now,
                CreatedBy = currentUserId
            };

        await repoCategory.AddAsync(entity);
        await _unitOfWork.SaveChangesAsync();

        return BaseResponse<string>
            .Success("Create category successfully");
    }

    public async Task<BaseResponse<string>> UpdateAsync(long currentUserId, UpdateStoreFoodCategoryRequest request)
    {
        var repoStore = _unitOfWork.GetRepository<Store>();
        var repoCategory = _unitOfWork.GetRepository<StoreFoodCategories>();

        var store =
            await repoStore.FirstOrDefaultAsync(x =>
                x.OwnerAccountId == currentUserId &&
                !x.IsDeleted);

        if (store is null)
        {
            return BaseResponse<string>
                .Fail("Store not found");
        }

        var category =
            await repoCategory.FirstOrDefaultAsync(x =>
                x.Id == request.Id &&
                x.StoreRefCode == store.RefCode &&
                !x.IsDelete);

        if (category is null)
        {
            return BaseResponse<string>
                .Fail("Category not found");
        }

        category.CategoryName = request.CategoryName;
        category.Description = request.Description;
        category.UpdatedAt = DateTime.Now;
        category.UpdatedBy = currentUserId;

        repoCategory.Update(category);

        await _unitOfWork.SaveChangesAsync();

        return BaseResponse<string>.Success("Update category successfully");
    }

    public async Task<BaseResponse<string>> DeleteAsync(long currentUserId, long id)
    {
        var repoStore = _unitOfWork.GetRepository<Store>();
        var repoCategory = _unitOfWork.GetRepository<StoreFoodCategories>();
        var repoFood = _unitOfWork.GetRepository<StoreFood>();

        var store =
            await repoStore.FirstOrDefaultAsync(x =>
                x.OwnerAccountId == currentUserId &&
                !x.IsDeleted);

        if (store is null)
        {
            return BaseResponse<string>
                .Fail("Store not found");
        }

        var category =
            await repoCategory.FirstOrDefaultAsync(x =>
                x.Id == id &&
                x.StoreRefCode == store.RefCode &&
                !x.IsDelete);

        if (category is null)
        {
            return BaseResponse<string>
                .Fail("Category not found");
        }

        var used =
            await repoFood.AnyAsync(x =>
                x.StoreFoodCategoryId == id &&
                !x.IsDeleted);

        if (used)
        {
            return BaseResponse<string>
                .Fail("Category is being used");
        }

        category.IsDelete = true;
        category.UpdatedAt = DateTime.Now;
        category.UpdatedBy = currentUserId;

        repoCategory.Update(category);

        await _unitOfWork.SaveChangesAsync();

        return BaseResponse<string>
            .Success("Delete category successfully");
    }
}
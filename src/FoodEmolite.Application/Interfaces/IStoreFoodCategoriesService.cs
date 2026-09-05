using FoodEmolite.Application.DTOs.StoreFoodCategories;
using FoodEmolite.Shared.Entities;
using FoodEmolite.Shared.Responses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FoodEmolite.Application.Interfaces
{
    public interface IStoreFoodCategoriesService
    {
        Task<BaseResponse<List<StoreFoodCategoryResponseDto>>> GetByStoreRefCodeAsync(GetByStoreRefCodeRequest request);
        Task<BaseTableResponse<StoreFoodCategoryResponseDto>> SearchAsync(long currentUserId, BaseSearchRequest<StoreFoodCategorySearchRequest> request);

        /// <summary>Danh sách danh mục toàn hệ thống (mọi cửa hàng) — dùng cho admin, không lọc theo currentUserId.</summary>
        Task<BaseTableResponse<StoreFoodCategoryResponseDto>> GetAllForAdminAsync(int page, int pageSize, string? keyword, string? storeRefCode, string? sortBy = null, bool asc = false);

        Task<BaseResponse<string>> CreateAsync(long currentUserId, string refCode, CreateStoreFoodCategoryRequest request);

        Task<BaseResponse<string>> UpdateAsync(long currentUserId, UpdateStoreFoodCategoryRequest request);

        Task<BaseResponse<string>> DeleteAsync(long currentUserId, long id);
    }
}

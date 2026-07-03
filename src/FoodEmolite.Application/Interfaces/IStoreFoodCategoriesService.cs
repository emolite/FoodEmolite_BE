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
        Task<BaseResponse<List<StoreFoodCategoryResponseDto>>> GetByStoreRefCodeAsync(string storeRefCode);
        Task<BaseTableResponse<StoreFoodCategoryResponseDto>> SearchAsync(long currentUserId, BaseSearchRequest<StoreFoodCategorySearchRequest> request);

        Task<BaseResponse<string>> CreateAsync(long currentUserId, string refCode, CreateStoreFoodCategoryRequest request);

        Task<BaseResponse<string>> UpdateAsync(long currentUserId, UpdateStoreFoodCategoryRequest request);

        Task<BaseResponse<string>> DeleteAsync(long currentUserId, long id);
    }
}

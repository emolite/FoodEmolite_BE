using FoodEmolite.Application.DTOs.Promotion;
using FoodEmolite.Shared.Entities;
using FoodEmolite.Shared.Responses;

namespace FoodEmolite.Application.Interfaces;

public interface IPromotionService
{
    Task<BaseTableResponse<PromotionResponseDto>> GetByStoreRefCodeAsync(long currentUserId, BaseSearchRequest<PromotionSearchRequest> request);

    Task<BaseResponse<PromotionResponseDto>> GetDetailAsync(long currentUserId, long id);

    /// <summary>
    /// Danh sách promotion đang ACTIVE của 1 cửa hàng — công khai, dùng cho trang khách hàng
    /// hiển thị giá khuyến mãi và tính giá khi tạo đơn.
    /// </summary>
    Task<BaseResponse<List<PromotionResponseDto>>> GetActiveByStoreRefCodeAsync(string storeRefCode);

    Task<BaseResponse<string>> CreateAsync(long currentUserId, string refCode, CreatePromotionRequestDto request);

    /// <summary>
    /// Chỉnh sửa chương trình — chỉ cho phép khi đang ở trạng thái DRAFT. Lưu với SaveAsDraft=false
    /// sẽ tính lại trạng thái theo lịch (SCHEDULED/ACTIVE), tức là "kích hoạt" bản nháp.
    /// </summary>
    Task<BaseResponse<string>> UpdateAsync(long currentUserId, long id, CreatePromotionRequestDto request);

    Task<BaseResponse<string>> PauseAsync(long currentUserId, long id);

    Task<BaseResponse<string>> ResumeAsync(long currentUserId, long id);

    Task<BaseResponse<string>> CancelAsync(long currentUserId, long id);

    Task<BaseResponse<string>> DeleteAsync(long currentUserId, long id);

    /// <summary>
    /// Quét lại toàn bộ promotion đang SCHEDULED/ACTIVE trên mọi cửa hàng và tự chuyển trạng thái
    /// theo lịch (dùng bởi background scheduler để đảm bảo trạng thái luôn realtime).
    /// </summary>
    Task RecomputeStatusesAsync();
}

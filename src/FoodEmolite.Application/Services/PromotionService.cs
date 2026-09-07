using FoodEmolite.Shared.Common;
using FoodEmolite.Application.DTOs.Promotion;
using FoodEmolite.Application.DTOs.Realtime;
using FoodEmolite.Application.ExternalService.Interfaces;
using FoodEmolite.Application.Helpers;
using FoodEmolite.Application.Interfaces;
using FoodEmolite.Domain.Entities;
using FoodEmolite.Domain.Interfaces;
using FoodEmolite.Shared.Entities;
using FoodEmolite.Shared.Responses;
using Microsoft.EntityFrameworkCore;

namespace FoodEmolite.Application.Services;

public class PromotionService : IPromotionService
{
    private static readonly string[] AllowedPromotionTypes = { "FIXED_PRICE", "PRODUCT_DISCOUNT", "BUY_X_GET_Y" };
    private static readonly string[] AllowedConditionTypes = { "NONE", "MIN_ORDER_AMOUNT", "MIN_QUANTITY" };
    private static readonly string[] AllowedDiscountTypes = { "PERCENT", "AMOUNT" };

    private readonly IUnitOfWork _unitOfWork;
    private readonly IRealtimeNotificationService _realtimeNotificationService;
    private readonly ICloudinaryService _cloudinaryService;

    public PromotionService(
        IUnitOfWork unitOfWork,
        IRealtimeNotificationService realtimeNotificationService,
        ICloudinaryService cloudinaryService)
    {
        _unitOfWork = unitOfWork;
        _realtimeNotificationService = realtimeNotificationService;
        _cloudinaryService = cloudinaryService;
    }

    public async Task<BaseTableResponse<PromotionResponseDto>> GetByStoreRefCodeAsync(long currentUserId, BaseSearchRequest<PromotionSearchRequest> request)
    {
        var repoStore = _unitOfWork.GetRepository<Store>();
        var repoPromotion = _unitOfWork.GetRepository<Promotion>();

        var store = await repoStore.FirstOrDefaultAsync(x => x.OwnerAccountId == currentUserId && !x.IsDeleted);

        if (store is null)
        {
            return new BaseTableResponse<PromotionResponseDto>
            {
                Items = [],
                Page = request.Page,
                PageSize = request.PageSize,
                TotalRecords = 0
            };
        }

        var promotions = await repoPromotion
            .Query()
            .Where(x => x.StoreRefCode == store.RefCode && !x.IsDeleted)
            .ToListAsync();

        await RecomputeAndBroadcastAsync(promotions);

        IEnumerable<Promotion> filtered = promotions;

        var search = request.SearchParams;

        if (!string.IsNullOrWhiteSpace(search?.Keyword))
        {
            var keyword = search.Keyword.Trim().ToLower();

            filtered = filtered.Where(x =>
                x.Name.ToLower().Contains(keyword) ||
                (x.PromotionCode != null && x.PromotionCode.ToLower().Contains(keyword)));
        }

        if (!string.IsNullOrWhiteSpace(search?.PromotionType))
        {
            filtered = filtered.Where(x => x.PromotionType == search.PromotionType);
        }

        if (!string.IsNullOrWhiteSpace(search?.Status))
        {
            filtered = filtered.Where(x => x.Status == search.Status);
        }

        filtered = request.SortBy?.ToLower() switch
        {
            "name" => request.Asc
                ? filtered.OrderBy(x => x.Name)
                : filtered.OrderByDescending(x => x.Name),

            "startdate" => request.Asc
                ? filtered.OrderBy(x => x.StartDate)
                : filtered.OrderByDescending(x => x.StartDate),

            _ => filtered.OrderByDescending(x => x.CreatedAt)
        };

        var materialized = filtered.ToList();
        var totalRecords = materialized.Count;

        var pageItems = materialized
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToList();

        var items = await MapToResponseDtosAsync(pageItems);

        return new BaseTableResponse<PromotionResponseDto>
        {
            Items = items,
            Page = request.Page,
            PageSize = request.PageSize,
            TotalRecords = totalRecords
        };
    }

    public async Task<BaseResponse<PromotionResponseDto>> GetDetailAsync(long currentUserId, long id)
    {
        var (_, promotion, error) = await GetOwnedPromotionAsync(currentUserId, id);

        if (error != null)
            return BaseResponse<PromotionResponseDto>.Fail(error);

        await RecomputeAndBroadcastAsync(new List<Promotion> { promotion! });

        var dto = (await MapToResponseDtosAsync(new List<Promotion> { promotion! })).First();

        return BaseResponse<PromotionResponseDto>.Success(dto);
    }

    public async Task<BaseResponse<List<PromotionResponseDto>>> GetActiveByStoreRefCodeAsync(string storeRefCode)
    {
        var repoPromotion = _unitOfWork.GetRepository<Promotion>();

        var promotions = await repoPromotion
            .Query()
            .AsNoTracking()
            .Where(x => x.StoreRefCode == storeRefCode && !x.IsDeleted && x.Status == PromotionStatusCalculator.Active)
            .ToListAsync();

        var items = await MapToResponseDtosAsync(promotions);

        return BaseResponse<List<PromotionResponseDto>>.Success(items);
    }

    public async Task<BaseResponse<bool>> CheckStoreWideDiscountEligibilityAsync(string storeRefCode, long? currentUserId, string? deviceId)
    {
        var repoPromotion = _unitOfWork.GetRepository<Promotion>();

        var activeStoreWidePromotionIds = await repoPromotion
            .Query()
            .AsNoTracking()
            .Where(x =>
                x.StoreRefCode == storeRefCode &&
                !x.IsDeleted &&
                x.PromotionType == "PRODUCT_DISCOUNT" &&
                x.ApplyToAllProducts &&
                x.Status == PromotionStatusCalculator.Active)
            .Select(x => x.Id)
            .ToListAsync();

        if (activeStoreWidePromotionIds.Count == 0)
            return BaseResponse<bool>.Success(false);

        long? customerId = null;

        if (currentUserId is null)
        {
            if (string.IsNullOrWhiteSpace(deviceId))
                return BaseResponse<bool>.Success(false);

            var customer = await _unitOfWork.GetRepository<Customer>()
                .FirstOrDefaultAsync(x => x.DeviceId == deviceId);

            if (customer is null)
                return BaseResponse<bool>.Success(false);

            customerId = customer.Id;
        }

        var alreadyRedeemed = await _unitOfWork.GetRepository<PromotionRedemption>()
            .AnyAsync(x =>
                activeStoreWidePromotionIds.Contains(x.PromotionId) &&
                ((currentUserId != null && x.CustomerAccountId == currentUserId) ||
                 (customerId != null && x.CustomerId == customerId)));

        return BaseResponse<bool>.Success(!alreadyRedeemed);
    }

    public async Task<BaseResponse<string>> CreateAsync(long currentUserId, string refCode, CreatePromotionRequestDto request)
    {
        var repoStore = _unitOfWork.GetRepository<Store>();
        var repoPromotion = _unitOfWork.GetRepository<Promotion>();

        var store = await repoStore.FirstOrDefaultAsync(x => x.OwnerAccountId == currentUserId && !x.IsDeleted);

        if (store is null)
            return BaseResponse<string>.Fail("Store not found");

        if (!string.IsNullOrWhiteSpace(request.PromotionCode))
        {
            var codeExisted = await repoPromotion.AnyAsync(x =>
                x.StoreRefCode == store.RefCode &&
                x.PromotionCode == request.PromotionCode &&
                !x.IsDeleted);

            if (codeExisted)
                return BaseResponse<string>.Fail("Mã khuyến mãi đã tồn tại");
        }

        var (storeFoodIds, validationError) = await ValidatePromotionItemsAsync(store, request);

        if (validationError != null)
            return BaseResponse<string>.Fail(validationError);

        var now = DateTimeHelper.VnNow;
        string status;

        if (request.SaveAsDraft)
        {
            status = PromotionStatusCalculator.Draft;
        }
        else
        {
            var conflictFoodName = await FindConflictingFoodNameAsync(store.RefCode, null, storeFoodIds);

            if (conflictFoodName != null)
                return BaseResponse<string>.Fail($"Món \"{conflictFoodName}\" đã thuộc chương trình khuyến mãi khác đang áp dụng");

            if (request.PromotionType == "PRODUCT_DISCOUNT" && request.ApplyToAllProducts &&
                await HasActiveStoreWideDiscountAsync(store.RefCode, null))
                return BaseResponse<string>.Fail("Cửa hàng đã có chương trình giảm giá áp dụng toàn bộ sản phẩm đang hoạt động");

            status = PromotionStatusCalculator.ComputeStatus(
                request.StartDate,
                request.EndDate,
                request.StartTime,
                request.EndTime,
                request.DaysOfWeekMask,
                now);

            if (status == PromotionStatusCalculator.Ended)
                return BaseResponse<string>.Fail("Khoảng thời gian áp dụng đã kết thúc");
        }

        var isStoreWideDiscount = request.PromotionType == "PRODUCT_DISCOUNT" && request.ApplyToAllProducts;

        var promotion = new Promotion
        {
            RefCode = refCode,
            StoreRefCode = store.RefCode,
            PromotionCode = string.IsNullOrWhiteSpace(request.PromotionCode) ? null : request.PromotionCode.Trim(),
            PromotionType = request.PromotionType,
            Name = request.Name.Trim(),
            Description = request.Description,
            Status = status,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            StartTime = request.StartTime,
            EndTime = request.EndTime,
            DaysOfWeekMask = request.DaysOfWeekMask,
            ConditionType = request.ConditionType,
            ConditionMinAmount = request.ConditionType == "MIN_ORDER_AMOUNT" ? request.ConditionMinAmount : null,
            ConditionMinQuantity = request.ConditionType == "MIN_QUANTITY" ? request.ConditionMinQuantity : null,
            ApplyToAllProducts = isStoreWideDiscount,
            DiscountType = isStoreWideDiscount ? request.DiscountType : null,
            DiscountValue = isStoreWideDiscount ? request.DiscountValue : null,
            MaxDiscountAmount = isStoreWideDiscount && request.DiscountType == "PERCENT" ? request.MaxDiscountAmount : null,
            CreatedAt = now,
            CreatedBy = currentUserId
        };

        await repoPromotion.AddAsync(promotion);
        await _unitOfWork.SaveChangesAsync();

        await CreatePromotionItemsAsync(promotion.Id, refCode, currentUserId, now, request);
        await _unitOfWork.SaveChangesAsync();

        if (status != PromotionStatusCalculator.Draft)
        {
            await _realtimeNotificationService.NotifyPromotionStatusChangedAsync(new PromotionStatusChangedDto
            {
                PromotionId = promotion.Id,
                StoreRefCode = store.RefCode,
                Status = status
            });
        }

        return BaseResponse<string>.Success("Tạo chương trình khuyến mãi thành công");
    }

    public async Task<BaseResponse<string>> UpdateAsync(long currentUserId, long id, CreatePromotionRequestDto request)
    {
        var (store, promotion, error) = await GetOwnedPromotionAsync(currentUserId, id);

        if (error != null)
            return BaseResponse<string>.Fail(error);

        if (promotion!.Status != PromotionStatusCalculator.Draft)
            return BaseResponse<string>.Fail("Chỉ có thể chỉnh sửa chương trình đang ở dạng nháp");

        if (!string.IsNullOrWhiteSpace(request.PromotionCode))
        {
            var codeExisted = await _unitOfWork.GetRepository<Promotion>().AnyAsync(x =>
                x.StoreRefCode == store!.RefCode &&
                x.PromotionCode == request.PromotionCode &&
                x.Id != id &&
                !x.IsDeleted);

            if (codeExisted)
                return BaseResponse<string>.Fail("Mã khuyến mãi đã tồn tại");
        }

        var (storeFoodIds, validationError) = await ValidatePromotionItemsAsync(store!, request);

        if (validationError != null)
            return BaseResponse<string>.Fail(validationError);

        var now = DateTimeHelper.VnNow;
        string status;

        if (request.SaveAsDraft)
        {
            status = PromotionStatusCalculator.Draft;
        }
        else
        {
            var conflictFoodName = await FindConflictingFoodNameAsync(store!.RefCode, promotion.Id, storeFoodIds);

            if (conflictFoodName != null)
                return BaseResponse<string>.Fail($"Món \"{conflictFoodName}\" đã thuộc chương trình khuyến mãi khác đang áp dụng");

            if (request.PromotionType == "PRODUCT_DISCOUNT" && request.ApplyToAllProducts &&
                await HasActiveStoreWideDiscountAsync(store!.RefCode, promotion.Id))
                return BaseResponse<string>.Fail("Cửa hàng đã có chương trình giảm giá áp dụng toàn bộ sản phẩm đang hoạt động");

            status = PromotionStatusCalculator.ComputeStatus(
                request.StartDate,
                request.EndDate,
                request.StartTime,
                request.EndTime,
                request.DaysOfWeekMask,
                now);

            if (status == PromotionStatusCalculator.Ended)
                return BaseResponse<string>.Fail("Khoảng thời gian áp dụng đã kết thúc");
        }

        var isStoreWideDiscount = request.PromotionType == "PRODUCT_DISCOUNT" && request.ApplyToAllProducts;

        promotion.PromotionCode = string.IsNullOrWhiteSpace(request.PromotionCode) ? null : request.PromotionCode.Trim();
        promotion.PromotionType = request.PromotionType;
        promotion.Name = request.Name.Trim();
        promotion.Description = request.Description;
        promotion.Status = status;
        promotion.StartDate = request.StartDate;
        promotion.EndDate = request.EndDate;
        promotion.StartTime = request.StartTime;
        promotion.EndTime = request.EndTime;
        promotion.DaysOfWeekMask = request.DaysOfWeekMask;
        promotion.ConditionType = request.ConditionType;
        promotion.ConditionMinAmount = request.ConditionType == "MIN_ORDER_AMOUNT" ? request.ConditionMinAmount : null;
        promotion.ConditionMinQuantity = request.ConditionType == "MIN_QUANTITY" ? request.ConditionMinQuantity : null;
        promotion.ApplyToAllProducts = isStoreWideDiscount;
        promotion.DiscountType = isStoreWideDiscount ? request.DiscountType : null;
        promotion.DiscountValue = isStoreWideDiscount ? request.DiscountValue : null;
        promotion.MaxDiscountAmount = isStoreWideDiscount && request.DiscountType == "PERCENT" ? request.MaxDiscountAmount : null;
        promotion.UpdatedAt = now;
        promotion.UpdatedBy = currentUserId;

        _unitOfWork.GetRepository<Promotion>().Update(promotion);

        await RemoveExistingPromotionItemsAsync(promotion.Id);
        await _unitOfWork.SaveChangesAsync();

        await CreatePromotionItemsAsync(promotion.Id, promotion.RefCode ?? string.Empty, currentUserId, now, request);
        await _unitOfWork.SaveChangesAsync();

        if (status != PromotionStatusCalculator.Draft)
        {
            await _realtimeNotificationService.NotifyPromotionStatusChangedAsync(new PromotionStatusChangedDto
            {
                PromotionId = promotion.Id,
                StoreRefCode = promotion.StoreRefCode,
                Status = status
            });
        }

        return BaseResponse<string>.Success("Cập nhật chương trình khuyến mãi thành công");
    }

    /// <summary>
    /// Validate các field dùng chung + item theo từng loại, trả về danh sách storeFoodId liên quan
    /// và thông báo lỗi (nếu có). Dùng chung cho cả CreateAsync và UpdateAsync.
    /// </summary>
    private async Task<(List<long> StoreFoodIds, string? Error)> ValidatePromotionItemsAsync(Store store, CreatePromotionRequestDto request)
    {
        if (!AllowedPromotionTypes.Contains(request.PromotionType))
            return (new List<long>(), "Loại chương trình không hợp lệ");

        if (string.IsNullOrWhiteSpace(request.Name))
            return (new List<long>(), "Tên chương trình là bắt buộc");

        if (request.EndDate.HasValue && request.EndDate.Value < request.StartDate)
            return (new List<long>(), "Ngày kết thúc phải sau ngày bắt đầu");

        if (request.StartTime.HasValue != request.EndTime.HasValue)
            return (new List<long>(), "Cần nhập đủ giờ bắt đầu và giờ kết thúc");

        if (request.StartTime.HasValue && request.EndTime.HasValue && request.StartTime.Value >= request.EndTime.Value)
            return (new List<long>(), "Giờ kết thúc phải sau giờ bắt đầu");

        if (!AllowedConditionTypes.Contains(request.ConditionType))
            return (new List<long>(), "Điều kiện áp dụng không hợp lệ");

        if (request.ConditionType == "MIN_ORDER_AMOUNT" && (request.ConditionMinAmount is null || request.ConditionMinAmount <= 0))
            return (new List<long>(), "Vui lòng nhập số tiền tối thiểu");

        if (request.ConditionType == "MIN_QUANTITY" && (request.ConditionMinQuantity is null || request.ConditionMinQuantity <= 0))
            return (new List<long>(), "Vui lòng nhập số lượng tối thiểu");

        List<long> storeFoodIds;

        switch (request.PromotionType)
        {
            case "FIXED_PRICE":
                if (request.FixedPriceItems is null || request.FixedPriceItems.Count == 0)
                    return (new List<long>(), "Vui lòng chọn ít nhất 1 món áp dụng đồng giá");

                if (request.FixedPriceItems.Any(x => x.FixedPrice <= 0))
                    return (new List<long>(), "Giá đồng giá phải lớn hơn 0");

                storeFoodIds = request.FixedPriceItems.Select(x => x.StoreFoodId).Distinct().ToList();
                break;

            case "PRODUCT_DISCOUNT":
                if (request.ApplyToAllProducts)
                {
                    if (!AllowedDiscountTypes.Contains(request.DiscountType))
                        return (new List<long>(), "Kiểu giảm giá không hợp lệ");

                    if (request.DiscountValue is null || request.DiscountValue <= 0)
                        return (new List<long>(), "Mức giảm phải lớn hơn 0");

                    if (request.DiscountType == "PERCENT" && request.DiscountValue > 100)
                        return (new List<long>(), "Giảm theo % không được vượt quá 100%");

                    storeFoodIds = new List<long>();
                }
                else
                {
                    if (request.DiscountItems is null || request.DiscountItems.Count == 0)
                        return (new List<long>(), "Vui lòng chọn ít nhất 1 món áp dụng giảm giá");

                    if (request.DiscountItems.Any(x => !AllowedDiscountTypes.Contains(x.DiscountType)))
                        return (new List<long>(), "Kiểu giảm giá không hợp lệ");

                    if (request.DiscountItems.Any(x => x.DiscountValue <= 0))
                        return (new List<long>(), "Mức giảm phải lớn hơn 0");

                    if (request.DiscountItems.Any(x => x.DiscountType == "PERCENT" && x.DiscountValue > 100))
                        return (new List<long>(), "Giảm theo % không được vượt quá 100%");

                    storeFoodIds = request.DiscountItems.Select(x => x.StoreFoodId).Distinct().ToList();
                }
                break;

            case "BUY_X_GET_Y":
                if (request.GiftItems is null || request.GiftItems.Count == 0)
                    return (new List<long>(), "Vui lòng chọn ít nhất 1 món quà tặng");

                if (request.GiftItems.Any(x => x.GiftQuantity <= 0))
                    return (new List<long>(), "Số lượng quà tặng phải lớn hơn 0");

                if (request.ConditionType == "NONE")
                    return (new List<long>(), "Chương trình Mua X tặng Y cần điều kiện mua tối thiểu");

                storeFoodIds = request.GiftItems.Select(x => x.StoreFoodId).Distinct().ToList();
                break;

            default:
                storeFoodIds = new List<long>();
                break;
        }

        var validFoodCount = await _unitOfWork.GetRepository<StoreFood>()
            .Query()
            .Where(x => storeFoodIds.Contains(x.Id) && x.StoreRefCode == store.RefCode && !x.IsDeleted)
            .CountAsync();

        if (validFoodCount != storeFoodIds.Count)
            return (storeFoodIds, "Có món ăn không hợp lệ hoặc không thuộc cửa hàng");

        return (storeFoodIds, null);
    }

    private async Task CreatePromotionItemsAsync(long promotionId, string refCode, long currentUserId, DateTime now, CreatePromotionRequestDto request)
    {
        if (request.PromotionType == "FIXED_PRICE")
        {
            var repoFixedPrice = _unitOfWork.GetRepository<PromotionFixedPriceItem>();

            foreach (var item in request.FixedPriceItems)
            {
                await repoFixedPrice.AddAsync(new PromotionFixedPriceItem
                {
                    RefCode = refCode,
                    PromotionId = promotionId,
                    StoreFoodId = item.StoreFoodId,
                    FixedPrice = item.FixedPrice,
                    CreatedAt = now,
                    CreatedBy = currentUserId
                });
            }
        }
        else if (request.PromotionType == "PRODUCT_DISCOUNT" && !request.ApplyToAllProducts)
        {
            var repoDiscount = _unitOfWork.GetRepository<PromotionDiscountItem>();

            foreach (var item in request.DiscountItems)
            {
                await repoDiscount.AddAsync(new PromotionDiscountItem
                {
                    RefCode = refCode,
                    PromotionId = promotionId,
                    StoreFoodId = item.StoreFoodId,
                    DiscountType = item.DiscountType,
                    DiscountValue = item.DiscountValue,
                    MaxDiscountAmount = item.DiscountType == "PERCENT" ? item.MaxDiscountAmount : null,
                    CreatedAt = now,
                    CreatedBy = currentUserId
                });
            }
        }
        else if (request.PromotionType == "BUY_X_GET_Y")
        {
            var repoGift = _unitOfWork.GetRepository<PromotionGiftItem>();

            foreach (var item in request.GiftItems)
            {
                await repoGift.AddAsync(new PromotionGiftItem
                {
                    RefCode = refCode,
                    PromotionId = promotionId,
                    StoreFoodId = item.StoreFoodId,
                    GiftQuantity = item.GiftQuantity,
                    SortOrder = item.SortOrder,
                    CreatedAt = now,
                    CreatedBy = currentUserId
                });
            }
        }
    }

    private async Task RemoveExistingPromotionItemsAsync(long promotionId)
    {
        var repoFixedPrice = _unitOfWork.GetRepository<PromotionFixedPriceItem>();
        var repoDiscount = _unitOfWork.GetRepository<PromotionDiscountItem>();
        var repoGift = _unitOfWork.GetRepository<PromotionGiftItem>();

        var fixedPriceItems = await repoFixedPrice.Query().Where(x => x.PromotionId == promotionId).ToListAsync();
        foreach (var item in fixedPriceItems)
            repoFixedPrice.Remove(item);

        var discountItems = await repoDiscount.Query().Where(x => x.PromotionId == promotionId).ToListAsync();
        foreach (var item in discountItems)
            repoDiscount.Remove(item);

        var giftItems = await repoGift.Query().Where(x => x.PromotionId == promotionId).ToListAsync();
        foreach (var item in giftItems)
            repoGift.Remove(item);
    }

    public async Task<BaseResponse<string>> PauseAsync(long currentUserId, long id)
    {
        var (_, promotion, error) = await GetOwnedPromotionAsync(currentUserId, id);

        if (error != null)
            return BaseResponse<string>.Fail(error);

        if (promotion!.Status != PromotionStatusCalculator.Scheduled && promotion.Status != PromotionStatusCalculator.Active)
            return BaseResponse<string>.Fail("Chỉ có thể tạm dừng chương trình đang diễn ra hoặc sắp diễn ra");

        await UpdateStatusAsync(promotion, PromotionStatusCalculator.Paused, currentUserId);

        return BaseResponse<string>.Success("Tạm dừng chương trình thành công");
    }

    public async Task<BaseResponse<string>> ResumeAsync(long currentUserId, long id)
    {
        var (_, promotion, error) = await GetOwnedPromotionAsync(currentUserId, id);

        if (error != null)
            return BaseResponse<string>.Fail(error);

        if (promotion!.Status != PromotionStatusCalculator.Paused)
            return BaseResponse<string>.Fail("Chỉ có thể tiếp tục chương trình đang tạm dừng");

        var ownStoreFoodIds = await GetPromotionStoreFoodIdsAsync(promotion);
        var conflictFoodName = await FindConflictingFoodNameAsync(promotion.StoreRefCode, promotion.Id, ownStoreFoodIds);

        if (conflictFoodName != null)
            return BaseResponse<string>.Fail($"Không thể tiếp tục: món \"{conflictFoodName}\" đã thuộc chương trình khác đang áp dụng");

        if (promotion.ApplyToAllProducts && await HasActiveStoreWideDiscountAsync(promotion.StoreRefCode, promotion.Id))
            return BaseResponse<string>.Fail("Cửa hàng đã có chương trình giảm giá áp dụng toàn bộ sản phẩm đang hoạt động");

        var status = PromotionStatusCalculator.ComputeStatus(
            promotion.StartDate,
            promotion.EndDate,
            promotion.StartTime,
            promotion.EndTime,
            promotion.DaysOfWeekMask,
            DateTimeHelper.VnNow);

        await UpdateStatusAsync(promotion, status, currentUserId);

        return BaseResponse<string>.Success("Tiếp tục chương trình thành công");
    }

    public async Task<BaseResponse<string>> CancelAsync(long currentUserId, long id)
    {
        var (_, promotion, error) = await GetOwnedPromotionAsync(currentUserId, id);

        if (error != null)
            return BaseResponse<string>.Fail(error);

        if (promotion!.Status == PromotionStatusCalculator.Ended)
            return BaseResponse<string>.Fail("Chương trình đã kết thúc");

        await UpdateStatusAsync(promotion, PromotionStatusCalculator.Ended, currentUserId);

        return BaseResponse<string>.Success("Huỷ chương trình thành công");
    }

    public async Task<BaseResponse<string>> DeleteAsync(long currentUserId, long id)
    {
        var (_, promotion, error) = await GetOwnedPromotionAsync(currentUserId, id);

        if (error != null)
            return BaseResponse<string>.Fail(error);

        if (promotion!.Status != PromotionStatusCalculator.Draft)
            return BaseResponse<string>.Fail("Chỉ có thể xoá chương trình đang ở dạng nháp");

        promotion.IsDeleted = true;
        promotion.UpdatedAt = DateTimeHelper.VnNow;
        promotion.UpdatedBy = currentUserId;

        _unitOfWork.GetRepository<Promotion>().Update(promotion);
        await _unitOfWork.SaveChangesAsync();

        return BaseResponse<string>.Success("Xoá chương trình thành công");
    }

    public async Task RecomputeStatusesAsync()
    {
        var repoPromotion = _unitOfWork.GetRepository<Promotion>();

        var promotions = await repoPromotion
            .Query()
            .Where(x => !x.IsDeleted && (x.Status == PromotionStatusCalculator.Scheduled || x.Status == PromotionStatusCalculator.Active))
            .ToListAsync();

        await RecomputeAndBroadcastAsync(promotions);
    }

    private async Task UpdateStatusAsync(Promotion promotion, string status, long currentUserId)
    {
        promotion.Status = status;
        promotion.UpdatedAt = DateTimeHelper.VnNow;
        promotion.UpdatedBy = currentUserId;

        _unitOfWork.GetRepository<Promotion>().Update(promotion);
        await _unitOfWork.SaveChangesAsync();

        await _realtimeNotificationService.NotifyPromotionStatusChangedAsync(new PromotionStatusChangedDto
        {
            PromotionId = promotion.Id,
            StoreRefCode = promotion.StoreRefCode,
            Status = promotion.Status
        });
    }

    private async Task RecomputeAndBroadcastAsync(List<Promotion> promotions)
    {
        var repoPromotion = _unitOfWork.GetRepository<Promotion>();
        var now = DateTimeHelper.VnNow;
        var changed = false;
        var events = new List<PromotionStatusChangedDto>();

        foreach (var promotion in promotions)
        {
            if (promotion.Status != PromotionStatusCalculator.Scheduled && promotion.Status != PromotionStatusCalculator.Active)
                continue;

            var computed = PromotionStatusCalculator.ComputeStatus(
                promotion.StartDate,
                promotion.EndDate,
                promotion.StartTime,
                promotion.EndTime,
                promotion.DaysOfWeekMask,
                now);

            if (computed == promotion.Status)
                continue;

            promotion.Status = computed;
            promotion.UpdatedAt = now;

            repoPromotion.Update(promotion);
            changed = true;

            events.Add(new PromotionStatusChangedDto
            {
                PromotionId = promotion.Id,
                StoreRefCode = promotion.StoreRefCode,
                Status = promotion.Status
            });
        }

        if (changed)
        {
            await _unitOfWork.SaveChangesAsync();
        }

        foreach (var evt in events)
        {
            await _realtimeNotificationService.NotifyPromotionStatusChangedAsync(evt);
        }
    }

    private async Task<List<long>> GetPromotionStoreFoodIdsAsync(Promotion promotion)
    {
        switch (promotion.PromotionType)
        {
            case "FIXED_PRICE":
                return await _unitOfWork.GetRepository<PromotionFixedPriceItem>()
                    .Query()
                    .Where(x => x.PromotionId == promotion.Id && !x.IsDeleted)
                    .Select(x => x.StoreFoodId)
                    .ToListAsync();

            case "PRODUCT_DISCOUNT":
                if (promotion.ApplyToAllProducts)
                    return new List<long>();

                return await _unitOfWork.GetRepository<PromotionDiscountItem>()
                    .Query()
                    .Where(x => x.PromotionId == promotion.Id && !x.IsDeleted)
                    .Select(x => x.StoreFoodId)
                    .ToListAsync();

            case "BUY_X_GET_Y":
                return await _unitOfWork.GetRepository<PromotionGiftItem>()
                    .Query()
                    .Where(x => x.PromotionId == promotion.Id && !x.IsDeleted)
                    .Select(x => x.StoreFoodId)
                    .ToListAsync();

            default:
                return new List<long>();
        }
    }

    /// <summary>
    /// Trả về tên món đầu tiên (nếu có) trong <paramref name="storeFoodIds"/> đã thuộc một promotion
    /// khác (không phải <paramref name="excludePromotionId"/>) đang SCHEDULED/ACTIVE/PAUSED cho cùng cửa hàng.
    /// </summary>
    private async Task<string?> FindConflictingFoodNameAsync(string storeRefCode, long? excludePromotionId, List<long> storeFoodIds)
    {
        if (storeFoodIds.Count == 0)
            return null;

        var repoPromotion = _unitOfWork.GetRepository<Promotion>();

        var reservedPromotionIds = await repoPromotion
            .Query()
            .Where(x =>
                x.StoreRefCode == storeRefCode &&
                !x.IsDeleted &&
                (x.Status == PromotionStatusCalculator.Scheduled ||
                 x.Status == PromotionStatusCalculator.Active ||
                 x.Status == PromotionStatusCalculator.Paused) &&
                (excludePromotionId == null || x.Id != excludePromotionId.Value))
            .Select(x => x.Id)
            .ToListAsync();

        if (reservedPromotionIds.Count == 0)
            return null;

        var repoFixedPrice = _unitOfWork.GetRepository<PromotionFixedPriceItem>();
        var repoDiscount = _unitOfWork.GetRepository<PromotionDiscountItem>();
        var repoGift = _unitOfWork.GetRepository<PromotionGiftItem>();

        var conflictFoodId = await repoFixedPrice.Query()
            .Where(x => reservedPromotionIds.Contains(x.PromotionId) && !x.IsDeleted && storeFoodIds.Contains(x.StoreFoodId))
            .Select(x => (long?)x.StoreFoodId)
            .FirstOrDefaultAsync();

        conflictFoodId ??= await repoDiscount.Query()
            .Where(x => reservedPromotionIds.Contains(x.PromotionId) && !x.IsDeleted && storeFoodIds.Contains(x.StoreFoodId))
            .Select(x => (long?)x.StoreFoodId)
            .FirstOrDefaultAsync();

        conflictFoodId ??= await repoGift.Query()
            .Where(x => reservedPromotionIds.Contains(x.PromotionId) && !x.IsDeleted && storeFoodIds.Contains(x.StoreFoodId))
            .Select(x => (long?)x.StoreFoodId)
            .FirstOrDefaultAsync();

        if (conflictFoodId is null)
            return null;

        var food = await _unitOfWork.GetRepository<StoreFood>()
            .FirstOrDefaultAsync(x => x.Id == conflictFoodId.Value);

        return food?.FoodName ?? "Món đã chọn";
    }

    /// <summary>
    /// Kiểm tra cửa hàng đã có chương trình PRODUCT_DISCOUNT dạng "áp dụng toàn bộ sản phẩm"
    /// (ApplyToAllProducts) đang SCHEDULED/ACTIVE/PAUSED hay chưa — chỉ cho phép tối đa 1 chương
    /// trình dạng này hoạt động cùng lúc để tránh nhập nhằng khi khách chọn món nhận giảm giá.
    /// </summary>
    private async Task<bool> HasActiveStoreWideDiscountAsync(string storeRefCode, long? excludePromotionId)
    {
        var repoPromotion = _unitOfWork.GetRepository<Promotion>();

        return await repoPromotion.Query().AnyAsync(x =>
            x.StoreRefCode == storeRefCode &&
            !x.IsDeleted &&
            x.PromotionType == "PRODUCT_DISCOUNT" &&
            x.ApplyToAllProducts &&
            (x.Status == PromotionStatusCalculator.Scheduled ||
             x.Status == PromotionStatusCalculator.Active ||
             x.Status == PromotionStatusCalculator.Paused) &&
            (excludePromotionId == null || x.Id != excludePromotionId.Value));
    }

    private async Task<(Store? Store, Promotion? Promotion, string? Error)> GetOwnedPromotionAsync(long currentUserId, long id)
    {
        var repoStore = _unitOfWork.GetRepository<Store>();
        var repoPromotion = _unitOfWork.GetRepository<Promotion>();

        var store = await repoStore.FirstOrDefaultAsync(x => x.OwnerAccountId == currentUserId && !x.IsDeleted);

        if (store is null)
            return (null, null, "Store not found");

        var promotion = await repoPromotion.FirstOrDefaultAsync(x => x.Id == id && x.StoreRefCode == store.RefCode && !x.IsDeleted);

        if (promotion is null)
            return (null, null, "Promotion not found");

        return (store, promotion, null);
    }

    private async Task<List<PromotionResponseDto>> MapToResponseDtosAsync(List<Promotion> promotions)
    {
        var promotionIds = promotions.Select(x => x.Id).ToList();

        var repoFixedPrice = _unitOfWork.GetRepository<PromotionFixedPriceItem>();
        var repoDiscount = _unitOfWork.GetRepository<PromotionDiscountItem>();
        var repoGift = _unitOfWork.GetRepository<PromotionGiftItem>();
        var repoStoreFood = _unitOfWork.GetRepository<StoreFood>();

        var fixedPriceItems = await repoFixedPrice.Query().AsNoTracking()
            .Where(x => promotionIds.Contains(x.PromotionId) && !x.IsDeleted)
            .ToListAsync();

        var discountItems = await repoDiscount.Query().AsNoTracking()
            .Where(x => promotionIds.Contains(x.PromotionId) && !x.IsDeleted)
            .ToListAsync();

        var giftItems = await repoGift.Query().AsNoTracking()
            .Where(x => promotionIds.Contains(x.PromotionId) && !x.IsDeleted)
            .OrderBy(x => x.SortOrder)
            .ToListAsync();

        var storeFoodIds = fixedPriceItems.Select(x => x.StoreFoodId)
            .Concat(discountItems.Select(x => x.StoreFoodId))
            .Concat(giftItems.Select(x => x.StoreFoodId))
            .Distinct()
            .ToList();

        var storeFoods = await repoStoreFood.Query().AsNoTracking()
            .Where(x => storeFoodIds.Contains(x.Id))
            .ToListAsync();

        string FoodName(long storeFoodId) => storeFoods.FirstOrDefault(x => x.Id == storeFoodId)?.FoodName ?? "";
        string? Thumbnail(long storeFoodId)
        {
            var thumbnailUrl = storeFoods.FirstOrDefault(x => x.Id == storeFoodId)?.ThumbnailUrl;
            return !string.IsNullOrWhiteSpace(thumbnailUrl)
                ? _cloudinaryService.BuildImageUrl(thumbnailUrl)
                : null;
        }
        decimal OriginalPrice(long storeFoodId) => storeFoods.FirstOrDefault(x => x.Id == storeFoodId)?.Price ?? 0;

        return promotions.Select(promotion => new PromotionResponseDto
        {
            Id = promotion.Id,
            RefCode = promotion.RefCode,
            StoreRefCode = promotion.StoreRefCode,
            PromotionCode = promotion.PromotionCode,
            PromotionType = promotion.PromotionType,
            Name = promotion.Name,
            Description = promotion.Description,
            Status = promotion.Status,
            StartDate = promotion.StartDate,
            EndDate = promotion.EndDate,
            StartTime = promotion.StartTime,
            EndTime = promotion.EndTime,
            DaysOfWeekMask = promotion.DaysOfWeekMask,
            ConditionType = promotion.ConditionType,
            ConditionMinAmount = promotion.ConditionMinAmount,
            ConditionMinQuantity = promotion.ConditionMinQuantity,
            ApplyToAllProducts = promotion.ApplyToAllProducts,
            DiscountType = promotion.DiscountType,
            DiscountValue = promotion.DiscountValue,
            MaxDiscountAmount = promotion.MaxDiscountAmount,
            CreatedAt = promotion.CreatedAt,

            FixedPriceItems = fixedPriceItems
                .Where(x => x.PromotionId == promotion.Id)
                .Select(x => new PromotionFixedPriceItemResponseDto
                {
                    Id = x.Id,
                    StoreFoodId = x.StoreFoodId,
                    FoodName = FoodName(x.StoreFoodId),
                    ThumbnailUrl = Thumbnail(x.StoreFoodId),
                    OriginalPrice = OriginalPrice(x.StoreFoodId),
                    FixedPrice = x.FixedPrice
                })
                .ToList(),

            DiscountItems = discountItems
                .Where(x => x.PromotionId == promotion.Id)
                .Select(x => new PromotionDiscountItemResponseDto
                {
                    Id = x.Id,
                    StoreFoodId = x.StoreFoodId,
                    FoodName = FoodName(x.StoreFoodId),
                    ThumbnailUrl = Thumbnail(x.StoreFoodId),
                    OriginalPrice = OriginalPrice(x.StoreFoodId),
                    DiscountType = x.DiscountType,
                    DiscountValue = x.DiscountValue,
                    MaxDiscountAmount = x.MaxDiscountAmount
                })
                .ToList(),

            GiftItems = giftItems
                .Where(x => x.PromotionId == promotion.Id)
                .Select(x => new PromotionGiftItemResponseDto
                {
                    Id = x.Id,
                    StoreFoodId = x.StoreFoodId,
                    FoodName = FoodName(x.StoreFoodId),
                    ThumbnailUrl = Thumbnail(x.StoreFoodId),
                    GiftQuantity = x.GiftQuantity,
                    SortOrder = x.SortOrder
                })
                .ToList()
        }).ToList();
    }
}

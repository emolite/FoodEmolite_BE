using FoodEmolite.Application.DTOs.Profile;
using FoodEmolite.Application.DTOs.Store;
using FoodEmolite.Application.ExternalService.Interfaces;
using FoodEmolite.Application.Interfaces;
using FoodEmolite.Domain.Entities;
using FoodEmolite.Domain.Interfaces;
using FoodEmolite.Shared.Responses;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace FoodEmolite.Application.Services;

public class ProfileService : IProfileService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICloudinaryService _cloudinaryService;

    public ProfileService(
        IUnitOfWork unitOfWork,
        ICloudinaryService cloudinaryService)
    {
        _unitOfWork = unitOfWork;
        _cloudinaryService = cloudinaryService;
    }

    public async Task<BaseTableResponse<UserProfileResponseDto>> GetAllAccountProfilesAsync(int page, int pageSize)
    {
        var repoAccount = _unitOfWork.GetRepository<Account>();
        var repoProfile = _unitOfWork.GetRepository<AccountProfile>();

        var accountQuery = repoAccount
            .Query()
            .AsNoTracking()
            .Where(x =>
                !x.IsDeleted &&
                x.Role == "User");

        var totalRecords = await accountQuery.CountAsync();

        var accounts = await accountQuery
            .OrderByDescending(x => x.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var accountIds = accounts
            .Select(x => x.Id)
            .ToList();

        var profiles = await repoProfile
            .Query()
            .AsNoTracking()
            .Where(x => accountIds.Contains(x.AccountId))
            .ToListAsync();

        var items = accounts.Select(account =>
        {
            var profile = profiles.FirstOrDefault(x =>
                x.AccountId == account.Id);

            return new UserProfileResponseDto
            {
                Account = new AccountDto
                {
                    Id = account.Id,
                    RefCode = account.RefCode,
                    Username = account.Username,
                    Email = account.Email,
                    Role = account.Role,
                    IsActive = account.IsActive
                },

                Profile = profile == null
                    ? null
                    : new AccountProfileDto
                    {
                        Id = profile.Id,
                        RefCode = profile.RefCode,
                        AccountId = profile.AccountId,
                        FullName = profile.FullName,
                        PhoneNumber = profile.PhoneNumber,
                        Gender = profile.Gender,
                        DateOfBirth = profile.DateOfBirth,
                        Address = profile.Address,
                        AvatarUrl = !string.IsNullOrWhiteSpace(profile.AvatarUrl)
                            ? _cloudinaryService.BuildImageUrl(profile.AvatarUrl)
                            : null
                    }
            };
        }).ToList();

        return new BaseTableResponse<UserProfileResponseDto>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalRecords = totalRecords
        };
    }

    public async Task<BaseTableResponse<MyProfileResponseDto>> GetAllAgentProfilesAsync(int page, int pageSize)
    {
        var repoAccount = _unitOfWork.GetRepository<Account>();
        var repoProfile = _unitOfWork.GetRepository<AccountProfile>();
        var repoBank = _unitOfWork.GetRepository<BankAccount>();
        var repoStore = _unitOfWork.GetRepository<Store>();

        var accountQuery = repoAccount
            .Query()
            .AsNoTracking()
            .Where(x =>
                !x.IsDeleted &&
                x.Role == "Agent");

        var totalRecords = await accountQuery.CountAsync();

        var accounts = await accountQuery
            .OrderByDescending(x => x.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var accountIds = accounts
            .Select(x => x.Id)
            .ToList();

        var profiles = await repoProfile
            .Query()
            .AsNoTracking()
            .Where(x => accountIds.Contains(x.AccountId))
            .ToListAsync();

        var bankAccounts = await repoBank
            .Query()
            .AsNoTracking()
            .Where(x =>
                accountIds.Contains(x.AccountId) &&
                x.IsActive)
            .ToListAsync();

        var stores = await repoStore
            .Query()
            .AsNoTracking()
            .Where(x =>
                x.OwnerAccountId.HasValue &&
                accountIds.Contains(x.OwnerAccountId.Value) &&
                !x.IsDeleted)
            .ToListAsync();

        var items = accounts.Select(account =>
        {
            var profile = profiles.FirstOrDefault(x => x.AccountId == account.Id);
            var store = stores.FirstOrDefault(x => x.OwnerAccountId == account.Id);

            return new MyProfileResponseDto
            {
                Account = new AccountDto
                {
                    Id = account.Id,
                    RefCode = account.RefCode,
                    Username = account.Username,
                    Email = account.Email,
                    Role = account.Role,
                    IsActive = account.IsActive
                },

                Profile = profile == null
                    ? null
                    : new AccountProfileDto
                    {
                        Id = profile.Id,
                        RefCode = profile.RefCode,
                        AccountId = profile.AccountId,
                        FullName = profile.FullName,
                        PhoneNumber = profile.PhoneNumber,
                        Gender = profile.Gender,
                        DateOfBirth = profile.DateOfBirth,
                        Address = profile.Address,
                        AvatarUrl = !string.IsNullOrWhiteSpace(profile.AvatarUrl)
                            ? _cloudinaryService.BuildImageUrl(profile.AvatarUrl)
                            : null
                    },

                BankAccounts = bankAccounts
                    .Where(x => x.AccountId == account.Id)
                    .Select(x => new BankAccountDto
                    {
                        Id = x.Id,
                        RefCode = x.RefCode,
                        AccountId = x.AccountId,
                        BankName = x.BankName,
                        BankCode = x.BankCode,
                        AccountNumber = x.AccountNumber,
                        AccountHolderName = x.AccountHolderName,
                        IsDefault = x.IsDefault,
                        IsActive = x.IsActive
                    })
                    .ToList(),

                Store = store == null
                    ? null
                    : new StoreResponseDto
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
                    }
            };
        }).ToList();

        return new BaseTableResponse<MyProfileResponseDto>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalRecords = totalRecords
        };
    }

    public async Task<BaseResponse<MyProfileResponseDto>> GetMyProfileAsync(long currentUserId)
    {
        var repoAccount = _unitOfWork.GetRepository<Account>();
        var repoProfile = _unitOfWork.GetRepository<AccountProfile>();
        var repoBank = _unitOfWork.GetRepository<BankAccount>();
        var repoStore = _unitOfWork.GetRepository<Store>();

        var account = await repoAccount.FirstOrDefaultAsync(x =>
            x.Id == currentUserId &&
            !x.IsDeleted);

        if (account is null)
            return BaseResponse<MyProfileResponseDto>.Fail("Account not found");

        var profile = await repoProfile.FirstOrDefaultAsync(x =>
            x.AccountId == currentUserId);

        var bankAccounts = await repoBank
            .Query()
            .AsNoTracking()
            .Where(x =>
                x.AccountId == currentUserId &&
                x.IsActive)
            .ToListAsync();

        var store = await repoStore.FirstOrDefaultAsync(x =>
            x.OwnerAccountId == currentUserId &&
            !x.IsDeleted);

        return BaseResponse<MyProfileResponseDto>.Success(
            new MyProfileResponseDto
            {
                Account = new AccountDto
                {
                    Id = account.Id,
                    RefCode = account.RefCode,
                    Username = account.Username,
                    Email = account.Email,
                    Role = account.Role,
                    IsActive = account.IsActive
                },

                Profile = profile == null
                    ? null
                    : new AccountProfileDto
                    {
                        FullName = profile.FullName,
                        PhoneNumber = profile.PhoneNumber,
                        Gender = profile.Gender,
                        DateOfBirth = profile.DateOfBirth,
                        Address = profile.Address,
                        AvatarUrl = !string.IsNullOrWhiteSpace(profile.AvatarUrl)
                            ? _cloudinaryService.BuildImageUrl(profile.AvatarUrl)
                            : null
                    },

                BankAccounts = bankAccounts
                    .Select(x => new BankAccountDto
                    {
                        Id = x.Id,
                        BankName = x.BankName,
                        BankCode = x.BankCode,
                        AccountNumber = x.AccountNumber,
                        AccountHolderName = x.AccountHolderName,
                        IsDefault = x.IsDefault,
                        IsActive = x.IsActive
                    })
                    .ToList(),

                Store = store == null
                    ? null
                    : new StoreResponseDto
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
                    }
            });
    }

    public async Task<BaseResponse<StorePaymentInfoResponseDto>> GetStorePaymentInfoAsync(string storeRefCode, decimal amount)
    {
        var repoStore = _unitOfWork.GetRepository<Store>();
        var repoBank = _unitOfWork.GetRepository<BankAccount>();

        var store = await repoStore.FirstOrDefaultAsync(x =>
            x.RefCode == storeRefCode &&
            x.IsActive &&
            !x.IsDeleted);

        if (store is null)
            return BaseResponse<StorePaymentInfoResponseDto>.Fail("Store not found");

        var bankAccount = await repoBank.FirstOrDefaultAsync(x =>
            x.AccountId == store.OwnerAccountId &&
            x.IsActive &&
            x.IsDefault);

        if (bankAccount is null)
            return BaseResponse<StorePaymentInfoResponseDto>.Fail("Bank account not found");

        var accountName = Uri.EscapeDataString(bankAccount.AccountHolderName ?? "");
        var addInfo = Uri.EscapeDataString($"Thanh toan don hang {store.StoreName}");
        var amountValue = Math.Round(amount, 0);

        var vietQrUrl =
            $"https://img.vietqr.io/image/{bankAccount.BankCode}-{bankAccount.AccountNumber}-compact2.png?amount={amountValue}&addInfo={addInfo}&accountName={accountName}";

        return BaseResponse<StorePaymentInfoResponseDto>.Success(
            new StorePaymentInfoResponseDto
            {
                StoreId = store.Id,
                StoreRefCode = store.RefCode,
                StoreName = store.StoreName,
                BankAccountId = bankAccount.Id,
                BankName = bankAccount.BankName,
                BankCode = bankAccount.BankCode,
                AccountNumber = bankAccount.AccountNumber,
                AccountHolderName = bankAccount.AccountHolderName,
                Amount = amount,
                VietQrUrl = vietQrUrl
            });
    }

    public async Task<BaseResponse<AccountProfileDto>> CreateAccountProfileAsync(long currentUserId, string currentUserRefCode, CreateAccountProfileRequestDto request)
    {
        var repoProfile = _unitOfWork.GetRepository<AccountProfile>();

        var existed = await repoProfile.FirstOrDefaultAsync(x =>
            x.AccountId == currentUserId);

        if (existed is not null)
            return BaseResponse<AccountProfileDto>.Fail("Profile already exists");

        string? avatarFileRefCode = null;

        if (request.AvatarUrl != null && request.AvatarUrl.Length > 0)
        {
            var uploadResult = await _cloudinaryService.UploadStoreImageAsync(
                request.AvatarUrl);

            if (!uploadResult.IsSuccess)
                return BaseResponse<AccountProfileDto>.Fail(uploadResult.Message);

            avatarFileRefCode = uploadResult.Data;
        }

        var profile = new AccountProfile
        {
            RefCode = currentUserRefCode,
            AccountId = currentUserId,
            FullName = request.FullName,
            PhoneNumber = request.PhoneNumber,
            Gender = request.Gender,
            DateOfBirth = request.DateOfBirth,
            Address = request.Address,
            AvatarUrl = avatarFileRefCode,
            CreatedBy = currentUserId
        };

        await repoProfile.AddAsync(profile);
        await _unitOfWork.SaveChangesAsync();

        return BaseResponse<AccountProfileDto>.Success(new AccountProfileDto
        {
            Id = profile.Id,
            RefCode = profile.RefCode,
            AccountId = profile.AccountId,
            FullName = profile.FullName,
            PhoneNumber = profile.PhoneNumber,
            Gender = profile.Gender,
            DateOfBirth = profile.DateOfBirth,
            Address = profile.Address,
            AvatarUrl = !string.IsNullOrWhiteSpace(profile.AvatarUrl)
                ? _cloudinaryService.BuildImageUrl(profile.AvatarUrl)
                : null
        });
    }

    public async Task<BaseResponse<AccountProfileDto>> UpdateAccountProfileAsync(long currentUserId, string currentUserRefCode, UpdateAccountProfileRequestDto request)
    {
        var repoProfile = _unitOfWork.GetRepository<AccountProfile>();

        var profile = await repoProfile.FirstOrDefaultAsync(x =>
            x.AccountId == currentUserId);

        if (profile is null)
            return BaseResponse<AccountProfileDto>.Fail("Profile not found");

        if (request.AvatarUrl != null && request.AvatarUrl.Length > 0)
        {
            var uploadResult = await _cloudinaryService.UploadStoreImageAsync(
                request.AvatarUrl);

            if (!uploadResult.IsSuccess)
                return BaseResponse<AccountProfileDto>.Fail(uploadResult.Message);

            profile.AvatarUrl = uploadResult.Data;
        }

        profile.RefCode = currentUserRefCode;
        profile.FullName = request.FullName;
        profile.PhoneNumber = request.PhoneNumber;
        profile.Gender = request.Gender;
        profile.DateOfBirth = request.DateOfBirth;
        profile.Address = request.Address;
        profile.UpdatedAt = DateTime.Now;
        profile.UpdatedBy = currentUserId;

        repoProfile.Update(profile);
        await _unitOfWork.SaveChangesAsync();

        return BaseResponse<AccountProfileDto>.Success(new AccountProfileDto
        {
            Id = profile.Id,
            RefCode = profile.RefCode,
            AccountId = profile.AccountId,
            FullName = profile.FullName,
            PhoneNumber = profile.PhoneNumber,
            Gender = profile.Gender,
            DateOfBirth = profile.DateOfBirth,
            Address = profile.Address,
            AvatarUrl = !string.IsNullOrWhiteSpace(profile.AvatarUrl)
                ? _cloudinaryService.BuildImageUrl(profile.AvatarUrl)
                : null
        });
    }

    public async Task<BaseResponse<BankAccountDto>> CreateBankAccountAsync(long currentUserId, string currentUserRefCode, CreateBankAccountRequestDto request)
    {
        var repoBank = _unitOfWork.GetRepository<BankAccount>();

        if (request.IsDefault)
        {
            var defaultBanks = await repoBank
                .Query()
                .Where(x =>
                    x.AccountId == currentUserId &&
                    x.IsDefault &&
                    x.IsActive)
                .ToListAsync();

            foreach (var item in defaultBanks)
            {
                item.IsDefault = false;
                item.UpdatedAt = DateTime.Now;
                item.UpdatedBy = currentUserId;

                repoBank.Update(item);
            }
        }

        var bankAccount = new BankAccount
        {
            RefCode = currentUserRefCode,
            AccountId = currentUserId,
            BankName = request.BankName,
            BankCode = request.BankCode,
            AccountNumber = request.AccountNumber,
            AccountHolderName = request.AccountHolderName,
            IsDefault = request.IsDefault,
            IsActive = true,
            CreatedAt = DateTime.Now,
            CreatedBy = currentUserId
        };

        await repoBank.AddAsync(bankAccount);
        await _unitOfWork.SaveChangesAsync();

        return BaseResponse<BankAccountDto>.Success(new BankAccountDto
        {
            Id = bankAccount.Id,
            RefCode = bankAccount.RefCode,
            AccountId = bankAccount.AccountId,
            BankName = bankAccount.BankName,
            BankCode = bankAccount.BankCode,
            AccountNumber = bankAccount.AccountNumber,
            AccountHolderName = bankAccount.AccountHolderName,
            IsDefault = bankAccount.IsDefault,
            IsActive = bankAccount.IsActive
        });
    }

    public async Task<BaseResponse<BankAccountDto>> UpdateBankAccountAsync(long currentUserId, string currentUserRefCode, UpdateBankAccountRequestDto request)
    {
        var repoBank = _unitOfWork.GetRepository<BankAccount>();

        var bankAccount = await repoBank.FirstOrDefaultAsync(x =>
            x.AccountId == currentUserId &&
            x.RefCode == currentUserRefCode &&
            x.IsActive);

        if (bankAccount is null)
            return BaseResponse<BankAccountDto>.Fail("Bank account not found");

        if (request.IsDefault)
        {
            var defaultBanks = await repoBank
                .Query()
                .Where(x =>
                    x.AccountId == currentUserId &&
                    x.Id != bankAccount.Id &&
                    x.IsDefault &&
                    x.IsActive)
                .ToListAsync();

            foreach (var item in defaultBanks)
            {
                item.IsDefault = false;
                item.UpdatedAt = DateTime.Now;
                item.UpdatedBy = currentUserId;

                repoBank.Update(item);
            }
        }

        bankAccount.BankName = request.BankName;
        bankAccount.BankCode = request.BankCode;
        bankAccount.AccountNumber = request.AccountNumber;
        bankAccount.AccountHolderName = request.AccountHolderName;
        bankAccount.IsDefault = request.IsDefault;
        bankAccount.UpdatedAt = DateTime.Now;
        bankAccount.UpdatedBy = currentUserId;

        repoBank.Update(bankAccount);
        await _unitOfWork.SaveChangesAsync();

        return BaseResponse<BankAccountDto>.Success(new BankAccountDto
        {
            Id = bankAccount.Id,
            RefCode = bankAccount.RefCode,
            AccountId = bankAccount.AccountId,
            BankName = bankAccount.BankName,
            BankCode = bankAccount.BankCode,
            AccountNumber = bankAccount.AccountNumber,
            AccountHolderName = bankAccount.AccountHolderName,
            IsDefault = bankAccount.IsDefault,
            IsActive = bankAccount.IsActive
        });
    }
}
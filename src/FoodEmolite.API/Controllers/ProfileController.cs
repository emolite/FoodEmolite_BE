using FoodEmolite.Application.DTOs.Profile;
using FoodEmolite.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FoodEmolite.API.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/profile")]
    public class ProfileController : BaseApiController
    {
        private readonly IProfileService _profileService;

        public ProfileController(IProfileService profileService)
        {
            _profileService = profileService;
        }

        [HttpGet("me")]
        public async Task<IActionResult> GetMyProfile()
        {
            var result = await _profileService.GetMyProfileAsync(CurrentUserId!.Value);
            return Ok(result);
        }

        [HttpPost("account-profile")]
        public async Task<IActionResult> CreateAccountProfile([FromForm] CreateAccountProfileRequestDto request)
        {
            var result = await _profileService.CreateAccountProfileAsync(
                CurrentUserId!.Value,
                CurrentUserRefCode!,
                request);

            return Ok(result);
        }

        [HttpPut("account-profile")]
        public async Task<IActionResult> UpdateAccountProfile([FromForm] UpdateAccountProfileRequestDto request)
        {
            var result = await _profileService.UpdateAccountProfileAsync(
                CurrentUserId!.Value, CurrentUserRefCode!,
                request);

            return Ok(result);
        }

        [HttpPost("bank-accounts")]
        public async Task<IActionResult> CreateBankAccount([FromBody] CreateBankAccountRequestDto request)
        {
            var result = await _profileService.CreateBankAccountAsync(
                 CurrentUserId!.Value,
                CurrentUserRefCode!,
                request);

            return Ok(result);
        }

        [HttpPut("bank-accounts/{refCode}")]
        public async Task<IActionResult> UpdateBankAccount([FromBody] UpdateBankAccountRequestDto request)
        {
            var result = await _profileService.UpdateBankAccountAsync(
                CurrentUserId!.Value,
                CurrentUserRefCode!,
                request);

            return Ok(result);
        }
    }
}

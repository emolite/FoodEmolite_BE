
using FoodEmolite.Application.DTOs.SePay;
using FoodEmolite.Application.ExternalService.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace FoodEmolite.API.Controllers
{
    [ApiController]
    [Route("api/payments/sepay")]
    public class SePayController : ControllerBase
    {
        private readonly ISePayWebhookService _service;
        private readonly IVietQrService _vietQrService;

        public SePayController(ISePayWebhookService service, IVietQrService vietQrService)
        {
            _service = service;
            _vietQrService = vietQrService;
        }

        [HttpPost("webhook")]
        public async Task<IActionResult> Webhook([FromBody] SePayWebhookRequest request)
        {
            await _service.HandleAsync(request);
            return Ok(new { success = true });
        }

        [HttpGet("vietqr")]
        public IActionResult GenerateVietQr([FromQuery] GenerateQrRequest request)
        {
            var qrUrl = _vietQrService.Generate(request);

            return Ok(new
            {
                success = true,
                data = qrUrl
            });
        }
    }
}
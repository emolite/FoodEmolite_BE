using System.Text.Json.Serialization;

namespace FoodEmolite.Application.DTOs.SePay
{
    public class SePayWebhookRequest
    {
        [JsonPropertyName("id")]
        public long TransactionId { get; set; }

        public string Content { get; set; } = string.Empty;

        public string AccountNumber { get; set; } = string.Empty;

        [JsonPropertyName("transferAmount")]
        public long Amount { get; set; }

        public string? Description { get; set; }

        public string? Gateway { get; set; }

        public string? TransactionDate { get; set; }

        [JsonPropertyName("referenceCode")]
        public string? ReferenceCode { get; set; }

        public string? Code { get; set; }

        public string? SubAccount { get; set; }

        public string? TransferType { get; set; }
    }
}
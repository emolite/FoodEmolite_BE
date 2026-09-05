namespace FoodEmolite.Application.DTOs.ActivityLog;

public class ActivityLogResponseDto
{
    public long Id { get; set; }
    public string ActorType { get; set; } = string.Empty;
    public long? ActorId { get; set; }
    public string? ActorName { get; set; }
    public string Action { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public class ActivityLogSearchRequest
{
    public string? Keyword { get; set; }
    public string? Action { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
}

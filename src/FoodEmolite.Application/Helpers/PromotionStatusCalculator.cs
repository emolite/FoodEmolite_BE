namespace FoodEmolite.Application.Helpers;

/// <summary>
/// Tính trạng thái "theo lịch" của một Promotion (SCHEDULED/ACTIVE/ENDED) dựa trên
/// ngày bắt đầu/kết thúc, khung giờ trong ngày và các ngày trong tuần được áp dụng.
/// Không đụng tới DRAFT/PAUSED — 2 trạng thái đó do người dùng chủ động set.
/// </summary>
public static class PromotionStatusCalculator
{
    public const string Draft = "DRAFT";
    public const string Scheduled = "SCHEDULED";
    public const string Active = "ACTIVE";
    public const string Paused = "PAUSED";
    public const string Ended = "ENDED";

    public static string ComputeStatus(
        DateOnly startDate,
        DateOnly? endDate,
        TimeOnly? startTime,
        TimeOnly? endTime,
        int daysOfWeekMask,
        DateTime now)
    {
        var today = DateOnly.FromDateTime(now);

        if (endDate.HasValue && today > endDate.Value)
            return Ended;

        if (today < startDate)
            return Scheduled;

        var currentTime = TimeOnly.FromDateTime(now);

        var dayMatches = DayMatches(daysOfWeekMask, now.DayOfWeek);

        var timeMatches =
            !startTime.HasValue ||
            !endTime.HasValue ||
            (currentTime >= startTime.Value && currentTime <= endTime.Value);

        return dayMatches && timeMatches ? Active : Scheduled;
    }

    // bit0 = Monday ... bit6 = Sunday
    private static bool DayMatches(int daysOfWeekMask, DayOfWeek dayOfWeek)
    {
        var bitIndex = ((int)dayOfWeek + 6) % 7;

        return (daysOfWeekMask & (1 << bitIndex)) != 0;
    }
}

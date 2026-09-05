namespace FoodEmolite.Shared.Common;

public static class DateTimeHelper
{
    private static readonly TimeSpan VietnamOffset = TimeSpan.FromHours(7);

    public static DateTime VnNow => DateTime.UtcNow.Add(VietnamOffset);
}

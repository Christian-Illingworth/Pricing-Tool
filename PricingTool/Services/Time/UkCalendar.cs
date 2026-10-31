namespace PricingTool.Services.Time;

// UK calendar dates. Prices are valid until the end of the UK day they were fetched, and start dates
// are checked against today in the UK (research R14), so both need Europe/London rather than UTC or
// the server's own clock. TimeProvider is injected so tests can stand anywhere in time.
public static class UkCalendar
{
    public static readonly TimeZoneInfo Zone = FindZone();

    public static DateOnly Today(TimeProvider time) => DateOf(time.GetUtcNow().UtcDateTime);

    public static DateOnly DateOf(DateTime utc)
    {
        var instant = utc.Kind switch
        {
            DateTimeKind.Local => utc.ToUniversalTime(),
            DateTimeKind.Unspecified => DateTime.SpecifyKind(utc, DateTimeKind.Utc),
            _ => utc,
        };
        return DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(instant, Zone));
    }

    // IANA id first (ICU, every platform since .NET 6); the Windows id if ICU is unavailable.
    private static TimeZoneInfo FindZone()
    {
        try { return TimeZoneInfo.FindSystemTimeZoneById("Europe/London"); }
        catch (TimeZoneNotFoundException) { return TimeZoneInfo.FindSystemTimeZoneById("GMT Standard Time"); }
    }
}

using PricingTool.Services.Time;
using PricingTool.Tests.Support;

namespace PricingTool.Tests.Time;

public class UkCalendarTests
{
    private static DateTime Utc(string iso) => DateTime.SpecifyKind(DateTime.Parse(iso), DateTimeKind.Utc);

    [Fact]
    public void WinterUkDateIsTheUtcDate()
    {
        Assert.Equal(new DateOnly(2026, 1, 15), UkCalendar.DateOf(Utc("2026-01-15T00:00:00")));
        Assert.Equal(new DateOnly(2026, 1, 15), UkCalendar.DateOf(Utc("2026-01-15T23:59:59")));
        Assert.Equal(new DateOnly(2026, 1, 16), UkCalendar.DateOf(Utc("2026-01-16T00:00:00")));
    }

    [Fact]
    public void SummerUkDayStartsAnHourBeforeUtcMidnight()
    {
        // 23:00 UTC on 15 July is midnight BST on 16 July.
        Assert.Equal(new DateOnly(2026, 7, 15), UkCalendar.DateOf(Utc("2026-07-15T22:59:59")));
        Assert.Equal(new DateOnly(2026, 7, 16), UkCalendar.DateOf(Utc("2026-07-15T23:00:00")));
    }

    [Fact]
    public void BritishSummerTimeStartsOnTheLastSundayInMarch()
    {
        // Clocks go forward at 01:00 UTC on 29 March 2026.
        Assert.Equal(new DateOnly(2026, 3, 28), UkCalendar.DateOf(Utc("2026-03-28T23:30:00")));   // still GMT
        Assert.Equal(new DateOnly(2026, 3, 29), UkCalendar.DateOf(Utc("2026-03-29T22:59:59")));   // BST, 23:59:59
        Assert.Equal(new DateOnly(2026, 3, 30), UkCalendar.DateOf(Utc("2026-03-29T23:00:00")));   // BST midnight
    }

    [Fact]
    public void BritishSummerTimeEndsOnTheLastSundayInOctober()
    {
        // Clocks go back at 01:00 UTC on 25 October 2026.
        Assert.Equal(new DateOnly(2026, 10, 25), UkCalendar.DateOf(Utc("2026-10-24T23:00:00")));  // BST midnight
        Assert.Equal(new DateOnly(2026, 10, 25), UkCalendar.DateOf(Utc("2026-10-25T23:59:59")));  // GMT again
        Assert.Equal(new DateOnly(2026, 10, 26), UkCalendar.DateOf(Utc("2026-10-26T00:00:00")));
    }

    [Fact]
    public void TodayUsesTheInjectedClock()
    {
        var clock = FakeTimeProvider.At("2026-06-30T23:30:00Z");
        Assert.Equal(new DateOnly(2026, 7, 1), UkCalendar.Today(clock));

        clock.Advance(TimeSpan.FromDays(1));
        Assert.Equal(new DateOnly(2026, 7, 2), UkCalendar.Today(clock));
    }

    [Fact]
    public void UnspecifiedKindIsTreatedAsUtc()
    {
        var unspecified = new DateTime(2026, 7, 15, 23, 30, 0, DateTimeKind.Unspecified);
        Assert.Equal(new DateOnly(2026, 7, 16), UkCalendar.DateOf(unspecified));
    }
}

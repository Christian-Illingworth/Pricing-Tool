using System.Globalization;

namespace PricingTool.Tests.Support;

// A clock the test controls, so date rules can be checked at UK midnight and across the British
// Summer Time changes without waiting (research R27). Saves a package for ten lines.
public sealed class FakeTimeProvider(DateTimeOffset utcNow) : TimeProvider
{
    public DateTimeOffset UtcNow { get; set; } = utcNow;

    public override DateTimeOffset GetUtcNow() => UtcNow;

    public void Advance(TimeSpan by) => UtcNow += by;

    public static FakeTimeProvider At(string isoUtc) =>
        new(DateTimeOffset.Parse(isoUtc, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal));
}

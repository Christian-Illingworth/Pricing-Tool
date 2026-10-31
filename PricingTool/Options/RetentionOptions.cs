namespace PricingTool.Options;

public sealed class RetentionOptions : IValidatableOptions
{
    public const string Section = "Retention";

    // When the daily clean-up runs, in UK time. The period itself is the admin setting Retention.Years.
    public TimeOnly RunAtUkTime { get; set; } = new(2, 0);

    public IEnumerable<string> Validate() => [];
}

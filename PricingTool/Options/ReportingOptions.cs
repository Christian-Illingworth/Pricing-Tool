namespace PricingTool.Options;

public sealed class ReportingOptions : IValidatableOptions
{
    public const string Section = "Reporting";

    public OutboxSettings Outbox { get; set; } = new();

    public sealed class OutboxSettings
    {
        public int PollSeconds { get; set; } = 30;
    }

    public IEnumerable<string> Validate()
    {
        if (Outbox.PollSeconds is < 5 or > 3600) yield return "Reporting:Outbox:PollSeconds must be between 5 and 3600.";
    }
}

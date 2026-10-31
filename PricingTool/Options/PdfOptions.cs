namespace PricingTool.Options;

public sealed class PdfOptions : IValidatableOptions
{
    public const string Section = "Pdf";

    public int MaxConcurrentRenders { get; set; } = 2;

    public IEnumerable<string> Validate()
    {
        if (MaxConcurrentRenders is < 1 or > 8) yield return "Pdf:MaxConcurrentRenders must be between 1 and 8.";
    }
}

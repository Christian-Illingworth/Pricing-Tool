namespace PricingTool.Options;

public sealed class UploadOptions : IValidatableOptions
{
    public const string Section = "Uploads";

    public int MaxRows { get; set; } = 200;

    public long MaxBytes { get; set; } = 2_097_152;

    public IEnumerable<string> Validate()
    {
        if (MaxRows is < 1 or > 1000) yield return "Uploads:MaxRows must be between 1 and 1000.";
        if (MaxBytes is < 1 or > 10_485_760) yield return "Uploads:MaxBytes must be between 1 and 10485760.";
    }
}

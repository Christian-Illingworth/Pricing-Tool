namespace PricingTool.Options;

public sealed class StorageOptions : IValidatableOptions
{
    public const string Section = "Storage";

    public LocalFolder Files { get; set; } = new() { LocalRoot = "App_Data/files" };

    public LocalFolder ReportingExport { get; set; } = new() { LocalRoot = "App_Data/reporting" };

    // A folder relative to the content root (git-ignored), until Blob Storage arrives with deployment.
    public sealed class LocalFolder
    {
        public string LocalRoot { get; set; } = "";
    }

    public IEnumerable<string> Validate()
    {
        if (OptionsRegistration.IsMissing(Files.LocalRoot)) yield return "Storage:Files:LocalRoot is required.";
        if (OptionsRegistration.IsMissing(ReportingExport.LocalRoot)) yield return "Storage:ReportingExport:LocalRoot is required.";
    }
}

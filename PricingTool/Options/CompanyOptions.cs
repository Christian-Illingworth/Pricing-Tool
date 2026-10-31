namespace PricingTool.Options;

// Our own details for the PDF header and footer. Configuration, not code, and not regulatory wording (R19).
public sealed class CompanyOptions : IValidatableOptions
{
    public const string Section = "Company";

    public string Name { get; set; } = "Consultiv Utilities";
    public string[] AddressLines { get; set; } = [];
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Website { get; set; }
    public string? RegistrationNumber { get; set; }

    public IEnumerable<string> Validate()
    {
        if (OptionsRegistration.IsMissing(Name)) yield return "Company:Name is required.";
    }
}

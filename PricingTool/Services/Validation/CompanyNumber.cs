using System.Text.RegularExpressions;
using PricingTool.Domain;

namespace PricingTool.Services.Validation;

public static partial class CompanyNumber
{
    // 8 digits, or 2 letters followed by 6 digits, such as SC123456 (FR-006).
    [GeneratedRegex("^([0-9]{8}|[A-Z]{2}[0-9]{6})$")]
    private static partial Regex Pattern();

    public static bool AppliesTo(BusinessType type) =>
        type is BusinessType.LimitedCompany or BusinessType.Llp or BusinessType.Plc;

    public static string? Normalise(string? input) =>
        string.IsNullOrWhiteSpace(input) ? null : input.Trim().Replace(" ", "").ToUpperInvariant();

    // The number is optional. When given, it must belong to a company type and have the right format.
    // Returns the error to show, or null.
    public static string? Validate(string? input, BusinessType? type)
    {
        var number = Normalise(input);
        if (number is null) return null;
        if (type is null || !AppliesTo(type.Value))
            return "A company number only applies to a limited company, LLP or PLC.";
        return Pattern().IsMatch(number)
            ? null
            : "A company number is 8 characters: 8 digits, or 2 letters followed by 6 digits.";
    }
}

using System.Text.RegularExpressions;

namespace PricingTool.Services.Validation;

public static partial class UkPhone
{
    [GeneratedRegex(@"^(\+44|0)\d{9,10}$")]
    private static partial Regex Pattern();

    // A UK number after removing spaces (FR-007, research R15). The normalised form is what's stored.
    public static bool TryNormalise(string? input, out string normalised)
    {
        normalised = string.Concat((input ?? "").Where(c => !char.IsWhiteSpace(c)));
        return Pattern().IsMatch(normalised);
    }
}

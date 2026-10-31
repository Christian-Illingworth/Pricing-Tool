using System.Text.RegularExpressions;
using PricingTool.Domain;

namespace PricingTool.Services.Validation;

public static partial class Postcode
{
    [GeneratedRegex("^[A-Z]{1,2}[0-9][A-Z0-9]?[0-9][A-Z]{2}$")]
    private static partial Regex UkPattern();

    // Uppercases, removes every space, checks the UK pattern, then puts one space before the last three
    // characters: "zz11aa" → "ZZ1 1AA". Null when it isn't a full UK postcode. Runs on the server before
    // any lookup (FR-008, research R5).
    public static NormalisedPostcode? Normalise(string? input)
    {
        if (string.IsNullOrWhiteSpace(input)) return null;
        var compact = string.Concat(input.Where(c => !char.IsWhiteSpace(c))).ToUpperInvariant();
        if (!UkPattern().IsMatch(compact)) return null;
        return new NormalisedPostcode($"{compact[..^3]} {compact[^3..]}");
    }

    public static bool TryNormalise(string? input, out NormalisedPostcode? postcode)
    {
        postcode = Normalise(input);
        return postcode is not null;
    }
}

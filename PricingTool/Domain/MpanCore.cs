namespace PricingTool.Domain;

// The 13-digit MPAN core (the bottom line), with its check digit and distribution region (FR-017,
// research R15). Only a valid core can be constructed.
public sealed record MpanCore
{
    private static readonly int[] Primes = [3, 5, 7, 13, 17, 19, 23, 29, 31, 37, 41, 43];

    private static readonly Dictionary<int, string> Regions = new()
    {
        [10] = "Eastern",
        [11] = "East Midlands",
        [12] = "London",
        [13] = "Merseyside and North Wales",
        [14] = "Midlands",
        [15] = "Northern",
        [16] = "North Western",
        [17] = "North Scotland",
        [18] = "South Scotland",
        [19] = "South Eastern",
        [20] = "Southern",
        [21] = "South Wales",
        [22] = "South Western",
        [23] = "Yorkshire",
    };

    public const string IdnoRegion = "Independent network (IDNO)";

    public string Value { get; }

    public string DistributorId => Value[..2];

    public string RegionName { get; }

    private MpanCore(string value, string regionName)
    {
        Value = value;
        RegionName = regionName;
    }

    public static bool TryParse(string? input, out MpanCore? core, out string? error)
    {
        core = null;
        var digits = (input ?? "").Replace(" ", "");

        if (digits.Length == 0)
        {
            error = "Enter the 13-digit MPAN core (the bottom line of the MPAN).";
            return false;
        }
        if (digits.Length != 13 || !digits.All(char.IsAsciiDigit))
        {
            error = "The MPAN core must be exactly 13 digits.";
            return false;
        }
        if (!HasValidCheckDigit(digits))
        {
            error = "The MPAN core's check digit is wrong. Check the number.";
            return false;
        }
        var region = RegionFor(digits[..2]);
        if (region is null)
        {
            error = $"The MPAN core starts with {digits[..2]}, which isn't a distribution region.";
            return false;
        }

        core = new MpanCore(digits, region);
        error = null;
        return true;
    }

    // (Σ dᵢ × pᵢ over the first 12 digits) mod 11 mod 10 must equal the 13th digit.
    public static bool HasValidCheckDigit(string digits13)
    {
        if (digits13.Length != 13 || !digits13.All(char.IsAsciiDigit)) return false;
        var sum = 0;
        for (var i = 0; i < 12; i++) sum += (digits13[i] - '0') * Primes[i];
        return sum % 11 % 10 == digits13[12] - '0';
    }

    // 10–23 are the 14 distribution regions; 24–99 are independent networks; 00–09 aren't valid.
    public static string? RegionFor(string distributorId)
    {
        if (!int.TryParse(distributorId, out var id) || distributorId.Length != 2) return null;
        if (Regions.TryGetValue(id, out var name)) return name;
        return id >= 24 ? IdnoRegion : null;
    }

    public override string ToString() => Value;
}

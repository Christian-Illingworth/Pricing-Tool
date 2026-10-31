namespace PricingTool.Domain;

// The MPAN top line (FR-015, FR-016, research R15), in one of two layouts:
//   legacy, 8 digits:      profile class (2) + meter time-switch code (3) + line loss factor class (3)
//   post-MHHS, 9 chars:    profile class (2) + standard settlement configuration (4) + DUoS tariff ID (3)
// The 9-character layout follows POWWR's example 040151661 and is unconfirmed (open item P-10), so it
// lives only in TryParse: a correction from POWWR is a change here and nowhere else.
public sealed record MpanTopLine(
    string Value,
    int ProfileClass,
    int? MeterTimeswitchClass,
    string? LineLossFactorClass,
    string? StandardSettlementConfig,
    string? DuosTariffId,
    bool MhhsMigrated)
{
    public static MpanTopLine Parse(string input) =>
        TryParse(input, out var topLine, out var error) ? topLine! : throw new FormatException(error);

    public static bool TryParse(string? input, out MpanTopLine? topLine, out string? error)
    {
        topLine = null;
        var value = (input ?? "").Replace(" ", "").ToUpperInvariant();

        if (value.Length is not (8 or 9))
        {
            error = "The MPAN top line must be 8 digits, or 9 characters for a meter on market-wide half-hourly settlement.";
            return false;
        }
        if (!IsDigits(value[..2]))
        {
            error = "The first two characters of the MPAN top line are the profile class, so they must be digits.";
            return false;
        }
        var profileClass = int.Parse(value[..2]);

        if (value.Length == 8)
        {
            if (!IsDigits(value[2..5]))
            {
                error = "Characters 3 to 5 of the MPAN top line are the meter time-switch code, so they must be digits.";
                return false;
            }
            if (!IsAlphanumeric(value[5..8]))
            {
                error = "The last three characters of the MPAN top line are the line loss factor class.";
                return false;
            }
            topLine = new MpanTopLine(value, profileClass, int.Parse(value[2..5]), value[5..8], null, null, MhhsMigrated: false);
        }
        else
        {
            if (!IsDigits(value[2..6]))
            {
                error = "Characters 3 to 6 of a 9-character MPAN top line are the standard settlement configuration, so they must be digits.";
                return false;
            }
            if (!IsAlphanumeric(value[6..9]))
            {
                error = "The last three characters of a 9-character MPAN top line are the DUoS tariff ID.";
                return false;
            }
            topLine = new MpanTopLine(value, profileClass, null, null, value[2..6], value[6..9], MhhsMigrated: true);
        }

        error = null;
        return true;
    }

    // The individual fields that appear in this top line and hold a different value. Blank fields
    // and fields this layout doesn't carry never disagree.
    public IReadOnlyList<string> Disagreements(
        int? profileClass,
        int? meterTimeswitchClass,
        string? lineLossFactorClass,
        string? standardSettlementConfig,
        string? duosTariffId)
    {
        var fields = new List<string>();
        if (profileClass.HasValue && profileClass != ProfileClass) fields.Add("ProfileClass");
        if (Differs(MeterTimeswitchClass, meterTimeswitchClass)) fields.Add("MeterTimeswitchClass");
        if (Differs(LineLossFactorClass, lineLossFactorClass)) fields.Add("LineLossFactorClass");
        if (Differs(StandardSettlementConfig, standardSettlementConfig)) fields.Add("StandardSettlementConfig");
        if (Differs(DuosTariffId, duosTariffId)) fields.Add("DuosTariffId");
        return fields;
    }

    private static bool Differs(int? inTopLine, int? field) =>
        inTopLine.HasValue && field.HasValue && inTopLine != field;

    private static bool Differs(string? inTopLine, string? field) =>
        inTopLine is not null && !string.IsNullOrWhiteSpace(field)
        && !string.Equals(inTopLine, field.Trim(), StringComparison.OrdinalIgnoreCase);

    private static bool IsDigits(string s) => s.All(char.IsAsciiDigit);

    private static bool IsAlphanumeric(string s) => s.All(char.IsAsciiLetterOrDigit);
}

namespace PricingTool.Domain;

// A gas meter point reference number: digits only, 6–10 long (FR-043). The spec defines no check digit.
public sealed record Mprn
{
    public string Value { get; }

    private Mprn(string value) => Value = value;

    public static bool TryParse(string? input, out Mprn? mprn, out string? error)
    {
        mprn = null;
        var digits = (input ?? "").Replace(" ", "");

        if (digits.Length == 0)
        {
            error = "Enter the MPRN.";
            return false;
        }
        if (digits.Length is < 6 or > 10 || !digits.All(char.IsAsciiDigit))
        {
            error = "The MPRN must be 6 to 10 digits.";
            return false;
        }

        mprn = new Mprn(digits);
        error = null;
        return true;
    }

    public override string ToString() => Value;
}

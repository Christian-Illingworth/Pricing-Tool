namespace PricingTool.Domain;

// A UK postcode in its full format with one space, for example "ZZ1 1AA". Only Postcode.Normalise can
// make one, so anything holding this type has already been validated (FR-008).
public sealed record NormalisedPostcode
{
    public string Value { get; }

    internal NormalisedPostcode(string value) => Value = value;

    public override string ToString() => Value;
}

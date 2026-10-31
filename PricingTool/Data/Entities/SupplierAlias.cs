namespace PricingTool.Data.Entities;

// Maps the meter lookup's supplier identifiers to a supplier (research R5, US8-4). At least one of the
// three is set, and each is unique where present.
public class SupplierAlias
{
    public int Id { get; set; }

    public int SupplierId { get; set; }
    public Supplier Supplier { get; set; } = null!;

    // For example BRITISHG-001-S.
    public string? PowwrId { get; set; }

    // For example ENRD. Also matched against the lookup's shortCode.
    public string? Mpid { get; set; }

    // For example "British Gas Business".
    public string? LookupName { get; set; }
}

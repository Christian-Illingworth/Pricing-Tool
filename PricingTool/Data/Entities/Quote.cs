using PricingTool.Domain;

namespace PricingTool.Data.Entities;

// One customer, one fuel, one or more sites (data-model.md "Quote").
public class Quote
{
    public int Id { get; set; }

    // CUQ-yy-nnnnnn, from the QuoteNumbers sequence (FR-078, research R28).
    public string Reference { get; set; } = "";

    // The only person who can change the quote (FR-003).
    public int OwnerId { get; set; }
    public StaffMember Owner { get; set; } = null!;

    // Fixed once created.
    public Fuel Fuel { get; set; }

    public QuoteType QuoteType { get; set; }

    // Required when ChangeOfTenancy (FR-013).
    public DateOnly? CotDate { get; set; }

    public bool OutOfContract { get; set; }

    public QuoteCustomer Customer { get; set; } = new();

    public QuoteContract Contract { get; set; } = new();

    // Low use is decided per site by the engine (FR-030), so it isn't a choice here.
    public VatCclTreatment VatCclTreatment { get; set; }

    // 0–100; required when Qualifying (FR-029).
    public decimal? QualifyingPercent { get; set; }

    // The display switch, saved and used on the PDF (FR-034).
    public bool IncludeVatCcl { get; set; }

    public QuoteStatus Status { get; set; }

    public DateTime CreatedUtc { get; set; }

    // Updated by every action; drives retention (FR-080).
    public DateTime LastActivityUtc { get; set; }

    // Set by SQL Server; null until the row is first saved.
    public byte[] RowVersion { get; set; } = null!;

    public List<Site> Sites { get; set; } = [];

    public List<QuoteUpliftOverride> UpliftOverrides { get; set; } = [];

    public List<PricingRun> PricingRuns { get; set; } = [];
}

// Owned by Quote, stored in its table (FR-005 to FR-007, FR-027).
public class QuoteCustomer
{
    public string BusinessName { get; set; } = "";
    public BusinessType BusinessType { get; set; }

    // Only for LimitedCompany, Llp and Plc.
    public string? CompanyNumber { get; set; }

    public string ContactName { get; set; } = "";
    public string ContactEmail { get; set; } = "";

    // Stored normalised (UkPhone).
    public string ContactPhone { get; set; } = "";

    // The microbusiness declaration: the customer's yes/no answers. Changes are audited (FR-028).
    public bool DeclaredFewerThan10Employees { get; set; }
    public bool DeclaredTurnoverOrBalanceUnder2m { get; set; }
}

// Owned by Quote, stored in its table (FR-024 to FR-026).
public class QuoteContract
{
    // Not in the past (UK date) when pricing.
    public DateOnly StartDate { get; set; }

    // A set from {12, 24, 36, 48, 60}, stored as "12,24".
    public string TermsMonths { get; set; } = "";

    public PaymentMethod PaymentMethod { get; set; }
    public PaymentFrequency? PaymentFrequency { get; set; }
    public PaymentAmount? PaymentAmount { get; set; }

    public SupplierFilterMode SupplierFilterMode { get; set; }

    // Comma-separated Supplier IDs.
    public string SupplierFilterIds { get; set; } = "";

    public bool NoStandingChargeOnly { get; set; }

    // p/kWh and p/day, ≥ 0 and ≤ the per-fuel maximum (FR-061).
    public decimal DefaultUnitRateUplift { get; set; }
    public decimal DefaultStandingChargeUplift { get; set; }

    public IReadOnlyList<int> Terms => ParseIds(TermsMonths);

    public IReadOnlyList<int> SupplierIds => ParseIds(SupplierFilterIds);

    public static string JoinIds(IEnumerable<int> ids) => string.Join(",", ids.Distinct().Order());

    private static List<int> ParseIds(string csv) =>
        csv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
           .Select(int.Parse)
           .ToList();
}

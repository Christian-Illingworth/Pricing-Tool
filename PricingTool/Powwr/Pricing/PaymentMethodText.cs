using PricingTool.Domain;

namespace PricingTool.Powwr.Pricing;

// POWWR's PaymentMethod string, such as "Monthly Direct Debit" or "Quarterly Fixed Direct Debit"
// (contracts/powwr-uk-pricing-mapping.md). The exact strings POWWR accepts are open question Q4, so
// they're built in this one place.
public static class PaymentMethodText
{
    public static string For(PaymentMethod method, PaymentFrequency? frequency, PaymentAmount? amount) => method switch
    {
        PaymentMethod.DirectDebit => string.Join(" ", new[] { frequency?.ToString(), amount?.ToString(), "Direct Debit" }.Where(p => p is not null)),
        PaymentMethod.Bacs => "BACS",
        PaymentMethod.CashCheque => "Cash/Cheque",
        _ => throw new ArgumentOutOfRangeException(nameof(method), method, null),
    };
}

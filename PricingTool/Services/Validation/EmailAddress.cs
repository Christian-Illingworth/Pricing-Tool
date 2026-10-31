using System.Net.Mail;

namespace PricingTool.Services.Validation;

public static class EmailAddress
{
    // A plain address that MailAddress accepts as it is (no display name), with a dotted domain
    // (FR-007, research R15).
    public static bool IsValid(string? input)
    {
        if (string.IsNullOrWhiteSpace(input)) return false;
        var trimmed = input.Trim();
        if (!MailAddress.TryCreate(trimmed, out var address)) return false;
        if (!string.Equals(address.Address, trimmed, StringComparison.OrdinalIgnoreCase)) return false;
        var host = address.Host;
        return host.Contains('.') && !host.StartsWith('.') && !host.EndsWith('.');
    }
}

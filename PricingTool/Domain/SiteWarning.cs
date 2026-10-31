namespace PricingTool.Domain;

// A warning saved with a site (FR-012, FR-045, FR-046). A domestic meter needs the agent's confirmation
// before pricing, which is recorded here.
public sealed record SiteWarning(SiteWarningCode Code, string Message, bool ConfirmedByAgent = false);

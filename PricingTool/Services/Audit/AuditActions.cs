namespace PricingTool.Services.Audit;

// Every AuditEntry.Action (data-model.md "Actions").
public static class AuditActions
{
    // Quote
    public const string QuoteCreated = "QuoteCreated";
    public const string QuoteInputsChanged = "QuoteInputsChanged";
    public const string SiteAdded = "SiteAdded";
    public const string SiteRemoved = "SiteRemoved";
    public const string SitesUploaded = "SitesUploaded";

    // Pricing
    public const string PricingRequested = "PricingRequested";
    public const string PricingReturned = "PricingReturned";
    public const string PricingFailed = "PricingFailed";
    public const string FailedSitesRetried = "FailedSitesRetried";

    // Uplift and selection
    public const string UpliftChanged = "UpliftChanged";
    public const string OptionsSelected = "OptionsSelected";
    public const string RecommendedChanged = "RecommendedChanged";
    public const string MicrobusinessDeclarationChanged = "MicrobusinessDeclarationChanged";

    // PDF and Outlook
    public const string PdfGenerated = "PdfGenerated";
    public const string PdfDownloaded = "PdfDownloaded";
    public const string OutlookDraftCreated = "OutlookDraftCreated";
    public const string OutlookDraftFailed = "OutlookDraftFailed";

    // Warnings
    public const string DomesticConfirmed = "DomesticConfirmed";

    // Admin
    public const string SettingChanged = "SettingChanged";
    public const string WordingApproved = "WordingApproved";
    public const string RateAdded = "RateAdded";
    public const string RateDeleted = "RateDeleted";
    public const string SupplierChanged = "SupplierChanged";
    public const string LimitChanged = "LimitChanged";
    public const string ReportExported = "ReportExported";

    // System
    public const string RetentionRun = "RetentionRun";
}

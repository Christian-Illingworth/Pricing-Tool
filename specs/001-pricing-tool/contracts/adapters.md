# Contract: adapter interfaces

These are the seams between the app and everything that differs between local, test and Azure
(Principle VII), or that talks to an outside system (Principle VI). Each interface has a mock or local
implementation that is the default, and the right one is chosen by configuration in `Program.cs`. The
signatures are the contract. Method bodies are implementation work, not part of the plan.

All methods take a `CancellationToken`. No interface exposes POWWR DTOs, HTTP types or secrets.

## POWWR UK Pricing: `IPowwrPricingClient`

Implementations: `MockPowwrPricingClient` (the default) and `PowwrPricingHttpClient`, which is used for
both `SwaggerHub` and `Live` mode. Mapping rules: [powwr-uk-pricing-mapping.md](powwr-uk-pricing-mapping.md).

```csharp
public interface IPowwrPricingClient
{
    Task<SitePriceResponse> GetElectricPricesAsync(ElectricPriceRequest request, CancellationToken ct);
    Task<SitePriceResponse> GetGasPricesAsync(GasPriceRequest request, CancellationToken ct);
    Task<RateStructureResponse> GetRateStructureAsync(RateStructureRequest request, CancellationToken ct);
    Task<IReadOnlyList<SupplierPriceChange>> GetLatestPriceChangesAsync(string? supplierName, CancellationToken ct);
    Task<bool> PingAsync(CancellationToken ct);
}
```

**Domain types**, records in `Powwr/Pricing/Models.cs`:

| Type | Fields |
|---|---|
| `ElectricPriceRequest` | `QuoteReference`, `AgentName`, `Postcode`, `QuoteType`, `CotDate?`, `OutOfContract`, `PaymentMethod` (a string built by `PaymentMethodText`), `BusinessType`, `CurrentSupplierPricingName?`, `StartDate`, `TermsMonths[]`, `DefaultUplift` (unit and standing charge), `SupplierPlans[]` (supplier pricing name + per-term uplift pair), and the electricity supply: top line, core, PC, MTC, LLFC, SSC, DUoS, MHHS, measurement class, meter type, smart, registers kWh, kVA, kVArh, forced structure, current contract end |
| `GasPriceRequest` | The same quote-level fields, plus MPRN, AQ kWh, smart, current contract end |
| `SitePriceResponse` | `Succeeded`, `ErrorCategory?`, `ErrorMessage?`, `PowwrReference?`, `FooterNotes?`, `Rates[]` (`ReturnedRate`), `NoQuotes[]` (`SupplierNoQuote`), `RequestJsonMasked`, `ResponseJson`, `SentUtc`, `ReceivedUtc` |
| `ReturnedRate` | Every `Rate` field: the strings exactly as returned and the `Raw*` decimals. `Term` is parsed to `int TermMonths` |
| `RateStructureResponse` | `Succeeded`, `Registers[]` (`Day`/`Night`/`Wend`), `RawPrompts[]`, `ErrorMessage?` |
| `SupplierPriceChange` | `Supplier`, `Utility`, `HalfHourly`, `DurationMonths`, `Product`, `PlanType`, `Version`, `LastPriceChange` (DateOnly) |

**Errors**: errors POWWR returns (`QuoteResult.Error`), timeouts, 429 and invalid data come back as
`SitePriceResponse.Succeeded = false` with a category. They are not thrown, so a multi-site run can
record per-site failures. Only programming errors throw.

## POWWR Meter Lookup: `IMeterLookupClient`

Implementations: `MockMeterLookupClient` (the default) and `MeterLookupHttpClient`, which is used for
`SwaggerHub`, `Sandbox` and `Live` mode. Mapping rules:
[powwr-meter-lookup-mapping.md](powwr-meter-lookup-mapping.md).

```csharp
public interface IMeterLookupClient
{
    Task<LookupResult<IReadOnlyList<LookupAddress>>> SearchAddressesAsync(NormalisedPostcode postcode, string userId, CancellationToken ct);
    Task<LookupResult<LookupMeters>> SearchMetersAsync(MeterSearch search, string userId, CancellationToken ct);
}
```

- `MeterSearch` holds exactly one of `MeterAddressId`, `Mpan` or `Mprn`.
- `LookupResult<T>` has `Status` (`Found`/`NotFound`/`Unavailable`/`Rejected`), `Value?` and
  `ProblemSummary?`, which is safe to show.
- `LookupMeters` has `Electric[]` (`LookupElectricMeter`) and `Gas[]` (`LookupGasMeter`). Their fields
  are already converted: integers for PC and MTC, a suggested smart flag, warnings, and a supplier match
  (`SupplierId?` plus the text as returned).

## Supplier matching: `ISupplierMatcher`

```csharp
public interface ISupplierMatcher
{
    SupplierMatch Match(string? powwrId, string? mpid, string? shortCode, string? name);
}
```

It matches against the admin supplier list (cached, and refreshed when a supplier changes): first
`PowwrId`, then `Mpid` or `ShortCode`, then `LookupName`. It returns `SupplierMatch(SupplierId?,
PricingName?, MatchedOn)`.

## Customer PDF: `ICustomerPdfRenderer`

Implementation: `PlaywrightCustomerPdfRenderer`. It renders `CustomerQuoteDocument.razor` with
`HtmlRenderer`, then converts it with Chromium (research R17).

```csharp
public interface ICustomerPdfRenderer
{
    Task<string> RenderHtmlAsync(CustomerQuoteDocumentModel model, CancellationToken ct); // unit-testable
    Task<byte[]> RenderPdfAsync(CustomerQuoteDocumentModel model, CancellationToken ct);
}
```

The input is **only** `CustomerQuoteDocumentModel` ([customer-pdf.md](customer-pdf.md)).

## File storage: `IFileStore`

Implementation: `LocalFileStore` (`Storage:Files:LocalRoot`). Azure Blob Storage comes later.

```csharp
public interface IFileStore
{
    Task SaveAsync(string key, Stream content, string contentType, CancellationToken ct);
    Task<Stream?> OpenReadAsync(string key, CancellationToken ct);
    Task DeleteAsync(string key, CancellationToken ct);
}
```

Keys are relative, use forward slashes, are built by the app, and can't contain `..`.

## Reporting export: `IReportingExportStore`

Implementation: `LocalFolderReportingExportStore` (`Storage:ReportingExport:LocalRoot`). Azure Blob
Storage comes later.

```csharp
public interface IReportingExportStore
{
    Task WriteAsync(string path, ReadOnlyMemory<byte> json, CancellationToken ct); // idempotent overwrite
    Task DeleteAsync(string path, CancellationToken ct);                          // retention
}
```

`path` is `quotes/yyyy/MM/dd/{HHmmssfff}-{eventType}-{recordId}.json`, built by
`ReportingExportPath.For(record)`.

## Outlook draft: `IOutlookDraftService`

Implementations: `GraphOutlookDraftService` (the default in Entra mode) and `EmlOutlookDraftService`
(the fallback, and always used with the Development sign-in). See [outlook-draft.md](outlook-draft.md).

```csharp
public interface IOutlookDraftService
{
    OutlookMode Mode { get; }
    Task<DraftResult> CreateDraftAsync(string fileName, byte[] pdf, CancellationToken ct);
}
```

`DraftResult` has `Succeeded`, `GraphMessageId?`, `WebLink?`, `EmlFile?` (bytes and file name) and
`UserMessage`.

## Staff directory: `IStaffDirectory`

Implementations: `GraphStaffDirectory` (Entra mode) and `DevStaffDirectory`.

```csharp
public interface IStaffDirectory
{
    Task<string?> GetPhoneAsync(StaffMember staff, CancellationToken ct); // cached 24h on StaffMember
}
```

## Clock

The built-in `TimeProvider` is injected wherever "now" matters. `UkCalendar.Today(TimeProvider)` and
`UkCalendar.DateOf(DateTime utc)` use `Europe/London` (research R14).

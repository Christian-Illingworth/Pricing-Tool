# Contract: configuration and secrets

All settings are read through `IConfiguration` (Principle I and VII). Each section binds to an options
class with `ValidateOnStart()`, so a wrong setting stops the app at startup with a message that names
the **key**, never a secret value. These are **operational** settings. Business settings (uplifts,
limits, rates, wording) are admin settings in the database (data-model.md).

## Where values live

| Where | What goes there |
|---|---|
| `PricingTool/appsettings.json` (committed) | Safe defaults: mock modes, URLs, local folders, company details |
| `PricingTool/appsettings.Development.json` (committed) | Local SQL Server Express connection (Windows sign-in, no password), `Authentication:Mode = Development` |
| .NET user secrets (`dotnet user-secrets`, per developer, never committed) | `AzureAd:ClientSecret`, `Powwr:Pricing:LicenceCode`, `Powwr:MeterLookup:ClientSecret`, and the Entra IDs once IT provides them |
| Later, Azure | App Service settings for non-secrets, and Key Vault (managed identity) for secrets. That is added by the deployment feature, with no code change |

## Keys

| Key | Default | Secret? | Notes |
|---|---|---|---|
| `ConnectionStrings:PricingDb` | `Server=.\SQLEXPRESS;Database=CuPricingTool;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=False` (Development file) | No locally | A password-based connection string later is a secret |
| `Authentication:Mode` | `Entra`; `Development` in the Development file | No | `Development` is refused outside the Development environment (research R2) |
| `Authentication:SessionHours` | `8` | No | Absolute lifetime of the sign-in cookie, with no sliding renewal (FR-094) |
| `AzureAd:Instance` | `https://login.microsoftonline.com/` | No | |
| `AzureAd:TenantId` | (from IT) | No | Single tenant |
| `AzureAd:ClientId` | (from IT) | No | |
| `AzureAd:ClientSecret` | — | **Yes** | User secrets locally, Key Vault later |
| `AzureAd:CallbackPath` | `/signin-oidc` | No | Local redirect URI: `https://localhost:8101/signin-oidc` |
| `AzureAd:AllowedGroupId` | (from IT) | No | Object ID of "Pricing Tool Users". Required in Entra mode |
| `Outlook:Mode` | `Graph` | No | `Graph` or `Eml`. Must be `Eml` when `Authentication:Mode = Development` |
| `Powwr:Pricing:Mode` | `Mock` | No | `Mock`, `SwaggerHub` or `Live` |
| `Powwr:Pricing:BaseUrl` | `https://udcoreapi.co.uk/Service.svc/web` | No | Ignored in Mock mode. SwaggerHub mode uses the virtserver URL automatically unless this is set |
| `Powwr:Pricing:LicenceCode` | — | **Yes** | Required only in `Live` mode |
| `Powwr:Pricing:MaxConcurrentRequests` | `4` | No | Research R6 |
| `Powwr:Pricing:TimeoutSeconds` | `45` | No | |
| `Powwr:Pricing:BusinessTypes` | Our labels | No | Map from our business type to POWWR's `BusinessType` string (Q4) |
| `Powwr:MeterLookup:Mode` | `Mock` | No | `Mock`, `SwaggerHub`, `Sandbox` or `Live` |
| `Powwr:MeterLookup:BaseUrl` | (by mode) | No | |
| `Powwr:MeterLookup:TokenUrl` | `https://accounts.sbx.powwr.com/connect/token` | No | |
| `Powwr:MeterLookup:ClientId` | — | No | From POWWR (Q2) |
| `Powwr:MeterLookup:ClientSecret` | — | **Yes** | |
| `Powwr:MeterLookup:OrganisationPowwrId` | — | No | `^[A-Z0-9]{1,8}-\d{3}-[SB]$` |
| `Powwr:Mock:ChangedSuppliers` | `[]` | No | Mock only: suppliers whose price-change check reports a change (FR-052 testing) |
| `Storage:Files:LocalRoot` | `App_Data/files` | No | Relative to the content root. Git-ignored |
| `Storage:ReportingExport:LocalRoot` | `App_Data/reporting` | No | Git-ignored |
| `Reporting:Outbox:PollSeconds` | `30` | No | |
| `Retention:RunAtUkTime` | `02:00` | No | The period itself is the admin setting `Retention.Years` |
| `Pdf:MaxConcurrentRenders` | `2` | No | |
| `Uploads:MaxRows` | `200` | No | |
| `Uploads:MaxBytes` | `2097152` | No | |
| `Company:Name` | `Consultiv Utilities` | No | PDF header |
| `Company:AddressLines` | (to confirm) | No | |
| `Company:Phone` | (to confirm) | No | Also the fallback for an agent with no phone |
| `Company:Email` | (to confirm) | No | |
| `Company:Website` | (to confirm) | No | |
| `Company:RegistrationNumber` | (to confirm) | No | PDF footer (FR-047) |
| `Logging:LogLevel:*` | Standard | No | Application Insights is added later through configuration (Principle VII) |

## User secrets (local)

Run these from the repository root. The project has a `UserSecretsId`.

```powershell
dotnet user-secrets set "Powwr:Pricing:LicenceCode" "<from POWWR>" --project PricingTool
dotnet user-secrets set "Powwr:MeterLookup:ClientSecret" "<from POWWR>" --project PricingTool
dotnet user-secrets set "AzureAd:ClientSecret" "<from IT>" --project PricingTool
dotnet user-secrets list --project PricingTool
```

None of these is needed to run with the mocks and the Development sign-in.

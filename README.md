# CU Pricing Tool

An internal web app for Consultiv Utilities' sales agents. An agent enters a business customer's
electricity or gas supply details, gets live prices from POWWR, compares suppliers and terms with our
uplift applied, and produces a branded PDF quote.

The specification, plan and task list are in [`specs/001-pricing-tool/`](specs/001-pricing-tool/).
The full local walkthrough, with the mock data to try, is in
[`specs/001-pricing-tool/quickstart.md`](specs/001-pricing-tool/quickstart.md).

## Prerequisites

- The **.NET 10 SDK**. Check with `dotnet --version`, which should show 10.0.x.
- **SQL Server Express** on this machine as `.\SQLEXPRESS`, with Windows sign-in.

Nothing else. PowerShell 7 (for Playwright's install script) and the EF Core tools come from the
repository's local .NET tool manifest (`.config/dotnet-tools.json`). There is no Python, Node.js or npm.

## One-time setup

Run these from the repository root in PowerShell:

```powershell
dotnet tool restore                          # dotnet-ef and pwsh (PowerShell 7) as local .NET tools
dotnet dev-certs https --trust               # local HTTPS for https://localhost:8101
dotnet build
dotnet pwsh PricingTool/bin/Debug/net10.0/playwright.ps1 install chromium   # PDF rendering
dotnet ef database update --project PricingTool                              # creates CuPricingTool on .\SQLEXPRESS
```

## Run

```powershell
dotnet run --project PricingTool
```

Open `https://localhost:8101`. In the Development environment the app uses the made-up staff test
sign-in, the mock POWWR adapters and the `.eml` Outlook fallback, so no secrets are needed.

## Test

```powershell
dotnet test
```

The tests need no network, POWWR, browser or database, and use made-up data only.

## Configuration and secrets

Operational settings are in `PricingTool/appsettings.json` and `appsettings.Development.json`. Every
key is listed in [`contracts/configuration.md`](specs/001-pricing-tool/contracts/configuration.md).
Secrets are **never** in these files or the repository. Locally they are .NET user secrets:

```powershell
dotnet user-secrets set "Powwr:Pricing:LicenceCode" "<from POWWR>" --project PricingTool
dotnet user-secrets set "Powwr:MeterLookup:ClientSecret" "<from POWWR>" --project PricingTool
dotnet user-secrets set "AzureAd:ClientSecret" "<from IT>" --project PricingTool
```

None of these is needed to run with the mocks and the Development sign-in.

## Still to write

These sections are filled in as the features that need them are built:

- Run modes (mock, SwaggerHub and live POWWR; Entra sign-in).
- The multi-site spreadsheet template.
- The reporting record format and export folder.

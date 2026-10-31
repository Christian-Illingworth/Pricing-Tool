using Microsoft.EntityFrameworkCore;
using PricingTool.Auth;
using PricingTool.Data;
using PricingTool.Infrastructure;
using PricingTool.Options;
using PricingTool.Powwr;
using PricingTool.Services.Audit;
using PricingTool.Services.Pricing;
using PricingTool.Services.Reporting;
using PricingTool.Services.Settings;

var builder = WebApplication.CreateBuilder(args);

// Every operational setting is bound and checked at startup (contracts/configuration.md).
// Secrets are read only through IConfiguration: user secrets locally, Key Vault later.
builder.Services.AddPricingToolOptions(builder.Configuration);

builder.Services.AddRazorPages();
builder.Services.AddSingleton(TimeProvider.System);

// app.js sends the antiforgery token as a header on fetch; forms send it as a field as usual.
builder.Services.AddAntiforgery(options => options.HeaderName = "RequestVerificationToken");

// Entra sign-in, or the Development test sign-in (Development environment only), plus the policies
// that every page and handler is checked against (research R2).
builder.Services.AddPricingToolAuthentication(builder.Configuration, builder.Environment);

// SQL Server Express locally, Azure SQL later: only the connection string differs (Principle VII).
// It's read when a context is first needed, so startup never touches the database.
builder.Services.AddSingleton<AppendOnlyGuardInterceptor>();
builder.Services.AddDbContext<PricingDbContext>((services, options) => options
    .UseSqlServer(services.GetRequiredService<IConfiguration>().GetConnectionString("PricingDb")
        ?? throw new InvalidOperationException("ConnectionStrings:PricingDb is not set."))
    .AddInterceptors(services.GetRequiredService<AppendOnlyGuardInterceptor>()));

// Settings, audit and supplier matching, used by every story.
builder.Services.AddScoped<SettingsService>();
builder.Services.AddScoped<AuditWriter>();
builder.Services.AddSingleton<ISupplierMatcher, SupplierMatcher>();

// The single pricing engine: stateless, so one instance serves every request (Principle VI).
builder.Services.AddSingleton<PricingEngine>();

// Reporting: events are recorded in the action's own transaction; exports go to a local folder now,
// Blob Storage later (the outbox worker arrives with US9).
builder.Services.AddScoped<ReportingRecorder>();
builder.Services.AddSingleton<IReportingExportStore, LocalFolderReportingExportStore>();

// Both POWWR adapters, mock unless configured otherwise (Powwr:Pricing:Mode, Powwr:MeterLookup:Mode).
builder.Services.AddPowwr(builder.Configuration);

var app = builder.Build();

// Nothing here touches the database: migrations are applied with `dotnet ef database update`.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseSecurityHeaders();
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.MapRazorPages();

app.Run();

// Lets PricingTool.Tests start the app with WebApplicationFactory<Program>.
public partial class Program;

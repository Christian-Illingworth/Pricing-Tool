using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace PricingTool.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateSequence<int>(
                name: "QuoteNumbers");

            migrationBuilder.CreateTable(
                name: "AuditEntries",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OccurredUtc = table.Column<DateTime>(type: "datetime2(3)", nullable: false),
                    ActorId = table.Column<int>(type: "int", nullable: true),
                    ActorName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Action = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    EntityType = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    EntityId = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    QuoteId = table.Column<int>(type: "int", nullable: true),
                    OldValueJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NewValueJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Detail = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditEntries", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "EffectiveRates",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Kind = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Value = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    EffectiveFrom = table.Column<DateOnly>(type: "date", nullable: false),
                    CreatedById = table.Column<int>(type: "int", nullable: true),
                    CreatedUtc = table.Column<DateTime>(type: "datetime2(3)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EffectiveRates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ReportingRecords",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SchemaVersion = table.Column<int>(type: "int", nullable: false),
                    EventType = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    OccurredUtc = table.Column<DateTime>(type: "datetime2(3)", nullable: false),
                    QuoteId = table.Column<int>(type: "int", nullable: false),
                    QuoteReference = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    AgentId = table.Column<int>(type: "int", nullable: false),
                    AgentName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    AgentEmail = table.Column<string>(type: "nvarchar(320)", maxLength: 320, nullable: false),
                    Fuel = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    QuoteType = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Regions = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    RecommendedCommissionTotal = table.Column<decimal>(type: "decimal(28,10)", precision: 28, scale: 10, nullable: true),
                    PdfGenerated = table.Column<bool>(type: "bit", nullable: false),
                    OpenedInOutlook = table.Column<bool>(type: "bit", nullable: false),
                    Payload = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReportingRecords", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SettingVersions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Key = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    Version = table.Column<int>(type: "int", nullable: false),
                    ValueJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsPlaceholder = table.Column<bool>(type: "bit", nullable: false),
                    ComplianceApprover = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    CreatedById = table.Column<int>(type: "int", nullable: true),
                    CreatedUtc = table.Column<DateTime>(type: "datetime2(3)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SettingVersions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "StaffMembers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EntraObjectId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    DisplayName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(320)", maxLength: 320, nullable: false),
                    Phone = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    PhoneCheckedUtc = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    CustomerViewOn = table.Column<bool>(type: "bit", nullable: false),
                    LastRolesSeen = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    FirstSeenUtc = table.Column<DateTime>(type: "datetime2(3)", nullable: false),
                    LastSeenUtc = table.Column<DateTime>(type: "datetime2(3)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StaffMembers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Suppliers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PricingName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    DisplayName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    SuppliesElectricity = table.Column<bool>(type: "bit", nullable: false),
                    SuppliesGas = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    UpfrontPercent = table.Column<decimal>(type: "decimal(9,4)", precision: 9, scale: 4, nullable: false),
                    MaxUnitRateUpliftElectricity = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: true),
                    MaxUnitRateUpliftGas = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: true),
                    MaxStandingChargeUpliftElectricity = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: true),
                    MaxStandingChargeUpliftGas = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Suppliers", x => x.Id);
                    table.CheckConstraint("CK_Suppliers_UpfrontPercent", "[UpfrontPercent] >= 0 AND [UpfrontPercent] <= 100");
                });

            migrationBuilder.CreateTable(
                name: "ReportingOutboxEntries",
                columns: table => new
                {
                    ReportingRecordId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Attempts = table.Column<int>(type: "int", nullable: false),
                    NextAttemptUtc = table.Column<DateTime>(type: "datetime2(3)", nullable: false),
                    ExportedUtc = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    ExportPath = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    LastError = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReportingOutboxEntries", x => x.ReportingRecordId);
                    table.ForeignKey(
                        name: "FK_ReportingOutboxEntries_ReportingRecords_ReportingRecordId",
                        column: x => x.ReportingRecordId,
                        principalTable: "ReportingRecords",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ReportingRecordSuppliers",
                columns: table => new
                {
                    ReportingRecordId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SupplierName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReportingRecordSuppliers", x => new { x.ReportingRecordId, x.SupplierName });
                    table.ForeignKey(
                        name: "FK_ReportingRecordSuppliers_ReportingRecords_ReportingRecordId",
                        column: x => x.ReportingRecordId,
                        principalTable: "ReportingRecords",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Quotes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Reference = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    OwnerId = table.Column<int>(type: "int", nullable: false),
                    Fuel = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    QuoteType = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    CotDate = table.Column<DateOnly>(type: "date", nullable: true),
                    OutOfContract = table.Column<bool>(type: "bit", nullable: false),
                    Customer_BusinessName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Customer_BusinessType = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Customer_CompanyNumber = table.Column<string>(type: "nvarchar(8)", maxLength: 8, nullable: true),
                    Customer_ContactName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Customer_ContactEmail = table.Column<string>(type: "nvarchar(320)", maxLength: 320, nullable: false),
                    Customer_ContactPhone = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Customer_DeclaredFewerThan10Employees = table.Column<bool>(type: "bit", nullable: false),
                    Customer_DeclaredTurnoverOrBalanceUnder2m = table.Column<bool>(type: "bit", nullable: false),
                    Contract_StartDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Contract_TermsMonths = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Contract_PaymentMethod = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Contract_PaymentFrequency = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    Contract_PaymentAmount = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    Contract_SupplierFilterMode = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Contract_SupplierFilterIds = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: false),
                    Contract_NoStandingChargeOnly = table.Column<bool>(type: "bit", nullable: false),
                    Contract_DefaultUnitRateUplift = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    Contract_DefaultStandingChargeUplift = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    VatCclTreatment = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    QualifyingPercent = table.Column<decimal>(type: "decimal(9,4)", precision: 9, scale: 4, nullable: true),
                    IncludeVatCcl = table.Column<bool>(type: "bit", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    CreatedUtc = table.Column<DateTime>(type: "datetime2(3)", nullable: false),
                    LastActivityUtc = table.Column<DateTime>(type: "datetime2(3)", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Quotes", x => x.Id);
                    table.CheckConstraint("CK_Quotes_QualifyingPercent", "[QualifyingPercent] IS NULL OR ([QualifyingPercent] >= 0 AND [QualifyingPercent] <= 100)");
                    table.ForeignKey(
                        name: "FK_Quotes_StaffMembers_OwnerId",
                        column: x => x.OwnerId,
                        principalTable: "StaffMembers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SupplierAliases",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SupplierId = table.Column<int>(type: "int", nullable: false),
                    PowwrId = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Mpid = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    LookupName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SupplierAliases", x => x.Id);
                    table.CheckConstraint("CK_SupplierAliases_AtLeastOne", "[PowwrId] IS NOT NULL OR [Mpid] IS NOT NULL OR [LookupName] IS NOT NULL");
                    table.ForeignKey(
                        name: "FK_SupplierAliases_Suppliers_SupplierId",
                        column: x => x.SupplierId,
                        principalTable: "Suppliers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PricingRuns",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    QuoteId = table.Column<int>(type: "int", nullable: false),
                    RunNumber = table.Column<int>(type: "int", nullable: false),
                    RequestedById = table.Column<int>(type: "int", nullable: false),
                    RequestedUtc = table.Column<DateTime>(type: "datetime2(3)", nullable: false),
                    FetchedUtc = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    InputsSnapshot = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SettingsSnapshot = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CommissionBasis = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    IsMicrobusiness = table.Column<bool>(type: "bit", nullable: false),
                    MicrobusinessSource = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    CombinedAnnualKwh = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PricingRuns", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PricingRuns_Quotes_QuoteId",
                        column: x => x.QuoteId,
                        principalTable: "Quotes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PricingRuns_StaffMembers_RequestedById",
                        column: x => x.RequestedById,
                        principalTable: "StaffMembers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "QuoteUpliftOverrides",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    QuoteId = table.Column<int>(type: "int", nullable: false),
                    SupplierId = table.Column<int>(type: "int", nullable: false),
                    TermMonths = table.Column<int>(type: "int", nullable: false),
                    UnitRateUplift = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    StandingChargeUplift = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QuoteUpliftOverrides", x => x.Id);
                    table.ForeignKey(
                        name: "FK_QuoteUpliftOverrides_Quotes_QuoteId",
                        column: x => x.QuoteId,
                        principalTable: "Quotes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_QuoteUpliftOverrides_Suppliers_SupplierId",
                        column: x => x.SupplierId,
                        principalTable: "Suppliers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Sites",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    QuoteId = table.Column<int>(type: "int", nullable: false),
                    Ordinal = table.Column<int>(type: "int", nullable: false),
                    AddressLine1 = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    AddressLine2 = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    AddressLine3 = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    AddressLine4 = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Town = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Postcode = table.Column<string>(type: "nvarchar(8)", maxLength: 8, nullable: false),
                    StartDate = table.Column<DateOnly>(type: "date", nullable: true),
                    LookupMeterAddressId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CurrentSupplierId = table.Column<int>(type: "int", nullable: true),
                    CurrentSupplierText = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    CurrentDayRate = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: true),
                    CurrentNightRate = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: true),
                    CurrentWendRate = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: true),
                    CurrentStandingCharge = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: true),
                    CurrentContractEndDate = table.Column<DateOnly>(type: "date", nullable: true),
                    ConsumptionDayKwh = table.Column<int>(type: "int", nullable: true),
                    ConsumptionNightKwh = table.Column<int>(type: "int", nullable: true),
                    ConsumptionWendKwh = table.Column<int>(type: "int", nullable: true),
                    ConsumptionEntryMode = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    ConsumptionTotalKwh = table.Column<int>(type: "int", nullable: true),
                    SplitDayPct = table.Column<decimal>(type: "decimal(9,4)", precision: 9, scale: 4, nullable: true),
                    SplitNightPct = table.Column<decimal>(type: "decimal(9,4)", precision: 9, scale: 4, nullable: true),
                    SplitWendPct = table.Column<decimal>(type: "decimal(9,4)", precision: 9, scale: 4, nullable: true),
                    GasAnnualQuantityKwh = table.Column<int>(type: "int", nullable: true),
                    Meter_MpanTopLine = table.Column<string>(type: "nvarchar(9)", maxLength: 9, nullable: true),
                    Meter_MpanCore = table.Column<string>(type: "nvarchar(13)", maxLength: 13, nullable: true),
                    Meter_DistributorId = table.Column<string>(type: "nvarchar(2)", maxLength: 2, nullable: true),
                    Meter_RegionName = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: true),
                    Meter_GspId = table.Column<string>(type: "nvarchar(4)", maxLength: 4, nullable: true),
                    Meter_ProfileClass = table.Column<int>(type: "int", nullable: true),
                    Meter_MeterTimeswitchClass = table.Column<int>(type: "int", nullable: true),
                    Meter_LineLossFactorClass = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: true),
                    Meter_StandardSettlementConfig = table.Column<string>(type: "nvarchar(4)", maxLength: 4, nullable: true),
                    Meter_DuosTariffId = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: true),
                    Meter_MhhsMigrated = table.Column<bool>(type: "bit", nullable: true),
                    Meter_MeasurementClass = table.Column<string>(type: "nvarchar(1)", maxLength: 1, nullable: true),
                    Meter_SmetsVersion = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    Meter_IsHalfHourly = table.Column<bool>(type: "bit", nullable: false),
                    Meter_KvaCapacity = table.Column<int>(type: "int", nullable: true),
                    Meter_KvarhConsumption = table.Column<int>(type: "int", nullable: true),
                    Meter_RateRegisters = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Meter_ForcedRateStructure = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    Meter_MeterType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Meter_SmartMeter = table.Column<bool>(type: "bit", nullable: false),
                    Meter_Mprn = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    Meter_Ldz = table.Column<string>(type: "nvarchar(4)", maxLength: 4, nullable: true),
                    Meter_ExitZone = table.Column<string>(type: "nvarchar(4)", maxLength: 4, nullable: true),
                    Meter_IsIgt = table.Column<bool>(type: "bit", nullable: false),
                    Meter_MeterCapacity = table.Column<int>(type: "int", nullable: true),
                    Meter_DailyMeteredQuantity = table.Column<int>(type: "int", nullable: true),
                    Meter_AmrIndicator = table.Column<string>(type: "nvarchar(2)", maxLength: 2, nullable: true),
                    Meter_IsLargeGasSite = table.Column<bool>(type: "bit", nullable: false),
                    Warnings = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FieldSources = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    LookupSnapshot = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Sites", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Sites_Quotes_QuoteId",
                        column: x => x.QuoteId,
                        principalTable: "Quotes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Sites_Suppliers_CurrentSupplierId",
                        column: x => x.CurrentSupplierId,
                        principalTable: "Suppliers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PriceOptions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PricingRunId = table.Column<int>(type: "int", nullable: false),
                    SupplierName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    SupplierId = table.Column<int>(type: "int", nullable: true),
                    PlanType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    TermMonths = table.Column<int>(type: "int", nullable: false),
                    RequestedUnitRateUplift = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    RequestedStandingChargeUplift = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    UnitRateUplift = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    StandingChargeUplift = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    IsCappedAtSupplierMax = table.Column<bool>(type: "bit", nullable: false),
                    SitesCovered = table.Column<int>(type: "int", nullable: false),
                    SitesTotal = table.Column<int>(type: "int", nullable: false),
                    IsPartial = table.Column<bool>(type: "bit", nullable: false),
                    AnyOutsidePricingWindow = table.Column<bool>(type: "bit", nullable: false),
                    HasNoStandingCharge = table.Column<bool>(type: "bit", nullable: false),
                    IsGreen = table.Column<bool>(type: "bit", nullable: true),
                    TotalAnnualCostExVatCcl = table.Column<decimal>(type: "decimal(28,10)", precision: 28, scale: 10, nullable: false),
                    TotalCcl = table.Column<decimal>(type: "decimal(28,10)", precision: 28, scale: 10, nullable: false),
                    TotalVat = table.Column<decimal>(type: "decimal(28,10)", precision: 28, scale: 10, nullable: false),
                    TotalAnnualCostIncVatCcl = table.Column<decimal>(type: "decimal(28,10)", precision: 28, scale: 10, nullable: false),
                    TotalSavingExVatCcl = table.Column<decimal>(type: "decimal(28,10)", precision: 28, scale: 10, nullable: true),
                    TotalSavingIncVatCcl = table.Column<decimal>(type: "decimal(28,10)", precision: 28, scale: 10, nullable: true),
                    CommissionContractTotal = table.Column<decimal>(type: "decimal(28,10)", precision: 28, scale: 10, nullable: false),
                    CommissionUpfront = table.Column<decimal>(type: "decimal(28,10)", precision: 28, scale: 10, nullable: false),
                    CommissionMonthly = table.Column<decimal>(type: "decimal(28,10)", precision: 28, scale: 10, nullable: false),
                    IsSelected = table.Column<bool>(type: "bit", nullable: false),
                    IsRecommended = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PriceOptions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PriceOptions_PricingRuns_PricingRunId",
                        column: x => x.PricingRunId,
                        principalTable: "PricingRuns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PricingRunSites",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PricingRunId = table.Column<int>(type: "int", nullable: false),
                    SiteId = table.Column<int>(type: "int", nullable: false),
                    Attempt = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    RequestJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ResponseJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PowwrReference = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    FooterNotes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ErrorCategory = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    ErrorMessage = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    SentUtc = table.Column<DateTime>(type: "datetime2(3)", nullable: false),
                    ReceivedUtc = table.Column<DateTime>(type: "datetime2(3)", nullable: false),
                    StartDateUsed = table.Column<DateOnly>(type: "date", nullable: false),
                    Treatment = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    AnnualKwh = table.Column<int>(type: "int", nullable: false),
                    VatStandardRate = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    VatReducedRate = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    CclRatePencePerKwh = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    QualifyingFraction = table.Column<decimal>(type: "decimal(9,6)", precision: 9, scale: 6, nullable: false),
                    CurrentAnnualCostExVatCcl = table.Column<decimal>(type: "decimal(28,10)", precision: 28, scale: 10, nullable: true),
                    CurrentAnnualCostIncVatCcl = table.Column<decimal>(type: "decimal(28,10)", precision: 28, scale: 10, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PricingRunSites", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PricingRunSites_PricingRuns_PricingRunId",
                        column: x => x.PricingRunId,
                        principalTable: "PricingRuns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PricingRunSites_Sites_SiteId",
                        column: x => x.SiteId,
                        principalTable: "Sites",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "NoQuoteResults",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PricingRunSiteId = table.Column<int>(type: "int", nullable: false),
                    SupplierName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    TermMonths = table.Column<int>(type: "int", nullable: true),
                    Reason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NoQuoteResults", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NoQuoteResults_PricingRunSites_PricingRunSiteId",
                        column: x => x.PricingRunSiteId,
                        principalTable: "PricingRunSites",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PriceOptionSites",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PriceOptionId = table.Column<int>(type: "int", nullable: false),
                    PricingRunSiteId = table.Column<int>(type: "int", nullable: false),
                    PowwrRateId = table.Column<int>(type: "int", nullable: true),
                    StandingCharge = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    DayUnitrate = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    NightUnitrate = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    WendUnitrate = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    Stod1 = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    Stod2 = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    Stod3 = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    Stod4 = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    Stod5 = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    Stod6 = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    CapacityCharge = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    ExcessCapacityCharge = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    ReactiveRate = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    FixedFee = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    FixedFeeUplift = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    FixedFeeSCUplift = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    DirectDebitDiscount = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    ChequeSurcharge = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    TransportationCharge = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    Fits = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    Uplift = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    StandingChargeUplift = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    AnnualPrice = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    Commission = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    TariffCode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Period = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ContractEndDate = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    RawAnnualPrice = table.Column<decimal>(type: "decimal(28,10)", precision: 28, scale: 10, nullable: true),
                    RawBaseAnnualPrice = table.Column<decimal>(type: "decimal(28,10)", precision: 28, scale: 10, nullable: true),
                    RawAnnualPriceInclusive = table.Column<decimal>(type: "decimal(28,10)", precision: 28, scale: 10, nullable: true),
                    RawCommission = table.Column<decimal>(type: "decimal(28,10)", precision: 28, scale: 10, nullable: true),
                    FlatDirectDebitDiscount = table.Column<decimal>(type: "decimal(28,10)", precision: 28, scale: 10, nullable: true),
                    OutsidePricingWindow = table.Column<bool>(type: "bit", nullable: false),
                    PricebookVersion = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    QuoteRateReference = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ExtraInfo = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FooterMessage = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BaseDayRate = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: true),
                    BaseNightRate = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: true),
                    BaseWendRate = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: true),
                    BaseStandingCharge = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: true),
                    AnnualCostExVatCcl = table.Column<decimal>(type: "decimal(28,10)", precision: 28, scale: 10, nullable: false),
                    Ccl = table.Column<decimal>(type: "decimal(28,10)", precision: 28, scale: 10, nullable: false),
                    Vat = table.Column<decimal>(type: "decimal(28,10)", precision: 28, scale: 10, nullable: false),
                    AnnualCostIncVatCcl = table.Column<decimal>(type: "decimal(28,10)", precision: 28, scale: 10, nullable: false),
                    SavingExVatCcl = table.Column<decimal>(type: "decimal(28,10)", precision: 28, scale: 10, nullable: true),
                    SavingIncVatCcl = table.Column<decimal>(type: "decimal(28,10)", precision: 28, scale: 10, nullable: true),
                    CommissionContractTotal = table.Column<decimal>(type: "decimal(28,10)", precision: 28, scale: 10, nullable: false),
                    CommissionUpfront = table.Column<decimal>(type: "decimal(28,10)", precision: 28, scale: 10, nullable: false),
                    CommissionMonthly = table.Column<decimal>(type: "decimal(28,10)", precision: 28, scale: 10, nullable: false),
                    CrossCheckAnnualCost = table.Column<decimal>(type: "decimal(28,10)", precision: 28, scale: 10, nullable: true),
                    CrossCheckDifference = table.Column<decimal>(type: "decimal(28,10)", precision: 28, scale: 10, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PriceOptionSites", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PriceOptionSites_PriceOptions_PriceOptionId",
                        column: x => x.PriceOptionId,
                        principalTable: "PriceOptions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PriceOptionSites_PricingRunSites_PricingRunSiteId",
                        column: x => x.PricingRunSiteId,
                        principalTable: "PricingRunSites",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "EffectiveRates",
                columns: new[] { "Id", "CreatedById", "CreatedUtc", "EffectiveFrom", "Kind", "Value" },
                values: new object[,]
                {
                    { 1, null, new DateTime(2026, 10, 30, 0, 0, 0, 0, DateTimeKind.Utc), new DateOnly(2020, 1, 1), "VatStandard", 20m },
                    { 2, null, new DateTime(2026, 10, 30, 0, 0, 0, 0, DateTimeKind.Utc), new DateOnly(2020, 1, 1), "VatReduced", 5m },
                    { 3, null, new DateTime(2026, 10, 30, 0, 0, 0, 0, DateTimeKind.Utc), new DateOnly(2025, 4, 1), "CclElectricity", 0.801m },
                    { 4, null, new DateTime(2026, 10, 30, 0, 0, 0, 0, DateTimeKind.Utc), new DateOnly(2025, 4, 1), "CclGas", 0.801m },
                    { 5, null, new DateTime(2026, 10, 30, 0, 0, 0, 0, DateTimeKind.Utc), new DateOnly(2020, 1, 1), "LowUseElectricityKwhPerDay", 33m },
                    { 6, null, new DateTime(2026, 10, 30, 0, 0, 0, 0, DateTimeKind.Utc), new DateOnly(2020, 1, 1), "LowUseGasKwhPerDay", 145m }
                });

            migrationBuilder.InsertData(
                table: "SettingVersions",
                columns: new[] { "Id", "ComplianceApprover", "CreatedById", "CreatedUtc", "IsPlaceholder", "Key", "ValueJson", "Version" },
                values: new object[,]
                {
                    { 1, null, null, new DateTime(2026, 10, 30, 0, 0, 0, 0, DateTimeKind.Utc), false, "Uplift.Default.Electricity.UnitRate", "1.0", 1 },
                    { 2, null, null, new DateTime(2026, 10, 30, 0, 0, 0, 0, DateTimeKind.Utc), false, "Uplift.Default.Electricity.StandingCharge", "5", 1 },
                    { 3, null, null, new DateTime(2026, 10, 30, 0, 0, 0, 0, DateTimeKind.Utc), false, "Uplift.Default.Gas.UnitRate", "0.5", 1 },
                    { 4, null, null, new DateTime(2026, 10, 30, 0, 0, 0, 0, DateTimeKind.Utc), false, "Uplift.Default.Gas.StandingCharge", "5", 1 },
                    { 5, null, null, new DateTime(2026, 10, 30, 0, 0, 0, 0, DateTimeKind.Utc), false, "UpliftLimit.Electricity.UnitRate", "3.0", 1 },
                    { 6, null, null, new DateTime(2026, 10, 30, 0, 0, 0, 0, DateTimeKind.Utc), false, "UpliftLimit.Electricity.StandingCharge", "25", 1 },
                    { 7, null, null, new DateTime(2026, 10, 30, 0, 0, 0, 0, DateTimeKind.Utc), false, "UpliftLimit.Gas.UnitRate", "2.0", 1 },
                    { 8, null, null, new DateTime(2026, 10, 30, 0, 0, 0, 0, DateTimeKind.Utc), false, "UpliftLimit.Gas.StandingCharge", "25", 1 },
                    { 9, null, null, new DateTime(2026, 10, 30, 0, 0, 0, 0, DateTimeKind.Utc), false, "Commission.Basis", "\"PerYear\"", 1 },
                    { 10, null, null, new DateTime(2026, 10, 30, 0, 0, 0, 0, DateTimeKind.Utc), false, "Payment.Default", "{\"Method\":0,\"Frequency\":0,\"Amount\":null}", 1 },
                    { 11, null, null, new DateTime(2026, 10, 30, 0, 0, 0, 0, DateTimeKind.Utc), false, "Microbusiness.Threshold.Electricity.Kwh", "100000", 1 },
                    { 12, null, null, new DateTime(2026, 10, 30, 0, 0, 0, 0, DateTimeKind.Utc), false, "Microbusiness.Threshold.Gas.Kwh", "293000", 1 },
                    { 13, null, null, new DateTime(2026, 10, 30, 0, 0, 0, 0, DateTimeKind.Utc), false, "LargeGas.Limit.AnnualQuantityKwh", "null", 1 },
                    { 14, null, null, new DateTime(2026, 10, 30, 0, 0, 0, 0, DateTimeKind.Utc), false, "LargeGas.Limit.MeterCapacity", "null", 1 },
                    { 15, null, null, new DateTime(2026, 10, 30, 0, 0, 0, 0, DateTimeKind.Utc), false, "LargeGas.Limit.DailyMeteredQuantity", "null", 1 },
                    { 16, null, null, new DateTime(2026, 10, 30, 0, 0, 0, 0, DateTimeKind.Utc), false, "Retention.Years", "6", 1 },
                    { 17, null, null, new DateTime(2026, 10, 30, 0, 0, 0, 0, DateTimeKind.Utc), true, "Wording.ValidityStatement", "\"PLACEHOLDER: These prices were fetched on {PricesFetchedDate} and are valid until the end of that day.\"", 1 },
                    { 18, null, null, new DateTime(2026, 10, 30, 0, 0, 0, 0, DateTimeKind.Utc), true, "Wording.IndicativeNotice", "\"PLACEHOLDER: These prices are indicative and may change until a contract is agreed with the supplier.\"", 1 },
                    { 19, null, null, new DateTime(2026, 10, 30, 0, 0, 0, 0, DateTimeKind.Utc), true, "Wording.CreditCheck", "\"All prices are subject to supplier credit check and acceptance\"", 1 },
                    { 20, null, null, new DateTime(2026, 10, 30, 0, 0, 0, 0, DateTimeKind.Utc), true, "Wording.CommissionDisclosure", "\"PLACEHOLDER: Commission disclosure to be supplied by compliance. It may include the commission for the recommended option, such as {CommissionTotal}.\"", 1 },
                    { 21, null, null, new DateTime(2026, 10, 30, 0, 0, 0, 0, DateTimeKind.Utc), true, "Wording.Microbusiness.CommissionDisclosure", "\"PLACEHOLDER: Microbusiness commission disclosure to be supplied by compliance.\"", 1 },
                    { 22, null, null, new DateTime(2026, 10, 30, 0, 0, 0, 0, DateTimeKind.Utc), true, "Wording.Microbusiness.DisputeResolution", "\"PLACEHOLDER: Dispute resolution scheme (the Energy Ombudsman) wording to be supplied by compliance.\"", 1 },
                    { 23, null, null, new DateTime(2026, 10, 30, 0, 0, 0, 0, DateTimeKind.Utc), true, "Wording.Microbusiness.CoolingOff", "\"PLACEHOLDER: Cooling-off period wording to be supplied by compliance.\"", 1 },
                    { 24, null, null, new DateTime(2026, 10, 30, 0, 0, 0, 0, DateTimeKind.Utc), true, "Wording.VatCclNote", "\"PLACEHOLDER: VAT and Climate Change Levy note to be supplied by compliance.\"", 1 },
                    { 25, null, null, new DateTime(2026, 10, 30, 0, 0, 0, 0, DateTimeKind.Utc), true, "Wording.Terms", "\"PLACEHOLDER: Terms and conditions statement to be supplied by compliance.\"", 1 },
                    { 26, null, null, new DateTime(2026, 10, 30, 0, 0, 0, 0, DateTimeKind.Utc), true, "Terms.Url", "\"https://www.example.com/terms-placeholder\"", 1 }
                });

            migrationBuilder.InsertData(
                table: "Suppliers",
                columns: new[] { "Id", "DisplayName", "IsActive", "MaxStandingChargeUpliftElectricity", "MaxStandingChargeUpliftGas", "MaxUnitRateUpliftElectricity", "MaxUnitRateUpliftGas", "PricingName", "SuppliesElectricity", "SuppliesGas", "UpfrontPercent" },
                values: new object[,]
                {
                    { 1, "British Gas", true, 25m, 25m, 3.0m, 2.0m, "British Gas", true, true, 0m },
                    { 2, "EDF", true, 25m, 25m, 3.0m, 2.0m, "EDF", true, true, 0m },
                    { 3, "E.ON Next", true, 25m, 25m, 3.0m, 2.0m, "E.ON Next", true, true, 0m },
                    { 4, "Octopus Energy", true, 25m, 25m, 3.0m, 2.0m, "Octopus Energy", true, true, 0m },
                    { 5, "Opus Energy", true, 25m, 25m, 3.0m, 2.0m, "Opus Energy", true, true, 0m },
                    { 6, "Scottish Power", true, 25m, 25m, 3.0m, 2.0m, "Scottish Power", true, true, 0m },
                    { 7, "Smartest Energy", true, 25m, 25m, 3.0m, 2.0m, "Smartest Energy", true, true, 0m },
                    { 8, "Valda Energy", true, 25m, 25m, 3.0m, 2.0m, "Valda Energy", true, true, 0m },
                    { 9, "Crown Gas & Power", true, 25m, 25m, 3.0m, 2.0m, "Crown Gas & Power", true, true, 0m },
                    { 10, "Yu Energy", true, 25m, 25m, 3.0m, 2.0m, "Yu Energy", true, true, 0m }
                });

            migrationBuilder.InsertData(
                table: "SupplierAliases",
                columns: new[] { "Id", "LookupName", "Mpid", "PowwrId", "SupplierId" },
                values: new object[,]
                {
                    { 1, "British Gas Business", "ENRD", "BRITISHG-001-S", 1 },
                    { 2, "EDF Energy", null, "EDFENERG-001-S", 2 },
                    { 3, "E.On Next", null, "EONNEXT-001-S", 3 },
                    { 4, "Octopus Energy", null, "OCTOPUSE-001-S", 4 },
                    { 5, "Opus Energy", null, "OPUSENER-001-S", 5 },
                    { 6, "Scottish Power", "SCT", "SCOTTISH-001-S", 6 },
                    { 7, "Smartest Energy", null, "SMARTEST-001-S", 7 },
                    { 8, "Valda Energy", null, "VALDAENE-001-S", 8 },
                    { 9, "Crown Gas & Power", null, "CROWNGAS-001-S", 9 },
                    { 10, "Yu Energy", null, "YUENERGY-001-S", 10 }
                });

            migrationBuilder.CreateIndex(
                name: "IX_AuditEntries_QuoteId",
                table: "AuditEntries",
                column: "QuoteId");

            migrationBuilder.CreateIndex(
                name: "IX_EffectiveRates_Kind_EffectiveFrom",
                table: "EffectiveRates",
                columns: new[] { "Kind", "EffectiveFrom" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_NoQuoteResults_PricingRunSiteId",
                table: "NoQuoteResults",
                column: "PricingRunSiteId");

            migrationBuilder.CreateIndex(
                name: "IX_PriceOptions_PricingRunId_SupplierName_TermMonths_PlanType",
                table: "PriceOptions",
                columns: new[] { "PricingRunId", "SupplierName", "TermMonths", "PlanType" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PriceOptionSites_PriceOptionId",
                table: "PriceOptionSites",
                column: "PriceOptionId");

            migrationBuilder.CreateIndex(
                name: "IX_PriceOptionSites_PricingRunSiteId",
                table: "PriceOptionSites",
                column: "PricingRunSiteId");

            migrationBuilder.CreateIndex(
                name: "IX_PricingRuns_QuoteId_RunNumber",
                table: "PricingRuns",
                columns: new[] { "QuoteId", "RunNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PricingRuns_RequestedById",
                table: "PricingRuns",
                column: "RequestedById");

            migrationBuilder.CreateIndex(
                name: "IX_PricingRunSites_PricingRunId",
                table: "PricingRunSites",
                column: "PricingRunId");

            migrationBuilder.CreateIndex(
                name: "IX_PricingRunSites_SiteId",
                table: "PricingRunSites",
                column: "SiteId");

            migrationBuilder.CreateIndex(
                name: "IX_Quotes_Customer_BusinessName",
                table: "Quotes",
                column: "Customer_BusinessName");

            migrationBuilder.CreateIndex(
                name: "IX_Quotes_LastActivityUtc",
                table: "Quotes",
                column: "LastActivityUtc");

            migrationBuilder.CreateIndex(
                name: "IX_Quotes_OwnerId_CreatedUtc",
                table: "Quotes",
                columns: new[] { "OwnerId", "CreatedUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_Quotes_Reference",
                table: "Quotes",
                column: "Reference",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_QuoteUpliftOverrides_QuoteId_SupplierId_TermMonths",
                table: "QuoteUpliftOverrides",
                columns: new[] { "QuoteId", "SupplierId", "TermMonths" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_QuoteUpliftOverrides_SupplierId",
                table: "QuoteUpliftOverrides",
                column: "SupplierId");

            migrationBuilder.CreateIndex(
                name: "IX_ReportingOutboxEntries_NextAttemptUtc_ExportedUtc",
                table: "ReportingOutboxEntries",
                columns: new[] { "NextAttemptUtc", "ExportedUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_ReportingRecords_AgentId",
                table: "ReportingRecords",
                column: "AgentId");

            migrationBuilder.CreateIndex(
                name: "IX_ReportingRecords_Fuel",
                table: "ReportingRecords",
                column: "Fuel");

            migrationBuilder.CreateIndex(
                name: "IX_ReportingRecords_OccurredUtc",
                table: "ReportingRecords",
                column: "OccurredUtc");

            migrationBuilder.CreateIndex(
                name: "IX_ReportingRecords_QuoteId",
                table: "ReportingRecords",
                column: "QuoteId");

            migrationBuilder.CreateIndex(
                name: "IX_ReportingRecords_QuoteType",
                table: "ReportingRecords",
                column: "QuoteType");

            migrationBuilder.CreateIndex(
                name: "IX_ReportingRecordSuppliers_SupplierName",
                table: "ReportingRecordSuppliers",
                column: "SupplierName");

            migrationBuilder.CreateIndex(
                name: "IX_SettingVersions_Key_Version",
                table: "SettingVersions",
                columns: new[] { "Key", "Version" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Sites_CurrentSupplierId",
                table: "Sites",
                column: "CurrentSupplierId");

            migrationBuilder.CreateIndex(
                name: "IX_Sites_Meter_MpanCore",
                table: "Sites",
                column: "Meter_MpanCore");

            migrationBuilder.CreateIndex(
                name: "IX_Sites_Meter_Mprn",
                table: "Sites",
                column: "Meter_Mprn");

            migrationBuilder.CreateIndex(
                name: "IX_Sites_Postcode",
                table: "Sites",
                column: "Postcode");

            migrationBuilder.CreateIndex(
                name: "IX_Sites_QuoteId",
                table: "Sites",
                column: "QuoteId");

            migrationBuilder.CreateIndex(
                name: "IX_StaffMembers_EntraObjectId",
                table: "StaffMembers",
                column: "EntraObjectId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SupplierAliases_LookupName",
                table: "SupplierAliases",
                column: "LookupName",
                unique: true,
                filter: "[LookupName] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_SupplierAliases_Mpid",
                table: "SupplierAliases",
                column: "Mpid",
                unique: true,
                filter: "[Mpid] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_SupplierAliases_PowwrId",
                table: "SupplierAliases",
                column: "PowwrId",
                unique: true,
                filter: "[PowwrId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_SupplierAliases_SupplierId",
                table: "SupplierAliases",
                column: "SupplierId");

            migrationBuilder.CreateIndex(
                name: "IX_Suppliers_PricingName",
                table: "Suppliers",
                column: "PricingName",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AuditEntries");

            migrationBuilder.DropTable(
                name: "EffectiveRates");

            migrationBuilder.DropTable(
                name: "NoQuoteResults");

            migrationBuilder.DropTable(
                name: "PriceOptionSites");

            migrationBuilder.DropTable(
                name: "QuoteUpliftOverrides");

            migrationBuilder.DropTable(
                name: "ReportingOutboxEntries");

            migrationBuilder.DropTable(
                name: "ReportingRecordSuppliers");

            migrationBuilder.DropTable(
                name: "SettingVersions");

            migrationBuilder.DropTable(
                name: "SupplierAliases");

            migrationBuilder.DropTable(
                name: "PriceOptions");

            migrationBuilder.DropTable(
                name: "PricingRunSites");

            migrationBuilder.DropTable(
                name: "ReportingRecords");

            migrationBuilder.DropTable(
                name: "PricingRuns");

            migrationBuilder.DropTable(
                name: "Sites");

            migrationBuilder.DropTable(
                name: "Quotes");

            migrationBuilder.DropTable(
                name: "Suppliers");

            migrationBuilder.DropTable(
                name: "StaffMembers");

            migrationBuilder.DropSequence(
                name: "QuoteNumbers");
        }
    }
}

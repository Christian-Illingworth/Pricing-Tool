using PricingTool.Domain;

namespace PricingTool.Services.Settings;

// Every SettingVersion key (data-model.md "Keys"). Values are stored as JSON.
public static class SettingKeys
{
    // Default uplifts, each ≤ its fuel's maximum (FR-025, FR-044).
    public const string DefaultUpliftElectricityUnitRate = "Uplift.Default.Electricity.UnitRate";
    public const string DefaultUpliftElectricityStandingCharge = "Uplift.Default.Electricity.StandingCharge";
    public const string DefaultUpliftGasUnitRate = "Uplift.Default.Gas.UnitRate";
    public const string DefaultUpliftGasStandingCharge = "Uplift.Default.Gas.StandingCharge";

    // Maximum uplifts for everyone (FR-058). Blank means no uplift is allowed (FR-060).
    public const string UpliftLimitElectricityUnitRate = "UpliftLimit.Electricity.UnitRate";
    public const string UpliftLimitElectricityStandingCharge = "UpliftLimit.Electricity.StandingCharge";
    public const string UpliftLimitGasUnitRate = "UpliftLimit.Gas.UnitRate";
    public const string UpliftLimitGasStandingCharge = "UpliftLimit.Gas.StandingCharge";

    public const string CommissionBasis = "Commission.Basis";
    public const string PaymentDefault = "Payment.Default";

    public const string MicrobusinessThresholdElectricityKwh = "Microbusiness.Threshold.Electricity.Kwh";
    public const string MicrobusinessThresholdGasKwh = "Microbusiness.Threshold.Gas.Kwh";

    // Blank to start: a blank limit isn't checked (FR-045).
    public const string LargeGasLimitAnnualQuantityKwh = "LargeGas.Limit.AnnualQuantityKwh";
    public const string LargeGasLimitMeterCapacity = "LargeGas.Limit.MeterCapacity";
    public const string LargeGasLimitDailyMeteredQuantity = "LargeGas.Limit.DailyMeteredQuantity";

    public const string RetentionYears = "Retention.Years";

    // Customer wording (constitution Principle IX). Placeholders until compliance approves them.
    public const string WordingValidityStatement = "Wording.ValidityStatement";
    public const string WordingIndicativeNotice = "Wording.IndicativeNotice";
    public const string WordingCreditCheck = "Wording.CreditCheck";
    public const string WordingCommissionDisclosure = "Wording.CommissionDisclosure";
    public const string WordingMicrobusinessCommissionDisclosure = "Wording.Microbusiness.CommissionDisclosure";
    public const string WordingMicrobusinessDisputeResolution = "Wording.Microbusiness.DisputeResolution";
    public const string WordingMicrobusinessCoolingOff = "Wording.Microbusiness.CoolingOff";
    public const string WordingVatCclNote = "Wording.VatCclNote";
    public const string WordingTerms = "Wording.Terms";

    public const string TermsUrl = "Terms.Url";

    public static string DefaultUnitRateUplift(Fuel fuel) =>
        fuel == Fuel.Electricity ? DefaultUpliftElectricityUnitRate : DefaultUpliftGasUnitRate;

    public static string DefaultStandingChargeUplift(Fuel fuel) =>
        fuel == Fuel.Electricity ? DefaultUpliftElectricityStandingCharge : DefaultUpliftGasStandingCharge;

    public static string UnitRateUpliftLimit(Fuel fuel) =>
        fuel == Fuel.Electricity ? UpliftLimitElectricityUnitRate : UpliftLimitGasUnitRate;

    public static string StandingChargeUpliftLimit(Fuel fuel) =>
        fuel == Fuel.Electricity ? UpliftLimitElectricityStandingCharge : UpliftLimitGasStandingCharge;

    public static string MicrobusinessThresholdKwh(Fuel fuel) =>
        fuel == Fuel.Electricity ? MicrobusinessThresholdElectricityKwh : MicrobusinessThresholdGasKwh;
}

using PricingTool.Domain;

namespace PricingTool.Services.Validation;

// What the validator needs besides the input. Today is the UK date (UkCalendar.Today).
public sealed record QuoteValidationContext(DateOnly Today);

// The single validator for a quote's inputs (data-model.md "Validation rules by requirement"). The form,
// the pricing service (as a guard) and upload rows all go through it, and nothing reaches POWWR until it
// returns no errors (constitution Principle III, FR-032). Errors are keyed by field path, such as
// "Sites[0].Meter.MpanCore", so a page can show each one beside its field.
public static class QuoteInputValidator
{
    public static readonly IReadOnlyList<int> AllowedTerms = [12, 24, 36, 48, 60];

    private const int MaxUpliftDecimalPlaces = 4;

    public static IReadOnlyDictionary<string, IReadOnlyList<string>> Validate(QuoteInput input, QuoteValidationContext context)
    {
        var errors = new Errors();

        ValidateQuote(input, errors);
        ValidateCustomer(input.Customer, errors);
        ValidateContract(input.Contract, context, errors);

        if (input.Sites.Count == 0)
            errors.Add("Sites", "Add at least one site.");

        var meterNumbers = new Dictionary<string, int>();
        for (var i = 0; i < input.Sites.Count; i++)
            ValidateSite(input.Fuel, input.Sites[i], $"Sites[{i}]", i, context, meterNumbers, errors);

        return errors.ToReadOnly();
    }

    private static void ValidateQuote(QuoteInput input, Errors errors)
    {
        if (input.QuoteType is null)
            errors.Add("QuoteType", "Choose the quote type.");
        else if (input.QuoteType == QuoteType.ChangeOfTenancy && input.CotDate is null)
            errors.Add("CotDate", "Enter the date the customer moved in.");

        if (input.VatCclTreatment == VatCclTreatment.Qualifying)
        {
            if (input.QualifyingPercent is null)
                errors.Add("QualifyingPercent", "Enter the qualifying percentage from the customer's declaration.");
            else if (input.QualifyingPercent is < 0m or > 100m)
                errors.Add("QualifyingPercent", "The qualifying percentage must be between 0 and 100.");
        }
    }

    private static void ValidateCustomer(CustomerInput customer, Errors errors)
    {
        if (string.IsNullOrWhiteSpace(customer.BusinessName))
            errors.Add("Customer.BusinessName", "Enter the business name.");
        else if (customer.BusinessName.Trim().Length > 200)
            errors.Add("Customer.BusinessName", "The business name can be at most 200 characters.");

        if (customer.BusinessType is null)
            errors.Add("Customer.BusinessType", "Choose the business type.");
        else if (CompanyNumber.Validate(customer.CompanyNumber, customer.BusinessType) is { } companyError)
            errors.Add("Customer.CompanyNumber", companyError);

        if (string.IsNullOrWhiteSpace(customer.ContactName))
            errors.Add("Customer.ContactName", "Enter the contact's name.");

        if (string.IsNullOrWhiteSpace(customer.ContactEmail))
            errors.Add("Customer.ContactEmail", "Enter the contact's email address.");
        else if (!EmailAddress.IsValid(customer.ContactEmail))
            errors.Add("Customer.ContactEmail", "Enter a valid email address, like name@example.com.");

        if (string.IsNullOrWhiteSpace(customer.ContactPhone))
            errors.Add("Customer.ContactPhone", "Enter the contact's phone number.");
        else if (!UkPhone.TryNormalise(customer.ContactPhone, out _))
            errors.Add("Customer.ContactPhone", "Enter a UK phone number, starting 0 or +44.");
    }

    private static void ValidateContract(ContractInput contract, QuoteValidationContext context, Errors errors)
    {
        if (contract.StartDate is null)
            errors.Add("Contract.StartDate", "Enter the contract start date.");
        else if (contract.StartDate < context.Today)
            errors.Add("Contract.StartDate", "The start date can't be in the past.");

        if (contract.TermsMonths.Count == 0)
            errors.Add("Contract.TermsMonths", "Choose at least one term.");
        else if (contract.TermsMonths.Any(t => !AllowedTerms.Contains(t)) || contract.TermsMonths.Distinct().Count() != contract.TermsMonths.Count)
            errors.Add("Contract.TermsMonths", "Terms must be 12, 24, 36, 48 or 60 months, each chosen once.");

        if (contract.PaymentMethod is null)
            errors.Add("Contract.PaymentMethod", "Choose the payment method.");

        if (contract.SupplierFilterMode != SupplierFilterMode.All && contract.SupplierFilterIds.Count == 0)
            errors.Add("Contract.SupplierFilterIds", contract.SupplierFilterMode == SupplierFilterMode.Include
                ? "Choose at least one supplier to include."
                : "Choose at least one supplier to exclude.");

        ValidateUplift(contract.DefaultUnitRateUplift, "Contract.DefaultUnitRateUplift", "unit-rate uplift (p/kWh)", errors);
        ValidateUplift(contract.DefaultStandingChargeUplift, "Contract.DefaultStandingChargeUplift", "standing-charge uplift (p/day)", errors);
    }

    // FR-025: never negative, at most 4 decimal places. The per-fuel maximum (FR-061) is US5's LimitPolicy.
    private static void ValidateUplift(decimal? uplift, string field, string label, Errors errors)
    {
        if (uplift is null)
            errors.Add(field, $"Enter the {label}.");
        else if (uplift < 0m)
            errors.Add(field, $"The {label} can't be negative.");
        else if (decimal.Round(uplift.Value, MaxUpliftDecimalPlaces) != uplift.Value)
            errors.Add(field, $"The {label} can have at most {MaxUpliftDecimalPlaces} decimal places.");
    }

    private static void ValidateSite(
        Fuel fuel, SiteInput site, string path, int index, QuoteValidationContext context,
        Dictionary<string, int> meterNumbers, Errors errors)
    {
        if (string.IsNullOrWhiteSpace(site.AddressLine1))
            errors.Add($"{path}.AddressLine1", "Enter the first line of the site address.");

        if (string.IsNullOrWhiteSpace(site.Postcode))
            errors.Add($"{path}.Postcode", "Enter the site postcode.");
        else if (Postcode.Normalise(site.Postcode) is null)
            errors.Add($"{path}.Postcode", "Enter a full UK postcode, like ZZ1 1AA.");

        if (site.StartDate is { } siteStart && siteStart < context.Today)
            errors.Add($"{path}.StartDate", "The site's start date can't be in the past.");

        if (site.Warnings.Any(w => w.Code == SiteWarningCode.Domestic && !w.ConfirmedByAgent))
            errors.Add($"{path}.Warnings", "This is a domestic meter. Confirm it's being supplied to a business before pricing.");

        NotNegative(site.CurrentDayRate, $"{path}.CurrentDayRate", errors);
        NotNegative(site.CurrentNightRate, $"{path}.CurrentNightRate", errors);
        NotNegative(site.CurrentWendRate, $"{path}.CurrentWendRate", errors);
        NotNegative(site.CurrentStandingCharge, $"{path}.CurrentStandingCharge", errors);

        if (fuel == Fuel.Electricity)
            ValidateElectricity(site, path, index, meterNumbers, errors);
        else
            ValidateGas(site, path, index, meterNumbers, errors);
    }

    private static void ValidateElectricity(SiteInput site, string path, int index, Dictionary<string, int> meterNumbers, Errors errors)
    {
        var meter = site.Meter;
        var meterPath = $"{path}.Meter";

        if (!MpanCore.TryParse(meter.MpanCore, out var core, out var coreError))
            errors.Add($"{meterPath}.MpanCore", coreError!);
        else
            CheckRepeated(core!.Value, $"{meterPath}.MpanCore", index, meterNumbers, errors);

        if (meter.ProfileClass is < 0 or > 8)
            errors.Add($"{meterPath}.ProfileClass", "The profile class must be from 00 to 08.");
        if (meter.MeterTimeswitchClass is < 0 or > 999)
            errors.Add($"{meterPath}.MeterTimeswitchClass", "The meter time-switch code is 3 digits.");
        if (!string.IsNullOrWhiteSpace(meter.LineLossFactorClass) && !IsAlphanumeric(meter.LineLossFactorClass.Trim(), 3))
            errors.Add($"{meterPath}.LineLossFactorClass", "The line loss factor class is 3 characters.");
        if (!string.IsNullOrWhiteSpace(meter.StandardSettlementConfig) && !IsDigits(meter.StandardSettlementConfig.Trim(), 4))
            errors.Add($"{meterPath}.StandardSettlementConfig", "The standard settlement configuration is 4 digits.");
        if (!string.IsNullOrWhiteSpace(meter.DuosTariffId) && !IsAlphanumeric(meter.DuosTariffId.Trim(), 3))
            errors.Add($"{meterPath}.DuosTariffId", "The DUoS tariff ID is 3 characters.");

        if (!string.IsNullOrWhiteSpace(meter.MpanTopLine))
        {
            if (!MpanTopLine.TryParse(meter.MpanTopLine, out var topLine, out var topLineError))
                errors.Add($"{meterPath}.MpanTopLine", topLineError!);
            else
            {
                foreach (var field in topLine!.Disagreements(meter.ProfileClass, meter.MeterTimeswitchClass,
                             meter.LineLossFactorClass, meter.StandardSettlementConfig, meter.DuosTariffId))
                    errors.Add($"{meterPath}.{field}", $"Doesn't match the MPAN top line ({topLine.Value}).");
            }
        }

        var profileClass = meter.ProfileClass
            ?? (MpanTopLine.TryParse(meter.MpanTopLine, out var parsedTop, out _) ? parsedTop!.ProfileClass : null);
        if (Meters.IsHalfHourly(profileClass, meter.MeasurementClass))
        {
            if (meter.KvaCapacity is null or <= 0)
                errors.Add($"{meterPath}.KvaCapacity", "A half-hourly meter needs the agreed supply capacity (kVA).");
            if (meter.KvarhConsumption is < 0)
                errors.Add($"{meterPath}.KvarhConsumption", "Reactive power (kVArh) can't be negative.");
        }

        ValidateConsumption(site, path, errors);
    }

    // FR-022: each register the rate structure uses needs kWh above zero, or a total with a split that
    // adds up to exactly 100%.
    private static void ValidateConsumption(SiteInput site, string path, Errors errors)
    {
        var registers = site.Meter.RateRegisters.Distinct().Order().ToList();
        if (registers.Count == 0)
        {
            errors.Add($"{path}.Meter.RateRegisters", "Choose the meter's rate structure.");
            return;
        }

        if (site.ConsumptionEntryMode == ConsumptionEntryMode.PerRegister)
        {
            foreach (var register in registers)
            {
                var (field, kwh) = register switch
                {
                    Register.Day => ("ConsumptionDayKwh", site.ConsumptionDayKwh),
                    Register.Night => ("ConsumptionNightKwh", site.ConsumptionNightKwh),
                    _ => ("ConsumptionWendKwh", site.ConsumptionWendKwh),
                };
                if (kwh is null or <= 0)
                    errors.Add($"{path}.{field}", $"Enter the {RegisterLabel(register)} consumption in kWh, above zero.");
            }
            return;
        }

        if (site.ConsumptionTotalKwh is null or <= 0)
            errors.Add($"{path}.ConsumptionTotalKwh", "Enter the total annual consumption in kWh, above zero.");

        var split = registers.Select(r => (Register: r, Pct: r switch
        {
            Register.Day => site.SplitDayPct,
            Register.Night => site.SplitNightPct,
            _ => site.SplitWendPct,
        })).ToList();

        var firstField = $"{path}.Split{registers[0]}Pct";
        if (split.Any(s => s.Pct is null or < 0m))
            errors.Add(firstField, "Enter a percentage for each register.");
        else if (!RegisterSplit.TotalsOneHundred(split.Select(s => s.Pct!.Value).ToList()))
            errors.Add(firstField, "The percentages must add up to exactly 100%.");
    }

    private static void ValidateGas(SiteInput site, string path, int index, Dictionary<string, int> meterNumbers, Errors errors)
    {
        if (!Mprn.TryParse(site.Meter.Mprn, out var mprn, out var mprnError))
            errors.Add($"{path}.Meter.Mprn", mprnError!);
        else
            CheckRepeated(mprn!.Value, $"{path}.Meter.Mprn", index, meterNumbers, errors);

        if (site.GasAnnualQuantityKwh is null or <= 0)
            errors.Add($"{path}.GasAnnualQuantityKwh", "Enter the annual quantity (AQ) in kWh, above zero.");
    }

    // A meter number can appear only once per quote (FR-070).
    private static void CheckRepeated(string number, string field, int index, Dictionary<string, int> seen, Errors errors)
    {
        if (seen.TryGetValue(number, out var first))
            errors.Add(field, $"This meter is already on the quote (site {first + 1}).");
        else
            seen[number] = index;
    }

    private static void NotNegative(decimal? value, string field, Errors errors)
    {
        if (value < 0m) errors.Add(field, "This can't be negative.");
    }

    private static string RegisterLabel(Register register) => register switch
    {
        Register.Day => "day",
        Register.Night => "night",
        _ => "evening and weekend",
    };

    private static bool IsDigits(string s, int length) => s.Length == length && s.All(char.IsAsciiDigit);

    private static bool IsAlphanumeric(string s, int length) => s.Length == length && s.All(char.IsAsciiLetterOrDigit);

    private sealed class Errors
    {
        private readonly Dictionary<string, List<string>> _byField = new(StringComparer.Ordinal);

        public void Add(string field, string message)
        {
            if (!_byField.TryGetValue(field, out var list)) _byField[field] = list = [];
            if (!list.Contains(message)) list.Add(message);
        }

        public IReadOnlyDictionary<string, IReadOnlyList<string>> ToReadOnly() =>
            _byField.ToDictionary(kv => kv.Key, kv => (IReadOnlyList<string>)kv.Value, StringComparer.Ordinal);
    }
}

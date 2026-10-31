using PricingTool.Domain;
using PricingTool.Services.Validation;

namespace PricingTool.Tests.Validation;

public class QuoteInputValidatorTests
{
    private static readonly DateOnly Today = new(2026, 10, 31);
    private static readonly QuoteValidationContext Context = new(Today);

    // A complete, valid single-site electricity quote with made-up details (quickstart ZZ1 1AA).
    private static QuoteInput ValidElectricity() => new()
    {
        Fuel = Fuel.Electricity,
        QuoteType = QuoteType.Renewal,
        Customer = new CustomerInput
        {
            BusinessName = "Acme Trading Ltd",
            BusinessType = BusinessType.LimitedCompany,
            CompanyNumber = "12345678",
            ContactName = "Jo Bloggs",
            ContactEmail = "jo@example.com",
            ContactPhone = "01234 567890",
        },
        Sites =
        [
            new SiteInput
            {
                AddressLine1 = "Unit 1, Test Park",
                Postcode = "ZZ1 1AA",
                ConsumptionEntryMode = ConsumptionEntryMode.PerRegister,
                ConsumptionDayKwh = 18_000,
                Meter = new MeterInput
                {
                    MpanTopLine = "03801631",
                    MpanCore = "1600000001013",
                    ProfileClass = 3,
                    MeterTimeswitchClass = 801,
                    LineLossFactorClass = "631",
                    StandardSettlementConfig = "0393",
                    RateRegisters = [Register.Day],
                },
            },
        ],
        Contract = new ContractInput
        {
            StartDate = new DateOnly(2026, 12, 1),
            TermsMonths = [12, 24, 36],
            PaymentMethod = PaymentMethod.DirectDebit,
            PaymentFrequency = PaymentFrequency.Monthly,
            DefaultUnitRateUplift = 1.5m,
            DefaultStandingChargeUplift = 5m,
        },
    };

    private static QuoteInput ValidGas()
    {
        var input = ValidElectricity();
        input.Fuel = Fuel.Gas;
        input.Sites[0].Meter = new MeterInput { Mprn = "1234567" };
        input.Sites[0].ConsumptionDayKwh = null;
        input.Sites[0].GasAnnualQuantityKwh = 40_000;
        return input;
    }

    private static IReadOnlyDictionary<string, IReadOnlyList<string>> Validate(QuoteInput input) =>
        QuoteInputValidator.Validate(input, Context);

    private static void AssertOnlyError(QuoteInput input, string field)
    {
        var errors = Validate(input);
        Assert.True(errors.ContainsKey(field), $"Expected an error on {field}; got: {string.Join(", ", errors.Keys)}");
        Assert.Single(errors);
        Assert.NotEmpty(errors[field]);
    }

    [Fact]
    public void ValidElectricityQuoteHasNoErrors() => Assert.Empty(Validate(ValidElectricity()));

    [Fact]
    public void ValidGasQuoteHasNoErrors() => Assert.Empty(Validate(ValidGas()));

    // ── FR-005 to FR-007: customer ──

    [Fact]
    public void BusinessNameIsRequired()
    {
        var input = ValidElectricity();
        input.Customer.BusinessName = "  ";
        AssertOnlyError(input, "Customer.BusinessName");
    }

    [Fact]
    public void BusinessTypeIsRequired()
    {
        var input = ValidElectricity();
        input.Customer.BusinessType = null;
        input.Customer.CompanyNumber = null;
        AssertOnlyError(input, "Customer.BusinessType");
    }

    [Fact]
    public void CompanyNumberFormatIsChecked()
    {
        var input = ValidElectricity();
        input.Customer.CompanyNumber = "1234";
        AssertOnlyError(input, "Customer.CompanyNumber");
    }

    [Fact]
    public void CompanyNumberIsRefusedForSoleTraders()
    {
        var input = ValidElectricity();
        input.Customer.BusinessType = BusinessType.SoleTrader;
        AssertOnlyError(input, "Customer.CompanyNumber");
    }

    [Fact]
    public void ContactDetailsAreRequiredAndChecked()
    {
        var input = ValidElectricity();
        input.Customer.ContactName = null;
        input.Customer.ContactEmail = "not-an-email";
        input.Customer.ContactPhone = "12345";
        var errors = Validate(input);
        Assert.Equal(["Customer.ContactEmail", "Customer.ContactName", "Customer.ContactPhone"], errors.Keys.Order());
    }

    [Fact]
    public void SitePostcodeAndAddressAreRequired()
    {
        var input = ValidElectricity();
        input.Sites[0].Postcode = "ZZ1";
        input.Sites[0].AddressLine1 = null;
        var errors = Validate(input);
        Assert.Equal(["Sites[0].AddressLine1", "Sites[0].Postcode"], errors.Keys.Order());
    }

    [Fact]
    public void AtLeastOneSiteIsRequired()
    {
        var input = ValidElectricity();
        input.Sites.Clear();
        AssertOnlyError(input, "Sites");
    }

    // ── FR-012: domestic meters need the agent's confirmation ──

    [Fact]
    public void UnconfirmedDomesticMeterIsRefused()
    {
        var input = ValidElectricity();
        input.Sites[0].Warnings = [new SiteWarning(SiteWarningCode.Domestic, "The lookup says this is a domestic meter.")];
        AssertOnlyError(input, "Sites[0].Warnings");
    }

    [Fact]
    public void ConfirmedDomesticMeterAndOtherWarningsAreAllowed()
    {
        var input = ValidElectricity();
        input.Sites[0].Warnings =
        [
            new SiteWarning(SiteWarningCode.Domestic, "Domestic meter.", ConfirmedByAgent: true),
            new SiteWarning(SiteWarningCode.DeEnergised, "De-energised."),
        ];
        Assert.Empty(Validate(input));
    }

    // ── FR-013: quote type ──

    [Fact]
    public void QuoteTypeIsRequired()
    {
        var input = ValidElectricity();
        input.QuoteType = null;
        AssertOnlyError(input, "QuoteType");
    }

    [Fact]
    public void ChangeOfTenancyNeedsTheMoveInDate()
    {
        var input = ValidElectricity();
        input.QuoteType = QuoteType.ChangeOfTenancy;
        AssertOnlyError(input, "CotDate");

        input.CotDate = new DateOnly(2026, 10, 1);
        Assert.Empty(Validate(input));
    }

    // ── FR-015 to FR-017: MPAN ──

    [Fact]
    public void MpanCoreIsRequired()
    {
        var input = ValidElectricity();
        input.Sites[0].Meter.MpanCore = null;
        AssertOnlyError(input, "Sites[0].Meter.MpanCore");
    }

    [Fact]
    public void WrongCheckDigitIsAFieldError()
    {
        var input = ValidElectricity();
        input.Sites[0].Meter.MpanCore = "1600000001014";
        AssertOnlyError(input, "Sites[0].Meter.MpanCore");
        Assert.Contains("check digit", Validate(input)["Sites[0].Meter.MpanCore"][0]);
    }

    [Fact]
    public void TopLineMustBeEightOrNineCharacters()
    {
        var input = ValidElectricity();
        input.Sites[0].Meter.MpanTopLine = "0380163";
        AssertOnlyError(input, "Sites[0].Meter.MpanTopLine");
    }

    [Fact]
    public void TopLineAndFieldsMustAgree()
    {
        var input = ValidElectricity();
        input.Sites[0].Meter.MeterTimeswitchClass = 845;
        AssertOnlyError(input, "Sites[0].Meter.MeterTimeswitchClass");
    }

    [Fact]
    public void NineCharacterTopLineChecksSsc()
    {
        var input = ValidElectricity();
        input.Sites[0].Meter.MpanTopLine = "030151661";
        input.Sites[0].Meter.StandardSettlementConfig = "0393";
        AssertOnlyError(input, "Sites[0].Meter.StandardSettlementConfig");
    }

    [Fact]
    public void ProfileClassMustBeZeroToEight()
    {
        var input = ValidElectricity();
        input.Sites[0].Meter.MpanTopLine = null;
        input.Sites[0].Meter.ProfileClass = 9;
        AssertOnlyError(input, "Sites[0].Meter.ProfileClass");
    }

    // ── FR-018: half-hourly needs kVA ──

    [Fact]
    public void HalfHourlyByProfileClassNeedsKva()
    {
        var input = ValidElectricity();
        input.Sites[0].Meter.MpanTopLine = null;
        input.Sites[0].Meter.ProfileClass = 0;
        AssertOnlyError(input, "Sites[0].Meter.KvaCapacity");

        input.Sites[0].Meter.KvaCapacity = 200;
        Assert.Empty(Validate(input));
    }

    [Fact]
    public void HalfHourlyByMeasurementClassNeedsKva()
    {
        var input = ValidElectricity();
        input.Sites[0].Meter.MeasurementClass = "C";
        input.Sites[0].Meter.KvaCapacity = 0;
        AssertOnlyError(input, "Sites[0].Meter.KvaCapacity");
    }

    // ── FR-022: consumption ──

    [Fact]
    public void EachRegisterNeedsConsumptionAboveZero()
    {
        var input = ValidElectricity();
        input.Sites[0].Meter.RateRegisters = [Register.Day, Register.Night];
        input.Sites[0].ConsumptionNightKwh = 0;
        AssertOnlyError(input, "Sites[0].ConsumptionNightKwh");
    }

    [Fact]
    public void TotalWithSplitMustAddUpToOneHundred()
    {
        var input = ValidElectricity();
        input.Sites[0].Meter.RateRegisters = [Register.Day, Register.Night];
        input.Sites[0].ConsumptionEntryMode = ConsumptionEntryMode.TotalWithSplit;
        input.Sites[0].ConsumptionTotalKwh = 90_000;
        input.Sites[0].SplitDayPct = 70m;
        input.Sites[0].SplitNightPct = 29m;
        AssertOnlyError(input, "Sites[0].SplitDayPct");

        input.Sites[0].SplitNightPct = 30m;
        Assert.Empty(Validate(input));
    }

    [Fact]
    public void TotalWithSplitNeedsATotal()
    {
        var input = ValidElectricity();
        input.Sites[0].ConsumptionEntryMode = ConsumptionEntryMode.TotalWithSplit;
        input.Sites[0].SplitDayPct = 100m;
        AssertOnlyError(input, "Sites[0].ConsumptionTotalKwh");
    }

    [Fact]
    public void CurrentRatesCannotBeNegative()
    {
        var input = ValidElectricity();
        input.Sites[0].CurrentDayRate = -1m;
        AssertOnlyError(input, "Sites[0].CurrentDayRate");
    }

    // ── FR-024: contract ──

    [Fact]
    public void StartDateCannotBeBeforeTodayInUkTime()
    {
        var input = ValidElectricity();
        input.Contract.StartDate = Today.AddDays(-1);
        AssertOnlyError(input, "Contract.StartDate");

        input.Contract.StartDate = Today;
        Assert.Empty(Validate(input));
    }

    [Fact]
    public void SiteStartDateOverrideCannotBeInThePast()
    {
        var input = ValidElectricity();
        input.Sites[0].StartDate = Today.AddDays(-1);
        AssertOnlyError(input, "Sites[0].StartDate");
    }

    [Fact]
    public void AtLeastOneTermIsRequired()
    {
        var input = ValidElectricity();
        input.Contract.TermsMonths = [];
        AssertOnlyError(input, "Contract.TermsMonths");
    }

    [Fact]
    public void TermsComeFromTheFixedList()
    {
        var input = ValidElectricity();
        input.Contract.TermsMonths = [12, 18];
        AssertOnlyError(input, "Contract.TermsMonths");
    }

    [Fact]
    public void PaymentMethodIsRequired()
    {
        var input = ValidElectricity();
        input.Contract.PaymentMethod = null;
        AssertOnlyError(input, "Contract.PaymentMethod");
    }

    [Fact]
    public void IncludeOrExcludeNeedsAtLeastOneSupplier()
    {
        var input = ValidElectricity();
        input.Contract.SupplierFilterMode = SupplierFilterMode.Include;
        AssertOnlyError(input, "Contract.SupplierFilterIds");
    }

    // ── FR-025: uplifts ──

    [Fact]
    public void UpliftsCannotBeNegative()
    {
        var input = ValidElectricity();
        input.Contract.DefaultUnitRateUplift = -0.1m;
        AssertOnlyError(input, "Contract.DefaultUnitRateUplift");
    }

    [Fact]
    public void UpliftsHaveAtMostFourDecimalPlaces()
    {
        var input = ValidElectricity();
        input.Contract.DefaultStandingChargeUplift = 1.23456m;
        AssertOnlyError(input, "Contract.DefaultStandingChargeUplift");

        input.Contract.DefaultStandingChargeUplift = 1.2345m;
        Assert.Empty(Validate(input));
    }

    [Fact]
    public void UpliftsAreRequired()
    {
        var input = ValidElectricity();
        input.Contract.DefaultUnitRateUplift = null;
        AssertOnlyError(input, "Contract.DefaultUnitRateUplift");
    }

    // ── FR-029: qualifying percentage ──

    [Fact]
    public void QualifyingTreatmentNeedsAPercentageFromZeroToOneHundred()
    {
        var input = ValidElectricity();
        input.VatCclTreatment = VatCclTreatment.Qualifying;
        AssertOnlyError(input, "QualifyingPercent");

        input.QualifyingPercent = 100.5m;
        AssertOnlyError(input, "QualifyingPercent");

        input.QualifyingPercent = 60m;
        Assert.Empty(Validate(input));
    }

    // ── FR-043: gas ──

    [Fact]
    public void MprnMustBeSixToTenDigits()
    {
        var input = ValidGas();
        input.Sites[0].Meter.Mprn = "12345";
        AssertOnlyError(input, "Sites[0].Meter.Mprn");
    }

    [Fact]
    public void GasNeedsAnAnnualQuantity()
    {
        var input = ValidGas();
        input.Sites[0].GasAnnualQuantityKwh = 0;
        AssertOnlyError(input, "Sites[0].GasAnnualQuantityKwh");
    }

    [Fact]
    public void GasIgnoresElectricityFields()
    {
        var input = ValidGas();
        input.Sites[0].Meter.MpanCore = "not used";
        Assert.Empty(Validate(input));
    }

    // ── FR-070: a meter only once per quote ──

    [Fact]
    public void RepeatedMeterIsRefused()
    {
        var input = ValidElectricity();
        var second = ValidElectricity().Sites[0];
        second.AddressLine1 = "Unit 2, Test Park";
        input.Sites.Add(second);
        AssertOnlyError(input, "Sites[1].Meter.MpanCore");
    }
}

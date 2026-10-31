using System.Text.Json;
using PricingTool.Domain;
using PricingTool.Powwr.Pricing;
using PricingTool.Powwr.Pricing.Dtos;

namespace PricingTool.Tests.Powwr;

// contracts/powwr-uk-pricing-mapping.md, the response side. POWWR's responses are external input
// (constitution Principle III), so anything that can't be trusted is rejected here.
public class PowwrPricingResponseMappingTests
{
    // A made-up response in POWWR's shape, read the way the HTTP client reads it.
    private const string ElectricResponse = """
        {
          "GetElectricRatesResult": {
            "Reference": "PWR-778899",
            "FooterNotes": "Prices from the supplier's pricebook.",
            "Error": null,
            "Rates": [
              {
                "Supplier": "EDF", "Term": "24", "PlanType": "Fixed",
                "StandingCharge": "62.00", "DayUnitrate": "26.912", "NightUnitrate": "17.450",
                "Uplift": "1.5", "StandingChargeUplift": "5",
                "AnnualPrice": "4269.23", "RawAnnualPrice": 4269.2327985,
                "RawBaseAnnualPrice": 3998.0012345, "RawCommission": 271.231564,
                "Commission": "271.23", "OutsidePricingWindow": false,
                "ExtraInfo": "Agent note", "FooterMessage": "Footer note",
                "PricebookVersion": "PB-2026-10-30", "QuoteRateReference": "QRR-1", "Id": 991,
                "ContractEndDate": "2028-11-30"
              },
              { "Supplier": "Valda Energy", "Term": "", "NoQuoteReason": "Meter type not supported" },
              { "Supplier": "Yu Energy", "Term": "60", "StandingCharge": "45.00", "DayUnitrate": "27.0",
                "RawAnnualPrice": 5000.1, "OutsidePricingWindow": true }
            ]
          }
        }
        """;

    private static QuoteResult Read(string json) =>
        JsonSerializer.Deserialize<GetElectricRatesResultWrapper>(json, PowwrJson.Options)!.GetElectricRatesResult!;

    private static QuoteResult WithRates(params Rate[] rates) => new() { Reference = "PWR-1", Rates = [.. rates] };

    private static Rate Priced(string supplier = "EDF", string term = "12", decimal? raw = 1000m) => new()
    {
        Supplier = supplier, Term = term, StandingCharge = "50.00", DayUnitrate = "25.000", RawAnnualPrice = raw,
    };

    [Fact]
    public void RateStringsAreKeptExactlyAndRawDecimalsAreExact()
    {
        var mapped = PowwrPricingMapper.FromQuoteResult(Read(ElectricResponse));
        var edf = mapped.Rates.Single(r => r.Supplier == "EDF");

        Assert.Equal("62.00", edf.StandingCharge);
        Assert.Equal("26.912", edf.DayUnitrate);
        Assert.Equal("17.450", edf.NightUnitrate);
        Assert.Equal(4269.2327985m, edf.RawAnnualPrice);
        Assert.Equal(3998.0012345m, edf.RawBaseAnnualPrice);
        Assert.Equal(271.231564m, edf.RawCommission);
        Assert.Equal(24, edf.TermMonths);
        Assert.Equal("Fixed", edf.PlanType);
        Assert.Equal("PB-2026-10-30", edf.PricebookVersion);
        Assert.Equal(991, edf.Id);
    }

    [Fact]
    public void ReferenceFooterAndAgentOnlyNotesAreKept()
    {
        var mapped = PowwrPricingMapper.FromQuoteResult(Read(ElectricResponse));
        Assert.True(mapped.Succeeded);
        Assert.Equal("PWR-778899", mapped.Reference);
        Assert.Equal("Prices from the supplier's pricebook.", mapped.FooterNotes);
        var edf = mapped.Rates.Single(r => r.Supplier == "EDF");
        Assert.Equal("Agent note", edf.ExtraInfo);
        Assert.Equal("Footer note", edf.FooterMessage);
    }

    [Fact]
    public void NoQuoteReasonBecomesASupplierNoQuote()
    {
        var mapped = PowwrPricingMapper.FromQuoteResult(Read(ElectricResponse));
        var valda = Assert.Single(mapped.NoQuotes);
        Assert.Equal("Valda Energy", valda.SupplierName);
        Assert.Null(valda.TermMonths);
        Assert.Equal("Meter type not supported", valda.Reason);
        Assert.False(valda.IsInvalidResponse);
        Assert.DoesNotContain(mapped.Rates, r => r.Supplier == "Valda Energy");
    }

    [Fact]
    public void NoQuoteWithATermKeepsTheTerm()
    {
        var mapped = PowwrPricingMapper.FromQuoteResult(WithRates(new Rate { Supplier = "EDF", Term = "60", NoQuoteReason = "Term not offered" }));
        Assert.Equal(60, Assert.Single(mapped.NoQuotes).TermMonths);
    }

    [Fact]
    public void OutsidePricingWindowIsFlagged()
    {
        var yu = PowwrPricingMapper.FromQuoteResult(Read(ElectricResponse)).Rates.Single(r => r.Supplier == "Yu Energy");
        Assert.True(yu.OutsidePricingWindow);
    }

    [Fact]
    public void PowwrErrorFailsTheSite()
    {
        var mapped = PowwrPricingMapper.FromQuoteResult(new QuoteResult
        {
            Error = new Error { Message = "The meter couldn't be priced.", ErrorDetail = "Internal detail for the logs" },
        });
        Assert.False(mapped.Succeeded);
        Assert.Equal(ErrorCategory.PowwrError, mapped.ErrorCategory);
        Assert.Equal("The meter couldn't be priced.", mapped.ErrorMessage);
        Assert.Equal("Internal detail for the logs", mapped.ErrorDetail);
        Assert.Empty(mapped.Rates);
    }

    [Fact]
    public void ALicenceRejectionIsACredentialsProblem()
    {
        var mapped = PowwrPricingMapper.FromQuoteResult(new QuoteResult { Error = new Error { Message = "Invalid licence code" } });
        Assert.Equal(ErrorCategory.CredentialsRejected, mapped.ErrorCategory);
    }

    [Fact]
    public void AnEmptyErrorObjectIsNotAnError()
    {
        var result = WithRates(Priced());
        result.Error = new Error();
        var mapped = PowwrPricingMapper.FromQuoteResult(result);
        Assert.True(mapped.Succeeded);
        Assert.Single(mapped.Rates);
    }

    [Fact]
    public void NonNumericTermIsRejectedAsInvalidResponse()
    {
        var mapped = PowwrPricingMapper.FromQuoteResult(WithRates(Priced(term: "two years"), Priced(supplier: "EON")));
        Assert.True(mapped.Succeeded);
        Assert.Equal(["EON"], mapped.Rates.Select(r => r.Supplier));
        var rejected = Assert.Single(mapped.NoQuotes);
        Assert.Equal("EDF", rejected.SupplierName);
        Assert.True(rejected.IsInvalidResponse);
        Assert.NotEmpty(mapped.Warnings);
    }

    [Fact]
    public void MissingRawAnnualPriceMakesTheRateInvalid()
    {
        var mapped = PowwrPricingMapper.FromQuoteResult(WithRates(Priced(raw: null)));
        Assert.Empty(mapped.Rates);
        Assert.True(Assert.Single(mapped.NoQuotes).IsInvalidResponse);
    }

    [Fact]
    public void NegativeRawAnnualPriceMakesTheRateInvalid()
    {
        var mapped = PowwrPricingMapper.FromQuoteResult(WithRates(Priced(raw: -1m)));
        Assert.Empty(mapped.Rates);
        Assert.True(Assert.Single(mapped.NoQuotes).IsInvalidResponse);
    }

    [Fact]
    public void RateStringThatIsNotANumberMakesTheRateInvalid()
    {
        var rate = Priced();
        rate.DayUnitrate = "about 25p";
        var mapped = PowwrPricingMapper.FromQuoteResult(WithRates(rate));
        Assert.Empty(mapped.Rates);
        Assert.True(Assert.Single(mapped.NoQuotes).IsInvalidResponse);
    }

    [Fact]
    public void MissingResultIsAnInvalidResponse()
    {
        var mapped = PowwrPricingMapper.FromQuoteResult(null);
        Assert.False(mapped.Succeeded);
        Assert.Equal(ErrorCategory.InvalidResponse, mapped.ErrorCategory);
    }

    // ── /prompts ──

    [Theory]
    [InlineData(new[] { "Day" }, new[] { Register.Day })]
    [InlineData(new[] { "Unit" }, new[] { Register.Day })]
    [InlineData(new[] { "Standard" }, new[] { Register.Day })]
    [InlineData(new[] { "Day", "Night" }, new[] { Register.Day, Register.Night })]
    [InlineData(new[] { "night", "DAY" }, new[] { Register.Day, Register.Night })]
    [InlineData(new[] { "Day", "Evening and Weekend" }, new[] { Register.Day, Register.Wend })]
    [InlineData(new[] { "Day", "Night", "Weekend" }, new[] { Register.Day, Register.Night, Register.Wend })]
    [InlineData(new[] { "Wend" }, new[] { Register.Wend })]
    public void PromptNamesMapToRegisters(string[] prompts, Register[] expected)
    {
        var response = PowwrPricingMapper.FromPrompts(new Prompts { NumberOfPrompts = prompts.Length, ThePrompts = [.. prompts] });
        Assert.True(response.Succeeded);
        Assert.Equal(expected, response.Registers);
        Assert.Empty(response.UnknownPrompts);
    }

    [Fact]
    public void UnknownPromptsAreReportedNotGuessed()
    {
        var response = PowwrPricingMapper.FromPrompts(new Prompts { NumberOfPrompts = 2, ThePrompts = ["Day", "Seasonal Peak"] });
        Assert.Equal([Register.Day], response.Registers);
        Assert.Equal(["Seasonal Peak"], response.UnknownPrompts);
        Assert.Equal(["Day", "Seasonal Peak"], response.RawPrompts);
    }

    [Fact]
    public void PromptsErrorFails()
    {
        var response = PowwrPricingMapper.FromPrompts(new Prompts { Error = new Error { Message = "MPAN not found" } });
        Assert.False(response.Succeeded);
        Assert.Equal("MPAN not found", response.ErrorMessage);
    }

    // ── /pricechange ──

    [Fact]
    public void PriceChangesMapToSupplierPriceChanges()
    {
        var changes = PowwrPricingMapper.FromPriceChanges(new PriceChangeResult
        {
            PriceChanges =
            [
                new LatestPriceChange
                {
                    Supplier = "EDF", Utility = "electric", HalfHourly = false, Duration = 24, Product = "Fixed",
                    PlanType = "Fixed", Version = "PB-2026-10-30", LastPriceChange = "2026-10-30",
                },
                new LatestPriceChange { Supplier = "British Gas", Utility = "gas", Duration = 12, LastPriceChange = "2026-10-29T06:00:00" },
            ],
        });

        Assert.Equal(2, changes.Count);
        Assert.Equal(new SupplierPriceChange("EDF", "electric", false, 24, "Fixed", "Fixed", "PB-2026-10-30", new DateOnly(2026, 10, 30)), changes[0]);
        Assert.Equal(new DateOnly(2026, 10, 29), changes[1].LastPriceChange);
        Assert.False(changes[1].HalfHourly);
    }

    [Fact]
    public void NoPriceChangesGivesAnEmptyList()
    {
        Assert.Empty(PowwrPricingMapper.FromPriceChanges(null));
    }
}

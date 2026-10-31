using PricingTool.Domain;
using PricingTool.Services.Validation;

namespace PricingTool.Tests.Validation;

public class FieldFormatTests
{
    // ── MPRN ──

    [Theory]
    [InlineData("123456")]       // 6
    [InlineData("1234567")]
    [InlineData("1234567890")]   // 10
    public void MprnIsSixToTenDigits(string mprn)
    {
        Assert.True(Mprn.TryParse(mprn, out var parsed, out _));
        Assert.Equal(mprn, parsed!.Value);
    }

    [Theory]
    [InlineData("12345")]         // too short (quickstart US2-5)
    [InlineData("12345678901")]   // too long
    [InlineData("12345A7")]
    [InlineData("")]
    [InlineData(null)]
    public void OtherMprnsAreRejected(string? mprn)
    {
        Assert.False(Mprn.TryParse(mprn, out _, out var error));
        Assert.NotNull(error);
    }

    // ── Postcode ──

    [Theory]
    [InlineData("zz11aa", "ZZ1 1AA")]
    [InlineData("ZZ1 1AA", "ZZ1 1AA")]
    [InlineData("  zz1   1aa ", "ZZ1 1AA")]
    [InlineData("m224db", "M22 4DB")]
    [InlineData("SW1A1AA", "SW1A 1AA")]
    [InlineData("b11aa", "B1 1AA")]
    public void PostcodesAreNormalisedWithOneSpace(string input, string expected)
    {
        Assert.Equal(expected, Postcode.Normalise(input)?.Value);
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("ZZ1")]          // partial
    [InlineData("1ZZ 1AA")]
    [InlineData("ZZ1 1A")]
    [InlineData("ZZ11 1AAA")]
    public void InvalidPostcodesGiveNull(string? input)
    {
        Assert.Null(Postcode.Normalise(input));
    }

    // ── Company number ──

    [Theory]
    [InlineData("12345678", BusinessType.LimitedCompany)]
    [InlineData("SC123456", BusinessType.LimitedCompany)]
    [InlineData("oc123456", BusinessType.Llp)]
    [InlineData("01234567", BusinessType.Plc)]
    public void CompanyNumberIsEightDigitsOrTwoLettersAndSixDigits(string number, BusinessType type)
    {
        Assert.Null(CompanyNumber.Validate(number, type));
    }

    [Theory]
    [InlineData("1234567")]      // 7
    [InlineData("123456789")]    // 9
    [InlineData("S1234567")]     // one letter
    [InlineData("SCO12345")]     // three letters
    public void BadCompanyNumbersAreRejected(string number)
    {
        Assert.NotNull(CompanyNumber.Validate(number, BusinessType.LimitedCompany));
    }

    [Theory]
    [InlineData(BusinessType.SoleTrader)]
    [InlineData(BusinessType.Partnership)]
    [InlineData(BusinessType.Charity)]
    [InlineData(BusinessType.PublicSector)]
    public void CompanyNumberOnlyAppliesToCompanies(BusinessType type)
    {
        Assert.False(CompanyNumber.AppliesTo(type));
        Assert.NotNull(CompanyNumber.Validate("12345678", type));
        Assert.Null(CompanyNumber.Validate(null, type));
        Assert.Null(CompanyNumber.Validate("  ", type));
    }

    [Fact]
    public void CompanyNumberIsOptional()
    {
        Assert.Null(CompanyNumber.Validate(null, BusinessType.LimitedCompany));
        Assert.Null(CompanyNumber.Validate("", BusinessType.Plc));
    }

    // ── UK phone ──

    [Theory]
    [InlineData("01234 567890", "01234567890")]
    [InlineData("+44 1234 567890", "+441234567890")]
    [InlineData("020 7946 0000", "02079460000")]
    [InlineData("0161 496 000", "0161496000")]     // 10 digits after 0
    public void UkPhonesNormaliseBySpaceRemoval(string input, string expected)
    {
        Assert.True(UkPhone.TryNormalise(input, out var normalised));
        Assert.Equal(expected, normalised);
    }

    [Theory]
    [InlineData("12345")]
    [InlineData("+33 1 23 45 67 89")]
    [InlineData("01234-567890")]
    [InlineData("0123456789012")]
    [InlineData("")]
    [InlineData(null)]
    public void OtherPhonesAreRejected(string? input)
    {
        Assert.False(UkPhone.TryNormalise(input, out _));
    }

    // ── Email ──

    [Theory]
    [InlineData("alex.agent@example.com")]
    [InlineData("accounts@acme-trading.co.uk")]
    public void ValidEmails(string email)
    {
        Assert.True(EmailAddress.IsValid(email));
    }

    [Theory]
    [InlineData("alex")]
    [InlineData("alex@example")]                       // no dotted domain
    [InlineData("Alex Agent <alex@example.com>")]      // a display name isn't an address
    [InlineData("alex@@example.com")]
    [InlineData("")]
    [InlineData(null)]
    public void InvalidEmails(string? email)
    {
        Assert.False(EmailAddress.IsValid(email));
    }
}

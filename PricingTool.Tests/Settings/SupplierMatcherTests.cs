using PricingTool.Services.Settings;

namespace PricingTool.Tests.Settings;

public class SupplierMatcherTests
{
    // Made-up aliases, in the shapes POWWR's examples use.
    private static readonly SupplierAliasEntry[] Aliases =
    [
        new(1, "British Gas", PowwrId: "BRITISHG-001-S", Mpid: "ENRD", LookupName: "British Gas Business"),
        new(6, "Scottish Power", PowwrId: "SCOTTISH-001-S", Mpid: "SCT", LookupName: "Scottish Power"),
        new(3, "E.ON Next", PowwrId: "EONNEXT-001-S", Mpid: null, LookupName: "E.On Next"),
        new(2, "EDF", PowwrId: null, Mpid: "EDFE", LookupName: null),
    ];

    private static SupplierMatch Match(string? powwrId = null, string? mpid = null, string? shortCode = null, string? name = null) =>
        SupplierMatcher.Match(Aliases, powwrId, mpid, shortCode, name);

    [Fact]
    public void BritishGasBusinessFromTheLookupIsBritishGasInPricing()
    {
        var match = Match(powwrId: "BRITISHG-001-S", mpid: "ENRD", name: "British Gas Business");
        Assert.Equal(1, match.SupplierId);
        Assert.Equal("British Gas", match.PricingName);
        Assert.Equal(SupplierMatchedOn.PowwrId, match.MatchedOn);
    }

    [Fact]
    public void PowwrIdIsTriedFirst()
    {
        // The name says Scottish Power, but the POWWR ID wins.
        var match = Match(powwrId: "EONNEXT-001-S", name: "Scottish Power");
        Assert.Equal("E.ON Next", match.PricingName);
        Assert.Equal(SupplierMatchedOn.PowwrId, match.MatchedOn);
    }

    [Fact]
    public void MpidIsTriedNext()
    {
        var match = Match(powwrId: "UNKNOWN-001-S", mpid: "EDFE", name: "Scottish Power");
        Assert.Equal("EDF", match.PricingName);
        Assert.Equal(SupplierMatchedOn.Mpid, match.MatchedOn);
    }

    [Fact]
    public void ShortCodeMatchesTheMpidColumn()
    {
        // Gas meters give a shortCode rather than an mpid.
        var match = Match(shortCode: "SCT");
        Assert.Equal("Scottish Power", match.PricingName);
        Assert.Equal(SupplierMatchedOn.ShortCode, match.MatchedOn);
    }

    [Fact]
    public void NameIsTriedLastIgnoringCase()
    {
        var match = Match(name: "e.on next");
        Assert.Equal("E.ON Next", match.PricingName);
        Assert.Equal(SupplierMatchedOn.Name, match.MatchedOn);
    }

    [Fact]
    public void NoMatchGivesNull()
    {
        var match = Match(powwrId: "NOBODY-001-S", mpid: "ZZZZ", name: "Nobody Energy");
        Assert.Null(match.SupplierId);
        Assert.Null(match.PricingName);
        Assert.Equal(SupplierMatchedOn.None, match.MatchedOn);
    }

    [Fact]
    public void BlankIdentifiersMatchNothing()
    {
        Assert.Equal(SupplierMatchedOn.None, Match(powwrId: "", mpid: " ", name: null).MatchedOn);
    }
}

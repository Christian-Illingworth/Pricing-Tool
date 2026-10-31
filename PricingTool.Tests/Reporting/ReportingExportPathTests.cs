using System.Text;
using PricingTool.Data.Entities;
using PricingTool.Domain;
using PricingTool.Services.Reporting;

namespace PricingTool.Tests.Reporting;

public class ReportingExportPathTests
{
    private static readonly Guid RecordId = Guid.Parse("8d4c6a3e-2f1b-4c9a-9e57-1b2c3d4e5f60");

    [Fact]
    public void PathIsDatedFoldersThenTimeEventAndId()
    {
        var record = new ReportingRecord
        {
            Id = RecordId,
            EventType = ReportingEventType.PricesReturned,
            OccurredUtc = new DateTime(2026, 10, 30, 10, 15, 4, 123, DateTimeKind.Utc),
        };

        Assert.Equal("quotes/2026/10/30/101504123-PricesReturned-8d4c6a3e-2f1b-4c9a-9e57-1b2c3d4e5f60.json", ReportingExportPath.For(record));
    }

    [Fact]
    public void PartsAreZeroPadded()
    {
        var path = ReportingExportPath.For(new DateTime(2027, 1, 2, 3, 4, 5, 6, DateTimeKind.Utc), "Failed", RecordId);
        Assert.StartsWith("quotes/2027/01/02/030405006-Failed-", path);
    }

    [Fact]
    public void FoldersFollowTheUtcDateNotTheUkDate()
    {
        // 23:30 UTC on 15 July is already 16 July in the UK; the export uses UTC.
        var path = ReportingExportPath.For(new DateTime(2026, 7, 15, 23, 30, 0, DateTimeKind.Utc), "PdfGenerated", RecordId);
        Assert.StartsWith("quotes/2026/07/15/233000000-", path);
    }

    [Fact]
    public async Task LocalStoreWritesOverwritesAndDeletes()
    {
        var root = Path.Combine(Path.GetTempPath(), "cu-pricing-tests", Guid.NewGuid().ToString("N"));
        try
        {
            var store = new LocalFolderReportingExportStore(root);
            const string path = "quotes/2026/10/30/101504123-PricesReturned-x.json";

            await store.WriteAsync(path, Encoding.UTF8.GetBytes("{\"v\":1}"), default);
            await store.WriteAsync(path, Encoding.UTF8.GetBytes("{\"v\":2}"), default);

            var file = Path.Combine(root, "quotes", "2026", "10", "30", "101504123-PricesReturned-x.json");
            Assert.Equal("{\"v\":2}", await File.ReadAllTextAsync(file));
            Assert.Single(Directory.GetFiles(Path.GetDirectoryName(file)!));   // no temporary files left

            await store.DeleteAsync(path, default);
            Assert.False(File.Exists(file));
            await store.DeleteAsync(path, default);   // deleting again is fine
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, recursive: true);
        }
    }

    [Theory]
    [InlineData("../outside.json")]
    [InlineData("quotes/../../outside.json")]
    [InlineData("C:/outside.json")]
    [InlineData("quotes\\2026\\x.json")]
    [InlineData("")]
    public async Task LocalStoreRefusesPathsOutsideItsFolder(string path)
    {
        var store = new LocalFolderReportingExportStore(Path.Combine(Path.GetTempPath(), "cu-pricing-tests", "never-written"));
        await Assert.ThrowsAsync<ArgumentException>(() => store.WriteAsync(path, new byte[] { 1 }, default));
    }
}

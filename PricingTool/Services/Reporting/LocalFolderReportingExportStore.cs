using Microsoft.Extensions.Options;
using PricingTool.Options;

namespace PricingTool.Services.Reporting;

// Exports to Storage:ReportingExport:LocalRoot (App_Data/reporting, git-ignored), relative to the
// content root. Each file is written to a temporary name and moved into place, so a reader never sees
// half a record (as Triage's AtomicFile does).
public sealed class LocalFolderReportingExportStore : IReportingExportStore
{
    private readonly string _root;

    public LocalFolderReportingExportStore(IOptions<StorageOptions> options, IHostEnvironment env)
        : this(Path.Combine(env.ContentRootPath, options.Value.ReportingExport.LocalRoot))
    {
    }

    internal LocalFolderReportingExportStore(string root) => _root = Path.GetFullPath(root);

    public async Task WriteAsync(string path, ReadOnlyMemory<byte> json, CancellationToken ct)
    {
        var target = FullPath(path);
        Directory.CreateDirectory(Path.GetDirectoryName(target)!);
        var temporary = $"{target}.tmp-{Guid.NewGuid():N}";
        try
        {
            await File.WriteAllBytesAsync(temporary, json, ct);
            File.Move(temporary, target, overwrite: true);
        }
        finally
        {
            if (File.Exists(temporary)) File.Delete(temporary);
        }
    }

    public Task DeleteAsync(string path, CancellationToken ct)
    {
        var target = FullPath(path);
        if (File.Exists(target)) File.Delete(target);
        return Task.CompletedTask;
    }

    // Paths are built by ReportingExportPath, never from user input; anything else is refused.
    private string FullPath(string path)
    {
        if (string.IsNullOrWhiteSpace(path) || path.Contains('\\') || Path.IsPathRooted(path)
            || path.Split('/').Any(part => part is ".." or "." or ""))
            throw new ArgumentException($"'{path}' isn't a valid export path.", nameof(path));

        var full = Path.GetFullPath(Path.Combine(_root, path));
        if (!full.StartsWith(_root + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException($"'{path}' is outside the export folder.", nameof(path));
        return full;
    }
}

using Microsoft.Build.Framework;
using MsBuildTask = Microsoft.Build.Utilities.Task;

namespace Novolis.Analyzers.Licensing;

/// <summary>
/// MSBuild task: validates the project's own <c>PackageLicenseExpression</c> (<c>NOV3001</c>).
/// </summary>
public sealed class ValidateOwnPackageLicenseTask : MsBuildTask
{
    /// <summary>Whether the project is packable.</summary>
    public bool IsPackable { get; set; }

    /// <summary>Value of PackageLicenseExpression.</summary>
    public string? PackageLicenseExpression { get; set; }

    /// <summary>When false, skip the check.</summary>
    public bool Enabled { get; set; } = true;

    /// <inheritdoc />
    public override bool Execute()
    {
        var finding = SafeLicenseValidator.ValidateOwnPackage(IsPackable, PackageLicenseExpression, Enabled);
        if (finding is null)
            return true;

        Log.LogError(
            subcategory: null,
            errorCode: finding.Code,
            helpKeyword: null,
            file: null,
            lineNumber: 0,
            columnNumber: 0,
            endLineNumber: 0,
            endColumnNumber: 0,
            message: finding.Message);
        return false;
    }
}

/// <summary>
/// MSBuild task: validates dependency package licenses from nuspec files (<c>NOV3002</c>/<c>NOV3003</c>).
/// </summary>
public sealed class ValidateDependencyLicensesTask : MsBuildTask
{
    /// <summary>When false, skip the check.</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>When true, unknown/file licenses are errors; otherwise warnings.</summary>
    public bool StrictUnknown { get; set; }

    /// <summary>NuGet packages folder (global packages or project packages path).</summary>
    public string? PackagesPath { get; set; }

    /// <summary>Package identities as "Id|Version" items.</summary>
    public ITaskItem[]? Packages { get; set; }

    /// <summary>Exempt package ids (optional metadata Version).</summary>
    public ITaskItem[]? ExemptPackages { get; set; }

    /// <inheritdoc />
    public override bool Execute()
    {
        if (!Enabled)
            return true;

        if (string.IsNullOrWhiteSpace(PackagesPath) || Packages is null || Packages.Length == 0)
            return true;

        var exempt = BuildExemptSet(ExemptPackages);
        var ok = true;

        foreach (var item in Packages)
        {
            var identity = item.ItemSpec;
            if (string.IsNullOrWhiteSpace(identity))
                continue;

            string packageId;
            string packageVersion;
            var parts = identity.Split('|');
            if (parts.Length >= 2)
            {
                packageId = parts[0].Trim();
                packageVersion = parts[1].Trim();
            }
            else
            {
                packageId = item.GetMetadata("PackageId");
                packageVersion = item.GetMetadata("PackageVersion");
                if (string.IsNullOrEmpty(packageId))
                    packageId = identity;
                if (string.IsNullOrEmpty(packageVersion))
                    packageVersion = item.GetMetadata("Version");
            }

            if (string.IsNullOrEmpty(packageId) || string.IsNullOrEmpty(packageVersion))
                continue;

            if (IsExempt(exempt, packageId, packageVersion))
                continue;

            var nuspecPath = FindNuspec(PackagesPath!, packageId, packageVersion);
            var license = string.IsNullOrEmpty(nuspecPath)
                ? new NuspecLicenseReader.LicenseInfo(NuspecLicenseReader.LicenseKind.Missing, null)
                : NuspecLicenseReader.ReadFromFile(nuspecPath!);

            var finding = SafeLicenseValidator.ValidateDependency(
                packageId,
                packageVersion,
                license,
                isExempt: false);

            if (finding is null)
                continue;

            if (finding.Code == SafeLicenseValidator.DisallowedDependencyCode)
            {
                Log.LogError(
                    subcategory: null,
                    errorCode: finding.Code,
                    helpKeyword: null,
                    file: null,
                    lineNumber: 0,
                    columnNumber: 0,
                    endLineNumber: 0,
                    endColumnNumber: 0,
                    message: finding.Message);
                ok = false;
            }
            else if (StrictUnknown)
            {
                Log.LogError(
                    subcategory: null,
                    errorCode: finding.Code,
                    helpKeyword: null,
                    file: null,
                    lineNumber: 0,
                    columnNumber: 0,
                    endLineNumber: 0,
                    endColumnNumber: 0,
                    message: finding.Message);
                ok = false;
            }
            else
            {
                Log.LogWarning(
                    subcategory: null,
                    warningCode: finding.Code,
                    helpKeyword: null,
                    file: null,
                    lineNumber: 0,
                    columnNumber: 0,
                    endLineNumber: 0,
                    endColumnNumber: 0,
                    message: finding.Message);
            }
        }

        return ok;
    }

    private static HashSet<(string Id, string? Version)> BuildExemptSet(ITaskItem[]? items)
    {
        var set = new HashSet<(string Id, string? Version)>(ExemptComparer.Instance);
        if (items is null)
            return set;

        foreach (var item in items)
        {
            var id = item.ItemSpec;
            if (string.IsNullOrWhiteSpace(id))
                continue;

            var version = item.GetMetadata("Version");
            if (string.IsNullOrWhiteSpace(version))
                version = null;

            set.Add((id.Trim(), version));
        }

        return set;
    }

    private static bool IsExempt(HashSet<(string Id, string? Version)> exempt, string packageId, string packageVersion)
    {
        if (exempt.Contains((packageId, null)))
            return true;

        return exempt.Contains((packageId, packageVersion));
    }

    private static string? FindNuspec(string packagesPath, string packageId, string packageVersion)
    {
        // NuGet folder layout: {packages}/{id}/{version}/{id}.nuspec (id lowercased)
        var idLower = packageId.ToLowerInvariant();
        var candidate = Path.Combine(packagesPath, idLower, packageVersion, idLower + ".nuspec");
        if (File.Exists(candidate))
            return candidate;

        // Alternate: {id}.{version}.nuspec beside the folder
        var alt = Path.Combine(packagesPath, idLower, packageVersion, packageId + ".nuspec");
        if (File.Exists(alt))
            return alt;

        if (!Directory.Exists(Path.Combine(packagesPath, idLower, packageVersion)))
            return null;

        var matches = Directory.GetFiles(
            Path.Combine(packagesPath, idLower, packageVersion),
            "*.nuspec",
            SearchOption.TopDirectoryOnly);
        return matches.FirstOrDefault();
    }

    private sealed class ExemptComparer : IEqualityComparer<(string Id, string? Version)>
    {
        public static readonly ExemptComparer Instance = new();

        public bool Equals((string Id, string? Version) x, (string Id, string? Version) y) =>
            StringComparer.OrdinalIgnoreCase.Equals(x.Id, y.Id)
            && string.Equals(x.Version, y.Version, StringComparison.OrdinalIgnoreCase);

        public int GetHashCode((string Id, string? Version) obj) =>
            StringComparer.OrdinalIgnoreCase.GetHashCode(obj.Id)
            ^ (obj.Version is null ? 0 : StringComparer.OrdinalIgnoreCase.GetHashCode(obj.Version));
    }
}

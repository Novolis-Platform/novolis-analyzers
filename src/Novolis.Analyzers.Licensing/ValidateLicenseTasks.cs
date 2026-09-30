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

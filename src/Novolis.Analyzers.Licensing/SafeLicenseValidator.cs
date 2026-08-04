namespace Novolis.Analyzers.Licensing;

/// <summary>
/// Shared validation used by MSBuild tasks and unit tests.
/// </summary>
public static class SafeLicenseValidator
{
    /// <summary>Diagnostic code for own-package license failures.</summary>
    public const string OwnPackageCode = "NOV3001";

    /// <summary>Diagnostic code for unknown / file-only dependency licenses.</summary>
    public const string UnknownDependencyCode = "NOV3002";

    /// <summary>Diagnostic code for disallowed dependency SPDX expressions.</summary>
    public const string DisallowedDependencyCode = "NOV3003";

    /// <summary>A license validation finding.</summary>
    public sealed class Finding
    {
        /// <summary>Creates a finding.</summary>
        public Finding(string code, string message)
        {
            Code = code;
            Message = message;
        }

        /// <summary>NOV3xxx code.</summary>
        public string Code { get; }

        /// <summary>Human-readable message.</summary>
        public string Message { get; }
    }

    /// <summary>
    /// Validates the project's own <c>PackageLicenseExpression</c>.
    /// </summary>
    public static Finding? ValidateOwnPackage(bool isPackable, string? packageLicenseExpression, bool enabled)
    {
        if (!enabled || !isPackable)
            return null;

        if (SafeLicenseExpression.IsMissing(packageLicenseExpression))
        {
            return new Finding(
                OwnPackageCode,
                "Packable projects must set PackageLicenseExpression to MIT or Apache-2.0 (or MIT OR Apache-2.0).");
        }

        if (!SafeLicenseExpression.IsAllowed(packageLicenseExpression))
        {
            return new Finding(
                OwnPackageCode,
                $"PackageLicenseExpression '{packageLicenseExpression}' is not allowed. Use MIT or Apache-2.0 only.");
        }

        return null;
    }

    /// <summary>
    /// Validates a single dependency package license.
    /// </summary>
    public static Finding? ValidateDependency(
        string packageId,
        string packageVersion,
        NuspecLicenseReader.LicenseInfo license,
        bool isExempt)
    {
        if (isExempt)
            return null;

        if (license.Kind == NuspecLicenseReader.LicenseKind.Expression)
        {
            if (SafeLicenseExpression.IsAllowed(license.Value))
                return null;

            return new Finding(
                DisallowedDependencyCode,
                $"Package {packageId} {packageVersion} license '{license.Value}' is not MIT or Apache-2.0. Add NovolisSafeLicensePackage to exempt deliberately.");
        }

        var detail = license.Kind == NuspecLicenseReader.LicenseKind.File
            ? "file-based license (no SPDX expression)"
            : "missing SPDX license expression";

        return new Finding(
            UnknownDependencyCode,
            $"Package {packageId} {packageVersion} has {detail}. Set NovolisSafeLicenseStrict=false to warn only, or add NovolisSafeLicensePackage.");
    }
}

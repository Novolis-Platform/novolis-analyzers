using System.Xml.Linq;

namespace Novolis.Analyzers.Licensing;

/// <summary>
/// Reads license metadata from a NuGet <c>.nuspec</c> file.
/// </summary>
public static class NuspecLicenseReader
{
    /// <summary>Kind of license metadata found in a nuspec.</summary>
    public enum LicenseKind
    {
        /// <summary>No SPDX expression (missing or legacy licenseUrl only).</summary>
        Missing,

        /// <summary>SPDX license expression.</summary>
        Expression,

        /// <summary>License shipped as a file inside the package.</summary>
        File,
    }

    /// <summary>Parsed license metadata from a nuspec.</summary>
    public readonly struct LicenseInfo
    {
        /// <summary>Creates a license info value.</summary>
        public LicenseInfo(LicenseKind kind, string? value)
        {
            Kind = kind;
            Value = value;
        }

        /// <summary>License kind.</summary>
        public LicenseKind Kind { get; }

        /// <summary>Expression text, file name, or legacy URL.</summary>
        public string? Value { get; }
    }

    /// <summary>
    /// Parses license information from nuspec XML text.
    /// </summary>
    public static LicenseInfo ReadFromXml(string nuspecXml)
    {
        var doc = XDocument.Parse(nuspecXml);
        XNamespace ns = doc.Root?.Name.Namespace ?? XNamespace.None;
        var metadata = doc.Root?.Element(ns + "metadata");
        if (metadata is null)
            return new LicenseInfo(LicenseKind.Missing, null);

        var license = metadata.Element(ns + "license");
        if (license is not null)
        {
            var type = (string?)license.Attribute("type");
            var value = (license.Value ?? string.Empty).Trim();
            if (string.Equals(type, "expression", StringComparison.OrdinalIgnoreCase))
                return new LicenseInfo(LicenseKind.Expression, value);

            if (string.Equals(type, "file", StringComparison.OrdinalIgnoreCase))
                return new LicenseInfo(LicenseKind.File, value);

            if (!string.IsNullOrEmpty(value))
                return new LicenseInfo(LicenseKind.Expression, value);
        }

        var licenseUrl = metadata.Element(ns + "licenseUrl");
        if (licenseUrl is not null && !string.IsNullOrWhiteSpace(licenseUrl.Value))
            return new LicenseInfo(LicenseKind.Missing, licenseUrl.Value.Trim());

        return new LicenseInfo(LicenseKind.Missing, null);
    }

    /// <summary>
    /// Reads license information from a nuspec file path.
    /// </summary>
    public static LicenseInfo ReadFromFile(string nuspecPath)
    {
        if (!File.Exists(nuspecPath))
            return new LicenseInfo(LicenseKind.Missing, null);

        return ReadFromXml(File.ReadAllText(nuspecPath));
    }
}

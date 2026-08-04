using Novolis.Analyzers.Licensing;
using TUnit.Core;

namespace Novolis.Analyzers.Tests.Licensing;

public sealed class SafeLicenseTests
{
    [Test]
    [Arguments("MIT", true)]
    [Arguments("Apache-2.0", true)]
    [Arguments("MIT OR Apache-2.0", true)]
    [Arguments("(MIT OR Apache-2.0)", true)]
    [Arguments("apache-2.0 OR mit", true)]
    [Arguments("GPL-3.0", false)]
    [Arguments("MIT OR GPL-3.0", false)]
    [Arguments("MIT AND Apache-2.0", false)]
    [Arguments("MIT WITH LLVM-exception", false)]
    [Arguments("", false)]
    [Arguments(null, false)]
    public async Task IsAllowed_MatchesPolicy(string? expression, bool expected)
    {
        await Assert.That(SafeLicenseExpression.IsAllowed(expression)).IsEqualTo(expected);
    }

    [Test]
    public async Task ValidateOwnPackage_RejectsGpl()
    {
        var finding = SafeLicenseValidator.ValidateOwnPackage(isPackable: true, "GPL-3.0", enabled: true);
        await Assert.That(finding).IsNotNull();
        await Assert.That(finding!.Code).IsEqualTo("NOV3001");
    }

    [Test]
    public async Task ValidateOwnPackage_AllowsMit()
    {
        var finding = SafeLicenseValidator.ValidateOwnPackage(isPackable: true, "MIT", enabled: true);
        await Assert.That(finding).IsNull();
    }

    [Test]
    public async Task ValidateOwnPackage_SkipsWhenNotPackable()
    {
        var finding = SafeLicenseValidator.ValidateOwnPackage(isPackable: false, "GPL-3.0", enabled: true);
        await Assert.That(finding).IsNull();
    }

    [Test]
    public async Task ReadNuspec_Expression()
    {
        const string xml = """
                           <?xml version="1.0"?>
                           <package xmlns="http://schemas.microsoft.com/packaging/2013/05/nuspec.xsd">
                             <metadata>
                               <id>Sample</id>
                               <version>1.0.0</version>
                               <license type="expression">MIT</license>
                             </metadata>
                           </package>
                           """;

        var info = NuspecLicenseReader.ReadFromXml(xml);
        await Assert.That(info.Kind).IsEqualTo(NuspecLicenseReader.LicenseKind.Expression);
        await Assert.That(info.Value).IsEqualTo("MIT");
    }

    [Test]
    public async Task ReadNuspec_FileLicense()
    {
        const string xml = """
                           <?xml version="1.0"?>
                           <package xmlns="http://schemas.microsoft.com/packaging/2013/05/nuspec.xsd">
                             <metadata>
                               <id>Sample</id>
                               <version>1.0.0</version>
                               <license type="file">LICENSE.txt</license>
                             </metadata>
                           </package>
                           """;

        var info = NuspecLicenseReader.ReadFromXml(xml);
        await Assert.That(info.Kind).IsEqualTo(NuspecLicenseReader.LicenseKind.File);
    }

    [Test]
    public async Task ValidateDependency_DisallowsGpl()
    {
        var license = new NuspecLicenseReader.LicenseInfo(
            NuspecLicenseReader.LicenseKind.Expression,
            "GPL-3.0-only");
        var finding = SafeLicenseValidator.ValidateDependency("Bad.Pkg", "1.0.0", license, isExempt: false);
        await Assert.That(finding).IsNotNull();
        await Assert.That(finding!.Code).IsEqualTo("NOV3003");
    }

    [Test]
    public async Task ValidateDependency_UnknownIsNov3002()
    {
        var license = new NuspecLicenseReader.LicenseInfo(NuspecLicenseReader.LicenseKind.Missing, null);
        var finding = SafeLicenseValidator.ValidateDependency("Old.Pkg", "1.0.0", license, isExempt: false);
        await Assert.That(finding).IsNotNull();
        await Assert.That(finding!.Code).IsEqualTo("NOV3002");
    }

    [Test]
    public async Task ValidateDependency_ExemptSkips()
    {
        var license = new NuspecLicenseReader.LicenseInfo(
            NuspecLicenseReader.LicenseKind.Expression,
            "GPL-3.0-only");
        var finding = SafeLicenseValidator.ValidateDependency("Bad.Pkg", "1.0.0", license, isExempt: true);
        await Assert.That(finding).IsNull();
    }
}

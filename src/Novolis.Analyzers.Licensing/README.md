<!-- novolis-pkg-brand:start -->
<p align="center">
  <a href="https://github.com/Novolis-Platform/novolis-analyzers">
    <img src="https://raw.githubusercontent.com/Novolis-Platform/.github/main/brand/logo-icon.svg" width="72" alt="Novolis"/>
  </a>
</p>
<!-- novolis-pkg-brand:end -->

# Novolis.Analyzers.Licensing

MSBuild tasks that enforce Novolis **safe licenses**: MIT and Apache-2.0 only (own package + dependencies).

| Code | Check |
|------|-------|
| `NOV3001` | Packable projects: `PackageLicenseExpression` must be `MIT`, `Apache-2.0`, or `MIT OR Apache-2.0` |
| `NOV3002` | Dependency missing / file-only license (warning; error when `NovolisSafeLicenseStrict=true`) |
| `NOV3003` | Dependency SPDX expression not on the allowlist (error) |

## Install

```xml
<PackageReference Include="Novolis.Analyzers.Licensing" Version="2026.1.*" PrivateAssets="all" />
```

Targets flow via `build` / `buildTransitive`.

## Options

```xml
<PropertyGroup>
  <!-- Opt out entirely -->
  <NovolisSafeLicenseCheck>false</NovolisSafeLicenseCheck>
  <!-- Treat unknown dependency licenses as errors -->
  <NovolisSafeLicenseStrict>true</NovolisSafeLicenseStrict>
</PropertyGroup>

<!-- Deliberate exceptions -->
<ItemGroup>
  <NovolisSafeLicensePackage Include="Some.PackageId" />
  <NovolisSafeLicensePackage Include="Other.Package" Version="1.2.3" />
</ItemGroup>
```

## Related packages

| Package | When to use |
|---------|-------------|
| `Novolis.Analyzers.StackBoundaries` | Layer / island rules |
| `Novolis.Analyzers.Conventions` | Naming conventions |

## More documentation

- [Design](https://github.com/Novolis-Platform/novolis-analyzers/blob/main/docs/design.md)
- [Package policy](https://github.com/Novolis-Platform/novolis-governance/blob/main/docs/package-policy.md)

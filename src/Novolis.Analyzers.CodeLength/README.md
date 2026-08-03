<!-- novolis-pkg-brand:start -->
<p align="center">
  <a href="https://github.com/Novolis-Platform/novolis-analyzers">
    <img src="https://raw.githubusercontent.com/Novolis-Platform/.github/main/brand/logo-icon.svg" width="72" alt="Novolis"/>
  </a>
</p>
<!-- novolis-pkg-brand:end -->

# Novolis.Analyzers.CodeLength

Roslyn analyzers that warn when classes or methods exceed configurable line limits (`FRANK1010`, `FRANK1011`).

## Install

```bash
dotnet add package Novolis.Analyzers.CodeLength
```

**Prerequisites:** [.NET SDK](https://dotnet.microsoft.com/download) (analyzer targets `netstandard2.0`).

## Quick start

```xml
<!-- Optional: override defaults in Directory.Build.props -->
<PropertyGroup>
  <MaxCodeLength>5</MaxCodeLength>
</PropertyGroup>
```

Adjust thresholds at runtime via `CodeLengthSettings.ClassMaxLines` and `CodeLengthSettings.MethodMaxLines` in analyzer configuration if needed.

## Related packages

| Package | When to use |
|---------|-------------|
| `Novolis.Analyzers.StackBoundaries` | Stack layering and numerics rules |
| `Novolis.Analyzers.AutoMapper` | AutoMapper `Map<>` diagnostics |

## More documentation

- [Getting started](https://github.com/Novolis-Platform/novolis-analyzers/blob/main/docs/getting-started.md)
- [Design](https://github.com/Novolis-Platform/novolis-analyzers/blob/main/docs/design.md)

## Support

Pre-release. Thresholds and diagnostic IDs may change.


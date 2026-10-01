<!-- novolis-pkg-brand:start -->
[![Novolis](https://raw.githubusercontent.com/Novolis-Platform/.github/main/brand/logo-icon.png)](https://novolis-platform.github.io/.github/novolis-analyzers/)

[Novolis](https://github.com/Novolis-Platform) · [Docs](https://novolis-platform.github.io/.github/novolis-analyzers/) · [Source](https://github.com/Novolis-Platform/novolis-analyzers)
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

## More documentation

- [Getting started](https://github.com/Novolis-Platform/novolis-analyzers/blob/main/docs/getting-started.md)
- [Design](https://github.com/Novolis-Platform/novolis-analyzers/blob/main/docs/design.md)

## Support

Pre-release. Thresholds and diagnostic IDs may change.


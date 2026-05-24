# Novolis.Analyzers.AutoMapper

Roslyn analyzer and code fix for AutoMapper `Map<>` generic usage (`AUTO001`).

## Install

```bash
dotnet add package Novolis.Analyzers.AutoMapper
```

**Prerequisites:** [.NET SDK](https://dotnet.microsoft.com/download) (analyzer targets `netstandard2.0`; consuming projects may use any supported TFM).

## Quick start

Reference the package; the analyzer runs automatically in the compiler pipeline. Fix `AUTO001` with the **Add missing type argument** code fix when offered.

## Related packages

| Package | When to use |
|---------|-------------|
| `Novolis.Analyzers.CodeLength` | Class/method line-count limits |
| `Novolis.Analyzers.StackBoundaries` | Novolis stack boundary rules |

## More documentation

- [Getting started](https://github.com/Novolis-Platform/novolis-analyzers/blob/main/docs/getting-started.md)
- [Design](https://github.com/Novolis-Platform/novolis-analyzers/blob/main/docs/design.md)

## Support

Pre-release. Analyzer APIs and rule IDs may change between releases.

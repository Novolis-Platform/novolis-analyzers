# Novolis.Analyzers.StackBoundaries

Roslyn analyzer enforcing Novolis stack rules: BCL numerics, no `Vector2`, camera placement, and Raylib/Simulation/rendering reference boundaries (`NOV2001`–`NOV2005`).

## Install

```bash
dotnet add package Novolis.Analyzers.StackBoundaries
```

**Prerequisites:** [.NET SDK](https://dotnet.microsoft.com/download) (analyzer targets `netstandard2.0`).

## Quick start

Import via `Novolis.StackAnalyzers.props` in governance, or add this package directly to Math, Physics, Simulation, or Raylib projects that must obey stack boundaries.

## Related packages

| Package | When to use |
|---------|-------------|
| `Novolis.Analyzers.CodeLength` | Line-count maintainability rules |
| `Novolis.Analyzers.AutoMapper` | AutoMapper-specific rules |

## More documentation

- [Getting started](https://github.com/Novolis-Platform/novolis-analyzers/blob/main/docs/getting-started.md)
- [Design](https://github.com/Novolis-Platform/novolis-analyzers/blob/main/docs/design.md)

## Support

Pre-release. Rule IDs and covered assemblies may expand as the stack evolves.

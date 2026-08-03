# Novolis.Analyzers.StackBoundaries

Roslyn analyzer enforcing Novolis stack rules: BCL numerics, no `Vector2`, camera placement, Raylib/Simulation/rendering reference boundaries, Avalonia isolation, and closed-spine layer ranks (`NOV2001`–`NOV2007`).

| ID | Rule |
|----|------|
| `NOV2001` | No BCL numerics duplicates |
| `NOV2002` | No `Vector2` in Math/Physics/Simulation |
| `NOV2003` | No `Camera` in Math |
| `NOV2004` | Raylib ↔ Simulation forbidden |
| `NOV2005` | Raylib must not reference rendering scene packages |
| `NOV2006` | Only `Novolis.Avalonia.*` may reference Avalonia UI assemblies |
| `NOV2007` | Math → Physics → Simulation → Gaming → Avalonia (no upward refs) |

## Install

```bash
dotnet add package Novolis.Analyzers.StackBoundaries
```

**Prerequisites:** [.NET SDK](https://dotnet.microsoft.com/download) (analyzer targets `netstandard2.0`).

## Quick start

Import via `Novolis.StackAnalyzers.props` in governance (applies to all `Novolis.*` libraries when the analyzers repo is checked out), or add this package directly. Package-level scan: `pwsh -File d:\novolis\novolis-governance\scripts\verify-layer-boundaries.ps1`.

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

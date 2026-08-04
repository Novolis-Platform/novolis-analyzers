# Novolis analyzers — design

Diagnostic ID ranges and package ownership:

| Range | Package | Kind |
|-------|---------|------|
| `NOV2001`–`NOV2009` | `Novolis.Analyzers.StackBoundaries` | Roslyn — stack / island / Avalonia |
| `NOV2101`–`NOV2102` | `Novolis.Analyzers.Conventions` | Roslyn — naming conventions |
| `NOV3001`–`NOV3003` | `Novolis.Analyzers.Licensing` | MSBuild tasks — safe licenses |
| `FRANK4010`–`FRANK4011` | `Novolis.Analyzers.CodeLength` | Roslyn — line counts (legacy IDs) |
| `AUTO001` | `Novolis.Analyzers.AutoMapper` | Roslyn + fixer (report path currently inactive) |

## StackBoundaries

| ID | Rule |
|----|------|
| `NOV2001` | No BCL numerics duplicates |
| `NOV2002` | No `Vector2` in Math/Physics/Simulation |
| `NOV2003` | No `Camera` in Math |
| `NOV2004` | Raylib ↔ Simulation forbidden |
| `NOV2005` | Raylib must not reference rendering scene packages |
| `NOV2006` | Only `Novolis.Avalonia.*` may reference Avalonia UI assemblies |
| `NOV2007` | Math → Physics → Simulation → Gaming → Avalonia (no upward refs) |
| `NOV2008` | Rendering ↔ Simulation forbidden |
| `NOV2009` | Gaming must not reference Raylib or Rendering |

Local wiring: `novolis-governance/build/Novolis.StackAnalyzers.props` (also loads Conventions).

## Conventions

| ID | Rule | Fixer |
|----|------|-------|
| `NOV2101` | Forbidden whole-word `desk` (`desktop` allowed) | No |
| `NOV2102` | No leftover `Frank.*` namespaces/usings in `Novolis.*` production assemblies | Yes — `Frank` → `Novolis` |

## Licensing

Allowlist: `MIT`, `Apache-2.0`, and `OR` combinations of only those.

| Code | Check |
|------|-------|
| `NOV3001` | Own `PackageLicenseExpression` on packable projects |
| `NOV3002` | Dependency missing / file-only license (warning; error if `NovolisSafeLicenseStrict=true`) |
| `NOV3003` | Dependency SPDX not allowlisted |

Opt out: `NovolisSafeLicenseCheck=false`. Exempt: `NovolisSafeLicensePackage` items.

## Publication

All packable projects must be listed in `Novolis.Analyzers.slnx` so merge/release pack workflows publish them to GitHub Packages / nuget.org.

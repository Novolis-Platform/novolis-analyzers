# Novolis analyzers — design

Diagnostic ID ranges and package ownership:

| Range | Package | Kind |
|-------|---------|------|
| `NOV2001`–`NOV2011` | `Novolis.Analyzers.StackBoundaries` | Roslyn — stack / island / Avalonia / MAUI |
| `NOV2101`–`NOV2102` | `Novolis.Analyzers.Conventions` | Roslyn — naming conventions |
| `NOV2201` | `Novolis.Analyzers.OneTypePerFile` | Roslyn — one top-level type per file, with fixer |
| `NOV2301` | `Novolis.Analyzers.ReadabilityComplexity` | Roslyn — readability-complexity advisory |
| `NOV3001`–`NOV3003` | `Novolis.Analyzers.Licensing` | MSBuild tasks — safe licenses |
| `FRANK4010`–`FRANK4011` | `Novolis.Analyzers.CodeLength` | Roslyn — line counts (legacy IDs) |

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
| `NOV2009` | Gaming must not reference Raylib |
| `NOV2010` | Only `Novolis.Maui.*` may reference Microsoft.Maui assemblies |
| `NOV2011` | MAUI ↔ Avalonia forbidden |

Local wiring: `novolis-governance/build/Novolis.StackAnalyzers.props` (also loads Conventions and OneTypePerFile).

## OneTypePerFile

| ID | Rule | Fixer |
|----|------|-------|
| `NOV2201` | More than one top-level type in a source file. Nested types stay with their parent. Partials of the same type count as one. The type whose name matches the file stays; otherwise the first type stays. | Yes — move each extra type to `{TypeName}.cs`. Fix All covers document, project, and solution. |

Warning severity. `Novolis.OneTypePerFile.props` adds `NOV2201` to `WarningsNotAsErrors`. Opt out: `NovolisOneTypePerFile=false`.

## ReadabilityComplexity

| ID | Rule | Fixer |
|----|------|-------|
| `NOV2301` | Executable body readability-complexity score ≥ soft threshold (default 20). Message includes total and primary contributors. | No |

Warning severity. Wired everywhere via `Novolis.ReadabilityComplexity.props` (imported from `Novolis.OneTypePerFile.props`). Listed in `WarningsNotAsErrors`. Opt out: `NovolisReadabilityComplexity=false`. Threshold: `.editorconfig` key `novolis_readability_complexity_threshold`.

Measures local cognitive bookkeeping (declarations, decisions, consumed results, explicit non-local mutation). Does **not** treat void/command calls, awaits, or physical line count as complexity. Constructor DI stores and capped instance chrome wiring in constructors / init-like members (`InitializeComponent`, `ConfigureServices`, `BuildLayout`, …) are discounted so UI/DI setup does not dominate.

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

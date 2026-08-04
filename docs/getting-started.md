# Getting started

## Packages

| Package | Use |
|---------|-----|
| `Novolis.Analyzers.StackBoundaries` | Layer / Avalonia / island rules (`NOV2001`–`NOV2009`) |
| `Novolis.Analyzers.Conventions` | Forbidden `desk`; no `Frank.*` leftovers (`NOV2101`–`NOV2102`) |
| `Novolis.Analyzers.Licensing` | MIT / Apache-2.0 own + dependency license checks (`NOV3001`–`NOV3003`) |
| `Novolis.Analyzers.CodeLength` | Method/class line limits |
| `Novolis.Analyzers.AutoMapper` | AutoMapper `Map<>` usage |

## Local multi-repo

When `novolis-analyzers` is checked out beside consumers, import:

```xml
<Import Project="..\novolis-governance\build\Novolis.StackAnalyzers.props"
        Condition="Exists('..\novolis-governance\build\Novolis.StackAnalyzers.props')" />
```

That wires **StackBoundaries** and **Conventions** as analyzer `ProjectReference`s for `Novolis.*` libraries.

For license checks (after GPR publish):

```xml
<PackageReference Include="Novolis.Analyzers.Licensing" Version="2026.1.*" PrivateAssets="all" />
```

Or locally import `novolis-governance/build/Novolis.LicensingAnalyzers.props`.

## Design

See [design.md](design.md) for the full diagnostic ID table.

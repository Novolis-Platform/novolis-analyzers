<!-- novolis-pkg-brand:start -->
<p align="center">
  <a href="https://github.com/Novolis-Platform/novolis-analyzers">
    <img src="https://raw.githubusercontent.com/Novolis-Platform/.github/main/brand/logo-icon.svg" width="72" alt="Novolis"/>
  </a>
</p>
<!-- novolis-pkg-brand:end -->

# Novolis.Analyzers.Conventions

Roslyn analyzers for Novolis naming conventions (`NOV2101`–`NOV2102`).

| ID | Rule | Fixer |
|----|------|-------|
| `NOV2101` | Forbidden whole-word `desk` (identifiers/strings error; comments warning). `desktop` allowed. | No |
| `NOV2102` | No leftover `Frank.*` namespaces/usings in `Novolis.*` production assemblies | Yes — rename `Frank` → `Novolis` |

## Install

```bash
dotnet add package Novolis.Analyzers.Conventions
```

**Prerequisites:** [.NET SDK](https://dotnet.microsoft.com/download) (analyzer targets `netstandard2.0`).

## Quick start

Import via `Novolis.StackAnalyzers.props` in governance when the analyzers repo is checked out, or add this package directly.

## Related packages

| Package | When to use |
|---------|-------------|
| `Novolis.Analyzers.StackBoundaries` | Layer / Avalonia / island rules |
| `Novolis.Analyzers.Licensing` | MIT / Apache-2.0 license checks |

## More documentation

- [Design](https://github.com/Novolis-Platform/novolis-analyzers/blob/main/docs/design.md)

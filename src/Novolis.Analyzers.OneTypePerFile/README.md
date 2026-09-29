<!-- novolis-pkg-brand:start -->
<p align="center">
  <a href="https://github.com/Novolis-Platform/novolis-analyzers">
    <img src="https://raw.githubusercontent.com/Novolis-Platform/.github/main/brand/logo-icon.svg" width="72" alt="Novolis"/>
  </a>
</p>
<!-- novolis-pkg-brand:end -->

# Novolis.Analyzers.OneTypePerFile

Roslyn analyzer and fixer for one top-level type per source file (`NOV2201`).

| ID | Rule | Fixer |
|----|------|-------|
| `NOV2201` | A source file declares more than one top-level type (class, record, struct, interface, enum, or delegate). Nested types stay with their parent. Partials of the same type count as one. | Yes — move each extra type into `{TypeName}.cs` |

The check is syntax-only, so file size does not change the rule. The fixer keeps the type whose name matches the file when one does, otherwise the first type. Fix All applies that move to the current document, the project, or the whole solution.

`NOV2201` is a warning. Platform builds list it in `WarningsNotAsErrors` so existing files keep compiling while the fixer is available.

## Install

```bash
dotnet add package Novolis.Analyzers.OneTypePerFile
```

**Prerequisites:** [.NET SDK](https://dotnet.microsoft.com/download) (analyzer targets `netstandard2.0`).

## Quick start

When `novolis-analyzers` is checked out beside a repo, `Novolis.OneTypePerFile.props` (imported directly, or through `Novolis.StackAnalyzers.props`) adds the analyzer project reference. Opt out with `<NovolisOneTypePerFile>false</NovolisOneTypePerFile>`.

## Related packages

| Package | When to use |
|---------|-------------|
| `Novolis.Analyzers.Conventions` | Forbidden `desk`; no leftover `Frank.*` |
| `Novolis.Analyzers.StackBoundaries` | Layer / Avalonia / island rules |

## More documentation

- [Design](https://github.com/Novolis-Platform/novolis-analyzers/blob/main/docs/design.md)

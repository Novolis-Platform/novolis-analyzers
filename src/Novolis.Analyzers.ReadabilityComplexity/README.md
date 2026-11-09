<!-- novolis-pkg-brand:start -->
[![Novolis](https://raw.githubusercontent.com/Novolis-Platform/.github/main/brand/logo-icon.png)](https://novolis-platform.github.io/.github/novolis-analyzers/)

[Novolis](https://github.com/Novolis-Platform) · [Docs](https://novolis-platform.github.io/.github/novolis-analyzers/) · [Source](https://github.com/Novolis-Platform/novolis-analyzers)
<!-- novolis-pkg-brand:end -->

# Novolis.Analyzers.ReadabilityComplexity

Roslyn analyzer that estimates how much mental bookkeeping a C# method requires (`NOV2301`).

It is **not** cyclomatic complexity, cognitive complexity, or a maintainability index. Command-style calls without consumed results are free. Explicit mutation of non-local / argument-owned state is expensive. Declarative switch expressions are cheap; pure dispatch switch statements stay cheap.

| ID | Rule | Fixer |
|----|------|-------|
| `NOV2301` | Executable body readability-complexity score ≥ advisory threshold (default **20**) | No — feedback only; mechanical “fixes” would game the metric |

Warning severity. Platform builds list `NOV2301` in `WarningsNotAsErrors` so it never becomes a hard gate while the heuristic is validated.

## Scoring (Phase 1)

| Construct | Points |
|---|---:|
| Local declaration | 1 |
| Local reassignment | 1 |
| `if` / `else if` | 1 (plain `else` is free) |
| Switch expression | 1 |
| Switch statement | 1 if every branch is pure dispatch; otherwise 1 per section |
| `for` / `foreach` / `while` / `do` | 2 |
| `catch` | 2 |
| `throw` | 1 |
| Consumed method/await result (top-level consumption site) | 1 |
| Void / fire-and-forget invocation | 0 |
| First explicit mutation of a distinct **external** target (parameter / foreign object) | 10 |
| Further mutations of the same external target | 1 |
| Mutation of a locally owned `new` / factory / field-alias object | 0 |
| Constructor DI store (`_repo = repo` / `repo ?? throw …`) | 0 |
| `ArgumentNullException` / `ArgumentException` guard throws | 0 |
| Event hookup (`Click += …`) | 0 |
| Own-instance / owned-control surface stamps (`_flag = …`, `_btn.IsEnabled = …`) | 1 each, capped at **10** |
| Constructor / init-like chrome wiring | 1 each, capped at **5** (further self-writes free) |

Init-like members (narrow list): `InitializeComponent`, `ConfigureServices`, `Main`, `OnCreate`, `OnFrameworkInitializationCompleted`, `Refresh*`, `BuildLayout` / `Build*Panel` / …, `Create*View` / `Create*Layout`, etc. Bare `Build` / `Initialize` are **not** treated as init-like.

Nested lambdas and local functions are scored on their own bodies, not folded into the parent.

## Install

```bash
dotnet add package Novolis.Analyzers.ReadabilityComplexity
```

**Prerequisites:** [.NET SDK](https://dotnet.microsoft.com/download) (analyzer targets `netstandard2.0`).

## Quick start

When `novolis-analyzers` is checked out beside a repo, `Novolis.ReadabilityComplexity.props` (imported through `Novolis.OneTypePerFile.props` / `Novolis.StackAnalyzers.props`) adds the analyzer project reference. Opt out with `<NovolisReadabilityComplexity>false</NovolisReadabilityComplexity>`.

Raise or lower the soft threshold in `.editorconfig`:

```editorconfig
novolis_readability_complexity_threshold = 20
```

## Related packages

| Package | When to use |
|---------|-------------|
| `Novolis.Analyzers.CodeLength` | Semicolon / line-count heuristics |
| `Novolis.Analyzers.OneTypePerFile` | One top-level type per file |
| `Novolis.Analyzers.Conventions` | Forbidden `desk`; no leftover `Frank.*` |

## More documentation

- [Design](https://github.com/Novolis-Platform/novolis-analyzers/blob/main/docs/design.md)

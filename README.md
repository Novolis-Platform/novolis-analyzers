<!-- novolis-marketing:start -->
<p align="center">
  <a href="https://github.com/Novolis-Platform">
    <img src="https://raw.githubusercontent.com/Novolis-Platform/.github/main/brand/logo-brand-transparent.svg" width="360" alt="Novolis"/>
  </a>
</p>

<p align="center">
  <img src="https://raw.githubusercontent.com/Novolis-Platform/.github/main/brand/banners/novolis-analyzers.svg" width="100%" alt="novolis-analyzers"/>
</p>

<p align="center">
  <strong>Roslyn that enforces the platform</strong><br/>
  Roslyn analyzers for stack boundaries, AutoMapper, and code-length discipline.
</p>

<p align="center">
  <a href="https://github.com/Novolis-Platform/novolis-analyzers/actions"><img src="https://img.shields.io/github/actions/workflow/status/Novolis-Platform/novolis-analyzers/merge.yml?branch=main&label=merge&logo=github" alt="merge"/></a>
  <a href="https://github.com/orgs/Novolis-Platform/packages?repo_name=novolis-analyzers"><img src="https://img.shields.io/badge/packages-GitHub%20Packages-0a7ea3?logo=nuget" alt="packages"/></a>
  <a href="https://github.com/Novolis-Platform"><img src="https://img.shields.io/badge/org-Novolis--Platform-111827" alt="org"/></a>
</p>

<p align="center">
  <a href="https://nuget.pkg.github.com/Novolis-Platform/index.json"><code>https://nuget.pkg.github.com/Novolis-Platform/index.json</code></a>
  ·
  <a href="https://github.com/Novolis-Platform/.github/blob/main/profile/README.md">Org landing</a>
  ·
  <a href="https://github.com/Novolis-Platform/novolis-governance">Governance</a>
</p>

---
<!-- novolis-marketing:end -->
<!-- novolis-package-index:start -->
> **GitHub Packages shows this repository README on every package page** (upstream limitation).
> Open the **package README** for install and quick start — embedded in each .nupkg and linked below.

## Published packages

| Package | Install | Package README |
|---------|---------|----------------|
| `Novolis.Analyzers.AutoMapper` | `dotnet add package Novolis.Analyzers.AutoMapper` | [README](https://github.com/Novolis-Platform/novolis-analyzers/blob/main/src/Novolis.Analyzers.AutoMapper/README.md) |
| `Novolis.Analyzers.CodeLength` | `dotnet add package Novolis.Analyzers.CodeLength` | [README](https://github.com/Novolis-Platform/novolis-analyzers/blob/main/src/Novolis.Analyzers.CodeLength/README.md) |
| `Novolis.Analyzers.StackBoundaries` | `dotnet add package Novolis.Analyzers.StackBoundaries` | [README](https://github.com/Novolis-Platform/novolis-analyzers/blob/main/src/Novolis.Analyzers.StackBoundaries/README.md) |

For NuGet.org and Visual Studio, the **embedded** README.md inside each package is authoritative.

<!-- novolis-package-index:end -->
# novolis-analyzers

Roslyn analyzers enforcing Novolis platform conventions (stack boundaries, maintainability, AutoMapper usage).

## Packages

| Package | Description |
|---------|-------------|
| [Novolis.Analyzers.StackBoundaries](src/Novolis.Analyzers.StackBoundaries/README.md) | Math/Physics/Simulation/Raylib reference rules (`NOV2001`–`NOV2005`) |
| [Novolis.Analyzers.CodeLength](src/Novolis.Analyzers.CodeLength/README.md) | Line-count maintainability rules |
| [Novolis.Analyzers.AutoMapper](src/Novolis.Analyzers.AutoMapper/README.md) | AutoMapper-specific diagnostics |

Import via `Novolis.StackAnalyzers.props` from **novolis-governance**, or reference packages directly.

## Install

```bash
dotnet add package Novolis.Analyzers.StackBoundaries
```

Restore from **GitHub Packages** (`2026.1.*`) and **nuget.org** only. Local multi-repo builds: **`Novolis.Platform.slnx`**.

## Quick start

Add the package to a `.csproj`, or import **`Novolis.StackAnalyzers.props`** from novolis-governance for the full analyzer set on Math, Physics, Simulation, and Raylib projects.

## Documentation

- [Getting started](docs/getting-started.md)
- [Design](docs/design.md)
- [Release](docs/release.md)

## Contributing

See [CONTRIBUTING.md](CONTRIBUTING.md).

## Security

See [SECURITY.md](SECURITY.md).


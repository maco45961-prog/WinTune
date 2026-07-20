# WinOptimizer Ecosystem

Open-source Windows optimization toolkit. PowerShell core engine + multiple C# .NET apps.

## Projects

| App | Description | Stack |
|-----|-------------|-------|
| **winoptimizer-core** | Core engine — tweaks, profiles, backup/restore | PowerShell 5.1+ |
| **winoptimizer-app** | Desktop optimizer UI | .NET 8 WPF |
| **winbenchmark-app** | System & Valorant benchmark | .NET 8 WPF |
| **winnettools-app** | Network diagnostics | .NET 8 WPF |
| **winsetupwizard-app** | Fresh Windows setup wizard | .NET 8 WPF |
| **wingamemodedaemon-app** | Background gaming profile daemon | .NET 8 WinForms |

## Release Process

### Prerequisites

- All changes merged to `main`
- CI passing on `main` (lint + test + build)

### Cut a Release

```bash
# 1. Tag the release (all apps share the same version number)
git tag v1.0.0

# 2. Push the tag
git push --tags
```

This triggers **all release workflows in parallel**:

| Workflow | Trigger | What it does |
|----------|---------|-------------|
| `release-core.yml` | `v*` tag | Validates core module, creates release with .zip |
| `release-optimizer.yml` | `v*` tag | Builds WinOptimizer, creates release with .zip |
| `release-benchmark.yml` | `v*` tag | Builds WinBenchmark, creates release with .zip |
| `release-nettools.yml` | `v*` tag | Builds WinNetTools, creates release with .zip |
| `release-setupwizard.yml` | `v*` tag | Builds WinSetupWizard, creates release with .zip |
| `release-gamemodedaemon.yml` | `v*` tag | Builds WinGameModeDaemon, creates release with .zip |

Each app releases **independently** — if one app fails to compile, the others are not blocked.

### What Gets Generated

For each app, a GitHub Release is created with:
- **Auto-generated release notes** from commits since the last tag
- **Zip file** containing the self-contained, single-file .exe (win-x64)
- **SmartScreen notice** in the release body

### First Release (v0.1.0)

```bash
git tag v0.1.0
git push --tags
```

All 6 workflows fire. Check the Actions tab to monitor progress.

## CI/CD

| Workflow | Runs on | Purpose |
|----------|---------|---------|
| `lint-and-test.yml` | Every push to `main`, every PR | Core lint/test + .NET build check |
| `release-*.yml` | `v*` tag push | Build, package, release |

## SmartScreen Warning

Binaries are not code-signed. Windows SmartScreen will show a warning on first run:

1. Click **More info**
2. Click **Run anyway**

If code signing is purchased later, add the certificate as a GitHub Secret and update the release workflow to sign the binary.

## License

MIT

# Architecture

Canonical reference for how the Fallout repo is laid out and how the pieces fit together.

## Top-level layout

```
.
├── .agents/skills/           Task-specific agent skills (SKILL.md per skill)
├── .assets/                  Images, icons, logos — anything binary and non-code
│   ├── icon.png              Package icon (referenced by Directory.Build.props)
│   └── images/               README / marketing imagery
├── .github/                  GitHub Actions workflows, issue/PR templates
├── .fallout/                 Build orchestrator runtime state (committed: schema, parameters)
├── build/                    The build orchestrator project (consumes Fallout itself — dogfooding)
│   ├── _build.csproj
│   └── Build.*.cs            Partial classes split by concern (CI, Licenses, etc.)
├── docs/                     Documentation site content + architecture notes (this file) + ADRs
├── src/                      All production library projects
│   ├── Fallout.<X>/Fallout.<X>.csproj
│   ├── Persistence/          The persistence ring: .sln/.slnx parsing + its public facade
│   └── Shims/                Transition shims for NUKE-era consumers (Nuke.* namespace)
├── tests/                    All test projects
│   └── Fallout.<X>.Tests/Fallout.<X>.Tests.csproj
├── AssemblyInfo.cs           Shared InternalsVisibleTo declarations (included by Directory.Build.props)
├── Directory.Build.props     Shared MSBuild properties + ItemGroups applied to every project
├── Directory.Build.targets   Smart PackageReference → ProjectReference logic
├── Directory.Packages.props  Central package version management — never put Version= inline
├── fallout.slnx              Solution file (new XML format, not .sln)
├── global.json               Pinned .NET SDK
├── version.json              Nerdbank.GitVersioning config
├── nuget.config              Restricts package sources to nuget.org with explicit mapping
├── AGENTS.md, CLAUDE.md      Agent brief (canonical) + the Claude Code pointer to it
└── build.{ps1,sh,cmd}        Bootstrap entry points
```

## Why the layout looks like this

### `src/` vs `tests/` split

Production code and tests live in separate top-level directories so:

- Project filters in IDEs map cleanly to "what ships" vs "what verifies."
- CI can target `tests/**` patterns without writing per-project exclusions.
- `IsPackable` is name-based (`MSBuildProjectName.EndsWith('Tests')` → false) — no manual opt-out per project.

The previous monorepo style under `source/` mixed both, and `source/Directory.Build.props` had to special-case the `*.Tests` projects. After the split, the split is structural.

### `.assets/` for binary content

Images, icons, logos, and other non-code binary content live under `.assets/`. The leading dot keeps it out of most CI path filters and signals "not source." The package icon (`.assets/icon.png`) is referenced via `$(MSBuildThisFileDirectory).assets\icon.png` in `Directory.Build.props` — independent of project depth.

### `build/` consumes the rest of the repo (dogfooding)

`build/_build.csproj` `ProjectReference`s `src/Fallout.Components`, `src/Fallout.Tooling.Generator`, and `src/Fallout.SourceGenerators`. Any change to the framework can be exercised by running `./build.ps1` — if the build itself breaks, you'd notice immediately.

### Shared build files hoisted to root

`Directory.Build.props` and `AssemblyInfo.cs` live at the repo root rather than under `src/` or `tests/`. Reason: both `src/<Project>/` and `tests/<Project>/` projects need to inherit them, and hoisting to root means MSBuild's directory walk finds them once without per-tree duplication.

## Project groupings under `src/`

| Area | Projects | Purpose |
|---|---|---|
| Reactor core | `Fallout.Core` | Pure domain physics — the immutable shape of the build execution pipeline and the graph algorithms that schedule it. No I/O, no process, no console, no logging. The bottom layer everything else sits on. |
| Core framework | `Fallout.Common`, `Fallout.Build`, `Fallout.Build.Shared`, `Fallout.Components`, `Fallout.Tooling` | The API consumers reference and the host runtime that executes targets. |
| Code generation | `Fallout.SourceGenerators`, `Fallout.Tooling.Generator` | Roslyn source generators that produce per-target code at compile time, plus the `.cs`-from-`.json` tool-wrapper generator. |
| Models | `Fallout.ProjectModel` | Strongly-typed wrapper over `.csproj`. |
| Persistence ring (`src/Persistence/`) | `Fallout.Persistence.Solution`, `Fallout.Solution` | `.sln` / `.slnx` parsing. `Fallout.Persistence.Solution` is the inlined parser — originally Microsoft's `Microsoft.VisualStudio.SolutionPersistence` under MIT, now owned outright, with attribution in `NOTICE` and the Microsoft file headers preserved verbatim. Its types are `internal` apart from a small whitelist that appears in `Fallout.Solution`'s public facade signatures. |
| Tooling | `Fallout.Cli`, `Fallout.MSBuildTasks` | The `dotnet fallout` global tool and the MSBuild tasks layer it builds on. |
| Migration | `Fallout.Migrate`, `Fallout.Migrate.Analyzers` | CLI for NUKE → Fallout repo migration, plus Roslyn analyzers and codefixes for the same. |
| Utilities | `Fallout.Utilities` + sub-packages (`IO.Compression`, `IO.Globbing`, `Net`, `Text.Json`, `Text.Yaml`) | Standalone helpers reusable outside the build context. |
| Transition shims (`src/Shims/`) | `Nuke.Common`, `Nuke.Build`, `Nuke.Components` | Typed wrappers in the `Nuke.*` namespace inheriting from the `Fallout.*` types, so pre-rename consumers compile against the new packages without source changes. Most types are generated by `Fallout.SourceGenerators.TransitionShimGenerator`. |

Every project under `src/` has a sibling under `tests/` (e.g. `src/Fallout.Common/` → `tests/Fallout.Common.Tests/`). The shims are covered by `tests/Nuke.Common.Shim.Tests/` and `tests/Nuke.Components.Shim.Tests/`; `tests/` also holds `Benchmarks/`, `Consumers/` and `integration/`.

Tool wrappers — the `.json` schemas most likely to be extended — live under `src/Fallout.Common/Tools/<Tool>/<Tool>.json`. See the [`adding-a-tool-wrapper` skill](../.agents/skills/adding-a-tool-wrapper/SKILL.md).

## Build conventions

- **Central package versions.** All `PackageReference` versions live in `Directory.Packages.props`. Never inline `Version=` on a `PackageReference` — the build will error.
- **Smart `PackageReference`.** `Directory.Build.targets` rewrites `PackageReference`s that match a project in the current solution into `ProjectReference`s. Lets us reference our own packages by ID across the dev/release boundary.
- **`AssemblyInfo.cs` at root.** Shared `InternalsVisibleTo` declarations. Included automatically via `Directory.Build.props`.
- **No per-file license headers.** The MIT notice lives in [`LICENSE`](https://github.com/Fallout-build/Fallout/blob/main/LICENSE) at the repo root. NuGet packages declare MIT via `PackageLicenseExpression` in `Directory.Build.props`. Vendored Microsoft code under `src/Persistence/Fallout.Persistence.Solution/` keeps its own headers — leave those alone.

## CI layout

| Workflow | Generated from | When it runs |
|---|---|---|
| `ubuntu-latest.yml` | `build/Build.CI.GitHubActions.cs` | PRs to `experimental` / `main` / `release/*` / `support/*`. **The only required check.** |
| `windows-latest.yml`, `macos-latest.yml` | `build/Build.CI.GitHubActions.cs` | Release-intent only — PR to `release/*`/`support/*`, or a `v*` tag push. |
| `experimental.yml`, `preview.yml` | Hand-written | Push to `experimental` / `main` → `-alpha` / `-preview` to GitHub Packages. |
| `release.yml` | Hand-written | `v*` tag push on a production branch → the publish fan-out. |

Linux gates PRs because it is cheap and fast; the other platforms are reserved for release intent — a deliberate cost trade-off. The full trigger invariants (and the rule that generated `.yml` must never be hand-edited) are in the [`editing-ci-workflows` skill](../.agents/skills/editing-ci-workflows/SKILL.md); the publish channels are in [branching-and-release.md](branching-and-release.md).

## What this doc deliberately does NOT cover

- API design decisions inside individual projects — read the project's tests for those.
- Rebrand status and migration strategy — see [rebrand-plan.md](rebrand-plan.md) and the [Fallout rebrand milestone](https://github.com/ChrisonSimtian/Fallout/milestone/1).
- Branching, versioning and releases — see [branching-and-release.md](branching-and-release.md).
- Contribution workflow — see `CONTRIBUTING.md`.
- The rules an AI coding tool must follow — see `AGENTS.md`.

When in doubt, the structure is whatever this file says it is. If you change the layout, update this file in the same PR.

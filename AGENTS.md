# AGENTS.md

The canonical brief for AI coding tools and human contributors. `CLAUDE.md` points here.

This file is loaded into **every** session, so it holds only what applies to every task. Task-specific
procedures live in `.agents/skills/`; long-form reference lives in `docs/`. Keep it short.

## What this project is

**Fallout** — a build automation system for C#/.NET, the hard-fork successor to
[NUKE](https://github.com/nuke-build/nuke), under new maintenance since 2026. The build is itself a C#
console app (`build/_build.csproj`), so any framework change can be dogfooded with `./build.ps1`.

The codebase is mature and large — **prefer matching existing patterns over introducing new ones**.
The rename has landed; legacy `Nuke.*` survives only as transition shims under `src/Shims/`.

## Stack

.NET SDK pinned in `global.json` · central package versions in `Directory.Packages.props` ·
xUnit + FluentAssertions + Verify.Xunit · solution file is `fallout.slnx` (XML format, not `.sln`).

```powershell
./build.ps1                  # default target = Pack
./build.ps1 Compile | Test
./build.ps1 GenerateTools    # regenerate tool wrappers from JSON
./build.ps1 --help           # all targets and parameters
dotnet test tests/Fallout.Common.Tests/Fallout.Common.Tests.csproj   # single project
```

## Versioning and channels

Calendar versioning `YYYY.MINOR.PATCH` (valid semver; major = year). A maturity ladder feeds the
production line: **GitHub Packages = test/preview, nuget.org = production.**

| Branch | Channel | Breaking changes | Review |
|---|---|---|---|
| `experimental` | `-alpha` per commit, GH Packages | **Only here** | Light, fast |
| `main` (default) | `-preview` per commit, GH Packages | Never | Ordinary |
| `release/YYYY` | `-rc.N` → GA, nuget.org opt-in | Never | Rigorous |
| `support/v10` | legacy `10.x`, on tag | Never | Security/critical only |

Ladder: `-alpha` < `-preview` < `-rc` < GA. Breaking changes accumulate on `experimental` and ship as
next year's major — mid-year `main` and production are strictly non-breaking. Unstable public API can
ship marked `[Experimental("FALLOUT0xx")]` on any channel.

## Critical rules

1. **Every PR gets a target label at creation time** (`target/vCurrent`, or `target/vNext` for a
   breaking change — check `gh label list` first, some repos still use the older `target/YYYY`).
   A breaking change also needs the `breaking-change` label, a `⚠️ Breaking change` callout, an
   `experimental` base branch, and a CHANGELOG entry under the next major. Review blocks
   otherwise — read the `creating-a-pr` skill.
2. **Default to backwards compatibility.** `[Obsolete]`, shims, `[Experimental]`, feature flags and
   overloads all beat a hard break ([#262](https://github.com/ChrisonSimtian/Fallout/issues/262)).
3. **Central package versions only** — `Directory.Packages.props`, never `Version=` inline. A
   meaningful new library also gets a row in [docs/dependencies.md](docs/dependencies.md).
4. **Tests next to code** — every `src/Foo` has a `tests/Foo.Tests` sibling. Mirror the namespace.
5. **Stay on xUnit + FluentAssertions + Verify.** No new test or assertion framework.
6. **No `.editorconfig` or `*.DotSettings`** — intentionally removed. Don't reintroduce without a
   maintainer-level decision.
7. **No per-file license headers** — [`LICENSE`](LICENSE) at the root is the single source of truth.
   Vendored third-party code keeps its own headers; leave those alone.
8. **Never hand-edit generated files.** Tool wrappers, shims, per-target code and the cross-platform
   CI workflows are generated. Fix the source, then regenerate.

## Where things live

`src/` production · `tests/` tests · `build/` the orchestrator · `docs/` reference + ADRs ·
`.agents/skills/` agent skills · `.assets/` binaries.

**[docs/architecture.md](docs/architecture.md) is the canonical layout reference** — the full tree,
project groupings, the shim strategy, build conventions and the reasoning. Written for contributors and
agents alike. Read it before moving files or adding a project, and update it in the same PR if the
layout changes.

## Doing a specific task? Read the skill

Skills live in `.agents/skills/<name>/SKILL.md`. Copilot CLI discovers them automatically; other tools
should open the file.

- [`creating-a-pr`](.agents/skills/creating-a-pr/SKILL.md) — opening a PR, commit messages, base branch, the breaking-change gate
- [`adding-a-tool-wrapper`](.agents/skills/adding-a-tool-wrapper/SKILL.md) — the `Tools/<Tool>/<Tool>.json` recipe
- [`marking-experimental-apis`](.agents/skills/marking-experimental-apis/SKILL.md) — shipping API that isn't stable yet
- [`editing-ci-workflows`](.agents/skills/editing-ci-workflows/SKILL.md) — `.github/workflows/` and CI config
- [`cutting-a-release`](.agents/skills/cutting-a-release/SKILL.md) — tagging, publishing, promoting, the yearly cut

## What not to do

- Don't reintroduce `source/` (now `src/` + `tests/`) or `images/` (now `.assets/`).
- Don't use conventional-commit prefixes (`feat:`, `fix:`, `docs:`) in commit subjects or PR titles.
  Write a functional title saying what the change does — see the `creating-a-pr` skill.
- Don't commit `output/`, `bin/`, `obj/`, `nuke-global.*`, or anything from `GenerateTools`.
- Don't bypass `Directory.Packages.props` or `Directory.Build.targets`.
- Don't disable the telemetry opt-out in test runs (`FALLOUT_TELEMETRY_OPTOUT=true`).
- Don't expose internal middleware/listener interfaces via `InternalsVisibleTo` to non-test assemblies.
  There is **no public plugin SDK yet** — that is a later major.

## Useful pointers

- `build/Build.*.cs` is the canonical example of consuming the framework.
- If a symbol seems missing, check whether `src/Fallout.SourceGenerators` generates it.
- Verify snapshots (`*.verified.*`) under `tests/` are the contract for generator output — review
  changes to them carefully.

## Documentation map

One topic, one home. If something appears in two layers, delete the copy — don't sync it.

- **Rules** → `AGENTS.md`. Applies to every task, always loaded, stays short.
- **Procedures** → `.agents/skills/`. Recipes, loaded only when the task matches.
- **Reference** → `docs/`. Architecture, [release runbook](docs/branching-and-release.md),
  [dependencies](docs/dependencies.md), [experimental APIs](docs/experimental-apis.md),
  [roadmap](docs/roadmap.md), [rebrand plan](docs/rebrand-plan.md). For contributors *and* agents —
  link to these rather than copying them.
- **Decisions** → [`docs/adr/`](docs/adr/). Why a model was chosen. Immutable once accepted.
- **Contributors** → `CONTRIBUTING.md`. Human-facing entry point; links here rather than restating.

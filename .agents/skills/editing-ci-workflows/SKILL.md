---
name: editing-ci-workflows
description: >-
  Invariants for Fallout's GitHub Actions setup — which workflows are generated versus hand-written,
  what may and may not go in a trigger, caching, and the single-invocation build rule. Use this before
  touching anything under .github/workflows/ or build/Build.CI.GitHubActions.cs.
---

# Editing CI workflows

GitHub Actions is the only CI provider in use. Before editing anything, work out which kind of file you
are looking at.

## Generated versus hand-written

| Workflow | Source |
|---|---|
| `ubuntu-latest.yml`, `windows-latest.yml`, `macos-latest.yml` | **Generated** from `build/Build.CI.GitHubActions.cs` |
| `experimental.yml`, `preview.yml`, `release.yml` | Hand-written |

For the generated ones, edit the attributes and constants in `build/Build.CI.GitHubActions.cs`
(`MainBranch`, `ExperimentalBranch`, the `*BranchPattern` constants) and regenerate with `./build.sh`.
**Never hand-edit the generated `.yml`** — the next regeneration silently reverts it.

## Trigger invariants

- **Feature branches run zero CI until a PR is opened.** Push triggers list **only** long-lived
  branches. Nothing fires on `feature/*`, `bugfix/*` and friends until they are PR'd against
  `experimental` / `main` / `release/*` / `support/*`. Do **not** add a working-branch pattern to any
  `OnPush*` or `branches:` trigger.
- **The Linux PR gate (`ubuntu-latest`) is the only required check.** It runs on PRs to the four
  long-lived branches.
- **`experimental` (push) → `-alpha`; `main` (push) → `-preview`**, both to GitHub Packages, via
  `experimental.yml` / `preview.yml`.
- **Cross-platform `windows` / `macos` are gated to release intent** — PR-to-`release/*`/`support/*` or
  a `v*` tag push only. They do **not** run on `main` / `experimental` pushes. This is a deliberate cost
  trade-off, not an oversight.
- **`concurrency: cancel-in-progress` on every build workflow except `release.yml`** — never cancel a
  publish mid-flight.
- **Canonical CI-ignore paths:** `docs/**`, `.assets/**`, `**/*.md` — applied to every PR and push
  trigger.

## Build invocation

**Every publishing lane runs `Test` before it publishes.** `experimental.yml`, `preview.yml` and
`release.yml` each run a single `dotnet fallout Test Pack` invocation. The build executes that as
discrete internal stages (Restore → Compile → Test → Pack) and fails at the breaking stage, so a test
failure stops the job before the push step.

**Don't split a lane into separate `dotnet fallout Compile` / `Test` / `Pack` steps.** Each invocation
re-runs the dependency graph, which means a double compile. The single invocation *is* the staged build.

## Caching

Every workflow caches `~/.nuget/packages` and `.fallout/temp`, keyed on `global.json` + `**/*.csproj` +
`Directory.Packages.props` — the dependency-affecting set — with a `restore-keys:` prefix fallback for
partial restores.

- There is no `packages.lock.json` to add to the key.
- Build outputs (`bin` / `obj`) are deliberately **not** cached: stale-artifact correctness risk.

## Don'ts

- Don't add `main` / `experimental` or any working-branch pattern to the **push** triggers of the
  cross-platform workflows.
- Don't add `submodules: recursive` to checkouts — there are no submodules (no `.gitmodules`), so it is
  a dead init step.

## Reference

- `docs/branching-and-release.md` — branch protection, the release pipeline and its publish fan-out.
- `docs/adr/0004-calendar-versioning-and-dual-pace-channels.md` — the channel ladder these triggers
  implement.

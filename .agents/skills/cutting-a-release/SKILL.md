---
name: cutting-a-release
description: >-
  Maintainer runbook entry point for shipping Fallout — tagging a release, the publish fan-out,
  opting into nuget.org, promoting work up the ladder, hotfixing an older line, and cutting a new
  calendar year. Use this when asked to release, tag, publish, promote, forward-port, or roll the
  version lanes forward.
---

# Cutting a release

Releases are maintainer-driven and tag-triggered. This skill is the decision tree and the trap list;
the exact commands live in `docs/branching-and-release.md`, which you should open before running
anything.

## Which situation are you in?

| Situation | Go to |
|---|---|
| Ship a stable release from `release/YYYY` | `docs/branching-and-release.md` → *Cutting a release* |
| Ship it to nuget.org as well | same section → *Stabilised release*, and read "The nuget.org gate" below |
| A publish failed partway through | same → *If a publish fails partway through*. Re-runs are safe |
| Move non-breaking work `experimental → main` | same → *Promotion and hotfixing* |
| Move a stable patch/minor `main → release/YYYY` | same → *Promotion and hotfixing* |
| Urgent fix that landed low and must not regress the fast lane | same → *Forward-porting* |
| Security/critical fix for 10.x | same → *Legacy `support/v10`* |
| Start a new calendar year | same → *Cutting a new year* |

## What fires on a tag push

Pushing a `v*` tag on a production branch (`release/YYYY` or `support/*`) runs `release.yml`. It
validates that the tag is reachable from such a branch, then fans a Test+Pack job out to three publish
jobs:

| Job | Fires on tag push? | What ships |
|---|---|---|
| `publish-nuget-org` | **No — opt-in only** | `Fallout.*.nupkg` to nuget.org |
| `publish-github-packages` | Yes | **All** `*.nupkg` (Fallout.\* + Nuke.\*) to GitHub Packages |
| `publish-github-releases` | Yes | All `*.nupkg` attached to a GitHub Release, auto-generated notes |

Pushes to `experimental` and `main` are separate lanes: they publish `-alpha` and `-preview`
prereleases to **GitHub Packages only**, never nuget.org and never a GitHub Release.

## The nuget.org gate

nuget.org is reserved for the deliberate publish of a stabilised `release/YYYY`, or a `support/v10`
legacy security patch. Three layers protect it:

1. `v*` tags are protected by a repository ruleset (creation, deletion, update — repo admins bypass).
2. You must run `workflow_dispatch` with `publish-to-nugetorg=true`. A tag push alone will not do it.
3. The `nuget-org` GitHub Environment has a required-reviewer rule, and `NUGET_API_KEY` only resolves
   inside that gated job.

`workflow_dispatch` inputs: `tag` (required, an existing tag) and `publish-to-nugetorg`
(boolean, default `false`).

## Traps

- **`Nuke.*` shims never go to nuget.org.** Those package IDs are owned by the original NUKE maintainer
  (see issue #47). They are permanently routed to GitHub Packages regardless of the input flag.
- **Re-runs are idempotent.** Every `dotnet nuget push` uses `--skip-duplicate`, so re-running a
  partially-failed publish will not choke on packages that already landed.
- **First publish of a brand-new `Fallout.X` package 403s.** The nuget.org prefix reservation is
  per-ID, not per-prefix-wildcard, so CI's first push for a never-published ID fails until someone
  manually web-uploads one nupkg to register it — **owned by the org, not a personal account**, and
  validation lags 5–30 minutes. Full walkthrough: `docs/branching-and-release.md` →
  *Adding a new `Fallout.X` package*.
- **Don't renumber `support/v10` into CalVer.** It stays on semver `10.x` indefinitely.
- **Keep `experimental` and `main` on the same version core** when rolling the lanes forward, so
  `-alpha` < `-preview` ordering stays honest.

## Reference

- `docs/branching-and-release.md` — the full runbook with copy-pasteable commands.
- `docs/adr/0004-calendar-versioning-and-dual-pace-channels.md` — the versioning model.
- `docs/adr/0001-release-branch-model.md`, `docs/adr/0002-v11-off-nuget-by-default.md` — why the
  channels are shaped this way.

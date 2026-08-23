---
name: creating-a-pr
description: >-
  The Fallout PR-creation procedure — choosing the base branch, labelling, the breaking-change gate,
  and the writing style for commits, PR bodies and issues. Use this whenever you open a pull request,
  write a commit message, file an issue, or decide which branch a change should target.
---

# Creating a pull request

Fallout batches breaking changes to a yearly major cut. Getting the base branch and the labels wrong
is the single most common review blocker, so do this at PR-creation time — not after, not as a
follow-up.

## 1. Pick the base branch

| Your change | Base branch |
|---|---|
| Deliberate improvement or bug fix, non-breaking | `main` |
| Fast-moving work, or **anything breaking** | `experimental` |
| Security/critical fix for the legacy 10.x line | `support/v10` (or the relevant `hotfix/v10.x`) |

A breaking change may target **`experimental` only** — never `main`, never a `release/YYYY` production
train. Name your branch `feature/<slug>`, `bugfix/<slug>`, `chore/<slug>`, or `docs/<slug>`.

## 2. Is it breaking?

A change is breaking if any of these hold:

- A conventional-commit subject carries the `!` suffix (`feat(globaltool)!: …`).
- The commit body has a `BREAKING CHANGE:` footer.
- A reviewer would reasonably call it breaking even without a marker — renamed or removed public API,
  package ID change, on-disk format change, or a CI/CD shape consumers depend on.

**Exception:** changes to surface marked `[Experimental("FALLOUT0xx")]` are never breaking. Adding or
removing the attribute is not breaking either. If you are about to break something, first ask whether
it can be additive instead — see the "prefer additive" rule in `AGENTS.md`.

## 3. Label and open

Every PR gets a **target label** saying which release line it ships on. Read the labels that actually
exist before you pick one — the repo is mid-transition between two schemes:

```bash
gh label list --limit 200 | grep target/
```

- **`target/vCurrent` / `target/vNext` / `target/backlog`** — the current relative scheme. Prefer these
  when they exist.
- **`target/YYYY`** (e.g. `target/2026`) — the older absolute scheme, still present on merged PRs.
- **`target/v10`** — legacy 10.x maintenance.

Then:

```bash
gh pr create --base main --label target/vCurrent --title "…" --body "…"
```

Default is the current line. A breaking change is held for the next yearly major, so it takes
`target/vNext` (or `target/<next-year>` under the old scheme).

## 4. Extra steps for a breaking change

All four, in the same PR:

1. Add the **`breaking-change`** label:
   `gh pr create --base experimental --label target/vNext --label breaking-change …`
2. **Open the PR body with a `⚠️ Breaking change` callout** naming the affected surface (public API,
   package ID, CLI flag, on-disk format, CI/CD shape) and the consumer-side impact in one sentence.
   This is the first thing reviewers and downstream consumers read.
3. **Confirm the base is `experimental`.** Do **not** bump `version.json`'s major — the major is set
   once, at the yearly cut.
4. **Add a `CHANGELOG.md` entry** under the next-major `[Unreleased]` heading, describing the change
   and the migration path. One paragraph minimum.

If you only discover the breaking nature mid-review, apply all four before requesting re-review.

## 5. Before you push

- Run `./build.ps1 Test` (or `./build.sh Test`) locally.
- Adding a package? It goes in `Directory.Packages.props`, never inline. If it is a meaningful library
  rather than a tiny transitive helper, add a row to `docs/dependencies.md` in the same PR — reviewers
  will ask.
- Don't commit anything produced by `./build.ps1 GenerateTools`.
- Add tests. Every `src/Foo` has a sibling `tests/Foo.Tests`; mirror the namespace.

## 6. Writing style

Applies to commit messages, PR titles and bodies, review comments and issues. Many readers are
non-native English speakers.

- Be short and precise. Lead with the point.
- Prefer bullet points over paragraphs.
- Plain, simple English. Short sentences, common words.
- Cut filler: no preamble, no hedging, no AI-flavoured padding.
- Say what changed and why. Drop the rest.

## 7. After opening

- The only required check is `ubuntu-latest`. Docs-only PRs hit a no-op shim reporting the same status
  check name. `windows-latest` / `macos-latest` run post-merge, not as PR gates.
- **Review rigour rises with the ladder.** PRs to `experimental` get light, fast review. PRs to `main`
  get ordinary review. Promotion to a `release/YYYY` train gets rigorous, unhurried review.
- Address feedback in additional commits rather than force-pushing.
- Merging is squash by default; rebase is opt-in for a curated commit sequence. Plain merge commits are
  disabled (branch protection requires linear history).

## Reference

- `docs/branching-and-release.md` — the full branch, channel and versioning model.
- `docs/adr/0004-calendar-versioning-and-dual-pace-channels.md` — why the model looks like this.

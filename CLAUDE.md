# CLAUDE.md

This project uses [`AGENTS.md`](AGENTS.md) as its canonical brief for AI coding tools. Claude Code imports it via the file-reference syntax below.

@./AGENTS.md

## Skills

Task-specific procedures live in `.agents/skills/<name>/SKILL.md`. Claude Code does not auto-discover
that directory, so **read the file yourself** when a task matches — each is a self-contained recipe, so
don't reconstruct these rules from memory.

- `creating-a-pr` — opening a PR, commit messages, filing an issue, picking a base branch. Covers the
  mandatory `target/YYYY` label and the breaking-change gate.
- `adding-a-tool-wrapper` — adding or extending a wrapper under `src/Fallout.Common/Tools/`.
- `marking-experimental-apis` — public API that isn't stable yet; allocating a `FALLOUT0xx` ID.
- `editing-ci-workflows` — `.github/workflows/` or `build/Build.CI.GitHubActions.cs`.
- `cutting-a-release` — tagging, publishing, promoting, hotfixing, cutting a new calendar year.

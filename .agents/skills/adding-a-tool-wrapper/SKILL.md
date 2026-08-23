---
name: adding-a-tool-wrapper
description: >-
  Recipe for adding or extending a Fallout CLI tool wrapper — the JSON schemas under
  src/Fallout.Common/Tools that generate the typed C# API. Use this when asked to add support for a
  new command-line tool, extend an existing wrapper with more arguments, or when touching any
  Tools/<Tool>/<Tool>.json file.
---

# Adding a tool wrapper

Tool wrappers are declared as JSON and compiled into typed C# by the tooling generator. The `.json` is
the source of truth; the `.cs` next to it is generated output.

## Where things live

```
src/Fallout.Common/Tools/<Tool>/<Tool>.json   ← source of truth, edit this
src/Fallout.Common/Tools/<Tool>/<Tool>.cs     ← generated, never commit
```

## Steps

1. **Find the closest existing tool** under `src/Fallout.Common/Tools/` and copy its shape. Matching a
   neighbour beats inventing a new structure — this codebase is mature and consistent.
2. **Cover a full command with all its arguments.** A half-covered command is worse than none; users
   hit the gap and drop to raw process invocation.
3. **Run `./build.ps1 GenerateTools`** and confirm it generates cleanly.
4. **Do not commit the generated `.cs`.** Generated code is regenerated manually once per release.

## Formatting rules for `help` text

Use these tags:

- `<c>` for inline code
- `<a>` for links
- `<ul>` / `<ol>` for lists
- `<em>` for emphasised text
- `<para/>` between paragraphs — **not** `<p>…</p>`

## Don'ts

- Don't write `secret: false` — it is the default.
- Don't write `default: xxx` — obsolete.
- Don't hand-edit the generated `.cs` to fix something. Fix the JSON and regenerate.

## Note for contributors

Tool wrappers are mechanical to add, so `CONTRIBUTING.md` asks people to send a PR rather than file an
issue for a missing tool.

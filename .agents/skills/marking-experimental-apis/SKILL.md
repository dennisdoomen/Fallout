---
name: marking-experimental-apis
description: >-
  How Fallout ships public API that is not yet stable, using [Experimental("FALLOUT0xx")] and the
  diagnostic-ID registry. Use this when adding public surface you are not ready to commit to, when
  allocating or retiring a FALLOUT diagnostic ID, or when deciding whether a change to unstable
  surface counts as breaking.
---

# Marking experimental APIs

Public APIs that aren't ready for a stability guarantee are marked with
[`ExperimentalAttribute`](https://learn.microsoft.com/dotnet/api/system.diagnostics.codeanalysis.experimentalattribute)
instead of being held back or shipped silently. The attribute ships in the .NET 8+ BCL — **no package
reference needed** (the repo targets .NET 10).

```csharp
using System.Diagnostics.CodeAnalysis;

[Experimental("FALLOUT001")]
public sealed class NewPluginHost
{
    // ...
}
```

## Rules

- **Diagnostic-ID scheme is `FALLOUT0xx`.** Each experimental surface gets its own ID, allocated
  **sequentially and never reused** — a retired ID stays retired. Register every allocation in
  `docs/experimental-apis.md` in the same PR that introduces it.
- **Consumers must explicitly opt in.** `ExperimentalAttribute` is an *error-by-default* diagnostic:
  code touching the API fails to compile until the consumer suppresses the exact ID — either
  `#pragma warning disable FALLOUT001` around the call site, or
  `<NoWarn>$(NoWarn);FALLOUT001</NoWarn>` in their project. Opting into instability is therefore a
  conscious, per-API choice, which is right for a *framework* — a product devs build on, not an app.
- **Promoting to stable = deleting the attribute.** The feature already rode the test lanes
  (`experimental` / `main`) and was promoted forward, so removing the `[Experimental]` line is the whole
  promotion. No cross-branch dance.
- **Adding or removing `[Experimental]` is not a breaking change.** Neither is changing the surface it
  guards — it carries no stability guarantee.
- **Channel discipline differs.** On the `experimental` (alpha) and `main` (preview) test lanes, churn
  is expected and the attribute is a courtesy. On a `release/YYYY` **production line**, any
  risky-but-shipped public surface **must** wear `[Experimental]` — that contract is what keeps the
  stable line trustworthy while still carrying new work.
- **Don't apply it speculatively.** Because the diagnostic is error-by-default, marking an API that is
  already used internally breaks the build everywhere it is referenced. Only mark genuinely unstable
  surface, and suppress every internal usage in the same change so the build stays green.

## Why this exists

It is the escape hatch that keeps rule #2 in `AGENTS.md` ("default to backwards compatibility")
workable: surface that isn't ready to commit to can ship behind the attribute instead of forcing either
a breaking change or an indefinite hold.

## Reference

- `docs/experimental-apis.md` — the diagnostic-ID registry. Update it in the same PR.
- `docs/adr/0004-calendar-versioning-and-dual-pace-channels.md` §5 — the decision.

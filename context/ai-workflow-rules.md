# AI Workflow Rules

## Approach

Build incrementally with a spec-driven workflow. The context files define what to build,
how to build it, and the current state of progress. Always implement against these specs —
do not infer or invent behavior from scratch.

## Scoping Rules

- Work on one feature unit at a time.
- Prefer small, verifiable increments over large speculative changes.
- Do not combine unrelated system boundaries in a single implementation step.

## When to Split Work

Split an implementation step if it combines:

- UI changes and background/domain changes (e.g. a hub method plus a scene tweak).
- Multiple unrelated areas (e.g. two different domain features in one commit).
- Behavior not clearly defined in the context files.

If a change cannot be verified end to end quickly, the scope is too broad — split it.

## Branch and PR Discipline

- Branch per task from `main`: `feat/<slug>` | `ci/<slug>` | `build/<slug>` | `fix/<slug>` | `docs/<slug>`.
- One PR per unit, base `main`. Conventional commits (`feat:`, `fix:`, `docs:`, `ci:`).
- The user merges; do not merge or force-push. Squash-merge is used.
- CodeRabbit reviews PRs automatically; triage its comments — fix real issues, dismiss false
  positives — and report the outcome to the user.

## Handling Missing Requirements

- Do not invent product behavior not defined in the context files.
- If a requirement is ambiguous, resolve it in the relevant context file before implementing.
- If a requirement is missing, add it as an open question in `progress-tracker.md` before continuing.

## Protected Files

- `src/HighLow/wwwroot/js/` and `src/HighLow/wwwroot/lib/` — gitignored build artifacts;
  never edit by hand, rebuild instead.
- `Dockerfile`, `compose*.yaml`, `Caddyfile`, `.github/workflows/*` — treat deploy
  pipeline changes as their own unit.

## Keeping Docs in Sync

Update the relevant context file whenever implementation changes:

- System architecture or boundaries
- Storage model decisions
- Code conventions or standards
- Feature scope
- Progress tracker (always, after every meaningful change)

## Before Moving to the Next Unit

1. The current unit works end to end within its defined scope.
2. No invariant defined in `architecture.md` was violated.
3. `progress-tracker.md` reflects the completed work.
4. `npm run build` passes; `dotnet test` passes; any e2e probe relevant to the change passes.
5. PR description is neutral third person with no AI references.

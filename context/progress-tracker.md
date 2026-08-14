# Progress Tracker

Update this file after every meaningful implementation change.

## Current Phase

Guide/UX polish complete. Live site deployed. Code quality tooling being added (CodeRabbit),
then a CSS token refactor.

## Current Goal

PR 15: introduce the agent context system (this folder + root AGENTS.md) and CodeRabbit config.
PR 16 (next): refactor `style.css` hardcoded hex values onto the tokens defined in `ui-context.md`.

## Completed

- Task 1: repo scaffold, solution, CI skeleton.
- Task 2: rules engine (RoundResolver, GameScores, PointDeck, Specials).
- Task 3: scoring and winner ranking.
- Task 4: room state machine — timers, gifts, reconnect, bot takeover.
- Task 5: default bot strategy.
- Task 6: room manager — codes, quick match, per-IP cap, TTL sweep.
- Task 7: SignalR hub, metrics, integration tests.
- Task 8: TypeScript client — lobby, table UI, event wiring.
- Task 9: Arena X visual pass.
- Task 10: containerize + deploy pipeline (Docker, Caddy+DuckDNS, GH Actions, VPS).
- Task 11: gameplay bugfixes — hand delivery via view pull, sweeper-driven deadlines,
  hint line/tooltips, countdown label, e2e probe.
- Task 12: Guide screen (11 animated steps), first-visit primer, colored backs as gameplay,
  rematch, landing polish (no window.prompt), no-arrow/no-hyphen copy.
- Task 13: guide scene feedback fixes (clear cards, labels above cards, sequenced tier flips).
- Task 14: guide polish (visible specials, deck flip-to-stack, pass back card, gift and
  tie-break animations, player chips).

## In Progress

- PR 15 (this): agent context system + `.coderabbit.yaml`. CodeRabbit app installed on the repo.

## Next Up

- PR 16: CSS token refactor — add `--surface-input`, `--border-input`, `--text-muted`,
  `--text-offline`, `--state-online`, `--text-primary` to `:root` and replace hardcoded
  hexes in `style.css`; keep `ui-context.md` in sync.
- Playtest the live site (primer, Guide, colored backs, gift animation, tie-break scene).

## Open Questions

- None blocking.

## Architecture Decisions

- In-memory rooms, no database — rooms are transient; TTL sweep cleans up (Task 4/6).
- Server-authoritative with a per-room `SemaphoreSlim` gate; sweeper drives all timeouts.
- Hidden cards expose only their tier (blue/gold) so colored backs work as gameplay (Task 12).
- Point deck is shuffled per game; 9 of the 10 cards are used across nine rounds and the
  leftover card is never dealt (matches the Guide's "one card is removed" story).
- No accounts; per-seat token in localStorage; host token authorizes start; per-IP room cap.
- Dark-only arena theme with tier accents (gold/blue/red/silver), serif display type.
- Copy style: no hyphens, no arrows, neutral third-person PR/commit text.

## Session Notes

- CodeRabbit installed on the repo (free OSS plan, public repo). Config `.coderabbit.yaml`
  is included in PR 15 so it is active from the first review. Install cannot be verified via
  `gh` (needs app-token auth); proof is a CodeRabbit review on PR 15.
- Vault docs in the Obsidian vault are the historical archive; `context/` is the live spec.

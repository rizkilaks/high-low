# Code Standards

## General

- Keep modules small and single-purpose; a file should be understandable without its caller.
- Fix root causes, do not layer workarounds; a guard in the shared function beats one in every caller.
- Do not mix unrelated concerns in one commit or one PR.
- No references to agents or AI tools in commits or PRs (e.g. "fixed by CodeRabbit",
  "the assistant"). Required tooling and file names (AGENTS.md, CodeRabbit config,
  GitHub Actions) are allowed where informative.
- PR descriptions and commit messages are neutral third person: what changed and why.

## C# (src/HighLow)

- Event-driven domain: server pushes record-based events to the room group; clients apply them.
- All room mutations run inside the per-room `SemaphoreSlim` gate — never outside it.
- Hidden information never leaves the server; hidden cards expose only their tier.
- `Random` and `IClock` are always injected (constructor); tests use `new Random(42)` and a `FakeClock`.
- `IRandom`/bot strategy injected; `RoomManager(Random, IBotStrategy)`.
- Records for immutable event shapes (PascalCase); keep them in `Game/RoomEvents.cs` / `Domain/`.
- No secrets in code; config via environment / `.env` only.

## TypeScript (wwwroot/ts)

- Strict mode is required; no `any` — use explicit interfaces or narrowly scoped types.
- All wire shapes live in `protocol.ts` (interfaces mirroring the C# records, camelCase).
- Client-facing SignalR method names are camelCase; C# records stay PascalCase.
- Validate unknown external input at the boundary (hub invoke results) before trusting it.
- DOM building is centralized in `ui.ts` / `scenes.ts`; keep state logic in `state.ts`.

## Styling (wwwroot/css)

- Use CSS custom property tokens from `context/ui-context.md` — no hardcoded hex values.
- Follow the border radius scale in `ui-context.md`.
- Dark-only; keep the arena theme (gold/blue/red/silver tiers, serif display).
- Extend `style.css`; do not add a CSS framework or component library.

## Tests (tests/HighLow.Tests)

- Server logic is tested directly against the Room/RoomManager/Domain classes (xUnit).
- Deterministic: inject `Random` (seeded) and `FakeClock`; advance time explicitly.
- Test behavior and invariants, not implementation details.

## File Organization

- `src/HighLow/Domain/` — pure rules, no I/O.
- `src/HighLow/Game/` — Room aggregate, RoomManager, Seat, event records.
- `src/HighLow/Hubs/` — GameHub, RoomSweeper.
- `src/HighLow/wwwroot/ts/` — client TS modules.
- `src/HighLow/wwwroot/css/` — `style.css` (tokens + components).
- `tests/HighLow.Tests/` — xUnit suite.
- `context/` — agent context files; the live spec for this project. They are updated
  whenever implementation changes what they document (per AGENTS.md).

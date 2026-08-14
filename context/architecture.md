# Architecture Context

## Stack

| Layer      | Technology                            | Role                                   |
| ---------- | ------------------------------------- | -------------------------------------- |
| Backend    | .NET 10 (ASP.NET Minimal API)         | SignalR hub, room aggregate, sweeper   |
| Realtime   | SignalR                               | Bidirectional events to room groups    |
| Frontend   | TypeScript (tsc only, no bundler)     | Lobby, table, Guide, primer            |
| Styling    | Vanilla CSS (`wwwroot/css/style.css`) | All UI; token-driven, dark-only        |
| Build      | Docker multi-stage + Dockerfile.caddy | Images for app and Caddy edge          |
| Reverse proxy | Caddy + DuckDNS (on VPS)           | TLS on port 8443                       |
| CI/CD      | GitHub Actions (`ci.yml`, `deploy.yml`) | Test/build on PR; deploy to VPS on main |
| Hosting    | VPS `43.134.104.216`, Docker Compose  | `highlow` (8080) + `caddy` (8443)      |

## System Boundaries

- `src/HighLow/Domain/` — pure C# rules engine, no I/O. `RoundResolver`, `GameScores`,
  `PointDeck`, `Special`, `Submission`, `RevealedCard`, `RoundResolution`.
- `src/HighLow/Game/` — `Room` aggregate (state machine, timers, bots, per-room gate),
  `RoomManager` (codes, quick match, per-IP cap, TTL sweep), `Seat`, event records.
- `src/HighLow/Hubs/` — `GameHub` (thin SignalR adapter, the only network code),
  `RoomSweeper` (background loop driving `Room.TickAsync`).
- `src/HighLow/wwwroot/ts/` — client modules: `app.ts` (wiring), `state.ts` (event/view
  application), `protocol.ts` (typed wire shapes), `ui.ts` (rendering), `guide.ts`,
  `primer.ts`, `scenes.ts` (animations).
- `src/HighLow/wwwroot/css/` — single `style.css` with the token set from `ui-context.md`.
- `tests/HighLow.Tests/` — xUnit suite; server logic tested directly (48+ tests).

## Storage Model

- **None (in-memory).** Rooms live in `RoomManager._rooms` (a `Dictionary<string, Room>`)
  guarded by a `SemaphoreSlim`. No database. Room lifetime is governed by:
  - Lobby idle TTL: 10 min.
  - Finished room TTL: 30 min.
  - A 500ms `RoomSweeper` tick removes stale rooms and drives `Room.TickAsync`
    (submit/gift deadlines, disconnect grace, bot takeover).

## Auth and Access Model

- **No accounts.** A server-issued per-seat token (`Guid` string) identifies a player,
  stored client-side in `localStorage`; used to reconnect and to authorize mutating hub calls.
- **Host** is the room creator; `HostToken` authorizes `StartWithBots`.
- **Per-IP cap**: at most 3 rooms per IP address.
- Rooms are joined by code (or quick match into a public lobby); seats fill to 4.

## Invariants

1. All room mutations run inside the per-room gate (`SemaphoreSlim(1,1)`); hub-triggered
   operations and the sweeper's `TickAsync` are serialized per room.
2. Hidden information never leaves the server: opponent hands and hidden card values are
   not sent to clients; hidden cards expose only their tier (blue/gold).
3. The client is a thin view — events and `RoomView` are the only source of truth; the
   client never decides game outcomes.
4. Timeouts are driven by the background `RoomSweeper`, never by client timers.
5. Only 9 of the 10 point deck cards are used per game: the deck is shuffled at room
   creation (`Room.cs:28`) and dealt by index (`_deck[Round-1]`, rounds 1..9); the
   card left over at index 9 is never dealt.
6. Card colors are tiers, not semantics: blue = 1-5, gold = 6-10, silver = specials,
   white = point cards.

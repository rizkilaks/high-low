# High and Low (Arena X)

## Overview

A real-time multiplayer browser card game inspired by "Bloody Game X". Four players
compete over nine rounds of hidden-number brinkmanship: each round a point card is
dealt, everyone plays one number card (1 to 10) on a special card (Normal or Reverse),
and the card closest to the point wins — or the direction flips when Reverses are played.
Overlapping values are voided, negative points can be gifted, and the top two scores
win the game. It is dark-only, arena-themed, playable solo against bots or with up to
4 human players in a shared room, with no accounts and no database.

## Goals

1. A stranger can land, learn the game from the Guide, and be inside a game in under a minute.
2. The server is authoritative: clients never receive hidden information they should not see.
3. Deploys are one-command (`docker compose up -d --build`) and fully automated via CI.
4. The UI stays consistent through the token system in `context/ui-context.md`.

## Core User Flow

1. Player opens the site; enters a name (inline input, no prompt).
2. First visit: a 15-second primer auto-plays (skippable); the full Guide is one click away.
3. Player creates a room, joins by code, or quick-matches. Bots fill empty seats on start.
4. Nine rounds: each round a point card is dealt; everyone plays one number card on a
   Normal or Reverse special; specials reveal first and decide the direction (HIGHEST or
   LOWEST); cards reveal; the winner takes the point (or gifts a negative point).
5. Top two scores win; a tie is broken by the higher single point card.
6. Finished screen offers a rematch with the same players.

## Features

### Onboarding and teaching

- Animated 15-second first-visit primer (Skip always available).
- Dedicated Guide screen: 11 animated steps covering every rule, reachable from the
  landing, the table, and the finished screen.

### Gameplay

- 9 rounds, point deck of 10 cards `-1, 2, -3, 4, -5, 6, -7, 8, -9, 10`, shuffled per game;
  the 9 cards dealt over the rounds are used, one card is never dealt.
- Number cards 1 to 10 (blue backs 1 to 5, gold backs 6 to 10), one Reverse special per
  player per game, one Normal special.
- Odd number of Reverses flips the round to LOWEST; even count cancels and stays HIGHEST.
- Overlapping card values are voided; next-best wins; no survivors means the point burns.
- Negative points: the winner may gift the negative to an overlapped player.
- Colored card backs are real gameplay: a hidden card's blue/gold tier is visible from behind.
- 60-second submit timer, 15-second gift timer; auto-submit at the deadline.

### Multiplayer and rooms

- Room codes, quick match, public rooms, up to 4 seats, per-IP room cap, idle TTL sweep.

## Scope

### In Scope

- Bots to fill seats (configurable on start), bot takeover after disconnects/misses.
- The Guide and primer as described above.
- Colored backs as gameplay.
- Rematch on the finished screen.

### Out of Scope

- Accounts, login, authentication (per-seat tokens only, stored in localStorage).
- Persistence / database (rooms are in-memory).
- Localization.
- Audio.
- PvP matchmaking beyond public quick match.

## Success Criteria

1. A first-time visitor can play a full game against bots with no instruction beyond the
   Guide/primer.
2. Hidden information (opponent hands, hidden card values) is never exposed by the server.
3. A push to `main` deploys automatically and the live site passes `/readyz`.
4. All UI uses the tokens in `context/ui-context.md`; no hardcoded hex values.

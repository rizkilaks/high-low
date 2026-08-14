# UI Context

## Theme

Dark only. No light mode. The design language is a dark technical arena — near-black
backgrounds, layered surfaces, and vivid tier accents (gold / blue / red / silver).
Headings, titles, prizes and card numerals use a serif display face (Georgia);
body and UI use `system-ui`. Interactions glow with the accent color on hover.

## Colors

All colors are CSS custom properties defined on `:root` in
`src/HighLow/wwwroot/css/style.css`. Components must use these tokens — no hardcoded
hex values.

| Role                    | CSS Variable        | Value    |
| ----------------------- | ------------------- | -------- |
| Page background         | `--black`           | `#050506` |
| Surface (panels)        | `--panel`           | `#0e0e12` |
| Surface 2 (raised)      | `--charcoal`        | `#1d1d20` |
| Surface 3 (pressed)     | `--charcoal2`       | `#2c2c30` |
| Input / button surface  | `--surface-input`   | `#151518` |
| Primary text            | `--text-primary`    | `#ffffff` |
| Muted text              | `--text-muted`      | `#9aa0a6` |
| Dimmed / offline text   | `--text-offline`    | `#6e7681` |
| Brand / winner accent   | `--gold`            | `#d4a017` |
| Interactive / you accent| `--blue`            | `#3b82f6` |
| Danger / negative       | `--red`             | `#e84040` |
| Neutral / secondary     | `--silver`          | `#c0c4cc` |
| Panel border            | `--panel-edge`      | `#2a2020` |
| Red border / negative edge | `--red-edge`     | `#5a1515` |
| Input border            | `--border-input`    | `#3a3a3f` |
| State online            | `--state-online`    | `#3fb950` |

Semantic use:

- **Gold** = brand, winner, prize, "you are up" emphasis.
- **Blue** = "this is you" (seat outline, start chip removed), interactive highlight.
- **Red** = danger, negative points, forced reveal, errors, urgent countdown.
- **Silver** = neutral/secondary text, special cards.
- **White** = point cards (they carry a white outline to stand apart from number cards).

## Typography

| Role             | Font                                      | Usage |
| ---------------- | ----------------------------------------- | ----- |
| UI text          | `system-ui, -apple-system, sans-serif`    | Body, buttons, inputs, labels |
| Display / serif  | `Georgia, 'Times New Roman', serif`       | Headings, titles, prizes, card numerals, round label |

Rule: card numerals and big prizes are always serif and bold; body UI is `system-ui`.

## Border Radius

| Context                    | Value |
| -------------------------- | ----- |
| Buttons, inputs, cards     | `8px` |
| Seat panels                | `10px`|
| Center table panel         | `12px`|
| Guide animation panel      | `10px`|
| Chips / pills              | `10px`|
| Circle button (e.g. `?`)   | `50%` |

## Component Library

None. All UI is hand-written vanilla CSS in `src/HighLow/wwwroot/css/style.css` plus
light DOM building in the TS modules (`ui.ts`, `scenes.ts`). Extend `style.css` with the
same patterns; do not introduce a framework or component library.

## Layout Patterns

- **Lobby**: centered column, `max-width: 520px`, `12vh` top margin; name input, room
  code input, Create/Join/Quick buttons, then seats list.
- **Table**: CSS grid `1fr 320px 1fr`; seat 0 top, seat 1 right, seat 2 bottom, seat 3
  left, center panel in the middle; hand and controls span all columns in rows 4-5.
- **Overlays/modals**: `position: fixed; inset: 0`, dark backdrop
  (`rgba(5,5,6,.85)`), centered flex column, `z-index: 80`; toasts `z-index: 100`.
- **Guide screen**: centered column, `max-width: 720px`, animation panel with step
  dots and Prev/Next/Replay controls.
- **Primer overlay**: full-screen overlay with an animation stage and a Skip button.

## Icons

No icon library. Inline text glyphs only: `⟳` for Reverse, `—` for Normal, the `X`
arena motif (title, card watermark, center pulse). Keep glyphs, not images/SVG.

## Card Faces

- Number cards: dark glass with tier border (blue/gold), serif numeral, `X` watermark.
- Specials: silver border; `⟳ REVERSE` or `— NORMAL` label (scene/UI).
- Hidden (back): dark face with tier-colored border and `?` — the blue/gold tier is
  visible from behind by design.
- Point cards: white border/outline.

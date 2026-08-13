import type { GameState } from "./state.js";

let selectedCard: number | null = null;
let pass = false;
let reverse = false;
let current: { state: GameState; mySeat: number; myName: string } | null = null;
let giftHandler: ((target: number | null) => void) | null = null;
let toastTimer: number | undefined;

export function getSelectedCard(): number | null { return selectedCard; }
export function setSelectedCard(c: number | null): void { selectedCard = c; }
export function getPass(): boolean { return pass; }
export function setPass(v: boolean): void { pass = v; }
export function getReverse(): boolean { return reverse; }
export function setReverse(v: boolean): void { reverse = v; }

function el(id: string): HTMLElement {
    return document.getElementById(id)!;
}

function rerender(): void {
    if (current) {
        renderTable(current.state, current.mySeat, current.myName);
        if (current.state.revealed.length > 0) renderReveal(current.state);
    }
}

export function showScreen(name: "lobby" | "table" | "guide"): void {
    el("screen-lobby").classList.toggle("hidden", name !== "lobby");
    el("screen-table").classList.toggle("hidden", name !== "table");
    el("screen-guide").classList.toggle("hidden", name !== "guide");
    el("btn-leave").classList.toggle("hidden", name !== "table");
}

export function renderLobbySeats(state: GameState): void {
    const codeBox = el("lobby-code");
    codeBox.classList.toggle("hidden", !state.roomCode);
    codeBox.querySelector("strong")!.textContent = state.roomCode;
    const list = el("lobby-seats");
    list.replaceChildren();
    if (state.seats.every(s => !s.name && !s.isBot)) {
        const msg = document.createElement("div");
        msg.className = "lobby-msg";
        msg.textContent = state.phase === "Lobby" && state.round === 0 ? "waiting for players…" : "in game…";
        list.appendChild(msg);
        return;
    }
    for (const s of state.seats) {
        if (!s.name && !s.isBot) continue;
        const row = document.createElement("div");
        row.textContent = `${s.name || "(bot)"}${s.isBot ? " · bot" : ""}${s.botControlled ? " · AI" : ""}`;
        list.appendChild(row);
    }
}

export function renderTable(state: GameState, mySeat: number, myName: string): void {
    current = { state, mySeat, myName };
    const codeLabel = el("room-code-label");
    codeLabel.classList.toggle("hidden", !state.roomCode);
    codeLabel.textContent = state.roomCode ? `Room ${state.roomCode}` : "";
    el("round-label").textContent = state.round > 0 ? `Round ${state.round} / ${state.totalRounds}` : "High and Low";
    const prizeEl = el("prize");
    prizeEl.textContent = state.prize != null ? (state.prize > 0 ? `+${state.prize}` : String(state.prize)) : "";
    prizeEl.classList.toggle("negative", state.prize != null && state.prize < 0);
    el("direction").textContent =
        state.direction === "Highest" ? "HIGHEST" :
        state.direction === "Lowest" ? "LOWEST" : "";
    el("winner-line").textContent = winnerLine(state);
    for (let i = 0; i < 4; i++) renderSeatPanel(state, i, mySeat, myName);
    renderHand(state);
    renderControls(state, mySeat);
    el("btn-start").classList.toggle("hidden", !(mySeat === 0 && state.phase === "Lobby"));
    el("hint").textContent = hintLine(state, mySeat);
    const logEl = el("event-log");
    logEl.replaceChildren();
    for (const line of state.log.slice(-12)) {
        const div = document.createElement("div");
        div.textContent = line;
        logEl.appendChild(div);
    }
}

function winnerLine(state: GameState): string {
    if (state.winnerSeat == null) return "";
    const name = state.seats[state.winnerSeat]?.name ?? "";
    const card = state.winnerCardValue != null ? ` · card ${state.winnerCardValue}` : " · (hidden)";
    const burned = state.burnedPrize != null ? ` · burned ${state.burnedPrize}` : "";
    return `Winner: ${name}${card}${burned}`;
}

function renderSeatPanel(state: GameState, i: number, mySeat: number, myName: string): void {
    const panel = el(`seat-${i}`);
    const s = state.seats[i];
    if (!s) return;
    panel.classList.toggle("you", i === mySeat);
    panel.classList.toggle("bot", s.botControlled);
    panel.replaceChildren();

    const head = document.createElement("div");
    head.className = "seat-head";
    const nameEl = document.createElement("span");
    nameEl.textContent = s.name || (i === mySeat ? myName || "you" : s.isBot ? "BOT" : "empty");
    head.appendChild(nameEl);
    const badge = document.createElement("span");
    badge.className = "badge";
    if (i === mySeat) badge.textContent = "YOU";
    else if (s.isBot) badge.textContent = "BOT";
    if (badge.textContent) head.appendChild(badge);
    panel.appendChild(head);

    const meta = document.createElement("div");
    meta.className = "seat-meta";
    meta.textContent = `✕ ${s.handCount} cards · ${s.score} pts · ${s.reverseLeft} rev`;
    panel.appendChild(meta);

    if (state.reverseSeats.includes(i)) {
        const rev = document.createElement("span");
        rev.className = "card-face special";
        rev.textContent = "REVERSE";
        panel.appendChild(rev);
    }

    const dot = document.createElement("span");
    dot.className = "dot";
    dot.classList.toggle("offline", !s.connected);
    panel.appendChild(dot);

    const reveal = document.createElement("div");
    reveal.className = "reveal";
    panel.appendChild(reveal);
}

function renderHand(state: GameState): void {
    const canPick = state.phase === "Submitting" && !state.mySubmitted;
    const hand = el("hand");
    hand.replaceChildren();
    for (const card of state.myHand) {
        const b = document.createElement("button");
        b.className = "card" + (card === selectedCard ? " selected" : "");
        b.classList.add(card <= 5 ? "blue" : "gold");
        b.textContent = String(card);
        b.disabled = !canPick;
        b.addEventListener("click", () => {
            setSelectedCard(selectedCard === card ? null : card);
            rerender();
        });
        hand.appendChild(b);
    }
}

function hintLine(state: GameState, mySeat: number): string {
    if (state.phase === "Finished") return "Game over, final scores above";
    if (state.phase !== "Submitting") return "";
    if (!state.mySubmitted) {
        return state.forcedRevealSeats.includes(mySeat)
            ? "Your move, forced to reveal. Pick a card and Submit"
            : "Your move. Pick a card, then Submit";
    }
    const waiting = state.seats
        .map((s, i) => ({ s, i }))
        .filter(({ s, i }) => i !== mySeat && !s.hasSubmitted && !s.isBot && !s.botControlled)
        .map(({ s }) => s.name)
        .filter(Boolean);
    return waiting.length > 0 ? `Waiting for ${waiting.join(", ")}…` : "Waiting for bots…";
}

function renderControls(state: GameState, mySeat: number): void {
    const canSubmit = state.phase === "Submitting" && !state.mySubmitted;
    const forced = state.forcedRevealSeats.includes(mySeat);
    const revLeft = state.seats[mySeat]?.reverseLeft ?? 0;

    const passBtn = el("btn-pass") as HTMLButtonElement;
    passBtn.textContent = pass ? "Pass ✓" : "Pass";
    passBtn.classList.toggle("selected", pass);
    passBtn.disabled = !canSubmit || forced;
    passBtn.title = passBtn.disabled && forced
        ? "The start seat is forced to reveal, cannot pass"
        : "";

    const revBtn = el("btn-reverse") as HTMLButtonElement;
    revBtn.textContent = `Reverse (${revLeft})`;
    revBtn.classList.toggle("selected", reverse);
    revBtn.disabled = !canSubmit || revLeft <= 0;
    revBtn.title = revBtn.disabled && revLeft <= 0 ? "No Reverse left" : "";

    const subBtn = el("btn-submit") as HTMLButtonElement;
    subBtn.disabled = !canSubmit || selectedCard == null;
    subBtn.title = subBtn.disabled && canSubmit ? "Select a card first" : "";
}

export function renderReveal(state: GameState): void {
    for (let i = 0; i < 4; i++) {
        const panel = el(`seat-${i}`);
        panel.classList.toggle("winner", state.winnerSeat === i);
        const reveal = panel.querySelector(".reveal");
        if (!reveal) continue;
        reveal.replaceChildren();
        reveal.classList.toggle("voided", state.voidedSeats.includes(i));
        if (state.voidedSeats.includes(i)) {
            const stamp = document.createElement("span");
            stamp.className = "void-stamp";
            stamp.textContent = "VOID";
            reveal.appendChild(stamp);
            continue;
        }
        const card = state.revealed.find(c => c.seat === i);
        if (!card) continue;
        const face = document.createElement("span");
        if (card.hidden) {
            face.className = "card-face hidden" + (card.tier === 1 ? " gold" : card.tier === 0 ? " blue" : "");
            face.textContent = "?";
        } else {
            face.className = "card-face" + (card.card != null ? (card.card <= 5 ? " blue" : " gold") : " special");
            face.textContent = card.card != null ? String(card.card) : "—";
        }
        reveal.appendChild(face);
    }
}

export function showGiftPrompt(state: GameState): void {
    const overlay = el("overlay-gift");
    const visible = state.phase === "GiftDecision" && state.mySeat === state.winnerSeat && state.deadlineMs != null;
    overlay.classList.toggle("hidden", !visible);
    if (!visible) return;
    const wrap = el("gift-targets");
    wrap.replaceChildren();
    for (const seat of state.giftTargets) {
        const b = document.createElement("button");
        b.textContent = state.seats[seat]?.name || `seat ${seat}`;
        b.addEventListener("click", () => { overlay.classList.add("hidden"); giftHandler?.(seat); });
        wrap.appendChild(b);
    }
    const keep = document.createElement("button");
    keep.textContent = "Keep the negative";
    keep.addEventListener("click", () => { overlay.classList.add("hidden"); giftHandler?.(null); });
    wrap.appendChild(keep);
}

export function onGiftChoice(cb: (target: number | null) => void): void {
    giftHandler = cb;
}

export function showFinished(state: GameState): void {
    const overlay = el("overlay-finished");
    overlay.classList.toggle("hidden", state.winnerSeats.length === 0);
    if (state.winnerSeats.length === 0) return;
    const winners = el("finished-winners");
    winners.replaceChildren();
    const w = document.createElement("div");
    w.className = "winner-name";
    w.textContent = state.winnerSeats.map(s => state.seats[s]?.name ?? `seat ${s}`).join(", ");
    winners.appendChild(w);
    const table = el("finished-scores");
    table.replaceChildren();
    for (const s of state.seats) {
        if (!s.name && !s.isBot) continue;
        const tr = document.createElement("tr");
        const tdName = document.createElement("td");
        tdName.textContent = s.name || "(bot)";
        const tdScore = document.createElement("td");
        tdScore.textContent = String(s.score);
        tr.appendChild(tdName);
        tr.appendChild(tdScore);
        table.appendChild(tr);
    }
}

export function toast(msg: string): void {
    const t = el("toast");
    t.textContent = msg;
    t.classList.remove("hidden");
    window.clearTimeout(toastTimer);
    toastTimer = window.setTimeout(() => t.classList.add("hidden"), 2500);
}

export function setConnBanner(visible: boolean, text: string): void {
    const b = el("conn-banner");
    b.textContent = text;
    b.classList.toggle("hidden", !visible);
}

export function clearSelection(): void {
    setSelectedCard(null);
    setPass(false);
    setReverse(false);
}

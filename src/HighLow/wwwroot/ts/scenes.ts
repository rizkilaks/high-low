// Animated scenes for the Guide and the primer. Pure DOM builders driven by
// CSS keyframes (see style.css). Each scene clears the container and builds
// its own stage so it can be replayed by simply running it again.

export type SceneBuilder = (root: HTMLElement) => void;

function el(tag: string, cls?: string): HTMLElement {
    const e = document.createElement(tag);
    if (cls) e.className = cls;
    return e;
}

function card(value: string, cls = "card-face", anim = ""): HTMLElement {
    const c = el("span", cls + (anim ? ` ${anim}` : ""));
    c.textContent = value;
    return c;
}

function tierCard(value: string, tier: "blue" | "gold", anim = ""): HTMLElement {
    return card(value, `card-face ${tier}`, anim);
}

function specialCard(label: string, anim = ""): HTMLElement {
    return card(label, "card-face special", anim);
}

function pointCard(value: string, anim = ""): HTMLElement {
    return card(value, "card-face point", anim);
}

function label(text: string, anim = ""): HTMLElement {
    return el("div", "scene-label" + (anim ? ` ${anim}` : ""));
}

function caption(text: string): HTMLElement {
    return el("div", "scene-caption");
}

function stage(root: HTMLElement): HTMLElement {
    root.replaceChildren();
    const s = el("div", "scene-stage");
    root.appendChild(s);
    return s;
}

function push(root: HTMLElement, text: string): HTMLElement {
    const c = caption(text);
    root.appendChild(c);
    return c;
}

function delay(e: HTMLElement, ms: number): HTMLElement {
    e.style.animationDelay = `${ms}ms`;
    return e;
}

const POINT_VALUES = ["-1", "2", "-3", "4", "-5", "6", "-7", "8", "-9", "+10"];

// 1 — title
function sceneTitle(root: HTMLElement): void {
    const s = stage(root);
    s.style.flexDirection = "column";
    s.style.gap = "18px";
    const t = el("div", "title-text anim-deal");
    t.textContent = "HIGH AND LOW";
    const row = el("div");
    row.style.display = "flex";
    row.style.gap = "10px";
    const b1 = tierCard("?", "blue", "anim-pop");
    const b2 = tierCard("?", "gold", "anim-pop");
    delay(b2, 200);
    row.append(b1, b2);
    s.append(t, row);
}

// 2 — point deck, 9 of 10, one removed
function sceneDeck(root: HTMLElement): void {
    const s = stage(root);
    s.style.flexDirection = "column";
    const row = el("div", "card-fan");
    POINT_VALUES.forEach((v, i) => {
        const c = pointCard(v, "anim-deal");
        delay(c, i * 70);
        row.appendChild(c);
    });
    s.appendChild(row);
    const removed = pointCard("?", "anim-slide-away");
    const deckRow = el("div");
    deckRow.style.marginTop = "16px";
    const deck = specialCard("DECK", "anim-pop");
    const cap = push(root, "10 point cards. One is removed. 9 are played over nine rounds.");
    delay(removed, 900);
    removed.style.position = "absolute";
    deckRow.append(deck, removed);
    s.appendChild(deckRow);
    void cap;
}

// 3 — hand of 1-10 + Reverse + Normal, colored backs
function sceneHand(root: HTMLElement): void {
    const s = stage(root);
    s.style.flexDirection = "column";
    const fan = el("div", "card-fan");
    for (let v = 1; v <= 10; v++) {
        const c = tierCard(String(v), v <= 5 ? "blue" : "gold", "anim-deal");
        delay(c, v * 50);
        fan.appendChild(c);
    }
    s.appendChild(fan);
    const specials = el("div");
    specials.style.marginTop = "14px";
    specials.style.display = "flex";
    specials.style.gap = "10px";
    const rev = specialCard("REVERSE", "anim-pop");
    const norm = specialCard("NORMAL", "anim-pop");
    delay(rev, 650);
    delay(norm, 750);
    specials.append(rev, norm);
    s.appendChild(specials);
    const backs = el("div");
    backs.style.marginTop = "12px";
    backs.style.display = "flex";
    backs.style.gap = "10px";
    const b1 = card("?", "card-face hidden blue anim-pop");
    const b2 = card("?", "card-face hidden gold anim-pop");
    delay(b1, 950);
    delay(b2, 1050);
    backs.append(b1, b2);
    s.appendChild(backs);
    push(root, "Hand of 1 to 10, one Reverse, one Normal. Blue backs 1 to 5, gold backs 6 to 10.");
}

// 4 — odd reverses flip to LOWEST
function sceneOddReverse(root: HTMLElement): void {
    const s = stage(root);
    const row = el("div");
    row.style.display = "flex";
    row.style.gap = "12px";
    row.append(
        tierCard("2", "blue", "anim-deal"),
        tierCard("7", "gold", "anim-deal"),
        tierCard("5", "blue", "anim-deal"),
        tierCard("9", "gold", "anim-deal"),
    );
    s.appendChild(row);
    const dir = label("LOWEST", "anim-pop");
    dir.style.color = "var(--red)";
    delay(dir, 800);
    s.appendChild(dir);
    push(root, "3 Normal, 1 Reverse. Odd Reverse flips the advantage. Lowest wins.");
}

// 5 — even reverses cancel
function sceneEvenReverse(root: HTMLElement): void {
    const s = stage(root);
    const row = el("div");
    row.style.display = "flex";
    row.style.gap = "12px";
    row.append(
        tierCard("6", "gold", "anim-deal"),
        tierCard("3", "blue", "anim-deal"),
        tierCard("8", "gold", "anim-deal"),
        tierCard("1", "blue", "anim-deal"),
    );
    s.appendChild(row);
    const dir = label("HIGHEST", "anim-pop");
    dir.style.color = "var(--gold)";
    delay(dir, 800);
    s.appendChild(dir);
    push(root, "2 Normal, 2 Reverse. They cancel out. Highest wins.");
}

// 6 — Reverse once per game
function sceneReverseOnce(root: HTMLElement): void {
    const s = stage(root);
    s.style.flexDirection = "column";
    const rev = specialCard("REVERSE", "anim-deal");
    s.appendChild(rev);
    const counter = el("div", "scene-label anim-pop");
    delay(counter, 500);
    counter.textContent = "1 left";
    counter.style.color = "var(--red)";
    s.appendChild(counter);
    push(root, "You have only one Reverse for the whole game.");
}

// 7 — starting player reveals, best wins
function sceneReveal(root: HTMLElement): void {
    const s = stage(root);
    const row = el("div");
    row.style.display = "flex";
    row.style.gap = "12px";
    const start = tierCard("5", "blue", "anim-flip");
    delay(start, 300);
    const winner = tierCard("10", "gold", "anim-flip");
    delay(winner, 600);
    winner.classList.add("scene-winner", "anim-glow");
    const c1 = tierCard("2", "blue", "anim-flip");
    const c2 = tierCard("4", "blue", "anim-flip");
    delay(c1, 900);
    delay(c2, 1000);
    row.append(start, c1, c2, winner);
    s.appendChild(row);
    push(root, "Starting player reveals first. The most advantageous card wins.");
}

// 8 — pass, forced reveal
function scenePass(root: HTMLElement): void {
    const s = stage(root);
    const row = el("div");
    row.style.display = "flex";
    row.style.gap = "12px";
    row.append(
        tierCard("6", "gold", "anim-deal"),
        tierCard("3", "blue", "anim-deal"),
        tierCard("8", "gold", "anim-deal"),
    );
    const passCard = card("PASS", "card-face hidden anim-pop");
    delay(passCard, 500);
    passCard.classList.add("anim-shake");
    row.appendChild(passCard);
    s.appendChild(row);
    push(root, "You can Pass to hide your card. But the starting player and previous passers must reveal.");
}

// 9 — overlap voids, next best wins; all void burns
function sceneVoid(root: HTMLElement): void {
    const s = stage(root);
    const row = el("div");
    row.style.display = "flex";
    row.style.gap = "12px";
    const v1 = tierCard("7", "gold", "anim-pop");
    const v2 = tierCard("7", "gold", "anim-pop");
    delay(v2, 200);
    const next = tierCard("9", "gold", "anim-pop");
    delay(next, 600);
    next.classList.add("scene-winner", "anim-glow");
    row.append(v1, next, v2);
    s.appendChild(row);
    v1.classList.add("scene-void", "anim-shake");
    v2.classList.add("scene-void", "anim-shake");
    push(root, "Overlapping cards are voided. The next best card wins. None left? The point burns.");
}

// 10 — negative point, gift to an overlapped player
function sceneGift(root: HTMLElement): void {
    const s = stage(root);
    const p = pointCard("-3", "anim-deal");
    s.appendChild(p);
    const row = el("div");
    row.style.marginTop = "14px";
    row.style.display = "flex";
    row.style.gap = "12px";
    const g = tierCard("4", "blue", "anim-pop");
    delay(g, 600);
    g.classList.add("scene-winner", "anim-glow");
    const target = tierCard("4", "blue", "anim-pop");
    delay(target, 800);
    target.classList.add("scene-void");
    row.append(g, target);
    s.appendChild(row);
    push(root, "Negative point, overlapped players. The winner hands the negative to one of them.");
}

// 11 — scores, tie broken by highest single point
function sceneScores(root: HTMLElement): void {
    const s = stage(root);
    s.style.flexDirection = "column";
    const row = el("div");
    row.style.display = "flex";
    row.style.gap = "24px";
    const a = el("div");
    a.textContent = "A: 5";
    a.style.color = "var(--silver)";
    a.style.fontSize = "1.4em";
    const b = el("div");
    b.textContent = "B: 5";
    b.style.color = "var(--gold)";
    b.style.fontSize = "1.4em";
    row.append(a, b);
    s.appendChild(row);
    const win = label("B wins (had a 4)", "anim-pop");
    win.style.color = "var(--gold)";
    delay(win, 700);
    s.appendChild(win);
    push(root, "Highest total wins. On a tie, the higher single point card wins.");
}

export const guideScenes: SceneBuilder[] = [
    sceneTitle,
    sceneDeck,
    sceneHand,
    sceneOddReverse,
    sceneEvenReverse,
    sceneReverseOnce,
    sceneReveal,
    scenePass,
    sceneVoid,
    sceneGift,
    sceneScores,
];

export const guideTexts: string[] = [
    "High and Low",
    "There will be nine rounds. Of the 10 point cards, 9 cards will be shown each round. One card is removed.",
    "Each player is given number cards 1 to 10 (1, 2, 3, 4, 5, 6, 7, 8, 9, 10), a Reverse card (special) and a Normal card. The back of number cards 1 to 5 is blue; the back of number cards 6 to 10 is gold.",
    "After the point card is revealed, you put one number card on one special card and submit. Once submitted, the special cards are revealed first, which decides the number advantage of the round. If an odd number of Reverse cards is submitted (4 players: 3 Normal, 1 Reverse), the advantage reverses and the lowest number wins.",
    "If an even number of Reverse cards is submitted (4 players: 2 Normal, 2 Reverse), they cancel each other out and higher numbers remain advantageous.",
    "Players can use the Reverse card only once throughout the game.",
    "The starting player reveals the front of their number card, and the player who submitted the most advantageous card wins the point card of the round.",
    "If you do not wish to reveal the front of your card, you can Pass. However, the starting player and anyone who passed in the previous round must reveal the front of their number card.",
    "If the number cards submitted by the players overlap, the player with the next most advantageous card wins the point card. If there are no advantageous cards left, the point card is void.",
    "In a round with a negative point card, the winner may hand the negative point to one of the overlapped players (whether or not they passed).",
    "Nine rounds are played, and the players with the highest total scores win. On a tie, the player with the higher single point card wins (A: 2 + 3 = 5 vs B: 1 + 4 = 5, B wins on the 4).",
];

export const primerScenes: SceneBuilder[] = [sceneTitle, sceneHand, sceneDeck];

export const primerCaptions: string[] = [
    "High and Low",
    "Each player gets cards 1 to 10, a Reverse and a Normal.",
    "Nine rounds. One point card is dealt each round.",
];

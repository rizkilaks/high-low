import { guideScenes, guideTexts } from "./scenes.js";

export type GuideOrigin = "lobby" | "table" | "finished";

let current = 0;
let origin: GuideOrigin = "lobby";
let onBack: ((origin: GuideOrigin) => void) | null = null;

function el(id: string): HTMLElement {
    return document.getElementById(id)!;
}

function renderStep(): void {
    const anim = el("guide-anim");
    guideScenes[current](anim);
    el("guide-text").textContent = guideTexts[current];
    el("guide-step-label").textContent = `${current + 1} / ${guideScenes.length}`;
    const dots = el("guide-dots");
    dots.replaceChildren();
    for (let i = 0; i < guideScenes.length; i++) {
        const d = document.createElement("div");
        d.className = "dot" + (i === current ? " on" : "");
        dots.appendChild(d);
    }
    const next = el("btn-guide-next") as HTMLButtonElement;
    next.textContent = current === guideScenes.length - 1 ? "Start playing" : "Next";
}

export function openGuide(from: GuideOrigin): void {
    origin = from;
    current = 0;
    renderStep();
}

export function bindGuide(
    prevBtn: HTMLButtonElement,
    nextBtn: HTMLButtonElement,
    replayBtn: HTMLButtonElement,
    backBtn: HTMLButtonElement,
    back: (origin: GuideOrigin) => void,
): void {
    onBack = back;
    prevBtn.addEventListener("click", () => {
        if (current > 0) { current--; renderStep(); }
    });
    nextBtn.addEventListener("click", () => {
        if (current < guideScenes.length - 1) { current++; renderStep(); }
        else back(origin);
    });
    replayBtn.addEventListener("click", () => renderStep());
    backBtn.addEventListener("click", () => back(origin));
}

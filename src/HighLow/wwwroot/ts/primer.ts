import { primerCaptions, primerScenes } from "./scenes.js";

const LS_TAUGHT = "hl-taught";

let doneCb: (() => void) | null = null;
let timer: number | undefined;

function el(id: string): HTMLElement {
    return document.getElementById(id)!;
}

export function isFirstVisit(): boolean {
    return localStorage.getItem(LS_TAUGHT) === null;
}

export function markTaught(): void {
    localStorage.setItem(LS_TAUGHT, "1");
}

function playScene(i: number): void {
    const anim = el("primer-anim");
    anim.replaceChildren();
    primerScenes[i](anim);
    const cap = document.createElement("div");
    cap.className = "scene-caption";
    cap.textContent = primerCaptions[i];
    anim.appendChild(cap);
}

function showActions(): void {
    window.clearTimeout(timer);
    const actions = el("primer-actions");
    actions.classList.remove("hidden");
    el("btn-primer-skip").classList.add("hidden");
}

function startSequence(): void {
    playScene(0);
    timer = window.setTimeout(() => playScene(1), 2200);
    window.setTimeout(() => playScene(2), 4400);
    window.setTimeout(showActions, 6600);
}

export function showPrimer(onDone: () => void): void {
    doneCb = onDone;
    const skip = el("btn-primer-skip");
    skip.classList.remove("hidden");
    el("primer-actions").classList.add("hidden");
    el("primer").classList.remove("hidden");
    startSequence();
}

function dismiss(): void {
    window.clearTimeout(timer);
    el("primer").classList.add("hidden");
    markTaught();
    doneCb?.();
}

export function bindPrimer(
    skipBtn: HTMLButtonElement,
    closeBtn: HTMLButtonElement,
    guideBtn: HTMLButtonElement,
    openGuide: () => void,
): void {
    skipBtn.addEventListener("click", dismiss);
    closeBtn.addEventListener("click", dismiss);
    guideBtn.addEventListener("click", () => {
        el("primer").classList.add("hidden");
        markTaught();
        openGuide();
    });
}

import type { HubConnection } from "@microsoft/signalr";
import type {
    CardsRevealedEvent,
    GameFinishedEvent,
    GameStartedEvent,
    GiftPromptEvent,
    JoinedRoom,
    LobbyStateEvent,
    PlayerStatusEvent,
    RoomView,
    RoundResolvedEvent,
    RoundStartedEvent,
    SpecialsRevealedEvent,
} from "./protocol.js";
import { applyEvent, applyView, initialState, submitOptimistic, type GameState } from "./state.js";
import {
    clearSelection, getPass, getReverse, getSelectedCard, onGiftChoice,
    renderLobbySeats, renderReveal, renderTable, setConnBanner, setPass, setReverse,
    showFinished, showGiftPrompt, showScreen, toast,
} from "./ui.js";

const LS_NAME = "hl-name";
const LS_ROOM = "hl-room";
const LS_TOKEN = "hl-token";
const LS_SEAT = "hl-seat";

let playerName = localStorage.getItem(LS_NAME) ?? "";
let state = initialState("", 0, "");
let conn!: HubConnection;

function el(id: string): HTMLElement {
    return document.getElementById(id)!;
}

function clearStored(): void {
    localStorage.removeItem(LS_ROOM);
    localStorage.removeItem(LS_TOKEN);
    localStorage.removeItem(LS_SEAT);
}

function handle(name: string, payload: unknown): void {
    const reaction = applyEvent(state, { name, payload });
    if (reaction.toast) toast(reaction.toast);
    render();
}

function render(): void {
    if (!state.roomCode) {
        showScreen("lobby");
        renderLobbySeats(state);
        return;
    }
    renderTable(state, state.mySeat, playerName);
    renderReveal(state);
    showGiftPrompt(state);
    showFinished(state);
}

async function tryReconnect(): Promise<void> {
    const room = localStorage.getItem(LS_ROOM);
    const token = localStorage.getItem(LS_TOKEN);
    const seat = Number(localStorage.getItem(LS_SEAT) ?? "-1");
    if (!room || !token || !Number.isInteger(seat) || seat < 0) return;
    const view = await conn.invoke<RoomView | null>("reconnect", room, token).catch(() => null);
    if (view) {
        state = initialState(view.roomCode, seat, token);
        const reaction = applyView(state, view);
        render();
        if (reaction.toast) toast(reaction.toast);
    } else {
        clearStored();
        state = initialState("", 0, "");
        showScreen("lobby");
        renderLobbySeats(state);
    }
}

async function enterRoom(promise: Promise<JoinedRoom | null>, name: string): Promise<void> {
    const joined = await promise;
    if (!joined) {
        toast("could not join");
        return;
    }
    playerName = name;
    localStorage.setItem(LS_NAME, name);
    localStorage.setItem(LS_ROOM, joined.roomCode);
    localStorage.setItem(LS_TOKEN, joined.token);
    localStorage.setItem(LS_SEAT, String(joined.seat));
    state = initialState(joined.roomCode, joined.seat, joined.token);
    showScreen("table");
    render();
}

function wireEvents(): void {
    conn.on("lobbyState", (e: LobbyStateEvent) => handle("lobbyState", e));
    conn.on("gameStarted", (e: GameStartedEvent) => handle("gameStarted", e));
    conn.on("roundStarted", (e: RoundStartedEvent) => handle("roundStarted", e));
    conn.on("specialsRevealed", (e: SpecialsRevealedEvent) => handle("specialsRevealed", e));
    conn.on("cardsRevealed", (e: CardsRevealedEvent) => handle("cardsRevealed", e));
    conn.on("roundResolved", (e: RoundResolvedEvent) => handle("roundResolved", e));
    conn.on("giftPrompt", (e: GiftPromptEvent) => handle("giftPrompt", e));
    conn.on("gameFinished", (e: GameFinishedEvent) => handle("gameFinished", e));
    conn.on("playerDisconnected", (e: PlayerStatusEvent) => handle("playerDisconnected", e));
    conn.on("playerReconnected", (e: PlayerStatusEvent) => handle("playerReconnected", e));
    conn.on("playerBotControlled", (e: PlayerStatusEvent) => handle("playerBotControlled", e));
    conn.on("rejectedMessage", (e: string) => handle("rejectedMessage", e));
    conn.on("roomClosed", (code: string) => {
        toast(`room ${code} closed`);
        clearStored();
        state = initialState("", 0, "");
        showScreen("lobby");
        renderLobbySeats(state);
    });
}

function wireButtons(): void {
    const nameInput = el("name-input") as HTMLInputElement;
    const codeInput = el("room-code-input") as HTMLInputElement;
    const publicCheck = el("public-check") as HTMLInputElement;
    const inputName = () => nameInput.value.trim() || playerName;

    el("btn-create").addEventListener("click", () => {
        void enterRoom(conn.invoke<JoinedRoom | null>("createRoom", inputName(), publicCheck.checked), inputName());
    });
    el("btn-join").addEventListener("click", () => {
        const code = codeInput.value.trim();
        if (!code) return;
        void enterRoom(conn.invoke<JoinedRoom | null>("joinRoom", code, inputName()), inputName());
    });
    el("btn-quick").addEventListener("click", () => {
        void enterRoom(conn.invoke<JoinedRoom | null>("quickMatch", inputName()), inputName());
    });

    el("btn-start").addEventListener("click", () => {
        void conn.invoke("startWithBots", state.roomCode, state.myToken);
    });

    const passBtn = el("btn-pass") as HTMLButtonElement;
    passBtn.addEventListener("click", () => { setPass(!getPass()); render(); });
    const revBtn = el("btn-reverse") as HTMLButtonElement;
    revBtn.addEventListener("click", () => { setReverse(!getReverse()); render(); });

    el("btn-submit").addEventListener("click", () => {
        const card = getSelectedCard();
        if (card == null) return;
        const special = getReverse() ? "Reverse" : "Normal";
        void conn.invoke<boolean>("submit", state.roomCode, state.myToken, card, special, getPass()).then((ok: boolean) => {
            if (ok) {
                submitOptimistic(state, card);
                clearSelection();
                render();
            }
        });
    });

    el("btn-leave").addEventListener("click", () => {
        // ponytail: no server leave call — socket stays connected; the 60s disconnect
        // grace only bot-takes-over the seat if the socket actually closes. Acceptable for now.
        clearStored();
        state = initialState("", 0, "");
        showScreen("lobby");
        renderLobbySeats(state);
    });

    onGiftChoice(target => {
        void conn.invoke("chooseGift", state.roomCode, state.myToken, target);
    });
}

async function boot(): Promise<void> {
    if (!playerName) {
        playerName = (window.prompt("Your name?") ?? "").trim();
        if (!playerName) return;
        localStorage.setItem(LS_NAME, playerName);
    }

    conn = new signalR.HubConnectionBuilder()
        .withUrl("/hubs/game")
        .withAutomaticReconnect()
        .build();
    conn.onreconnecting(() => setConnBanner(true, "reconnecting…"));
    conn.onreconnected(() => { setConnBanner(false, ""); void tryReconnect(); });
    wireEvents();
    wireButtons();

    const room = localStorage.getItem(LS_ROOM);
    const token = localStorage.getItem(LS_TOKEN);
    const seat = Number(localStorage.getItem(LS_SEAT) ?? "-1");
    if (room && token && Number.isInteger(seat) && seat >= 0) {
        showScreen("table");
        await tryReconnect();
    } else {
        renderLobbySeats(state);
    }

    setInterval(() => {
        const cd = el("countdown");
        if (state.deadlineMs == null) {
            cd.textContent = "";
            return;
        }
        const secs = Math.max(0, Math.ceil((state.deadlineMs - Date.now()) / 1000));
        cd.textContent = secs > 0 ? `${secs}s left` : "…";
    }, 500);
}

void boot();

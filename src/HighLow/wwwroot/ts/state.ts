import type {
    CardsRevealedEvent,
    GameFinishedEvent,
    GameStartedEvent,
    GiftPromptEvent,
    LobbyStateEvent,
    PlayerStatusEvent,
    PublicCard,
    RoomView,
    RoundResolvedEvent,
    RoundStartedEvent,
    ScoreLine,
    SeatInfo,
    SpecialsRevealedEvent,
} from "./protocol.js";

export interface SeatState {
    name: string;
    isBot: boolean;
    botControlled: boolean;
    connected: boolean;
    score: number;
    reverseLeft: number;
    handCount: number;
}

export interface GameState {
    roomCode: string;
    mySeat: number;
    myToken: string;
    phase: string;
    round: number;
    totalRounds: number;
    prize: number | null;
    direction: string | null;
    winnerSeat: number | null;
    winnerCardValue: number | null;
    giftTargetSeat: number | null;
    burnedPrize: number | null;
    startSeat: number;
    forcedRevealSeats: number[];
    deadlineMs: number | null;
    seats: SeatState[];
    myHand: number[];
    revealed: PublicCard[];
    voidedSeats: number[];
    mySubmitted: boolean;
    log: string[];
    giftTargets: number[];
    winnerSeats: number[];
}

export interface Reaction {
    toast?: string;
    overlay?: "gift" | "finished" | null;
}

const EMPTY_SEAT: SeatState = { name: "", isBot: false, botControlled: false, connected: true, score: 0, reverseLeft: 0, handCount: 0 };

export function initialState(roomCode: string, mySeat: number, myToken: string): GameState {
    return {
        roomCode, mySeat, myToken,
        phase: "Lobby", round: 0, totalRounds: 9,
        prize: null, direction: null,
        winnerSeat: null, winnerCardValue: null,
        giftTargetSeat: null, burnedPrize: null,
        startSeat: 0, forcedRevealSeats: [],
        deadlineMs: null,
        seats: Array.from({ length: 4 }, () => ({ ...EMPTY_SEAT })),
        myHand: [],
        revealed: [], voidedSeats: [],
        mySubmitted: false,
        log: [],
        giftTargets: [], winnerSeats: [],
    };
}

function toSeatState(s: SeatInfo): SeatState {
    return {
        name: s.name, isBot: s.isBot, botControlled: s.botControlled, connected: s.connected,
        score: s.score, reverseLeft: s.reverseLeft, handCount: s.handCount,
    };
}

function seatName(state: GameState, seat: number): string {
    return state.seats[seat]?.name ?? `seat ${seat}`;
}

function applyScores(state: GameState, scores: ScoreLine[]): void {
    for (const s of scores) {
        const seat = state.seats[s.seat];
        if (seat) seat.score = s.score;
    }
}

export function applyEvent(state: GameState, evt: { name: string; payload: any }): Reaction {
    const log = (msg: string) => state.log.push(msg);
    switch (evt.name) {
        case "lobbyState": {
            const p = evt.payload as LobbyStateEvent;
            state.seats = p.seats.map(toSeatState);
            state.phase = "Lobby";
            log("room lobby");
            break;
        }
        case "gameStarted": {
            const p = evt.payload as GameStartedEvent;
            state.seats = p.seats.map(toSeatState);
            state.phase = "Submitting";
            log("game started");
            break;
        }
        case "roundStarted": {
            const p = evt.payload as RoundStartedEvent;
            state.round = p.round;
            state.totalRounds = p.totalRounds;
            state.prize = p.prize;
            state.startSeat = p.startSeat;
            state.forcedRevealSeats = p.forcedRevealSeats;
            state.deadlineMs = p.phaseDeadlineUtcMs;
            state.seats = p.seats.map(toSeatState);
            state.revealed = [];
            state.voidedSeats = [];
            state.mySubmitted = false;
            state.phase = "Submitting";
            state.direction = null;
            state.winnerSeat = null;
            state.winnerCardValue = null;
            state.giftTargetSeat = null;
            state.burnedPrize = null;
            log(`round ${p.round} — prize ${p.prize}`);
            break;
        }
        case "specialsRevealed": {
            const p = evt.payload as SpecialsRevealedEvent;
            state.direction = p.direction;
            log(`specials: ${p.reverses} reverse${p.reverses === 1 ? "" : "s"} · ${p.direction}`);
            break;
        }
        case "cardsRevealed": {
            const p = evt.payload as CardsRevealedEvent;
            state.revealed = p.cards;
            state.voidedSeats = p.voidedSeats;
            break;
        }
        case "roundResolved": {
            const p = evt.payload as RoundResolvedEvent;
            state.winnerSeat = p.winnerSeat;
            state.winnerCardValue = p.winnerCardValue;
            state.giftTargetSeat = p.giftTargetSeat;
            state.burnedPrize = p.burnedPrize;
            state.deadlineMs = null;
            applyScores(state, p.scores);
            if (p.winnerSeat != null) {
                const card = p.winnerCardValue != null ? `card ${p.winnerCardValue}` : "(hidden)";
                const burned = p.burnedPrize != null ? ` · burned ${p.burnedPrize}` : "";
                log(`winner: ${seatName(state, p.winnerSeat)} — ${card}${burned}`);
            } else {
                log("no winner this round");
            }
            break;
        }
        case "giftPrompt": {
            const p = evt.payload as GiftPromptEvent;
            state.giftTargets = p.targetSeats;
            state.deadlineMs = p.phaseDeadlineUtcMs;
            state.phase = "GiftDecision";
            log("gift choice");
            return { overlay: "gift" };
        }
        case "gameFinished": {
            const p = evt.payload as GameFinishedEvent;
            state.winnerSeats = p.winnerSeats;
            state.phase = "Finished";
            state.deadlineMs = null;
            applyScores(state, p.scores);
            log(`game over — winner: ${p.winnerSeats.map(w => seatName(state, w)).join(", ")}`);
            return { overlay: "finished" };
        }
        case "playerDisconnected":
        case "playerReconnected":
        case "playerBotControlled": {
            const p = evt.payload as PlayerStatusEvent;
            const seat = state.seats[p.seat];
            if (seat) {
                if (evt.name === "playerDisconnected") seat.connected = false;
                else if (evt.name === "playerReconnected") seat.connected = true;
                else seat.botControlled = true;
            }
            log(`${evt.name} — ${seatName(state, p.seat)} (${p.status})`);
            break;
        }
        case "rejectedMessage":
            return { toast: evt.payload as string };
    }
    return {};
}

export function applyView(state: GameState, view: RoomView): Reaction {
    state.roomCode = view.roomCode;
    state.phase = view.phase;
    state.round = view.round;
    state.totalRounds = view.totalRounds;
    state.prize = view.prize;
    state.direction = view.direction;
    state.winnerSeat = view.winnerSeat;
    state.winnerCardValue = view.winnerCardValue;
    state.giftTargetSeat = view.giftTargetSeat;
    state.burnedPrize = view.burnedPrize;
    state.startSeat = view.startSeat;
    state.forcedRevealSeats = view.forcedRevealSeats;
    state.deadlineMs = view.phaseDeadlineUtcMs;
    state.seats = view.seats.map(toSeatState);
    state.myHand = view.myHand;
    state.giftTargets = view.giftTargets ?? [];
    state.winnerSeats = view.winnerSeats ?? [];
    state.revealed = [];
    state.voidedSeats = [];
    state.mySubmitted = false;
    state.log.push("reconnected");
    return view.phase === "Finished" ? { overlay: "finished" } : {};
}

export function submitOptimistic(state: GameState, card: number): void {
    state.myHand = state.myHand.filter(c => c !== card);
    state.mySubmitted = true;
}

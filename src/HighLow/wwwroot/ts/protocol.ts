export type Direction = "Highest" | "Lowest";
export type Special = "Normal" | "Reverse";
export type Phase = "Lobby" | "Submitting" | "GiftDecision" | "Finished";

export interface JoinedRoom {
    roomCode: string;
    seat: number;
    token: string;
}

export interface SeatInfo {
    name: string;
    isBot: boolean;
    botControlled: boolean;
    connected: boolean;
    score: number;
    tieAbs: number;
    tieMax: number;
    reverseLeft: number;
    handCount: number;
    hasSubmitted: boolean;
}

export interface LobbyStateEvent {
    roomCode: string;
    seats: SeatInfo[];
    canStart: boolean;
}

export interface GameStartedEvent {
    seats: SeatInfo[];
    startSeat: number;
}

export interface RoundStartedEvent {
    round: number;
    totalRounds: number;
    prize: number;
    startSeat: number;
    forcedRevealSeats: number[];
    hiddenWinnerCardValue: number | null;
    phaseDeadlineUtcMs: number;
    seats: SeatInfo[];
}

export interface SpecialsRevealedEvent {
    reverses: number;
    direction: Direction;
    reverseSeats: number[];
}

export interface PublicCard {
    seat: number;
    card: number | null;
    hidden: boolean;
}

export interface CardsRevealedEvent {
    cards: PublicCard[];
    voidedSeats: number[];
}

export interface ScoreLine {
    seat: number;
    score: number;
    tieAbs: number;
    tieMax: number;
}

export interface RoundResolvedEvent {
    winnerSeat: number | null;
    winnerCardValue: number | null;
    giftTargetSeat: number | null;
    burnedPrize: number | null;
    scores: ScoreLine[];
}

export interface GiftPromptEvent {
    targetSeats: number[];
    phaseDeadlineUtcMs: number;
}

export interface GameFinishedEvent {
    winnerSeats: number[];
    scores: ScoreLine[];
}

export interface PlayerStatusEvent {
    seat: number;
    status: string;
}

export interface RoomView {
    roomCode: string;
    phase: Phase;
    round: number;
    totalRounds: number;
    prize: number | null;
    direction: Direction | null;
    winnerSeat: number | null;
    winnerCardValue: number | null;
    giftTargetSeat: number | null;
    burnedPrize: number | null;
    startSeat: number;
    forcedRevealSeats: number[];
    hiddenWinnerCardValue: number | null;
    phaseDeadlineUtcMs: number;
    seats: SeatInfo[];
    myHand: number[];
    winnerSeats: number[] | null;
    giftTargets: number[] | null;
}
